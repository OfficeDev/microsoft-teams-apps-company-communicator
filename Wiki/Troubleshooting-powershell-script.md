# **General template issues**

**Generic possible issues**

Certain issues can arise that are common to many of the app templates. Please check [here](https://github.com/OfficeDev/microsoft-teams-stickers-app/wiki/Troubleshooting)  for reference to these.

**Problems related to PowerShell script**

**1. File is not digitally signed**

While running PowerShell script, sometimes user gets an error showing &#39;File is not digitally signed&#39;.

**Fix** : If this type of error occurs then run this: &quot;Set-ExecutionPolicy -ExecutionPolicy unrestricted&quot;

**2. Azure subscription access failed**

Connect-AzAccount : The provided account \*\*.onmicrosoft.com does not have access to subscription ID &quot;XXXX-&quot;. Please try logging in with different credentials or a different subscription ID.

**Fix** : User must be added as a contributor on the Azure subscription.&quot;

**3. Failed to acquire a token**

Exception calling &quot;AcquireAccessToken&quot; with &quot;1&quot; argument(s): &quot;multiple\_matching\_tokens\_detected: The cache contains multiple tokens satisfying the requirements

**Fix** : This means user is logged-in with multiple accounts in the current powershell session. Close the shell window and open a new one.&quot;

**4. Azure AD app permission consent error**

**Description**

The apps created by this app template requires an admin consent for &quot;User.Read&quot; graph permission so it can operate correctly.

![Powershell deployment guide](images/consent_error.png)

**Fix**

Please ask your tenant administrator to consent the &quot;User.Read&quot; permission for AD app.

![Powershell deployment guide](images/graph_access.png)

**5. Error while deploying the ARM Template**

**Description**

This happens when the resources are already created or due to some conflicts.

Errors: The resource operation completed with terminal provisioning state &#39;Failed&#39;

**Fix**

In case of such a scenario, the user needs to navigate to the deployment center section of failed/conflict resources through the Azure portal and check the error logs to get the actual errors and fix them accordingly.

Redeploy it after fixing the issue/conflict.