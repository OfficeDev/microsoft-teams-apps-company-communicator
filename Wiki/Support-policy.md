## Help and Improvements
### For Company Communicator users

Hello users, Welcome to the **Company Communicator** support page. Please read our support policies below,

#### Support Policy: Official Versions Only
We are committed to giving our users excellent help while keeping our application secure and dependable.

#### Our Pledge to You:
- **Reliable Assistance:** We are committed to promptly addressing any issues or inquiries you might encounter with our official versions.
- **Transparent Communication:** We will keep you informed of any updates, fixes, and suggestions to enhance your experience.

#### Our Support Scope:
To ensure the highest quality of service, we focus our support efforts on official versions of our application. Here is why:
- **Challenges with Custom Versions:** We respect the originality and ingenuity of custom versions, but our focus is on the official releases of *Company Communicator*. Therefore, we might not be able to offer extensive support for custom builds.

#### How You Can Help Us Help You:
For the smoothest support experience, we kindly ask for your cooperation:
1. **Stick to Official Versions:** Whenever possible, utilize the official releases of *Company Communicator* for our full support capabilities.
2. **Give Specific Details:** When asking for help, please provide relevant information such as the version number and a detailed explanation of the problem.

For assistance or inquiries, you can reach out to our support team by opening a new issue on *Company Communicator* [GitHub Issue page](https://github.com/OfficeDev/microsoft-teams-apps-company-communicator/issues).

**Please Switch to [our latest version](https://github.com/OfficeDev/microsoft-teams-apps-company-communicator/wiki/v5-migration-guide) to get the most recent updates from Company Communicator.**

## For owners of personalized Company Communicator editions

If you are using modified versions of Company Communicator, you may need to adjust your code base to stay current with the latest updates from the official repo. Here are the principal areas that have been impacted by the recent updates:

- **Authorization problem:** Version 5.5 has been launched by the official repo to address the authorization issue that happened when the redirect removed the Authorization header. This version offers a temporary solution. 
We have released a permanent fix to the authorization issue in v5.5.2 which involves change in Azure front door routing rules. Please find the below link for detailed steps to mitigate the authorization issue: [Authorization issue mitigation steps](https://github.com/OfficeDev/microsoft-teams-apps-company-communicator/wiki/Authorization-issue-fix).
- **Application Insights migration:** The official repo has moved to workspace-based Application Insights, as the classic Application Insights in Azure Monitor will be retired on 29 February 2024. This version creates a new Log Analytics workspace as part of version 5.5.1. For details, please refer to the [We’re retiring Classic Application Insights on 29 February 2024](https://azure.microsoft.com/en-us/updates/we-re-retiring-classic-application-insights-on-29-february-2024/).