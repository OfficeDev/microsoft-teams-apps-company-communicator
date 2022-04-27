# Known Limitations
## 1. Author/publishing experience is not supported on Mobile

The tab where authors/creators of messages create a message is not supported on mobile. The recommended approach is to create the messages on the desktop only.

# FAQs

## 1. Are messages sent to guest users?
As of version 4.1.1, guest users are excluded from receiving messages. Note that they will still be able to view messages posted to a channel.

> **IMPORTANT:** If you are using a version of Company Communicator **v4.1.1**, please update to the latest version, and see the guidance in [Excluding guest users from messages](https://github.com/OfficeDev/microsoft-teams-apps-company-communicator/wiki/Excluding-guest-users-from-messages).

## 2. Does Company Communicator respond with a message to users who ask a question or reply to a message?
No, by default the bot only sends messages and does not respond with a message. The bot can be customized to reply with a custom message or connected to a knowledge base to respond with answers from the knowledge base.

## 3. Is it mandatory to choose multi-tenant account types while app registration?
Yes. Bot Channels Registration only supports multi-tenant account types. Please choose multi-tenant type options only even if the app users belong to single-tenant only. Please refer [here](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-quickstart-registration?view=azure-bot-service-4.0#manual-app-registration) for more information

| Type | Description |
|--|--|
| Accounts in any organizational directory (Any Azure AD - Multitenant) | This option provides less exposure by restricting access and in case OAuth is not supported. |
| Accounts in any organizational directory (Any Azure AD - Multitenant) and personal Microsoft accounts (for example, Xbox, Outlook.com) | This option is well-suited to support OAuth and bot authentication. |

## 4. How to clone the GitHub repository?
Please follow this [link](https://docs.github.com/en/github/creating-cloning-and-archiving-repositories/cloning-a-repository) for detailed instructions on cloning GitHub repository to create a local copy on your computer and sync between the two locations.
