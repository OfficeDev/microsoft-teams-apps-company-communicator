As an alternative to using Azure Front Door, you can register a custom domain for your app.

## Fresh deployment

Follow the steps in the [Deployment guide](Deployment-guide), modified as follows:
1. Follow **Step 1: Register Azure AD application** unchanged.
2. When filling in the template parameters in **Step 2: Deploy to your Azure subscription**, set
    * **Custom domain option** = "Custom domain name (recommended)"
3. Pause after Step 2.
3. Assign a custom domain name to the Azure App Service that was created by the template. You have several options:
    * You can purchase a domain name directly through Azure: https://docs.microsoft.com/en-us/azure/app-service/manage-custom-dns-buy-domain
    * If your organization can create its own domain names, create one through your system and map it to the app service: https://docs.microsoft.com/en-us/azure/app-service/app-service-web-tutorial-custom-domain
4. Bind an SSL certificate to your Azure App Service: https://docs.microsoft.com/en-us/azure/app-service/app-service-web-tutorial-custom-ssl
5. Test the custom domain **before** proceeding. Ensure that you can access `https://<your_custom_domain>/health` without any errors. (This is a blank page that returns HTTP 200 OK.)
6. Go to the Azure App Service page in the Azure Portal, click on "Configuration", then set
    * **AzureAd:ApplicationIdURI** = `api://<your_domain_name>` (for example, `api://companycommunicator.contoso.com`)
6. Continue with the rest of the deployment guide, substituting your domain name for `%appDomain%`.
    * For example, if your custom domain is `companycommunicator.contoso.com`, you would set the Redirect URI of your Azure AD application to `https://companycommunicator.contoso.com/signin-simple-end`, and its Application URI would be `api://companycommunicator.contoso.com`.