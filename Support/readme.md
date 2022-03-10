## Why
This folder is created to aid in the troubleshooting when deploying this application into your tenant/environment.

# **Start-ChatWithUser.ps1**
This script is designed for the purpose of testing the chat functionality of the bot within the Teams App.  Some modifications are needed before you can start using this. In the "Variables need to be replaced to start using the script" section, there are a couple of variables you will need to replace:

* $tenantId = "tenant.onmicrosoft.com" #or in GUID format "00000000-0000-0000-0000-000000000000"
* $teamsAppId = "00000000-0000-0000-0000-000000000000" # AppId of the Teams App Manifest 
* $graphAppId = "00000000-0000-0000-0000-000000000000"
* $graphAppSecret= "secret"
* $userAppId = "00000000-0000-0000-0000-000000000000"
* $userAppSecret = "secret" 

For the secrets, I recommend to an extra secret per App which you can delete after using this script. This way, it won't interfere with the configuration of the application. And as an added bonus, it's more secure because the script will only run with the newly created secrets.

# **Check-AppRegistrations.ps1**
This script is meant to retrieve the provisioned App Registrations and the secrets which are stored in the KeyVault are working together correctly or not. If an access token is being returned, it means that the combinations of AppId + AppSecrets are working.

# **Check-GraphApp.ps1**
This a simplified version of the Check-AppRegistrations script. If you just want to see if the AppId + AppSecret is working correctly, please use this one. It is meant to use if the App registration of the Graph App (e.g. the main App) is working correctly (e.g. combination of AppId + AppSecret) and also if the API Permissions are set correct to retrieve Groups and Users