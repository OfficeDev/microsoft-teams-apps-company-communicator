## Message throttling

Unless your bot is whitelisted for higher limits, the bot's attempt at sending a message will get throttled at 8,000 messages every 30 minutes (rolling window). If you exceed this limit, you will see messages in the throttled state. 

## Retries

If your message is throttled, the app will try to send the messages again. Therefore, enabling the eventual delivery of the messages. For example, if the app is sending 15K messages, it should take a maximum of an hour or so to deliver.

## App installation is required

The app can only send messages to users and teams who have the app installed. To mitigate this limitation and reach a wider audience, install the app in personal scope for all the intended recipients in the tenant, as well as in the teams to allow a targeted message to be delivered. This is to be done outside the app manually. 

## Author/publishing experience not supported on Mobile

The tab where authors/creators of messages create a message is currently not supported on mobile. The recommended approach is to create the messages on desktop only.