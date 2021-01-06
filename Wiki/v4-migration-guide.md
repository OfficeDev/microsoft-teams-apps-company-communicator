## Company Communicator v4 Migration Guide

## Upgrading from v3 to v4
If you have the CCv3 deployed and plan to migrate to CCv4, perform the following steps:

### 1. Read CCv3 deployment parameters:
Copy all the parameters from the previous deployment (CCv3), and make sure you have the following:
  * Name of the Azure subscription.
  * Name of the Azure resource group.
  * Base resource name.
  * Bot tenant ID.
  * Bot client ID.(Referred as User Client Id in CCv4)
  * Bot client secret.(Referred as User Client Secret in CCv4)
  * Sender UPN list.

We will use them in the next steps.

Please refer [step 2](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Deployment-guide) in the Deployment guide for more details about the above values.

### 2. Register Azure AD application.
1. Register an Azure AD application in your tenant's directory for author bot.

2. Log in to the Azure Portal for your subscription, and go to the [App registrations](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps) blade.

3. Click **New registration** to create an Azure AD application.
    - **Name**: Name of your Teams App - if you are following the template for a default deployment, we recommend "Company Communicator (Authors)".
    - **Supported account types**: Select "Accounts in any organizational directory" (*refer image below*).
    - Leave the "Redirect URI" field blank for now.

    ![Azure AD app registration page](images/multitenant_app_creation.png)

4. Click **Register** to complete the registration.

5. When the app is registered, you'll be taken to the app's "Overview" page. Copy the **Application (client) ID**; we will need it later. Verify that the "Supported account types" is set to **Multiple organizations**.

    ![Azure AD app overview page](images/multitenant_app_overview_1.png)

6. On the side rail in the Manage section, navigate to the "Certificates & secrets" section. In the Client secrets section, click on "+ New client secret". Add a description for the secret, and choose when the secret will expire. Click "Add".

    ![Azure AD app secret](images/multitenant_app_secret.png)

7. Once the client secret is created, copy its **Value**; we will need it later.

### 3. Clean the Company Communicator v3 app registration

1. Go to **App Registrations** page [here](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps) and open the app you created in Company Communicator v3(in Step 1).

1. Under **Manage**, click on **Authentication** to bring up authentication settings.

    1. Delete the entry to **Redirect URIs**.

    1. Under **Implicit grant**, un-check **ID tokens**.

    1. Click **Save** to commit your changes.

1. Back under **Manage**, click on **Expose an API**.This step is to remove the registered domain from current registration so that it can be migrated to author's registration, as backend api will be exposed by author's application. Please follow the below steps as per the order mentioned.

    1. First, delete the list of Authorized client applications. 

    1. Then, click on the scope defined and disable the scope. Click on Save to commit your changes.

    1. Now, click on the scope defined and then click on Delete.

    1. Then, to delete the **Application ID URI** there are multiple steps involved. The steps will involve delete and update operation to completely remove the **Application ID URI** from the current Azure AD object Id.
    
    1. Delete the **Application ID URI**.
    ![Azure AD expose an api page](images/delete_application_uri.png)
    1. Click on the Set **Application ID URI** and then Click on Save.
    ![Azure AD expose an api page](images/set_application_uri.png)
    1. Click **Save** to commit your changes.

1. Back under **Manage**, click on **Manifest**.

   1. In the editor that appears, find the `optionalClaims` property in the JSON Azure AD application manifest, and replace it with the following block:

   ```
   "optionalClaims": null,
   ```

   1. Click **Save** to commit your changes.

1. Select **API Permissions** blade from the left hand side.

    1. Click on **Group.Read.All** permission and then click on remove permission.
     ![Azure AD api permission page](images/remove_permission.png)

    1. Repeat the same for other permissions. Note: Do not delete the **User.Read** permission.

### 4. Deploy to your Azure subscription

