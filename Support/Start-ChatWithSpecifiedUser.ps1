<#
.SYNOPSIS
    Script to test the functionality of messaging users directly via a bot
.DESCRIPTION
    Use this script to see if the configuration of the Bot and the Teams App is correctly.
    The script makes use of the Graph to get details of conversations between users and the Teams App,
    this is needed to start or continue the conversation using the Bot Framework. Without this, we cannot use
    the Bot API to send messages. 
.PARAMETER UserUPN
    Username of the to send the message to in UPN format
.PARAMETER Message
    Message to send to the specified user
.PARAMETER Install
    If the App is not installed for the specified user, by passing this, the script will try to install the app for the specified user
    This is needed to start the conversation, without the installation this script will fail to send a message
.NOTES
	Author: Robin Meure MSFT
	ChangeLog:
        1.0.0 - Robin Meure, 2022-Feb-23 - First Release.
    
    For more information on the API's being used please see:
    https://docs.microsoft.com/en-us/microsoftteams/platform/graph-api/proactive-bots-and-messages/graph-proactive-bots-and-messages?tabs=dotnet#additional-code-samples
    https://docs.microsoft.com/en-us/graph/auth-v2-service#4-get-an-access-token

#>

[CmdLetBinding()]
param(
	# TimeSpan
	[Parameter(Mandatory = $true, HelpMessage = "User who to send messages to from the bot in UPN format (e.g. user@contoso.onmicrosoft.com)")]
	[string]
	$userUpn,
    [Parameter(Mandatory = $true, HelpMessage = "The contents of the message to send to the specified user")]
	[string]
	$message,
    [Parameter(Mandatory = $false, HelpMessage = "When passed, it will try to install the App for the specified user when the current state is that App is not installed.")]
	[switch]
	$install
    
)

#############################################################################################################
#  Variables need to be replaced to start using the script                                                  #
#############################################################################################################

# Global variables
$tenantId = "tenant.onmicrosoft.com" #or in GUID format "00000000-0000-0000-0000-000000000000"
$teamsAppId = "<guid>" # AppId of the Teams App Manifest 

# App Registration details to make the Graph API calls
$graphAppId = "<GraphAppId>"
$graphAppSecret= "<GraphAppSecret>"

# Bot App registration details
$userAppId = "<BotUserAppId>"
$userAppSecret = "<BotUserAppSecre>"

#############################################################################################################
#  Authentication section                                                                                   #
#############################################################################################################

# Bot framework variables
$serviceUrl = "https://smba.trafficmanager.net/emea"
$botAuthorityUrl = "https://login.microsoftonline.com/botframework.com/oauth2/v2.0/token"
$botResource = "api.botframework.com"

# Graph API specific variables
$graphAuthorityUrl = [string]::Format("https://login.microsoftonline.com/{0}/oauth2/v2.0/token", $tenantId)
$graphResource = "graph.microsoft.com"

# Graph auth section (fetching access token)
$graphBody = [string]::Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=https%3A%2F%2F{2}%2F.default", $graphAppId, $graphAppSecret, $graphResource)
$graphToken = Invoke-RestMethod -Uri $graphAuthorityUrl -Method Post -Body $graphBody
Write-Output ("Fetching Graph Access token using {0}." -f $graphAppId)
$graphAccessToken = $graphToken.access_token
if ($graphAccessToken -eq $null)
{
    Write-Error -Message "No Graph access token found"
    return
}

# Bot auth section
$userAppBody = [string]::Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=https%3A%2F%2F{2}%2F.default", $userAppId, $userAppSecret, $botResource)
$userToken = Invoke-RestMethod -Uri $botAuthorityUrl -Method Post -Body $userAppBody
Write-Output ("Fetching MSBot Access token using {0}." -f $userAppId)
$userAccessToken = $userToken.access_token
if ($userAccessToken -eq $null)
{
    Write-Error -Message "No bot access token found"
    return
}

