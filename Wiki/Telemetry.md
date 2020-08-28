The Company Communicator app logs telemetry to [Azure Application Insights](https://azure.microsoft.com/en-us/services/monitor/). You can go to the Application Insights blade of the Azure App Service to view basic telemetry about your services, such as requests, failures, and dependency errors.

The Teams Bot integrates with Application Insights to gather bot activity analytics, as described [here](https://blog.botframework.com/2019/03/21/bot-analytics-behind-the-scenes/).

The app logs a few kinds of events:

`Trace` logs keeps the track of application events.

`Exceptions` logs keeps the records of exceptions tracked in the application.
