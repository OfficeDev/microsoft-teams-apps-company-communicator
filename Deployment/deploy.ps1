function ValidateSecureUrl {
    param(
        [Parameter(Mandatory = $true)] [string] $url
    )
    # Url with https prefix REGEX matching
    return ($url -match "https:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)")
}


function ValidateUrlParameters {
    $isValidUrl = $true
    $isValidUrl = $isValidUrl -and (ValidateSecureUrl $parameters.WebsiteUrl.Value)
    $isValidUrl = $isValidUrl -and (ValidateSecureUrl $parameters.PrivacyUrl.Value)
    $isValidUrl = $isValidUrl -and (ValidateSecureUrl $parameters.TermsOfUseUrl.Value)
    return $isValidUrl
}

function validateresourcesnames {
    write-host "checking for the availability of resources..."

    $authorizationtoken = get-accesstokenfromcurrentuser -erroraction stop

    $resources = @(@{
            name               = $parameters.baseresourcename.value
            servicetype        = 'webapp'
            authorizationtoken = $authorizationtoken
        },
        @{
            name               = $parameters.baseresourcename.value + '-data-function'
            servicetype        = 'webapp'
            authorizationtoken = $authorizationtoken
        },
        @{
            name               = $parameters.baseresourcename.value + '-function'
            servicetype        = 'webapp'
            authorizationtoken = $authorizationtoken
        },
        @{
            name               = $parameters.baseresourcename.value + '-prep-function'
            servicetype        = 'webapp'
            authorizationtoken = $authorizationtoken
        },
        @{
            name        = $parameters.baseresourcename.value
            servicetype = 'applicationinsights'
        })

    $allresourcesavailable = $true
    foreach ($resource in $resources) {
        $isresourcenameavailable = validateresourcenames $resource -erroraction stop
        $allresourcesavailable = $allresourcesavailable -and $isresourcenameavailable
    }

    if (!$allresourcesavailable) {
        $confirmationtitle = "Some of the resource types names already exist. If you proceed, this will update the existing resources."
        $confirmationquestion = "Are you sure you want to proceed?"
        $confirmationchoices = "&yes", "&no" # 0 = yes, 1 = no
        
        $updatedecision = $host.ui.promptforchoice($confirmationtitle, $confirmationquestion, $confirmationchoices, 1)
        if ($updatedecision -eq 0) {
            return $true
        }
        else {
            return $false
        }
    }
}

