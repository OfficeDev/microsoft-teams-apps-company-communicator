<#
.SYNOPSIS
    Script to test if the Applications can get an access token using the stored secrets
.DESCRIPTION
    To make sure that the configuration which is stored in the keyvault is correct this script will
    1. Fetch the applicationId's from the Application Registrations
    2. Fetch the application secrets from the KeyVault
    3. Try to fetch an acces token using the combination of the AppId and AppSecret

.PARAMETER ConfigFile
    Path to where the parameters.json is stored which was used to deploy the application.
.PARAMETER TenantId
    TenantId (see the Azure Portal to retrieve this GUID, this is also known as the DirectoryId which can be found in the Application Registration as well)
.PARAMETER BaseResourceName
    "Base" name of the resources (e.g. how all the resources are named) in the Resource Group, this is the same name as in the parameters.json file
    See https://github.com/OfficeDev/microsoft-teams-apps-company-communicator/wiki/Deployment-guide step 5), this name is used in the script to find all the components in the Resource Group
.NOTES
	Author: Robin Meure MSFT
	ChangeLog:
        1.0.0 - Robin Meure, 2022-Feb-23 - First Release.
    
    Make sure that the account which is used to connect to the Azure environment has read access on the KeyVault secrets.
    Azure Portal -> Company Communicator Resource Group -> KeyVault > Access policies -> Add user to read the secrets
    See https://docs.microsoft.com/en-us/azure/key-vault/general/assign-access-policy?tabs=azure-portal for more information.

#>

[CmdletBinding(DefaultParametersetName="Variables")]
Param
(
    [Parameter( ValueFromPipeline=$true,
                ValueFromPipelineByPropertyName=$true,
                ParameterSetName="ConfigFile",
                HelpMessage="Load the configfile of the deployment folder.")]
    [switch]$ConfigFile,    
    
    [Parameter( ParameterSetName="ConfigFile",
    HelpMessage="The path where the parameters.json which is used for the deployment is located.")] 
    [string]$configFilePath,  

    [Parameter( ParameterSetName="Variables",
                HelpMessage="The TenantId where the application is deployed.")] 
    [string]$tenantId,
    [Parameter( ParameterSetName="Variables",
                HelpMessage="'Base' name of the resources (e.g. how all the resources are named) in the Resource Group.")] 
    [string]$baseName
)

Function Get-AccessToken
{
    param(
        [Parameter(Mandatory = $true, HelpMessage = "ApplicationId")]
        [string]
        $appId,
        [Parameter(Mandatory = $true, HelpMessage = "ApplicationSecret")]
        [string]
        $appSecret,
        [Parameter(Mandatory = $true, HelpMessage = "Resource to authenticate against (e.g. https://graph.microsoft.com)")]
        [string]
        $resource,
        [Parameter(Mandatory = $true, HelpMessage = "Authority to receive the token from (e.g. 'https://login.microsoftonline.com/tenant/oauth2/v2.0/token'))")]
        $authority   
    )

    $body = [string]::Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=https%3A%2F%2F{2}%2F.default", $appId, $appSecret, $resource)
    
    Write-Output ("Fetching access token using for Application: {0}." -f $appId)
    $token = Invoke-RestMethod -Uri $authority -Method Post -Body $body
    
    if ($token.access_token -ne $null)
    {   
        Write-Output ("Got an access token")
    }
    else
    {
        Write-Output ("Failed to retrieve an access token")
    }
}

# check if we have a config file we're using and need to parse
if ($ConfigFile)
{
    if ([string]::IsNullOrEmpty($configFilePath))
    {
        Write-Output ("No config file is set, trying to fetch the configuration from the deployment folder.")
        $config = Get-Content '..\deployment\parameters.json' | Out-String | ConvertFrom-Json
        Write-Verbose ("Found following configuration: {0}." -f $config)
    }
    else
    {
        Write-Output ("FilePath set, trying to fetch the configuration from the specified folder.")
        $config = Get-Content -Path $configFilePath | Out-String | ConvertFrom-Json
    }
    # Fetching the configuration of the deployment
    $tenantId = $config.tenantId.Value
    $baseName = $config.baseResourceName.Value
}



# Connecting to the Azure Portal, please make sure you're connecting using the same account as for the deployment
$connectionSucceeded = $false
try{
    $connectionSucceeded = Connect-AzAccount -Tenant $tenantId
}
catch{
    $connectionSucceeded = $false
}

# if we could successfully connect to Azure, then we're going to try to fetch the App Registrations and secrets from the KeyVault
if ($connectionSucceeded)
{
    # Fetching the App registrations with their Id's
    $applications = Get-AzADApplication -DisplayNameStartWith $baseName | Select-Object ApplicationId, DisplayName
    $graphAppId = $applications | Where-Object { $_.DisplayName -eq $baseName}| Select-Object ApplicationId -ExpandProperty ApplicationId
    $userAppId = $applications | Where-Object { $_.DisplayName -eq [string]::Format("{0}-users",$baseName)} | Select-Object ApplicationId -ExpandProperty ApplicationId
    $authorAppId = $applications | Where-Object { $_.DisplayName -eq [string]::Format("{0}-authors",$baseName)}| Select-Object ApplicationId -ExpandProperty ApplicationId

    # setting up keyvaultName
    $keyVaultName = [string]::Format("{0}{1}", $baseName,"vault")

    # Defining the secretNames to retrieve the actual secrets from the Keyvault
    $graphAppSecretName = [string]::Format("{0}{1}", $keyVaultName, "GraphAppPassword")
    $userAppSecretName = [string]::Format("{0}{1}", $keyVaultName, "UserAppPassword")
    $authorAppSecretName = [string]::Format("{0}{1}", $keyVaultName, "AuthorAppPassword")

    # Fetch the secrets from the KeyVault
    $graphAppSecret = Get-AzKeyVaultSecret -VaultName $keyVaultName -Name $graphAppSecretName -AsPlainText
    $userAppSecret = Get-AzKeyVaultSecret -VaultName $keyVaultName -Name $userAppSecretName -AsPlainText
    $authorAppSecret = Get-AzKeyVaultSecret -VaultName $keyVaultName -Name $authorAppSecretName -AsPlainText

    # First check the Graph App
    $graphAuthorityUrl = [string]::Format("https://login.microsoftonline.com/{0}/oauth2/v2.0/token", $tenantId)
    $graphResource = "graph.microsoft.com"

    Get-AccessToken -appId $graphAppId -appSecret $graphAppSecret -authority $graphAuthorityUrl -resource $graphResource

    # Checking the User and Author Apps
    $botAuthorityUrl = [string]::Format("https://login.microsoftonline.com/{0}/oauth2/v2.0/token", "botframework.com")
    $botResource = "api.botframework.com"

    Get-AccessToken -appId $userAppId -appSecret $userAppSecret -authority $botAuthorityUrl -resource $botResource
    Get-AccessToken -appId $authorAppId -appSecret $authorAppSecret -authority $botAuthorityUrl -resource $botResource
}