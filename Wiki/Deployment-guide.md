# Prerequisites
To begin, you will need: 
* An Azure subscription where you can create the following kinds of resources:  
    * App Service
    * App Service Plan
    * Bot Channels Registration
    * Azure Function
    * Azure Storage Account
    * Service Bus
    * Application Insights
* A team with the users who will be sending messages with this app. (You can add and remove team members later!)
* A copy of the Company Communicator app GitHub repo (https://github.com/OfficeDev/microsoft-teams-company-communicator-app)
* **Important**: If you wish to use this template to send messages to all users/large number of users on Teams, you need to ensure all such users have the app installed in their personal scope. To programmatically install the app for the users you will need to use Pre-install Graph APIs. Documentation available [HERE](https://docs.microsoft.com/en-us/microsoftteams/platform/concepts/bots/bot-conversations/bots-conv-proactive#proactively-install-your-app-using-graph). 

> **NOTE:** If you plan to use a custom domain name instead of relying on Azure Front Door, read the instructions [here](Custom-domain-option) first.

# Step 1: Register Azure AD application

Register an Azure AD applications in your tenant's directory for the bot.

1. Log in to the Azure Portal for your subscription, and go to the "App registrations" blade [here](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps).

1. Click on "New registration", and create an Azure AD application.
    1. **Name**: The name of your Teams app - if you are following the template for a default deployment, we recommend "Company Communicator".
    1. **Supported account types**: Select "Accounts in any organizational directory"
    1. Leave the "Redirect URI" field blank for now.

    ![Azure AD app registration page](images/multitenant_app_creation.png)

1. Click on the "Register" button.

1. When the app is registered, you'll be taken to the app's "Overview" page. Copy the **Application (client) ID**; we will need it later. Verify that the "Supported account types" is set to **Multiple organizations**.

    ![Azure AD app overview page](images/multitenant_app_overview.png)

1. On the side rail in the Manage section, navigate to the "Certificates & secrets" section. In the Client secrets section, click on "+ New client secret". Add a description for the secret, and choose when the secret will expire. Click "Add".

    ![Azure AD app overview page](images/multitenant_app_secret.png)

1. Once the client secret is created, copy its **Value**; we will need it later.

    At this point you have 3 unique values:
    * Application (client) ID for the bot
    * Client secret for the bot
    * Directory (tenant) ID

    We recommend that you copy these values into a text file, using an application like Notepad. We will need these values later.

    ![image2](images/azure-config-app-step3.png)

# Step 2: Deploy to your Azure subscription
1. Click on the "Deploy to Azure" button below.
   
[![Deploy to Azure](https://azuredeploy.net/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FOfficeDev%2Fmicrosoft-teams-company-communicator-app%2Fmaster%2FDeployment%2Fazuredeploy.json)

1. When prompted, log in to your Azure subscription.

1. Azure will create a "Custom deployment" based on the ARM template and ask you to fill in the template parameters. Please ensure that you don't use underscore (_) or spaces in any of the field values otherwise the deployment may fail. Additionally, if your Azure subscription is in a different tenant than your Teams app, please change the tenantID field to the tenant in which you are deploying this Teams app.

1. Select a subscription and resource group.
   * We recommend creating a new resource group.
   * The resource group location MUST be in a datacenter that supports: Application Insights; Service Bus; Azure Functions; App Service; Storage Accounts. For an up-to-date list, click [here](https://azure.microsoft.com/en-us/global-infrastructure/services/?products=storage,app-service,monitor,service-bus,functions), and select a region where all of the following services are available:
     * Storage Accounts
     * Application Insights
     * Azure Functions
     * Service Bus
     * App Service

1. Enter a "Base Resource Name", which the template uses to generate names for the other resources.
   * The app service name `[Base Resource Name]` must be available. For example, if you select `contosocommunicator` as the base name, the name `contosocommunicator` must be available (not taken); otherwise, the deployment will fail with a Conflict error.
   * Remember the base resource name that you selected. We will need it later.

1. Fill in the various IDs in the template:
    1. **Bot Client ID**: The application (client) ID of the Microsoft Teams bot app (from above)
    1. **Bot Client Secret**: The client secret of the Microsoft Teams bot app (from above)
    1. **Tenant Id**: The tenant ID (from above)

    Make sure that the values are copied as-is, with no extra spaces. The template checks that GUIDs are exactly 36 characters.

1. Fill in the "Sender UPN List", which is a semicolon-delimited list of users who will be allowed to send messages using Company Communicator.
    * For example, to allow Megan Bowen (meganb@contoso.com) and Adele Vance (adelev@contoso.com) to send messages, set this parameter to `meganb@contoso.com;adelev@contoso.com`.
    * You can change this list later by going to the app service's "Configuration" blade.

1. If you wish to change the app name, description, and icon from the defaults, modify the corresponding template parameters.

1. Agree to the Azure terms and conditions by clicking on the check box "I agree to the terms and conditions stated above" located at the bottom of the page.

1. Click on "Purchase" to start the deployment.

1. Wait for the deployment to finish. You can check the progress of the deployment from the "Notifications" pane of the Azure Portal. It can take **up to an hour** for the deployment to finish.

    > If the deployment fails, see [this section](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Troubleshooting#1-code-deployment-failure) of the Troubleshooting guide.

1. Once the deployment has finished, go to the deployment's "Outputs" tab, and note down the values. We will need them later.
    * **botId:** This is the Microsoft Application ID for the Company Communicator app. For the following steps, it will be referred to as `%botId%`.
    * **appDomain:** This is the base domain for the Company Communicator app. For the following steps, it will be referred to as `%appDomain%`.

> **IMPORTANT:** If you plan to use a custom domain name instead of relying on Azure Front Door, read the instructions [here](Custom-domain-option) before continuing any further.

# Step 3: Set up authentication

1. Note that you have the `%botId%` and `%appDomain%` values from the previous step.

    > If you have lost these, see [this section](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Troubleshooting#2-forgetting-the-botId-or-appDomain) of the Troubleshooting guide.

1. Go back to the "App Registrations" page [here](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps).

1. Click on the app you previously created in the application list. Under "Manage", click on "Authentication" to bring up authentication settings.

1. Add a new entry to "Redirect URIs":
    * **Type**: Web
    * **Redirect URI**: Enter `https://%appDomain%/signin-simple-end` for the URL e.g. `https://appName.azurefd.net/signin-simple-end`

1. Under "Implicit grant", check "ID tokens".

1. Click "Save" to commit your changes.

1. Back under "Manage", click on "Expose an API".

1. Click on the "Set" link next to "Application ID URI", and change the value to "`api://%appDomain%`" e.g. `api://appName.azurefd.net`.

1. Click "Save" to commit your changes.

1. Click on "Add a scope", under "Scopes defined by this API". In the flyout that appears, enter the following values:
    * **Scope name:** access_as_user
    * **Who can consent?:** Admins and users
    * **Admin and user consent display name:** Access the API as the current logged-in user
    * **Admin and user consent description:**  Access the API as the current logged-in user

1. Click "Add scope" to commit your changes.

1. Click "Add a client application", under "Authorized client applications". In the flyout that appears, enter the following values:
    * **Client ID**: `5e3ce6c0-2b1f-4285-8d4b-75ee78787346`
    * **Authorized scopes**: Select the scope that ends with `access_as_user`. (There should only be 1 scope in this list.)

1. Click "Add application" to commit your changes.

1. Repeat the previous two steps, but with client ID = `1fec8e78-bce4-4aaf-ab1b-5451cc387264`. After this step you should have **two** client applications (`5e3ce6c0-2b1f-4285-8d4b-75ee78787346` and `1fec8e78-bce4-4aaf-ab1b-5451cc387264`) listed under "Authorized client applications".

1. Back under "Manage", click on "Manifest".

1. In the editor that appears, find the `accessTokenAcceptedVersion` property in the JSON Azure AD application, and set the value to `2`. After updating it will appear like below:
    ```
       "accessTokenAcceptedVersion": 2
    ```

1. Then find the `optionalClaims` property in the JSON Azure AD application manifest, and replace it with the following block:
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

1. Click "Save" to commit your changes.

# Step 4: Assign Permission to your app

Continuing from the Azure AD app registration page where we ended Step 3.

1. Select **API Permissions** blade from the left hand side.

2. Click on **Add a permission** button to add permission to your app.

3. In Microsoft APIs under Select an API label, select the particular service and give the following permissions,

    * Under “Commonly used Microsoft APIs”, 

    * Select “Microsoft Graph”, then select **Delegated permissions** and check the following permissions,
        1. **Group.Read.All**

    * then select **Application permissions** and check the following permissions,
        1. **Group.Read.All**
        2. **User.Read.All**

    * Click on **Add Permissions** to commit your changes.

    > Please refer to [Solution overview](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Solution-overview) for more details about the above permissions.

4. If you are logged in as the Global Administrator, click on the “Grant admin consent for %tenant-name%” button to grant admin consent, else inform your Admin to do the same through the portal or follow the steps provided here  to create a link and sent it to your Admin for consent.

5. Global Administrator can also grant consent using following link: https://login.microsoftonline.com/common/adminconsent?client_id=%appId%. Please replace the `%appId%` with the `Application (client) ID` of Microsoft Teams bot app (from above).

# Step 5: Create the Teams app packages

Create two Teams app packages: one to be installed to the authors team and one for recipients to install personally and/or to teams.

1. Make sure you have cloned the app repository locally.

1. Open the `Manifest\manifest_authors.json` file in a text editor.

1. Change the placeholder fields in the manifest to values appropriate for your organization.
    * `developer.name` ([What's this?](https://docs.microsoft.com/en-us/microsoftteams/platform/resources/schema/manifest-schema#developer))
    * `developer.websiteUrl`
    * `developer.privacyUrl`
    * `developer.termsOfUseUrl`

1. Change the `<<appDomain>>` placholder in the configurationUrl setting to be the `%appDomain%` value e.g. "`https://appName.azurefd.net/configtab`".

1. Change the `<<botId>>` placeholder in the botId setting to be the `%botId%` value - this is your Azure AD application's ID from above. This is the same GUID that you entered in the template under "Bot Client ID". Please note that there are two places in the manifest (for authors) where you will need to update Bot ID.

1. Change the `<<appDomain>>` placeholder in the validDomains setting to be the `%appDomain%` value e.g. "`appName.azurefd.net`".

1. Change the `<<botId>>` placeholder in the id setting of the webApplicationInfo section to be the `%botId%` value. Change the `<<appDomain>>` placeholder in the resource setting of the webApplicationInfo section to be the `%appDomain%` value e.g. "`api://appName.azurefd.net`".

1. Copy the `manifest_authors.json` file to a file named `manifest.json`.

1. Create a ZIP package with the `manifest.json`,`color.png`, and `outline.png`. The two image files are the icons for your app in Teams.
    * Name this package `company-communicator-authors.zip`, so you know that this is the app for the author teams.
    * Make sure that the 3 files are the _top level_ of the ZIP package, with no nested folders.  
    ![image10](images/file-explorer.png)

1. Delete the `manifest.json` file.

Repeat the steps above but with the file `Manifest\manifest_users.json`. Note: you will not need to change anything for the configurationUrl or webApplicationInfo section because the recipients app does not have the configurable tab. Name the resulting package `company-communicator-users.zip`, so you know that this is the app for the recipients.


# Step 6: Run the apps in Microsoft Teams

If your tenant has sideloading apps enabled, you can install your app by following the instructions [here](https://docs.microsoft.com/en-us/microsoftteams/platform/concepts/apps/apps-upload#load-your-package-into-teams).

You can also upload it to your tenant's app catalog, so that it can be available for everyone in your tenant to install. See [here](https://docs.microsoft.com/en-us/microsoftteams/tenant-apps-catalog-teams).

1. Install the authors app (the `company-communicator-authors.zip` package) to your team of message authors.
    * Note that even if non-authors install the app, the UPN list in the app configuration will prevent them from accessing the message authoring experience. Only the users in the sender UPN list will be able to compose and send messages.

> **IMPORTANT:** We recommend installing the authors app to the appropriate team as a custom (sideloaded) app. Do NOT use [app permission policies](https://docs.microsoft.com/en-us/microsoftteams/teams-app-permission-policies) to restrict access to this app to the members of the authors team. Otherwise, members of the authoring team may not receive messages sent from Company Communicator.

1. Add the configurable tab to the team of authors, so that they can compose and send messages.

1. Install the end-user app (the `company-communicator-users.zip` package) to the users and teams that will be the target of messages. As noted in the [Known limitations](Known-limitations) page, authors can send messages only to the users and teams that have this app installed. 


# Troubleshooting
Please see our [Troubleshooting](Troubleshooting) page.