function validateresourcenames {
    param(
        [parameter(mandatory = $true)] $resourceinfo
    )

    if ($resourceinfo.servicetype -eq "applicationinsights") {
        if ($null -eq (get-azapplicationinsights | where-object name -eq $resourceinfo.name)) {
            write-host "Application Insights resource ($($resourceinfo.name)) is available." -foregroundcolor green
            return $true
        }
        else {
            write-host "Application Insights resource ($($resourceinfo.name)) is not available." -foregroundcolor yellow
            return $false
        }
    }
    else {
        $availabilityresult = $null
        $availabilityresult = test-aznameavailability @resourceinfo -erroraction stop
    
        if ($availabilityresult.available) {
            write-host "resource: $($resourceinfo.name) of type $($resourceinfo.servicetype) is available." -foregroundcolor green
            return $true
        }
        else {
            write-host "resource $($resourceinfo.name) is not available." -foregroundcolor yellow
            write-host $availabilityresult.message -foregroundcolor yellow
            return $false
        }
    }
}
#get access token from the logged-in user.
function get-accesstokenfromcurrentuser {
    try {
        $azcontext = get-azcontext
        $azprofile = [microsoft.azure.commands.common.authentication.abstractions.azurermprofileprovider]::instance.profile
        $profileclient = new-object -typename microsoft.azure.commands.resourcemanager.common.rmprofileclient -argumentlist $azprofile
        $token = $profileclient.acquireaccesstoken($azcontext.subscription.tenantid)
        ('bearer ' + $token.accesstoken)
    }        
    catch {
        throw
    }
} 
#to check if the name of resource is available.
function test-aznameavailability {
    param(
        [parameter(mandatory = $true)] [string] $authorizationtoken,
        [parameter(mandatory = $true)] [string] $name,
        [parameter(mandatory = $true)] [validateset(
            'apimanagement', 'keyvault', 'managementgroup', 'sql', 'storageaccount', 'webapp', 'cognitiveservice')]
        $servicetype
    )

    $uribyservicetype = @{
        apimanagement    = 'https://management.azure.com/subscriptions/{subscriptionid}/providers/microsoft.apimanagement/checknameavailability?api-version=2019-01-01'
        keyvault         = 'https://management.azure.com/subscriptions/{subscriptionid}/providers/microsoft.keyvault/checknameavailability?api-version=2019-09-01'
        managementgroup  = 'https://management.azure.com/providers/microsoft.management/checknameavailability?api-version=2018-03-01-preview'
        sql              = 'https://management.azure.com/subscriptions/{subscriptionid}/providers/microsoft.sql/checknameavailability?api-version=2018-06-01-preview'
        storageaccount   = 'https://management.azure.com/subscriptions/{subscriptionid}/providers/microsoft.storage/checknameavailability?api-version=2019-06-01'
        webapp           = 'https://management.azure.com/subscriptions/{subscriptionid}/providers/microsoft.web/checknameavailability?api-version=2020-06-01'
        cognitiveservice = 'https://management.azure.com/subscriptions/{subscriptionid}/providers/microsoft.cognitiveservices/checkdomainavailability?api-version=2017-04-18'
    }

    $typebyservicetype = @{
        apimanagement    = 'microsoft.apimanagement/service'
        keyvault         = 'microsoft.keyvault/vaults'
        managementgroup  = '/providers/microsoft.management/managementgroups'
        sql              = 'microsoft.sql/servers'
        storageaccount   = 'microsoft.storage/storageaccounts'
        webapp           = 'microsoft.web/sites'
        cognitiveservice = 'microsoft.cognitiveservices/accounts'
    }

    $uri = $uribyservicetype[$servicetype] -replace ([regex]::escape('{subscriptionid}')), $parameters.subscriptionid.value
    $nameproperty = if ($servicetype -eq 'cognitiveservice') { "subdomainname" } else { "name" }
    $body = '"{0}": "{1}", "type": "{2}"' -f $nameproperty, $name, $typebyservicetype[$servicetype]

    $response = (invoke-webrequest -uri $uri -method post -body "{$body}" -contenttype "application/json" -headers @{authorization = $authorizationtoken } -usebasicparsing).content
    $response | convertfrom-json |
    select-object @{n = 'name'; e = { $name } }, @{n = 'type'; e = { $servicetype } }, @{n = 'available'; e = { $_ | select-object -expandproperty *available } }, reason, message
}

# to get the ADapp detail. 
function GetAzureADApp {
    param ($appName)

    $app = az ad app list --filter "displayName eq '$appName'" | ConvertFrom-Json

    return $app

}
# Create/re-set AD app.
function CreateAzureADApp {
    param(
        [Parameter(Mandatory = $true)] [string] $AppName,
        [Parameter(Mandatory = $false)] [bool] $MultiTenant = $true,
        [Parameter(Mandatory = $false)] [bool] $AllowImplicitFlow,
        [Parameter(Mandatory = $false)] [bool] $ResetAppSecret = $true
    )
        
    try {
        Write-Host "`r`n### AZURE AD APP CREATION ($appName) ###"

        # Check if the app already exists - script has been previously executed
        $app = GetAzureADApp $appName

        if (-not ([string]::IsNullOrEmpty($app))) {

            # Update Azure AD app registration using CLI
            $confirmationTitle = "The Azure AD app '$appName' already exists. If you proceed, this will update the existing app configuration."
            $confirmationQuestion = "Are you sure you want to proceed?"
            $confirmationChoices = "&Yes", "&No" # 0 = Yes, 1 = No
            
            $updateDecision = $Host.UI.PromptForChoice($confirmationTitle, $confirmationQuestion, $confirmationChoices, 1)
            if ($updateDecision -eq 0) {
                Write-Host "Updating the existing app..." -ForegroundColor Yellow

                az ad app update --id $app.appId --available-to-other-tenants $MultiTenant --oauth2-allow-implicit-flow $AllowImplicitFlow --required-resource-accesses './AadAppManifest.json'

                Write-Host "Waiting for app update to finish..."

                Start-Sleep -s 10

                Write-Host "Azure AD App ($appName) is updated." -ForegroundColor Green

            }
            else {
                Write-Host "Deployment cancelled. Please use a different name for the Azure AD app and try again." -ForegroundColor Yellow
                return $null
            }
        } 
        else {
            # Create the app
            Write-Host "Creating Azure AD App - ($appName)..."

            # Create Azure AD app registration using CLI
            az ad app create --display-name $appName --end-date '2299-12-31T11:59:59+00:00' --available-to-other-tenants $MultiTenant --oauth2-allow-implicit-flow $AllowImplicitFlow --required-resource-accesses './AadAppManifest.json'

            Write-Host "Waiting for app creation to finish..."

            Start-Sleep -s 10

            Write-Host "Azure AD App ($appName) is created." -ForegroundColor Green

        }

        $app = GetAzureADApp $appName
        
        $appSecret = $null;
        if ($ResetAppSecret) {
            Write-Host "Updating app secret..."
            $appSecret = az ad app credential reset --id $app.appId --append | ConvertFrom-Json;
        }

        Write-Host "### AZURE AD APP ($appName) CREATED/REGISTERED SUCCESSFULLY. ###" -ForegroundColor Green
        return $appSecret
    }
    catch {
        $errorMessage = $_.Exception.Message
        Write-Host "Failed to register/configure the Azure AD app. Error message: $errorMessage" -ForegroundColor Red
    }
    return $null
}

