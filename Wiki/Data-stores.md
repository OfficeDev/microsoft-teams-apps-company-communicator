The app uses the following data stores:
1. Azure Storage Account
1. Application Insights

All these resources are created in your Azure subscription. None are hosted directly by Microsoft.

## Azure Table Storage

### Teams Data

The Teams Collection stores the teams who have installed Company Communicator app.

| Value         | Description
| ---           | ---
| PartitionKey  | Constant value as 'Team Data'.
| RowKey        | The team Id in teams.
| Timestamp     | The latest DateTime record.
| Name          | The team's name.
| ServiceUrl    | The service URL that can be used to fetch the team's roster.
| TeamId        | The team Id in teams.
| TenantId      | The teams's tenant identifier.

### Users Collection

The Users Collection stores the users who have installed Company Communicator app.

| Value         | Description
| ---           | ---
| PartitionKey  | Constant value as 'User Data' or 'Author Data'.
| RowKey        | The user's azure active directory object identifier.
| Timestamp     | The latest DateTime record.
| AadId         | The user's azure active directory object identifier.
| ConversationId| The conversation identifier for the chat between the user and the bot.
| ServiceUrl    | The user's service URL that can be used to notify the user.
| TenantId      | The user's tenant identifier.
| UserId        | The user's Id in teams.

### AppConfig Collection

The App Config Collection stores the user app Id and service url.

| Value         | Description
| ---           | ---
| PartitionKey  | Constant value as 'Settings'.
| RowKey        | Constants as "ServiceUrl" or "UserAppId". "ServiceUrl" - The value stored is service url. "UserAppId" - The value stored is user app Id.
| Timestamp     | The latest DateTime record.
| Value         | The user's app Id or the service url.

### Notification Data

The Notification Collection stores the notification data.

| Value             | Description
| ---               | ---
| PartitionKey      | Constants as "DraftNotifications" or "SendingNotifications" or "SentNotifications" or "GlobalSendingNotificationData". "DraftNotifications" - The notification is stored in this partition when it is in draft state. "SendingNotifications" - This partition stores the notifcation entry that is used for sending the notification and serialized JSON content. "SentNotifications" - The notification is moved to this partition when it is sent to the recipient.  "GlobalSendingNotificationData" - This partition stores the Retry Delay time when the system is being throttled.
| RowKey            | The notification unique identifier.
| Timestamp         | The latest DateTime record.
| Id                | The notification identifier.
| Title             | The title text of notification's content.
| ImageLink         | The image link of notification's content.
| Summary           | The summary text of notification's content.
| Author            | The author text of the notification's content.
| ButtonLink        | The button link of the notification's content.
| ButtonTitle       | The button title of the notification's content.
| TotalMessageCount | The total number of messages to send.
| Succeeded         | The number of recipients who have received the notification succesfully.
| Failed            | The number of recipients who failed to receive the notification succesfully.
| AllUsers          | Indicating if the notification should be sent to all known users.
| TeamsInString     | The list of team identifiers.
| RostersInString   | The list of roster identifiers.
| GroupsInString    | The list of group identifiers.
| IsCompleted       | [Deprecated] Indicating if the notification sending process is completed.
| IsDraft           | Indicating if the notifcation is a draft.
| IsPreparingToSend | [Deprecated] Indicating if the notification is in the "preparing to send" state.
| Unknown           | The number of recipients who have an unknown status.
| Content           | The content of the notification in serialized JSON form.
| NotificationId    | The notification identifier.
| RecipientNotFound | The number of not found recipients.
| CreatedBy         | The user that created the notification.
| CreatedDate       | The DateTime when notification was created.
| SendingStartDate  | The DateTime when the notification sending was started.
| SentDate          | The DateTime when the notification's sending was completed.
| WarningMessage    | The warning message for the notification if there was a warning given when preparing and sending notification.
| ErrorMessage      | The error message for the notification if there was a failure in preparing and sending notification.
| Status            | The notification status.

### SentNotification Data

The SentNotification Collection stores the sent notification data.

| Value             | Description
| ---               | ---
| PartitionKey      | The notification unique identifier.
| RowKey            | The user's identifier.
| Timestamp         | The latest DateTime record.
| AllSendStatusCodes| The comma separated list representing all of the status code responses received when trying to send the notification to the recipient.
| ConversationId    | The conversation identifier for the recipient.
| DeliveryStatus    | The delivery status for the notification to the recipient.
| IsStatusCodeFromCreateConversation| Indicating if the status code is from the create conversation call.
| NumberOfFunctionAttemptsToSend    | The number of times an Azure Function instance attempted to send the notification to the recipient.
| RecipientId       | The recipients unique identifier.
| RecipientType     | Indicating which type of recipient the notification was sent.
| SentDate          | The DateTime when the notification's sending was completed.
| ServiceUrl        | The service URL of the recipient.
| StatusCode        | The status code for the notification received by the bot.
| TenantId          | The tenant identifier of the recipient.
| TotalNumberOfSendThrottles        | The total number of throttle responses the bot received when trying to send the notification to the recipient.
| UserId            | The user identifier of the recipient.

### SendBatches Data [Deprecated] 

The SendBatches Collection stoes the notification batches data.

| Value             | Description
| ---               | ---
| PartitionKey      | Notification Batch unique identifier.
| RowKey            | User Unique identifier.
| Timestamp         | The latest DateTime record.
| ConversationId    | The conversation identifier for the recipient.
| IsStatusCodeFromCreateConversation| Indicating if the status code is from the create conversation call.
| NumberOfFunctionAttemptsToSend    | The number of times an Azure Function instance attempted to send the notification to the recipient.
| RecipientId       | The recipient's user identifier.
| RecipientType     | The recipient type.
| ServiceUrl        | The service URL of the recipient.
| StatusCode        | The status code for the notification received by the bot.
| TenantId          | The tenant identifier of the recipient.
| TotalNumberOfSendThrottles        | The total number of throttle responses the bot received when trying to send the notification to the recipient
| UserId            | The user identifier of the recipient.

### Export Data

The Export Collection stores the export data.

| Value             | Description
| ---               | ---
| PartitionKey      | The user's azure active directory identifier.
| RowKey            | The notification identifier.
| Timestamp         | The latest DateTime record.
| FileName          | The file name for the export data.
| FileConsentId     | The response identifier of file consent card.
| SendDate          | The export send date.
| Status            | The file export status.

## Application Insights

See [Telemetry](Telemetry)
