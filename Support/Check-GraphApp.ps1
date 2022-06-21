<#
.SYNOPSIS
    Script to test if the created Applications in the Application Registration work
.DESCRIPTION
    To make sure that the AppReg itself is working, this script is meant to test the basic AppId + AppSecret combination to see if an access token is being returned.
    Also, this can be used to see the App has the proper permissions granted to lookup users and/our groups.   
.PARAMETER TenantId
    TenantId (see the Azure Portal to retrieve this GUID, this is also known as the DirectoryId which can be found in the Application Registration as well)
.PARAMETER AppId
    ApplicationId of the Graph App (main application, not the user nor the author bot App registration)
.PARAMETER AppSecret
    Secret of the Application
.PARAMETER FetchDataFromGraph
    When supplied, a call is done to the Graph API using the App and the access token to retrieve O365 groups and/or users
.NOTES
	Author: Robin Meure MSFT
	ChangeLog:
        1.0.0 - Robin Meure, 2022-Feb-23 - First Release.
    
    This script does not make use of any Graph libraries/SDK's but just 'simple' Invoke-RestMethod cmdlets,
    this to eliminate the dependencies and/or elevated powershell sessions to install these dependencies like MSGraph
#>

[CmdLetBinding()]
param(
    [Parameter(Mandatory = $true, HelpMessage = "The TenantId where the application is deployed")]
	[string]
	$tenantId,
	[Parameter(Mandatory = $true, HelpMessage = "The ApplicationId of the application")]
	[string]
	$AppId,
    [Parameter(Mandatory = $true, HelpMessage = "The Application secret of the application.")]
	[string]
	$AppSecret,
    [Parameter(Mandatory = $false, HelpMessage = "When provided, will call into the graph using the access token.")]
	[switch]
	$FetchDataFromGraph
)

$graphAuthorityUrl = [string]::Format("https://login.microsoftonline.com/{0}/oauth2/v2.0/token", $tenantId)
$graphResource = "graph.microsoft.com"

$body = [string]::Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=https%3A%2F%2F{2}%2F.default", $AppId, $AppSecret, $graphResource)
$token = Invoke-RestMethod -Uri $graphAuthorityUrl -Method Post -Body $body

if (!$token)
{
    Write-Warning ("No token received")
}
else {
    Write-Output ("Fetched access token using for Application: {0}." -f $appId)
    $graphAccessToken = $token.access_token
}


if ($FetchDataFromGraph)
{
    # This is fetching O365 groups when drafting a message as option 4 to send the message to.
    # https://graph.microsoft.com/v1.0/groups?$filter=groupTypes/any(c:c+eq+'Unified')
    
    # GroupMember.Read.All check
    $fetchGroupsUrl = [string]::Format("https://graph.microsoft.com/v1.0/groups?`$filter=groupTypes/any(c:c+eq+'Unified')")
    $fetchGroupsResponse = Invoke-RestMethod -Uri $fetchGroupsUrl -Headers @{Authorization = "Bearer $graphAccessToken"} -ContentType "application/json" -Method Get
    if ($fetchGroupsResponse -ne $null)
    {
        Write-Output ("Fetched o365 groups using graph API for Application: {0}." -f $appId)
        $fetchGroupsResponse.value | Select-Object DisplayName, Id
        Write-Output ("---------------------------------------------------------")
    }

    # This is fetching users from AAD (e.g. used to send a message to all users (option 3 in the message ux))
    # https://graph.microsoft.com/v1.0/users

    # User.Read.All check
    $fetchUsersUrl = [string]::Format("https://graph.microsoft.com/v1.0/users")
    $fetchUsersResponse = Invoke-RestMethod -Uri $fetchUsersUrl -Headers @{Authorization = "Bearer $graphAccessToken"} -ContentType "application/json" -Method Get
    if ($fetchUsersResponse -ne $null)
    {
        Write-Output ("Fetched AAD users using graph API for Application: {0}." -f $appId)
        $fetchUsersResponse.value | Select-Object DisplayName, Id
    }
}