#to get the deployment log with the help of logged in user detail.
function CollectARMDeploymentLogs {
    $logsPath = '.\DeploymentLogs'
    $activityLogPath = "$logsPath\activity_log.log"
    $deploymentLogPath = "$logsPath\deployment_operation.log"

    $logsFolder = New-Item -ItemType Directory -Force -Path $logsPath

    az deployment operation group list --resource-group $parameters.ResourceGroupName.Value --subscription $parameters.subscriptionId.Value --name azuredeploy --query "[?properties.provisioningState=='Failed'].properties.statusMessage.error" | Set-Content $deploymentLogPath

    $activityLog = $null
    $retryCount = 5
    DO {
        Write-Host "Collecting deployment logs..."

        # Wait for async logs to persist
        Start-Sleep -s 30

        # Returns empty [] if logs are not available yet
        $activityLog = az monitor activity-log list -g $parameters.ResourceGroupName.Value --subscription $parameters.subscriptionId.Value --caller $userAlias --status Failed --offset 30m

        $retryCount--

    } While (($activityLog.Length -lt 3) -and ($retryCount -gt 0))

    $activityLog | Set-Content $activityLogPath

    # collect web apps deployment logs
    $activityLogErrors = ($activityLog | ConvertFrom-Json) | Where-Object { ($null -ne $_.resourceType) -and ($_.resourceType.value -eq "Microsoft.Web/sites/sourcecontrols") }
    $resourcesLookup = @($activityLogErrors | Select-Object resourceId, @{Name = "resourceName"; Expression = { GetResourceName $_.resourceId } })
    if ($resourcesLookup.length -gt 0) {
        foreach ($resourceInfo in $resourcesLookup) {
            if ($null -ne $resourceInfo.resourceName) {
                az webapp log download --ids $resourceInfo.resourceId --log-file "$logsPath\$($resourceInfo.resourceName).zip"
            }
        }
    }
    
    # Generate zip archive and delete folder
    $compressManifest = @{
        Path             = $logsPath
        CompressionLevel = "Fastest"
        DestinationPath  = "logs.zip"
    }
    Compress-Archive @compressManifest -Force
    Get-ChildItem -Path $logsPath -Recurse | Remove-Item -Force -Recurse -ErrorAction Continue
    Remove-Item $logsPath -Force -ErrorAction Continue
    
    Write-Host "Deployment logs generation finished. Please share Deployment\logs.zip file with the app template team to investigate..." -ForegroundColor Yellow
}

