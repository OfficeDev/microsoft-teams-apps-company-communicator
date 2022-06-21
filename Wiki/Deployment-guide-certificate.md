- Deployment Guide
    - [Prerequisites](#prerequisites) 
    - [Steps](#Deployment-Steps)
        - [Register AD Application](#1-register-azure-ad-application)
        - [Deploy to Azure subscription](#2-deploy-to-your-azure-subscription)
        - [Set-up Authentication](#3-set-up-authentication)
        - [Add Permissions to your app](#4-add-permissions-to-your-app)
        - [Create the Teams app packages](#5-create-the-teams-app-packages)
        - [Install the apps in Microsoft Teams](#6-install-the-apps-in-microsoft-teams)
    - [Troubleshooting](#troubleshooting)
- - -

# Prerequisites
>    * The recommendation is to use [Deployment guide using powershell](Deployment-guide-powershell).
>    * If you already have previous version of Company Communicator installed, then please use this [v5 migration guide](v5-migration-guide).

To begin, you will need: 
* An Azure subscription where you can create the following kinds of resources:  
    * App Service
    * App Service Plan
    * Bot Channels Registration
    * Azure Function
    * Azure Storage Account
    * Service Bus
    * Application Insights
    * Azure Key vault
* An role to assign roles in Azure RBAC. To check if you have permission to do this, 
    * Goto the subscription page in Azure portal. Then, goto Access Control(IAM) and click on `View my access` button.
    * Click on your `role` and in search permissions text box, search for `Microsoft.Authorization/roleAssignments/Write`.
    * If your current role does not have the permission, then you can grant yourself the built in role `User Access Administrator` or create a custom role.
    * Please follow this [link](https://docs.microsoft.com/en-us/azure/role-based-access-control/custom-roles#steps-to-create-a-custom-role) to create a custom role. Use this action `Microsoft.Authorization/roleAssignments/Write` in the custom role to assign roles in Azure RBAC.
* A team with the users who will be sending messages with this app. (You can add or remove team members later!)
* A copy of the Company Communicator app GitHub repo (https://github.com/OfficeDev/microsoft-teams-company-communicator-app)

> **NOTE:** If you plan to use a custom domain name instead of relying on Azure Front Door, read the instructions [here](Custom-domain-option) first.

- - -

# Deployment Steps

## 1. Register Azure AD application

Register three Azure AD application in your tenant's directory: one for author bot, one for user bot and another for graph app.

1. Log in to the Azure Portal for your subscription, and go to the [App registrations](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps) blade.

1. Click **New registration** to create an Azure AD application.
    - **Name**: Name of your Teams App - if you are following the template for a default deployment, we recommend "Company Communicator User".
    - **Supported account types**: Select "Accounts in any organizational directory" (*refer image below*).
    - Leave the "Redirect URI" field blank for now.

    ![Azure AD app registration page](images/multitenant_app_creation.png)

1. Click **Register** to complete the registration.

1. When the app is registered, you'll be taken to the app's "Overview" page. Copy the **Application (client) ID**; we will need it later. Verify that the "Supported account types" is set to **Multiple organizations**.

    ![Azure AD app overview page](images/multitenant_app_overview_1.png)

1. Go back to "App registrations", then repeat step no. 2 to create another Azure AD application for the author bot.
    - **Name**: Name of your Teams App - if you are following the template for a default deployment, we recommend "Company Communicator Author".
    - **Supported account types**: Select "Accounts in any organizational directory".
    - Leave the "Redirect URI" field blank for now.

1. Go back to "App registrations", then repeat step no. 2 to create another Azure AD application for the Microsoft Graph app.
    - **Name**: Name of your Teams App - if you are following the template for a default deployment, we recommend "Company Communicator App".
    - **Supported account types**: Select "Accounts in this organizational directory only(Default Directory only - Single tenant)".
    - Leave the "Redirect URI" field blank for now.


    At this point you should have the following 4 values:
    1. Application (client) ID for the user bot.
    2. Directory (tenant) ID.
    3. Application (client) ID for the author bot.
    4. Application (client) ID for the Microsoft Graph App.

    We recommend that you copy the values, we will need them later.

    ![Azure AD app overview page](images/multitenant_app_overview_2.png)

## 2. Deploy to your Azure subscription
1. Click on the **Deploy to Azure** button below.
   
   [![Deploy to Azure](images/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FOfficeDev%2Fmicrosoft-teams-company-communicator-app%2Fmain%2FDeployment%2Fazuredeploywithcert.json)

1. When prompted, log in to your Azure subscription.

1. Azure will create a "Custom deployment" based on the Company Communicator ARM template and ask you to fill in the template parameters.

    > **Note:** Please ensure that you don't use underscore (_) or space in any of the field values otherwise the deployment may fail.

1. Select a subscription and a resource group.
   * We recommend creating a new resource group.
   * The resource group location MUST be in a datacenter that supports all the following:
     * Storage Accounts
     * Application Insights
     * Azure Functions
     * Service Bus
     * App Service

    For an up-to-date list of datacenters that support the above, click [here](https://azure.microsoft.com/en-us/global-infrastructure/services/?products=storage,app-service,monitor,service-bus,functions)

1. Enter a **Base Resource Name**, which the template uses to generate names for the other resources.
   * The `[Base Resource Name]` must be available. For example, if you select `contosocommunicator` as the base name, the name `contosocommunicator` must be available (not taken); otherwise, the deployment will fail with a Conflict error.
   * Remember the base resource name that you selected. We will need it later.

1. Update the following fields in the template:
    1. **User Client ID**: The application (client) ID of the Microsoft Teams user bot app. (from Step 1)
    2. **User App Certificate Name**:  Provide the name for creating the new certificate of user bot Azure AD app in Azure Key vault
    3. **Tenant Id**: The tenant ID. (from Step 1)
    4. **Author Client ID**: The application (client) ID of the Microsoft Teams author bot app. (from Step 1)
    5. **Author App Certificate Name**:  Provide the name for creating the new certificate of author bot Azure AD app in Azure Key vault
    6. **Microsoft Graph App Client ID**: The application (client) ID of the Microsoft Graph Azure AD app. (from Step 1)
    7. **Microsoft Graph App Certificate Name**:  Provide the name for creating the new certificate of Microsoft Graph Azure AD app in Azure Key vault
    8. **Proactively Install User App [Optional]**: Default value is `true`. You may set it to `false` if you want to disable the feature.
    9. **User App ExternalId [Optional]**: Default value is `148a66bb-e83d-425a-927d-09f4299a9274`. This **MUST** be the same `id` that is in the Teams app manifest for the user app.
    10. **Hosting Plan SKU  [Optional]**: The pricing tier for the hosting plan. Default value is `Standard`. You may choose between Basic, Standard and Premium.
    11. **Hosting Plan Size  [Optional]**: The size of the hosting plan (small - 1, medium - 2, or large - 3). Default value is `2`.
    
        > **Note:** The default value is 2 to minimize the chances of an error during app deployment. After deployment you can choose to change the size of the hosting plan.
    12. **Service Bus Web App Role Name Guid [Optional]**: Default value is `958380b3-630d-4823-b933-f59d92cdcada`. This **MUST** be the same `id` per app deployment.
   
        > **Note:** Make sure to keep the same values for an upgrade. Please change the role name GUIDs in case of another Company Communicator Deployment in same subscription.

    13. **Service Bus Prep Func Role Name Guid [Optional]**: Default value is `ce6ca916-08e9-4639-bfbe-9d098baf42ca`. This **MUST** be the same `id` per app deployment.
    14. **Service Bus Send Func Role Name Guid [Optional]**: Default value is `960365a2-c7bf-4ff3-8887-efa86fe4a163`. This **MUST** be the same `id` per app deployment.
    15. **Service Bus Data Func Role Name Guid [Optional]**: Default value is `d42703bc-421d-4d98-bc4d-cd2bb16e5b0a`. This **MUST** be the same `id` per app deployment.
    16. **Storage Account Web App Role Name Guid [Optional]**: Default value is `edd0cc48-2cf7-490e-99e8-131311e42030`. This **MUST** be the same `id` per app deployment.
    17. **Storage Account Prep Func Role Name Guid [Optional]**: Default value is `9332a9e9-93f4-48d9-8121-d279f30a732e`. This **MUST** be the same `id` per app deployment.
    18. **Storage Account Data Func Role Name Guid [Optional]**: Default value is `5b67af51-4a98-47e1-9d22-745069f51a13`. This **MUST** be the same `id` per app deployment.
    19. **DefaultCulture [Optional]**: By default the application uses `en-US` locale. You can choose the locale from the list, if you wish to use the app in different locale.Also, you may add/update the resources for other locales and update this configuration if desired.
    20. **SupportedCultures [Optional]**: This is the list of locales that application supports currently.You may add/update the resources for other locales and update this configuration if desired.


    > **Note:** Make sure that the values are copied as-is, with no extra spaces. The template checks that GUIDs are exactly 36 characters.

    > **Note:** If your Azure subscription is in a different tenant than the tenant where you want to install the Teams App, please update the `Tenant Id` field with the tenant where you want to install the Teams App.

1. Update the "Sender UPN List", which is a semicolon-delimited list of users (Authors) who will be allowed to send messages using the Company Communicator.
    * For example, to allow Megan Bowen (meganb@contoso.com) and Adele Vance (adelev@contoso.com) to send messages, set this parameter to `meganb@contoso.com;adelev@contoso.com`.
    * You can change this list later by going to the App Service's "Configuration" blade.

1. If you wish to change the app name, description, and icon from the defaults, modify the corresponding template parameters.

1. Agree to the Azure terms and conditions by clicking on the check box "I agree to the terms and conditions stated above" located at the bottom of the page.

1. Click on "Purchase" to start the deployment.

1. Wait for the deployment to finish. You can check the progress of the deployment from the "Notifications" pane of the Azure Portal. It may take **up to an hour** for the deployment to finish.

    > If the deployment fails, see [this section](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Troubleshooting#1-code-deployment-failure) of the Troubleshooting guide.

1. Once the deployment is successfully completed, go to the deployment's "Outputs" tab, and note down the follwing values. We will need them later.
    * **keyVaultName:** This is the Key Vault Name for the Company Communicator app. For the following steps, it will be referred to as `%keyVaultName%`.
    * **authorBotId:** This is the Microsoft Application ID for the Company Communicator app. For the following steps, it will be referred to as `%authorBotId%`.
    * **userBotId:** This is the Microsoft Application ID for the Company Communicator app. For the following steps, it will be referred to as `%userBotId%`.
    * **appDomain:** This is the base domain for the Company Communicator app. For the following steps, it will be referred to as `%appDomain%`.

> **IMPORTANT:** If you plan to use a custom domain name instead of relying on Azure Front Door, read the instructions [here](Custom-domain-option) before continuing any further.

## 3. Create Key vault Certificate
1. On the Key vault page, select **Certificates**.
3. Click on **Generate/Import**.
3. On the **Create a certificate** screen choose the following values:
    - **Method of Certificate Creation**: Generate.
    - **Certificate Name**: AuthorAppCertificateName. This should be the same value given in step no. 2.
    - **Subject**: CN=`%appDomain%`.
    - Leave the other values to their defaults. (By default, if you don't specify anything special in Advanced policy, it'll be usable as a client auth certificate.)
4. Click **Create**.

Once that you receive the message that the certificate has been successfully created, you may click on it on the list. You can then see some of the properties. If you click on the current version, you can see the value you specified in the previous step.

![Certificate properties](images/create-certificate.png)

Please repeat the steps for User bot certificate and Microsoft Graph app certificate.

## 4. Export Certificate from Key Vault

Download the certificate for all apps i.e. Author Bot, User Bot, Microsoft Graph app.

You can download by Clicking "Download in CER format" button.
![Export Certificate](images/export-cert.png)

## 5. Upload Certificate Azure AD App

1. Go to **App Registrations** page [here](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps) and open the Microsoft Graph Azure AD app you created (in Step 1) from the application list.

2. On the side rail in the Manage section, navigate to the "Certificates & secrets" section. In the Certificates section, click on "Upload certificate". Select the certificate file downloaded in Step 4 for the Graph Azure AD app and click on Add.

3. Please repeat the same for Author app and User app and upload the respective certificate downloaded in Step 3.

## 6. Import Certificates from Key Vault to app and functions.

1. Go to **App Service** page created as part of this deployment.
1. From the left navigation of your app, **select TLS/SSL settings > Private Key Certificates (.pfx) > Import Key Vault Certificate**
![Import Key Vault Certificate](images/import-key-vault-cert.png)
1. Use the following table to help you select the certificate.

    | Setting | Description |
    |-|-|
    | Subscription | The subscription that the Key Vault belongs to. |
    | Key Vault | The vault with the certificate you want to import. |
    | Certificate | Select from the list of PKCS12 certificates in the vault. All PKCS12 certificates in the vault are listed with their thumbprints, but not all are supported in App Service. |

1. When the operation completes, you see the certificate in the **Private Key Certificates** list.
![Import Key Vault certificate finished](images/import-app-service-cert-finished.png)

1. Please import all the certificates from the Key Vault.
1. Repeat the above steps for the function apps.

> NOTE : If you update your certificate in Key Vault with a new certificate, App Service automatically syncs your certificate within 24 hours.

## 7. Set-up Authentication

1. Note that you have the `%authorBotId%`, `%userBotId%` and `%appDomain%` values from the previous step (Step 2).

    > If do not have these values, refer [this section](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Troubleshooting#2-forgetting-the-botId-or-appDomain) of the Troubleshooting guide for steps to get these values.

1. Go to **App Registrations** page [here](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps) and open the Microsoft Graph Azure AD app you created (in Step 1) from the application list.

    > NOTE: This step is to set-up authentication for Microsoft Graph Azure AD app.

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

    2. Click **Save** to commit your changes.

## 8. Add Permissions to your Microsoft Graph Azure AD app

Continuing from the Microsoft Graph Azure AD app registration page where we ended Step 3.

1. Select **API Permissions** blade from the left hand side.

2. Click on **Add a permission** button to add permission to your app.

3. In Microsoft APIs under Select an API label, select the particular service and give the following permissions,

    * Under **Commonly used Microsoft APIs**, 

    * Select “Microsoft Graph”, then select **Delegated permissions** and check the following permissions,
        1. **GroupMember.Read.All**
        2. **AppCatalog.Read.All**

    * then select **Application permissions** and check the following permissions,
        1. **GroupMember.Read.All**
        2. **User.Read.All**
        3. **TeamsAppInstallation.ReadWriteForUser.All**

    * Click on **Add Permissions** to commit your changes.

    ![Azure AD API permissions](images/multitenant_app_permissions_1.png)
    ![Azure AD API permissions](images/multitenant_app_permissions_2.png)

    > Please refer to [Solution overview](https://github.com/OfficeDev/microsoft-teams-company-communicator-app/wiki/Solution-overview#microsoft-graph-api) for more details about the above permissions.

4. If you are logged in as the Global Administrator, click on the “Grant admin consent for %tenant-name%” button to grant admin consent, else inform your Admin to do the same through the portal.
   <br/>
   Alternatively you may follow the steps below:
   - Prepare link - https://login.microsoftonline.com/common/adminconsent?client_id=%appId%. Replace the `%appId%` with the `Application (client) ID` of Microsoft Graph Azure AD app (from above).
   - Global Administrator can grant consent using the link above.

## 9. Create the Teams app packages

Company communicator app comes with 2 applications – Author, User. The Author application is intended for employees who create and send messages in the organization, and the User application is intended for employees who receive the messages.

Create two Teams app packages: one to be installed to an Authors team and other for recipients to install personally and/or to teams.

1. Make sure you have cloned the app repository locally.

1. Open the `Manifest\manifest_authors.json` file in a text editor.

1. Change the placeholder fields in the manifest to values appropriate for your organization.
    * `developer.name` ([What's this?](https://docs.microsoft.com/en-us/microsoftteams/platform/resources/schema/manifest-schema#developer))
    * `developer.websiteUrl`
    * `developer.privacyUrl`
    * `developer.termsOfUseUrl`

1. Change the `<<appDomain>>` placholder in the configurationUrl setting to be the `%appDomain%` value e.g. "`https://appName.azurefd.net/configtab`".

1. Change the `<<botId>>` placeholder in the botId setting to be the `%authorBotId%` value - this is your author Azure AD application's ID from above. This is the same GUID that you entered in the template under "Author Client ID". Please note that there are two places in the manifest (for authors) where you will need to update Bot ID.

1. Change the `<<appDomain>>` placeholder in the validDomains setting to be the `%appDomain%` value e.g. "`appName.azurefd.net`".

1. Change the `<<botId>>` placeholder in the id setting of the webApplicationInfo section to be the `%authorBotId%` value. Change the `<<appDomain>>` placeholder in the resource setting of the webApplicationInfo section to be the `%appDomain%` value e.g. "`api://appName.azurefd.net`".

1. Copy the `manifest_authors.json` file to a file named `manifest.json`.

1. Create a ZIP package with the `manifest.json`,`color.png`, and `outline.png`. The two image files are the icons for your app in Teams.
    * Name this package `company-communicator-authors.zip`, so you know that this is the app for the author teams.
    * Make sure that the 3 files are the _top level_ of the ZIP package, with no nested folders.  
    ![image10](images/file-explorer.png)

1. Delete the `manifest.json` file.

Repeat the steps above but with the file `Manifest\manifest_users.json` and use `%userBotId%` for `<<botId>>` placeholder. Note: you will not need to change anything for the configurationUrl or webApplicationInfo section because the recipients app does not have the configurable tab. Name the resulting package `company-communicator-users.zip`, so you know that this is the app for the recipients.

## 10. Install the apps in Microsoft Teams

1. Install the authors app (the `company-communicator-authors.zip` package) to your team of message authors.
    * Note that even if non-authors install the app, the UPN list in the app configuration will prevent them from accessing the message authoring experience. Only the users in the sender UPN list will be able to compose and send messages. 
    * If your tenant has sideloading apps enabled, you can install your app by following the instructions [here](https://docs.microsoft.com/en-us/microsoftteams/platform/concepts/apps/apps-upload#load-your-package-into-teams).

2. Add the configurable tab to the team of authors, so that they can compose and send messages.

3. [Upload](https://docs.microsoft.com/en-us/microsoftteams/tenant-apps-catalog-teams) the User app to your tenant's app catalog so that it is available for everyone in your tenant to install.
> **IMPORTANT:** Proactive app installation will work only if you upload the User app to your tenant's app catalog.

4. Install the User app (the `company-communicator-users.zip` package) to the users and teams that will be the target audience.
> If `proactiveAppInstallation` is enabled, you may skip this step. The service will install the app for all the recipients when authors send a message.

> **NOTE:** If you are deploying a version of Company Communicator prior to version 4, do NOT use app permission policies to restrict the authors app to the members of the authors team. Microsoft Teams does not support applying different policies to the same bot via two different app packages. 

---

# Troubleshooting
Please check the [Troubleshooting](Troubleshooting) guide.
