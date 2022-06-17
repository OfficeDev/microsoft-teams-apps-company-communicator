# Scale
If you need to scale the app for large number of users(greater than 200K), please follow the below steps.

## 1. Create new Storage Account for Durable function.

1. Find and goto the resource group which is used for the Company Communicator app deployment.
1. Click on **+ Create**.
1. Then, search for **Storage Account** and click on **Create**.
1. Now, fill in the fields such as **Storage account name** and click on **Review + Create**.
1. Once the resource is created, then goto Storage Account. 
1. Under **Security + networking**, click on access keys.
1. Then, click on **Show keys** and copy the key1 connection string.
1. Now, goto the Company Communicator prep function and click on **Configuration**.
1. Update the value of **Azure.WebJobsStorage** with the connection string from step 7.
1. Click on **Save** to commit changes.

    > Please make sure to keep the below value less than 50rps, as going over this rate can get the bot blocked.
     ```
        "serviceBus": {
        "messageHandlerOptions": {
            "maxConcurrentCalls": 30
        }
        }
    ```