function DeployARMTemplate {
    Param(
        [Parameter(Mandatory = $true)] $appId,
        [Parameter(Mandatory = $true)] $secret,
		[Parameter(Mandatory = $true)] $userappId,
        [Parameter(Mandatory = $true)] $usersecret
    )
    try { 
        if ((az group exists --name $parameters.ResourceGroupName.Value --subscription $parameters.subscriptionId.Value) -eq $false) {
            Write-Host "Creating resource group $($parameters.ResourceGroupName.Value)..." -ForegroundColor Yellow
            az group create --name $parameters.ResourceGroupName.Value --location $parameters.location.Value --subscription $parameters.subscriptionId.Value
        }
        
        # Deploy ARM templates
        Write-Host "Deploying app services, Azure function, bot service, and other supporting resources..." -ForegroundColor Yellow
        az deployment group create --resource-group $parameters.ResourceGroupName.Value --subscription $parameters.subscriptionId.Value --template-file 'azuredeploy.json' --parameters "baseResourceName=$($parameters.baseResourceName.Value)" "authorClientId=$appId" "authorClientSecret=$secret" "userClientId=$userappId" "userClientSecret=$usersecret" "senderUPNList=$($parameters.senderUPNList.Value)" "customDomainOption=$($parameters.customDomainOption.Value)" "appDisplayName=$($parameters.appDisplayName.Value)" "appDescription=$($parameters.appDescription.Value)" "appIconUrl=$($parameters.appIconUrl.Value)" "tenantId=$($parameters.tenantId.Value)" "hostingPlanSku=$($parameters.hostingPlanSku.Value)" "hostingPlanSize=$($parameters.hostingPlanSize.Value)" "location=$($parameters.location.Value)" "gitRepoUrl=$($parameters.gitRepoUrl.Value)" "gitBranch=$($parameters.gitBranch.Value)" "ProactivelyInstallUserApp=$($parameters.proactivelyInstallUserApp.Value)" "UserAppExternalId=$($parameters.userAppExternalId.Value)" "DefaultCulture=$($parameters.defaultCulture.Value)" "SupportedCultures=$($parameters.supportedCultures.Value)"
        if ($LASTEXITCODE -ne 0) {
            CollectARMDeploymentLogs
            Throw "ERROR: ARM template deployment error."
        }
        Write-Host "Finished deploying resources." -ForegroundColor Green
        #get the output of current deployment
        $value = Get-AzResourceGroupDeployment -ResourceGroupName $parameters.ResourceGroupName.Value -Name azuredeploy
        return $value
    }
    catch {
        Write-Host "Error occured while deploying Azure resources." -ForegroundColor Red
        throw
    }
}