#############################################################################################################
#  Main logic - this is where the real magic happens :)                                                     #
#############################################################################################################

# In order to send a message to an user via a bot, we first need to get the conversation between the bot and the user,
# this can be fetched via the Graph API using the https://docs.microsoft.com/en-us/graph/api/chat-get?view=graph-rest-1.0&tabs=http&preserve-view=true&viewFallbackFrom=graph-rest-v1.0 endpoint
# Using the current implementation of the API being used, our App needs to have Chat.Read.All permission

# If no chat/conversation history can be found between the App and the user, we need to deploy the App for the user
# So, if the "installation" property is being passed on, we're going to deploy the Teams App to the user to start the conversation

Write-Output ("Trying to fetch TeamsApp installation instance for the user." -f $userAppId)
$getAppsForUserUrl = [string]::Format("https://graph.microsoft.com/v1.0/users/{0}/chats?`$filter=installedApps/any(a:a/teamsApp/id eq '{1}')", $userUpn, $teamsAppId)
try
{
    $installAppsForUserData = Invoke-RestMethod -Headers @{Authorization = "Bearer $graphAccessToken"} -ContentType "application/json" -Uri $getAppsForUserUrl -Method Get
    $userConversationId = $installAppsForUserData.value.id
    Write-Output ("Got the conversationId ({0}) needed to send the message." -f $userConversationId)
}
catch [Net.WebException] 
{
    Write-Warning -Message "No installation and thus no conversation found"
}


if ($userConversationId -eq $null)
{
    if ($install)
    {
        Write-Warning -Message "Trying to install App"
        # We need to have the API permissions as outlined in the following article
        # https://docs.microsoft.com/en-us/graph/api/userteamwork-post-installedapps?view=graph-rest-1.0&tabs=http&preserve-view=true&viewFallbackFrom=graph-rest-v1.0
        
        $installationUrl = [string]::Format("https://graph.microsoft.com/v1.0/users/{0}/teamwork/installedApps", $userUpn)
        $installationBody = "{
            'teamsApp@odata.bind':'https://graph.microsoft.com/v1.0/appCatalogs/teamsApps/$teamsAppId'
        }"

        try
        {
            # try installing the app
            $installationResult = Invoke-RestMethod -Uri $installationUrl -Headers @{Authorization = "Bearer $graphAccessToken"} -ContentType "application/json" -Body $installationBody -Method Post
            
            # once installed correctly, try to fetch the conversation that the App was installed
            $installAppsForUserData = Invoke-RestMethod -Headers @{Authorization = "Bearer $graphAccessToken"} -ContentType "application/json" -Uri $getAppsForUserUrl -Method Get
            $userConversationId = $installAppsForUserData.value.id
            Write-Output ("Got the conversationId ({0}) needed to send the message." -f $userConversationId)
        }
        catch [Net.WebException] 
        {
            [System.Net.HttpWebResponse] $resp = [System.Net.HttpWebResponse] $_.Exception.Response  
            Write-Warning ("Failed to install the Application because of {0}" -f, $resp.StatusDescription)
        }

    }
}


if ($userConversationId -eq $null)
{
    Write-Output ("Could not send message because of missing conversationId (unique identifier of the App and the user).")
    return
}

# If we have the chat, we can continue the thread and send our message using the BotFramework REST API
Write-Output ("Sending message {1} to user {0}" -f $userUpn, $message)
$userConversationsUrl = [string]::Format("{0}/v3/conversations/{1}/activities", $serviceUrl, $userConversationId)
$postBody = "{
    'type': 'message',
    'text': '$message'
}"

$messageResult = Invoke-RestMethod -Uri $userConversationsUrl -Headers @{Authorization = "Bearer $userAccessToken"} -ContentType "application/json" -Body $postBody -Method Post
if ($messageResult)
{
    Write-Output ("Message sent successfully.")
}

