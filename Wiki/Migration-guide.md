If you already have version 1 of the Company Communicator app deployed in Azure, then it be can easily migrated to version 2 by using the following steps:
##### 1. Find out the following information of your Company Communicator v1 deployment. They are required in the migration:
  * The name of the Azure subscription. 
  * The name of the Azure resource group.
  * The base resource name.
  * The Bot Tenant Id.
  * The Bot client id.
  * The Bot client secret.

    > Please refer to step 2 in the Deployment guide for more details about the above values.
https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Deployment-guide

##### 2: Assign Permission to your app

1. Go to the **App Registrations** page [here](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps).

2. Select **API Permissions** blade from the left hand side.

3. Click on **Add a permission** button to add permission to your app.

4. In Microsoft APIs under Select an API label, select the particular service and give the following permissions,

    * Under “Commonly used Microsoft APIs”,
    
    * Select “Microsoft Graph”, then select **Delegated permissions** and check the following permissions,
      1. **Group.Read.All**

    * Then select **Application permissions** and check the following permissions,
      1. **Group.Read.All**
      2. **User.Read.All**

    * Click on **Add Permissions** to commit your changes.

    > Please refer to [Solution overview](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Solution-overview) for more details about the above permissions.

5. If you are logged in as the Global Administrator, click on the “Grant admin consent for %tenant-name%” button to grant admin consent, else inform your Admin to do the same through the portal or follow the steps provided here  to create a link and sent it to your Admin for consent.

6. Global Administrator can also grant consent using following link: https://login.microsoftonline.com/common/adminconsent?client_id=%appId%. Please replace the `%appId%` with the `bot client id` of Microsoft Teams bot app (from above).

##### 3. Click on the "Deploy to Azure" button below
[![Deploy to Azure](images/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FOfficeDev%2Fmicrosoft-teams-company-communicator-app%2Fmain%2FDeployment%2Fazuredeploy.json)
  * When prompted, log in to the Azure subscription.
  
    > Please use the same subscription being used for your Company Communicator v1 deployment.

  * Azure will create a "Custom deployment" based on the ARM template and ask you to fill in the template parameters. Please ensure that you don't use underscore (_) or spaces in any of the field values otherwise the deployment may fail. Additionally, if your Azure subscription is in a different tenant than your Teams app, please change the tenantID field to the tenant in which you are deploying this Teams app.

  * Please select the same subscription and resource group used for your Company Communicator v1 deployment.
 
  * Enter "Base Resource Name", which the template uses to generate names for the other resources. Please use the same name used for your Company Communicator v1 deployment.

  * Fill in the various IDs in the template:
    1. **Bot Client ID**: The application (client) ID of the Microsoft Teams bot app. Please use the same id used for your Company Communicator v1 deployment.
    1. **Bot Client Secret**: The client secret of the Microsoft Teams bot app. Please use the same secret used for your Company Communicator v1 deployment.
    1. **Tenant Id**: The tenant ID. Please use the same id used for your Company Communicator v1 deployment.

    Make sure that the values are copied as-is, with no extra spaces. The template checks that GUIDs are exactly 36 characters.

  * Fill in the "Sender UPN List", which is a semicolon-delimited list of users who will be allowed to send messages using Company Communicator.
    * For example, to allow Megan Bowen (meganb@contoso.com) and Adele Vance (adelev@contoso.com) to send messages, set this parameter to `meganb@contoso.com;adelev@contoso.com`.
    * You can change this list later by going to the app service's "Configuration" blade.

  * Agree to the Azure terms and conditions by clicking on the check box "I agree to the terms and conditions stated above" located at the bottom of the page.

  * Click on "Purchase" to start the deployment.

  * Wait for the deployment to finish. You can check the progress of the deployment from the "Notifications" pane of the Azure Portal. It can take **up to an hour** for the deployment to finish.

    > If the deployment fails, see [this section](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Troubleshooting#1-code-deployment-failure) of the Troubleshooting guide.


##### 4. Update the Teams app package

You need to only update the author's team package.

  1. Open the `Manifest\manifest_authors.json` file in a text editor.

  2. Change the value of `supportFiles` from `false` to `true`.

  > Please refer to step 5 in the Deployment guide for more details on creating the 
  Teams app package 
  [this section](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Deployment-guide).

  > Please refer to 
  [this link](https://docs.microsoft.com/en-us/microsoftteams/manage-apps#upload-a-new-app) 
  to update the app package.

##### 5. The migration is done once the deployment completed.
No need to change either AAD App Registration. It is pretty strait-forward. 