# AD app update. Assigning Admin-consent,RedirectUris,IdentifierUris,Optionalclaim etc. 
function ADAppUpdate {
    Param(
        [Parameter(Mandatory = $true)] $appdomainName,
        [Parameter(Mandatory = $true)] $appId
    )
            $configAppId = $appId
            $azureDomainBase = $appdomainName
            $configAppUrl = "https://$azureDomainBase"
            $RedirectUris = ($configAppUrl + '/signin-simple-end')
            $IdentifierUris = "api://$azureDomainBase"
            $appName = $parameters.baseResourceName.Value + '-authors'

    function CreatePreAuthorizedApplication(
        [string] $applicationIdToPreAuthorize,
        [string] $scopeId) {
        $preAuthorizedApplication = New-Object 'Microsoft.Open.MSGraph.Model.PreAuthorizedApplication'
        $preAuthorizedApplication.AppId = $applicationIdToPreAuthorize
        $preAuthorizedApplication.DelegatedPermissionIds = @($scopeId)
        return $preAuthorizedApplication
    }

    function CreateScope(
        [string] $value,
        [string] $userConsentDisplayName,
        [string] $userConsentDescription,
        [string] $adminConsentDisplayName,
        [string] $adminConsentDescription) {
        $scope = New-Object Microsoft.Open.MsGraph.Model.PermissionScope
        $scope.Id = New-Guid
        $scope.Value = $value
        $scope.UserConsentDisplayName = $userConsentDisplayName
        $scope.UserConsentDescription = $userConsentDescription
        $scope.AdminConsentDisplayName = $adminConsentDisplayName
        $scope.AdminConsentDescription = $adminConsentDescription
        $scope.IsEnabled = $true
        $scope.Type = "User"
        return $scope
    }

    $confirmationTitle = "Admin consent permissions is required for app registration using CLI"
    $confirmationQuestion = "Are you sure you want to proceed?"
    $confirmationChoices = "&Yes", "&No" # 0 = Yes, 1 = No
    $consentErrorMessage = "Current user does not have the privilege to consent the `"User.Read`" permission on this app. Please ask your tenant administrator to consent."
            
    $updateDecision = $Host.UI.PromptForChoice($confirmationTitle, $confirmationQuestion, $confirmationChoices, 1)
    if ($updateDecision -eq 0) {
        # Grant admin consent for app registration required permissions using CLI
        az ad app permission admin-consent --id $configAppId
        Write-Host "Waiting for admin consent to finish..."
        if (0 -ne $LastExitCode) {
            Write-Host $consentErrorMessage -ForegroundColor Yellow
            [Console]::ResetColor()
        }
        else {
            Write-Host "Admin consent has been granted." -ForegroundColor Green
        }
    }
    else {
        Write-Host "Please check the below link to provide admin consent manually. `nhttps://docs.microsoft.com/en-us/azure/active-directory/manage-apps/grant-admin-consent#:~:text=Select%20Azure%20Active%20Directory%20then,the%20permissions%20the%20application%20requires."
    }
    Import-Module AzureAD
            

    $apps = Get-AzureADApplication -Filter "DisplayName eq '$appName'"

    if (0 -eq $apps.Length) {
        $app = New-AzureADApplication -DisplayName $appName
    }
    else {
        $app = $apps[0]
    }

    $applicationObjectId = $app.ObjectId

    $app = Get-AzureADMSApplication -ObjectId $applicationObjectId

    # Do nothing if the app has already been configured
    if ($app.IdentifierUris.Count -gt 0) {
        Write-Host "Exiting, application already configured." -ForegroundColor Red
        return
    }
             
    # Expose an API
            $appId = $app.AppId
            Set-AzureADMSApplication -ObjectId $app.Id -IdentifierUris "$IdentifierUris"
                    
            $configApp = az ad app update --id $configAppId --reply-urls $RedirectUris
                    
            az ad app update --id $configAppId --optional-claims './AadOptionalClaims.json'
                    
            Write-Host "App URI,Urls, Optionalclaim set."
                    
            # Create access_as_user scope
            # Add all existing scopes first
            $scopes = New-Object System.Collections.Generic.List[Microsoft.Open.MsGraph.Model.PermissionScope]
            $app.Api.Oauth2PermissionScopes | foreach-object { $scopes.Add($_) }
            $scope = CreateScope -value "access_as_user"  `
                -userConsentDisplayName "Access the API as the current logged-in user."  `
                -userConsentDescription "Access the API as the current logged-in user."  `
                -adminConsentDisplayName "Access the API as the current logged-in user."  `
                -adminConsentDescription "Access the API as the current logged-in user."
            $scopes.Add($scope)
            $app.Api.Oauth2PermissionScopes = $scopes
            Set-AzureADMSApplication -ObjectId $app.Id -Api $app.Api
            Write-Host "Scope access_as_user added."
             
    # Authorize Teams mobile/desktop client and Teams web client to access API
            $preAuthorizedApplications = New-Object 'System.Collections.Generic.List[Microsoft.Open.MSGraph.Model.PreAuthorizedApplication]'
            $teamsRichClientPreauthorization = CreatePreAuthorizedApplication `
                -applicationIdToPreAuthorize '1fec8e78-bce4-4aaf-ab1b-5451cc387264' `
                -scopeId $scope.Id
            $teamsWebClientPreauthorization = CreatePreAuthorizedApplication `
                -applicationIdToPreAuthorize '5e3ce6c0-2b1f-4285-8d4b-75ee78787346' `
                -scopeId $scope.Id
            $preAuthorizedApplications.Add($teamsRichClientPreauthorization)
            $preAuthorizedApplications.Add($teamsWebClientPreauthorization)   
            $app = Get-AzureADMSApplication -ObjectId $applicationObjectId
            $app.Api.PreAuthorizedApplications = $preAuthorizedApplications
            Set-AzureADMSApplication -ObjectId $app.Id -Api $app.Api
            Write-Host "Teams mobile/desktop and web clients applications pre-authorized."
     
}