1. Click on the **Deploy to Azure** button below.
   
   [![Deploy to Azure](https://azuredeploy.net/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FOfficeDev%2Fmicrosoft-teams-company-communicator-app%2Fmaster%2FDeployment%2Fazuredeploy.json)

2. When prompted, log in to your Azure subscription.
    > Please use the same subscription being used for your Company Communicator v3 deployment (from step 1).

3. Azure will create a "Custom deployment" based on the Company Communicator ARM template and ask you to fill in the template parameters.

    > **Note:** Please ensure that you don't use underscore (_) or space in any of the field values otherwise the deployment may fail.

4. Select a subscription and a resource group.
    > Please use the same `subscription`, `resource group` being used for your Company Communicator v3 deployment. (from step 1)

5. Enter a **Base Resource Name**.
    > Please use the same `Base resource name` being used for your Company Communicator v3 deployment. (from step 1)

6. Update the following fields in the template:
    1. **User Client ID**: The application (client) ID of the Microsoft Teams bot app. (please use the same `Bot Id` being used for your Company Communicator v3 deployment.) (from step 1)
    2. **User Client Secret**: The client secret of the Microsoft Teams bot app. (please use the same `Bot Secret` being used for your Company Communicator v3 deployment.) (from step 1)
    3. **Tenant Id**: The tenant ID. (please use the same `Tenant Id` being used for your Company Communicator v3 deployment.) (from step 1)
    4. **Author Client ID**: The application (client) ID of the Microsoft Teams author bot app. (from Step 1)
    5. **Author Client Secret**: The client secret of the Microsoft Teams author bot app. (from Step 1)
    7. **Proactively Install User App [Optional]**: Default value is `true`. You may set it to `false` if you want to disable the feature.
    8. **User App ExternalId [Optional]**: Default value is `148a66bb-e83d-425a-927d-09f4299a9274`. This **MUST** be the same `id` that is in the Teams app manifest for the user app.
    9. **DefaultCulture, SupportedCultures [Optional]**: By default the application contains `en-US` resources. You may add/update the resources for other locales and update this configuration if desired.

    > **Note:** For ids, make sure that the values are copied as-is, with no extra spaces. The template checks that GUIDs are exactly 36 characters.

7. Fill in the "Sender UPN List", which is a semicolon-delimited list of users who will be allowed to send messages using Company Communicator.
    * For example, to allow Megan Bowen (meganb@contoso.com) and Adele Vance (adelev@contoso.com) to send messages, set this parameter to `meganb@contoso.com;adelev@contoso.com`.
    * You can change this list later by going to the app service's "Configuration" blade.

8. Agree to the Azure terms and conditions by clicking on the check box "I agree to the terms and conditions stated above" located at the bottom of the page.

9. Click on "Purchase" to start the deployment.

10. Wait for the deployment to finish. You can check the progress of the deployment from the "Notifications" pane of the Azure Portal. It can take **up to an hour** for the deployment to finish.

    > If the deployment fails, see [this section](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Troubleshooting#1-code-deployment-failure) of the Troubleshooting guide.

11. Then go to the "Deployment Center" section of the app service. Click on the "Sync" to update the existing app service to the latest code in the GitHub repository.

  ![Screenshot of refreshing code deployment](images/troubleshooting_sourcecontrols.png)

12. Please repeat the above step (step 11) for the function apps.


## 5. Set-up Authentication

1. Note that you have the `%authorBotId%`, `%userBotId%` and `%appDomain%` values from the previous step (Step 2).

    > If do not have these values, refer [this section](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Troubleshooting#2-forgetting-the-botId-or-appDomain) of the Troubleshooting guide for steps to get these values.

1. Go to **App Registrations** page [here](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps) and open the author app you created (in Step 1) from the application list.

1. Under **Manage**, click on **Authentication** to bring up authentication settings.

    1. Add a new entry to **Redirect URIs**:
        - **Type**: Web
        - **Redirect URI**: Enter `https://%appDomain%/signin-simple-end` for the URL e.g. `https://appName.azurefd.net/signin-simple-end`

    1. Under **Implicit grant**, check **ID tokens**.

    1. Click **Save** to commit your changes.

1. Back under **Manage**, click on **Expose an API**.

    1. Click on the **Set** link next to **Application ID URI**, and change the value to `api://%appDomain%` e.g. `api://appName.azurefd.net`.

    1. Click **Save** to commit your changes.

    1. Click on **Add a scope**, under **Scopes defined by this API**. In the flyout that appears, enter the following values:
        * **Scope name:** access_as_user
        * **Who can consent?:** Admins and users
        * **Admin and user consent display name:** Access the API as the current logged-in user
        * **Admin and user consent description:**  Access the API as the current logged-in user

    1. Click **Add scope** to commit your changes.

    1. Click **Add a client application**, under **Authorized client applications**. In the flyout that appears, enter the following values:
        * **Client ID**: `5e3ce6c0-2b1f-4285-8d4b-75ee78787346`
        * **Authorized scopes**: Select the scope that ends with `access_as_user`. (There should only be 1 scope in this list.)

    1. Click **Add application** to commit your changes.

    1. **Repeat the previous two steps**, but with client ID = `1fec8e78-bce4-4aaf-ab1b-5451cc387264`. After this step you should have **two** client applications (`5e3ce6c0-2b1f-4285-8d4b-75ee78787346` and `1fec8e78-bce4-4aaf-ab1b-5451cc387264`) listed under **Authorized client applications**.

1. Back under **Manage**, click on **Manifest**.

   1. In the editor that appears, find the `optionalClaims` property in the JSON Azure AD application manifest, and replace it with the following block:
    ```
        "optionalClaims": {
            "idToken": [],
            "accessToken": [
                {
                    "name": "upn",
                    "source": null,
                    "essential": false,
                    "additionalProperties": []
                }
            ],
            "saml2Token": []
        },
    ```

1. Click **Save** to commit your changes.

## 6. Add Permissions to your app

Continuing from the Azure AD author app registration page where we ended Step 3.

1. Select **API Permissions** blade from the left hand side.

2. Click on **Add a permission** button to add permission to your app.

3. In Microsoft APIs under Select an API label, select the particular service and give the following permissions,

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

4. If you are logged in as the Global Administrator, click on the “Grant admin consent for %tenant-name%” button to grant admin consent, else inform your Admin to do the same through the portal.
   <br/>
   Alternatively you may follow the steps below:
   - Prepare link - https://login.microsoftonline.com/common/adminconsent?client_id=%appId%. Replace the `%appId%` with the `Application (client) ID` of Microsoft Teams author bot app (from above).
   - Global Administrator can grant consent using the link above.

### 5. Create the Teams app package

You need to only update the author's team package.

  1. Open the `Manifest\manifest_authors.json` file in a text editor.

  2. Change the `<<botId>>` placeholder in the botId setting to be the `%authorBotId%` value - this is your author Azure AD application's ID from above. This is the same GUID that you entered in the template under "Author Client ID". Please note that there are two places in the manifest (for authors) where you will need to update Bot ID.

  > Please refer to step 5 in the Deployment guide for more details on creating the 
  Teams app package 
  [this section](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Deployment-guide).

### 6. Install the authors app in Microsoft Teams.

1. Delete the current authors app from the team.
> **IMPORTANT :** The team id will be deleted from the target audience after deleting the app. Install the User app (the `company-communicator-users.zip` package) to the team to add it to the target audience.

2. Update the authors app package. Please refer to 
  [this link](https://docs.microsoft.com/en-us/microsoftteams/manage-apps#upload-a-new-app) 
  to update the app package.
    * If your tenant has sideloading apps enabled, you can install your app by following the instructions [here](https://docs.microsoft.com/en-us/microsoftteams/platform/concepts/apps/apps-upload#load-your-package-into-teams).

3. Add the authors app (the `company-communicator-authors.zip` package) to your team of message authors.
    * Note that even if non-authors install the app, the UPN list in the app configuration will prevent them from accessing the message authoring experience. Only the users in the sender UPN list will be able to compose and send messages. 

### Migration Status
If you have performed all the steps, migration completes after successful deployment.