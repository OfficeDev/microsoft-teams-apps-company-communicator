# **Prerequisites**

To begin, you will need:

- An Azure subscription where you can create the following kinds of resources:
  - App Service
  - App Service Plan
  - Bot Channels Registration
  - Azure Function
  - Azure Storage Account
  - Service Bus
  - Application Insights
- A team with the users who will be sending messages with this app. (You can add and remove team members later!)
- A copy of the Company Communicator app GitHub repo ([https://github.com/OfficeDev/microsoft-teams-company-communicator-app](https://github.com/OfficeDev/microsoft-teams-company-communicator-app))
- **Important** : If you wish to use this template to send messages to all users/large number of users on Teams, you need to ensure all such users have the app installed in their personal scope. To programmatically install the app for the users you will need to use Pre-install Graph APIs. Documentation available [HERE](https://docs.microsoft.com/en-us/microsoftteams/platform/bots/how-to/conversations/send-proactive-messages?tabs=dotnet#proactively-install-your-app-using-graph).

**NOTE:**  If you plan to use a custom domain name instead of relying on Azure Front Door, read the instructions [here](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Custom-domain-option) first.

**1: Deploy to your Azure subscription**

\*\* For manual deployment please use this [deployment guide](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Deployment-guide).

Please follow below steps to deploy app template:

- Download the whole solution folder from [GitHub](https://github.com/OfficeDev/microsoft-teams-company-communicator-app)
- Open the PowerShell in  **administrator**  mode
- Navigate to deploy.ps1 in your local machine.
  - cd \PathToLocalFolder\Deployment
- Before running the script, some installations are needed for the user who is running the script for the first time. Please find the steps below:
  - In the above-navigated path in PowerShell, run the command &quot; **Set-ExecutionPolicy -ExecutionPolicy RemoteSigned**&quot;. This command will allow the user to run deploy.ps1 as execution policy is restricted by default. You can change it to restricted again after successful deployment.
  - You will need to unblock the deployment script file before executing the script &quot; **Unblock-File -Path .\deploy.ps1**&quot;


  - Reboot the machine after installing the module.
  - Open a new PowerShell window and in administrator mode. Go to the path of deploy.ps1 script file again.
- Fill-in the Deployment\parameters.json file with required parameters values for the script. Replace << value >>; with the correct value for each parameter.

![Powershell deployment guide](images/param_file_view.png)

The script requires the following parameters:

- **TenantId** - Id of the tenant to deploy to (If you are not sure how to get Tenant ID, please check Azure Active Directory in Azure Portal. Under Manage, click Properties. The tenant ID is shown in the Directory ID box). e.g 98f3ece2-3a5a-428b-aa4f-4c41b3f6eef0
- **SubscriptionId** - Azure subscription to deploy the solution to (MUST be associated with the Azure AD of the Office 365 tenant that you wish to deploy this solution to.) e.g. 22f602c4-1b8f-46df-8b73-45d7bdfbf58e

- **Location** - Azure region in which to create the resources. The internal name should be used e.g. eastus.
- **Resource Group Name** - Name for a new resource group to deploy the solution to - the script will create this resource group. e.g. CompanyCommunicatorRG.
- **Base Resource Name** - which the template uses to generate names for the other resources.
  - The [Base Resource Name] must be available. For example, if you select contosocommunicator as the base name, the name contosocommunicator must be available (not taken); otherwise, it will prompt you to confirmation dialog to update the existing resources.
  - [Base Resource Name] -data-function, [Base Resource Name] -function etc.
- **CompanyName** - Your company name which will added to the app metadata.
- **WebsiteUrl** - Your company website.
- **PrivacyUrl** - Your company privacy url.
- **TermsOfUseUrl** - Your company terms of use url.
- **Proactively Install User App [Optional]**: Default value is true. You may set it to false if you want to disable the feature.
- **User App ExternalId [Optional]**: Default value is 148a66bb-e83d-425a-927d-09f4299a9274. This is the external Id provided in the User app manifest.
- **DefaultCulture, SupportedCultures [Optional]**: By default the application contains en-US resources. You may add/update the resources for other locales and update this configuration if desired.

**Note:**  Make sure that the values are copied as-is, with no extra spaces. The template checks that GUIDs are exactly 36 characters.

**Note:**  If your Azure subscription is in a different tenant than the tenant where you want to install the Teams App, please update the Tenant Id field with the tenant where you want to install the Teams App.

- **SenderUPNList** - Update the &quot;Sender UPN List&quot;, which is a semicolon-delimited list of users (Authors) who will be allowed to send messages using the Company Communicator.

- For example, to allow Megan Bowen ([meganb@contoso.com](mailto:meganb@contoso.com)) and Adele Vance ([adelev@contoso.com](mailto:adelev@contoso.com)) to send messages, set this parameter to meganb@contoso.com;adelev@contoso.com.
- You can change this list later by going to the App Service&#39;s &quot;Configuration&quot; blade.

- **CustomDomainOption** -How the app will be hosted on a domain that is not \*.azurewebsites.net. Azure Front Door is an easy option that the template can set up automatically, but it comes with ongoing monthly costs.

The script has some optional parameters with default values. You can change the default values to fit your needs:

- **AppDisplayName** - The app (and bot) display name. Default value:Company Communicator.
- **AppDescription** - The app (and bot) description. Default value: Broadcast messages to multiple teams and people in one go.
- **AppIconUrl** - The link to the icon for the app. It must resolve to a PNG file. Default value [https://raw.githubusercontent.com/OfficeDev/microsoft-teams-company-communicator-app/master/Manifest/color.png](https://raw.githubusercontent.com/OfficeDev/microsoft-teams-company-communicator-app/master/Manifest/color.png)
- **Sku** - The pricing tier for the hosting plan. Default value: Standard
- **PlanSize** - The size of the hosting plan (small, medium, or large). Default value: 1
- **GitRepoUrl** - The URL to the GitHub repository to deploy. Default value: [https://github.com/OfficeDev/microsoft-teams-company-communicator-app.git](https://github.com/OfficeDev/microsoft-teams-company-communicator-app.git)
- **GitBranch** - The branch of the GitHub repository to deploy. Default value: master
- Execute the following script in Powershell window:

**PS C:\\&gt; .\deploy.ps1**

Once the script will start running first it will check for azure CLI application.

If the CLI is not installed, it will prompt you to confirmation dialog to install azure CLI.

![Powershell deployment guide](images/azure-cli.png)

**Note** : After azure CLI installation the PowerShell session needs to be restarted. The script will automatically **exit** , just after CLI application installation. It will ask you to open a new session and re-run the script as previous steps followed.

If the azure CLI application is already installed, the script will move to the next step to check for the availability of modules which needs to be available for executing the script to the logged in user. The script will prompt to confirmation dialog to update/install modules.

![Powershell deployment guide](images/modules.png)

The script will prompt for authentication **twice** during execution, once to get access to the Azure subscription, and the other to get access to Azure Active Directory. Please login using an account that has  **contributor**  role or higher.

![Powershell deployment guide](images/login_screen_1.png)

![Powershell deployment guide](images/login_screen_2.png)

Then the script will validate the existence of Azure resources in the selected region and whether the resources names are available or not. If resources with same name already exist, the script will show a confirmation box to proceed with updating existing resources.

![Powershell deployment guide](images/name_availability_check.png)

If Azure AD applications already exist on tenant, The script will show confirmation dialog to update current applications&#39; configurations.

![Powershell deployment guide](images/ad_app_update.png)

If the ARM template deployment has completed, script will prompt you to update the AD app settings &quot;Admin consent permissions is required for app registration using CLI&quot;. After choosing yes, the script will provide admin consent to AD app.

![Powershell deployment guide](images/admin-consent.png)


When the script has completed a &quot; **DEPLOYMENT SUCCEEDED**&quot; message will be displayed and folder that contains the .zip file will be open.

![Powershell deployment guide](images/success_message.png)


![Powershell deployment guide](images/manifest_folder_view.png)


- After running the script. AD apps, Bot/Config Apps, and all required resources will be created.
- If PowerShell script breaks during deployment, you may run the deployment again if there is no conflict (a resource name already exist in other resource group or another tenant) or refer to Troubleshooting page.
- If PowerShell script keeps failing, you may share deployment logs (generated in Deployment\logs.zip) with the app template support team.

![Powershell deployment guide](images/log_folder_view.png)


**2. Install the apps in Microsoft Teams**

- Install the authors app (the cc-authors.zip package) to your team of message authors.
- Note that even if non-authors install the app, the UPN list in the app configuration will prevent them from accessing the message authoring experience. Only the users in the sender UPN list will be able to compose and send messages.
- If your tenant has sideloading apps enabled, you can install your app by following the instructions [here](https://docs.microsoft.com/en-us/microsoftteams/platform/concepts/apps/apps-upload#load-your-package-into-teams) .
- **IMPORTANT:**  We recommend installing the authors app to the appropriate team as a custom (sideloaded) app. Do NOT use [app permission policies](https://docs.microsoft.com/en-us/microsoftteams/teams-app-permission-policies)  to restrict access to this app to the members of the authors team. Otherwise, members of the authoring team may not receive messages sent from Company Communicator.
- Add the configurable tab to the team of authors, so that they can compose and send messages.
- Upload the User app to your tenant&#39;s app catalog so that it is available for everyone in your tenant to install. See [here](https://docs.microsoft.com/en-us/microsoftteams/tenant-apps-catalog-teams) .
- **IMPORTANT:**  Proactive app installation will work only if you upload the User app to your tenant&#39;s app catalog.
- Install the User app (the cc-users.zip package) to the users and teams that will be the target of messages.
- If proactiveAppInstallation is enabled, you may skip this step. The service will install the app for all the recipients when authors send a message.