#Removing existing access of user app.
function ADAppUpdateUser {
    Param(
        [Parameter(Mandatory = $true)] $appId
	)
            az ad app update --id $appId --remove replyUrls --remove IdentifierUris
            $IdentifierUris = "api://$appId"
			az ad app update --id $appId --identifier-uris "$IdentifierUris"
			az ad app update --id $appId --remove requiredResourceAccess
}
#update manifest file and create a .zip file.
function GenerateAppManifestPackage {
    Param(
        [Parameter(Mandatory = $true)] [ValidateSet('authors', 'users')] $manifestType,
        [Parameter(Mandatory = $true)] $appdomainName,
        [Parameter(Mandatory = $true)] $appId
    )

        Write-Host "Generating package for $manifestType..."

        $azureDomainBase = $appdomainName
        $sourceManifestPath = "..\Manifest\manifest_$manifestType.json"
        $destManifestFilePath = '..\Manifest\manifest.json'
        $destinationZipPath = "..\manifest\CC-$manifestType.zip"
    
    if (!(Test-Path $sourceManifestPath)) {
        throw "$sourceManifestPath does not exist. Please make sure you download the full app template source."
    }

    copy-item -path $sourceManifestPath -destination $destManifestFilePath -Force

    # Replace merge fields with proper values in manifest file and save
        $mergeFields = @{
            '<<companyName>>'   = $parameters.companyName.Value 
            '<<botId>>'         = $appId
            '<<appDomain>>'     = $azureDomainBase
            '<<websiteUrl>>'    = $parameters.websiteUrl.Value
            '<<privacyUrl>>'    = $parameters.privacyUrl.Value
            '<<termsOfUseUrl>>' = $parameters.termsOfUseUrl.Value
        }
        $appManifestContent = Get-Content $destManifestFilePath
        foreach ($mergeField in $mergeFields.GetEnumerator()) {
            $appManifestContent = $appManifestContent.replace($mergeField.Name, $mergeField.Value)
        }
        $appManifestContent | Set-Content $destManifestFilePath -Force

    # Generate zip archive 
        $compressManifest = @{
            LiteralPath      = "..\manifest\color.png", "..\manifest\outline.png", $destManifestFilePath
            CompressionLevel = "Fastest"
            DestinationPath  = $destinationZipPath
        }
        Compress-Archive @compressManifest -Force

        Remove-Item $destManifestFilePath -ErrorAction Continue

        Write-Host "Package has been created under this path $(Resolve-Path $destinationZipPath)" -ForegroundColor Green
}

# Script starting line
# Check for presence of Azure CLI
    If (-not (Test-Path -Path "C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2")) {
        Write-Host "AZURE CLI NOT INSTALLED!"
        $confirmationtitle      = "Please select YES to install Azure CLI."
        $confirmationquestion   = "Are you sure you want to proceed?"
        $confirmationchoices    = "&yes", "&no" # 0 = yes, 1 = no
            
        $updatedecision = $host.ui.promptforchoice($confirmationtitle, $confirmationquestion, $confirmationchoices, 1)
        if ($updatedecision -eq 0) {
            Write-Host "Installing Azure Cli ..."-ForegroundColor Yellow
            Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi; Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'; rm .\AzureCLI.msi
            Write-Host "AZURE CLI IS INSTALLED!.. Please close the PowerShell window and re-run this script in a new PowerShell session."            
            return
        }
        else {
            Write-Host "AZURE CLI NOT INSTALLED!`nPLEASE INSTALL THE CLI FROM https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest and re-run this script in a new PowerShell session" -ForegroundColor Red
            break
        }
    }

# Installing required modules
    Write-Host "Checking for required modules..." -ForegroundColor Yellow
    $confirmationTitle = Write-Host "To run this script. Below module needs to be installed. `n 1.Az module`n 2.AzureAD module `n 3.WriteAscii module`nif you proceed, the script will install the modules."
    $confirmationQuestion = "Are you sure you want to proceed?"
    $confirmationChoices = "&Yes", "&No" # 0 = Yes, 1 = No
                
    $updateDecision = $Host.UI.PromptForChoice($confirmationTitle, $confirmationQuestion, $confirmationChoices, 1)
        if ($updateDecision -eq 0) {
            if (-not (Get-Module -ListAvailable -Name "Az")) {
                Write-Host "Installing AZ module..." -ForegroundColor Yellow
                Install-Module Az -AllowClobber -Scope CurrentUser
            } 
            if (-not (Get-Module -ListAvailable -Name "AzureAD")) {
                Write-Host "Installing AzureAD module..." -ForegroundColor Yellow
                Install-Module AzureAD -Scope CurrentUser -Force
            } 
            if (-not (Get-Module -ListAvailable -Name "WriteAscii")) {
                Write-Host "Installing WriteAscii module..." -ForegroundColor Yellow
                Install-Module WriteAscii -Scope CurrentUser -Force
            }
                    
        }
        else {
            Write-Host "You can install modules manually, by following below link and re-run the script. `nhttps://docs.microsoft.com/en-us/powershell/module/powershellget/install-module?view=powershell-7"
        }

