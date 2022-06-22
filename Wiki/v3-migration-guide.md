## Company Communicator v3 Migration Guide

## Upgrading from v2.x to v3
If you have the CCv2.x deployed and plan to migrate to CCv3, perform the following steps:

### 1. Read CCv2.x deployment parameters:
Copy all the parameters from the previous deployment (CCv2.x), and make sure you have the following:
  * Name of the Azure subscription.
  * Name of the Azure resource group.
  * Base resource name.
  * Bot tenant ID.
  * Bot client ID.
  * Bot client secret.
  * Sender UPN list.

We will use them in the next steps.

Please refer [step 2](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Deployment-guide) in the Deployment guide for more details about the above values.

### 2. Deploy to your Azure subscription
1. Click on the **Deploy to Azure** button below.
   
   [![Deploy to Azure](images/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FOfficeDev%2Fmicrosoft-teams-company-communicator-app%2Fmain%2FDeployment%2Fazuredeploy.json)

2. When prompted, log in to your Azure subscription.
    > Please use the same subscription being used for your Company Communicator v2 deployment (from step 1).

3. Azure will create a "Custom deployment" based on the Company Communicator ARM template and ask you to fill in the template parameters.

    > **Note:** Please ensure that you don't use underscore (_) or space in any of the field values otherwise the deployment may fail.

4. Select a subscription and a resource group.
    > Please use the same `subscription`, `resource group` being used for your Company Communicator v2 deployment. (from step 1)

5. Enter a **Base Resource Name**.
    > Please use the same `Base resource name` being used for your Company Communicator v2 deployment. (from step 1)
 
6. Update the following fields in the template:
    1. **Bot Client ID**: The application (client) ID of the Microsoft Teams bot app. (from Step 1)
    2. **Bot Client Secret**: The client secret of the Microsoft Teams bot app. (from Step 1)
    3. **Tenant Id**: The tenant ID. (from Step 1)
    4. **Proactively Install User App [Optional]**: Default value is `true`. You may set it to `false` if you want to disable the feature.
    5. **User App ExternalId [Optional]**: Default value is `148a66bb-e83d-425a-927d-09f4299a9274`. This **MUST** be the same `id` that is in the Teams app manifest for the user app.
    6. **DefaultCulture, SupportedCultures [Optional]**: By default the application contains `en-US` resources. You may add/update the resources for other locales and update this configuration if desired.

    > **Note:** For ids, make sure that the values are copied as-is, with no extra spaces. The template checks that GUIDs are exactly 36 characters.

7. Update the "Sender UPN List", which is a semicolon-delimited list of users (Authors) who will be allowed to send messages using the Company Communicator.
    * For example, to allow Megan Bowen (meganb@contoso.com) and Adele Vance (adelev@contoso.com) to send messages, set this parameter to `meganb@contoso.com;adelev@contoso.com`.
    * You can change this list later by going to the App Service's "Configuration" blade.
   > You may use the same value being used for your Company Communicator v2 deployment. (step 1)

8. Agree to the Azure terms and conditions by clicking on the check box "I agree to the terms and conditions stated above" located at the bottom of the page.

9. Click on "Purchase" to start the deployment.

10. Wait for the deployment to finish. You can check the progress of the deployment from the "Notifications" pane of the Azure Portal. It may take **up to an hour** for the deployment to finish.

    > If the deployment fails, see [this section](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Troubleshooting#1-code-deployment-failure) of the Troubleshooting guide.

11. Then go to the "Deployment Center" section of the app service. Click on the "Sync" to update the existing app service to the latest code in the GitHub repository.
  ![Screenshot of refreshing code deployment](images/troubleshooting_sourcecontrols.png)

12. Please repeat the above step (step 11) for the three function apps.
    * [Base Resource Name]-prepare-function
    * [Base Resource Name]-function
    * [Base Resource Name]-data-function

### 3. Add Permissions to your app

We have added new features in CCv3 - sync all users in a tenant and proactively install user application. These operations require additional graph permissions. Please follow the steps and ensure all the permissions are added.

1. Go to the **App Registrations** page [here](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps). 
2. Select **API Permissions** blade from the left hand side.

3. Click on **Add a permission** button to add permission to your app.

4. In Microsoft APIs under Select an API label, select the particular service and give the following permissions,

    * Under **Commonly used Microsoft APIs**, 

    * Select “Microsoft Graph”, then select **Delegated permissions** and check the following permissions,
        1. **Group.Read.All**
        2. **AppCatalog.Read.All**

    * then select **Application permissions** and check the following permissions,
        1. **Group.Read.All**
        2. **User.Read.All**
        3. **TeamsAppInstallation.ReadWriteForUser.All**

    * Click on **Add Permissions** to commit your changes.

    ![Azure AD API permissions](images/multitenant_app_permissions_1.png)
    ![Azure AD API permissions](images/multitenant_app_permissions_2.png)

    > Please refer to [Solution overview](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Solution-overview#microsoft-graph-api) for more details about the above permissions.

5. If you are logged in as the Global Administrator, click on the “Grant admin consent for %tenant-name%” button to grant admin consent, else inform your Admin to do the same through the portal.
   <br/>
   Alternatively you may follow the steps below:
   - Prepare link - https://login.microsoftonline.com/common/adminconsent?client_id=%appId%. Replace the `%appId%` with the `Application (client) ID` of Microsoft Teams bot app (from above).
   - Global Administrator can grant consent using the link above.

### 4. Upload User app to App Catalog

1. Upload the User app to your tenant's app catalog so that it is available for everyone in your tenant to install. See [here](https://docs.microsoft.com/en-us/microsoftteams/tenant-apps-catalog-teams).
> **IMPORTANT:** Proactive app installation will work only if you upload the User app to your tenant's app catalog.

2. Install the User app (the `company-communicator-users.zip` package) to the users and teams that will be the target of messages. 
> If `proactiveAppInstallation` is enabled, you may skip this step. The service will install the app for all the recipients when authors send a message.

> **Note:** You may skip this step if you decide to disable Proactive app installation / if you've already uploaded the app to App Catalog as part of CCv2 deployment.

### Migration Status
If you have performed all the steps, migration completes after successful deployment.
