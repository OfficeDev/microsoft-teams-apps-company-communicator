## Release Notes

Cumulative improvements in Company Communicator App.

### Version history

|Version |Release Date |
|---|---|
| 5.4.1 | June 14, 2023
| 5.4 | May 10, 2023
| 5.3 | Dec 14, 2022
| 5.2 | Jul 26, 2022
| 5.1 | Apr 28, 2022
| 5.0 | Nov 10, 2021
| 4.1.5 | Sep 29, 2021
| 4.1.4 | Sep 14, 2021
| 4.1.3 | Jul 2, 2021
| 4.1.2 | Jun 25, 2021
| 4.1.1 | Jun 12, 2021
| 4.1 | Mar 19, 2021
| 4.0 | Dec 30, 2020
| 3.0 | Oct 29, 2020
| 2.1 | Oct 16, 2020
| 2.0 | Aug 20, 2020
| 1.1 | Jun 08, 2020
| 1.0 | Dec 20, 2019

### Company Communicator feature release notes
#### 5.4.1 (June 14, 2023)
##### Changes introduced
- Enhanced user interface.
- Upgraded packages (fluentui, typescript, react-redux/scripts) for better UI and security alert fixes.
- Compliance with Microsoft Accessibility guidelines in Web, Windows and Mac OS.

#### 5.4 (May 10, 2023)
##### Changes introduced
- Code changes to support Government Community Cloud High (GCCH) environment.

#### 5.3 (Dec 14, 2022)
##### Changes introduced
- .NET upgrade from .NET Core 3.1 to .NET 6.0
- Updated Azure App Service app to use .NET 6.0.
- Updated Azure Function apps to use runtime version 4.x.
- Capture the sender info in the Author app.
- Bug fix for the issue with clean up function. 

#### 5.2 (Jul 26, 2022)
##### Changes introduced
- Ability to upload image.
- Ability to format the text.
- New header banner with logo, support and feedback option.
- Fix for PS deployment failure

#### 5.1 (Apr 28, 2022)
##### Changes introduced
- Ability to cancel a notification.
- Export installation errors.
- Arm fixes.

#### 5.0 (Nov 10, 2021)
##### Changes introduced
- Added Key Vault and Managed Identity.
- Support certificate authentication.
- Bug fix to resolve expired delta url. 

#### 4.1.5 (Sep 29, 2021)
##### Changes introduced
- Limit the size of the error and warning messages stored to 1024 characters.

#### 4.1.4 (Sep 14, 2021)
##### Changes introduced
- Support large number of users.
- Reduce memory usage.

#### 4.1.3 (Jul 2, 2021)
##### Changes introduced
- Export report for users who have left tenant.

#### 4.1.2 (Jun 25, 2021)
##### Changes introduced
- Exclude existing guest users with user app installed from receiving message.
- Identify UserType using export report functionality.
- Bug fix preventing proactive installations.

#### 4.1.1 (Jun 12, 2021)
##### Changes introduced
- Exclude guest users when sending message to:
  - Members of one or more Teams.
  - Members of one or more Groups.
- Bug fix with the author app interface in dark and high-contrast themes.
- Resolved potential out of memory errors when sending message to large audience.

#### 4.1 (Mar 19, 2021)
##### Changes introduced
- Locale support for multiple languages.
- Migration to fluent ui northstar.
- Migrating graph beta apis to v1.0.
- Improved Test coverage.

#### 4.0 (Dec 30, 2020)
##### Changes intoduced
- Separate Bots for User and Author.
- Automated deployment using Powershell script.
- Improved Test coverage.

#### 3.0 (Oct 29, 2020)
###### Changes introduced
- Proactive User app installation.
- Send message to all the users in a tenant.
- Multi-Locale support in backend and client application.
- Granular status updates after sending a message.
- Performance improvements.
- Quality and reliability fixes.

#### 2.1 (Oct 16, 2020)
###### Changes introduced
- Bug fix.
- Performance improvements.

#### 2.0 (Aug 20, 2020)
###### Changes introduced
- Send a message to an M365 group, SG or DG.
- Search an M365 group, SG or DG.
- Export data for messages sent.
- Updated to MSBuild v16.

#### 1.1 (Jun 08, 2020)
###### Changes introduced
- Upgraded NPM Packages.

#### 1.0 (Dec 20, 2019)
###### Changes introduced
- Company Communicator template released.
- Ability to draft/send messages.
- Ability to send a message to:
  - Members of one or more Teams.
  - General channel of one or more Teams.
  - All the users who install the User app.