# Loading Parameters from JSON meta-data file
    $parametersListContent = Get-Content '.\parameters.json' -ErrorAction Stop
    $missingRequiredParameter = $parametersListContent | % { $_ -match '<<value>>' }
    If ($missingRequiredParameter -contains $true) {
        Write-Host "Some required parameters are missing values. Please replace all <<value>> occurrences in parameters.json file with correct values." -ForegroundColor Red
        Exit
    }

# Parse & assign parameters
    $parameters = $parametersListContent | ConvertFrom-Json
    
    
# Validate Https Urls parameters.
    if (!(ValidateUrlParameters)) {
        Write-Host "WebsiteUrl, PrivacyUrl, TermsOfUseUrl parameters must be in correct format and start with https:// prefix. Please correct values in parameters.json file." -ForegroundColor Red
        Exit
    }

#Deployment started message to user.
    Write-Ascii -InputObject "Company Communicator" -ForegroundColor Magenta
    Write-Host "### DEPLOYMENT SCRIPT STARTED ###" -ForegroundColor Magenta

# Initialise connections - Azure Az/CLI/AzureAD
    Write-Host "Launching Azure sign-in..."
    Connect-AzAccount -Subscription $parameters.subscriptionId.Value -ErrorAction Stop
    $user = az login
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Login failed for user..." -ForegroundColor Red
        return
    }
    Write-Host "AzureAD sign-in..."
    $ADaccount = Connect-AzureAD -Tenant $parameters.tenantId.Value -ErrorAction Stop
    $userAlias = ($user | ConvertFrom-Json).user.name

# Function Call to valiadte the name of resources to be created.
    $validateName = validateresourcesnames

#Function call to create AD app and get the creds.	
    $appcredUser = CreateAzureADApp $parameters.baseresourcename.value
    if ( $appCredUser -eq $null) {
        Write-Host "Failed to create or update user app in Azure Active Directory, this script is now exiting."
        Exit
    }
	
	$authorsApp = $parameters.baseResourceName.Value + '-authors'
	$appCred = CreateAzureADApp $authorsApp
    if ( $appCred -eq $null) {
        Write-Host "Failed to create or update authors app in Azure Active Directory, this script is now exiting."
        Exit
    }
#Function call to Deploy ARM Template.
    $deploymentOutput = DeployARMTemplate $appCred.appId $appCred.password $appCredUser.appId $appCredUser.password
    if ($deploymentOutput -eq $null) {
        Write-Host "Encountered error during ARM template deployment, this script is now exiting..."
        Exit
    }

# Reading the deployment output.
    Write-Host "Fetching deployment outputs..."-ForegroundColor Yellow

# Assigning return values to variable. 
    $appdomainName = $deploymentOutput.Outputs.appDomain.Value

# Function call to update reply-urls and uris for registered app.
    
    Write-Host "Updating required parameters and urls..."-ForegroundColor Yellow
    ADAppUpdateUser $appcredUser.appId
    ADAppUpdate $appdomainName $appCred.appId
	

# Function call to generate manifest.zip folder for User and Author. 
    GenerateAppManifestPackage 'authors' $appdomainName $appCred.appId
    GenerateAppManifestPackage 'users' $appdomainName $appcredUser.appId


#Log out to avoid tokens caching
    $logOut = az logout
    $disAzAcc = Disconnect-AzAccount

# Open manifest folder
    Invoke-Item ..\Manifest\

    Write-Ascii -InputObject "DEPLOYMENT SUCCEEDED." -ForegroundColor Green