## Assumptions

The estimate below assumes:
* 1000 users in the tenant
* 1 message sent to all users each week (~5/month)
* Administrator opts to create a custom domain name and obtain an SSL certificate for the site. 
    * When purchased through Azure, this is *typically* ~$12 for a domain name, and $75/year for the SSL certificate.
    * If you choose to use Azure Front Door, that adds a monthly cost of $46 (for 2 routing rules + minimal bandwidth consumption).

> The template defaults to using Azure Front Door, to reduce the cost of trying out and experimenting with the template, rather than requiring you to get a custom domain outright. For instance, you can run your instance for a few days, then turn off the services, and only pay for your actual Azure usage when the services were on.

We ignore:
* Operations associated with app installations, as that happens only once per user/team
* Operations associated with the authors viewing the tab, given the assumption that they are sending only 5 messages/month.
* Executions associated with Prep or Send function, as the execution count is trivial to the calculations.

## SKU recommendations

The recommended SKUs for a production environment are:
* App Service: Standard (S2)
* Service Bus: Basic

## Estimated load

**Number of messages sent**: 1000 users * 5 messages/month = 5000 messages

**Data storage**: 1 GB max    
* Messages are on the order of KBs

**Table data operations**:

* Prep function
    * (3 read * 5000 messages) = 15000 reads

* Send function
    * (4 read * 5000 messages) = 20000 reads
    * (1 write * 5000 messages) = 5000 writes

* Data function
    * Aggregating status: (1 read * 5000 messages) = 5000 reads

**Service bus operations**:
* ((1 write + 1 read) operations/sent message * 5000 messages) = 10000 operations

**Azure Function**:
> For Gb-sec pricing calculation, please refer this formula.
(Memory Size * Execution Time * Execution Count)/1024000.
Min. memory size = 128 Mb. 
Min. execution time = 100 ms.

* Send function
    * (1 invocation * 5000 messages) = 5000 invocations
    * (128 Mb * 3000 * 5000 messages)/1024000 = 1875 Gb-sec

## Estimated cost

**IMPORTANT:** This is only an estimate, based on the above usage assumptions. Your actual costs may vary (might be less) depending on the usage.

Prices were taken from the [Azure Pricing Overview](https://azure.microsoft.com/en-us/pricing/) on June 14 2023, for the West US 2 region.

Use the [Azure Pricing Calculator](https://azure.com/e/c3bb51eeb3284a399ac2e9034883fcfa) to model different service tiers and usage patterns.

Resource                                    | Tier          | Load              | Monthly price
---                                         | ---           | ---               | --- 
Storage account (Table)                     | Standard_LRS  | < 1GB data, 45000 operations | $0.045 + $0.01 = $0.05
Storage account (Blob)                      | Standard_LRS  | < 1GB data        | $0.29 + $0.02 + $0.02 = $0.34
Bot Channels Registration                   | F0            | N/A               | Free
App Service Plan                            | S2            | 730 hours         | $146.00
App Service (Bot + Tab)                     | -             |                   | (charged to App Service Plan) 
Azure Function                              | Dedicated     | 10000 executions   | (free up to 1 million executions)
Service Bus                                 | Basic         | 10000 operations  | $0.01
Azure Front Door                            | Azure Front Door Classic          | < 1GB data transfer, 2 routing rules        | $0.50 + $0.01 + $43.65 = $44.16
Application Insights                        | -             | < 5GB data        | $0.02
**Total**                                   |               |                   | **$190.58**


## Estimated load - 1M messages

**Data storage**: 3 GB max    
* Messages are on the order of KBs

**Number of messages sent**: 1M messages

**Table data operations**:

* Prep function
    * (3 read/prep * 1M messages) = 3M reads

* Send function
    * (4 read * 1M messages) = 4M reads
    * (1 write * 1M messages) = 1M writes

* Data function
    * Aggregating status: (1 read * 1M messages) = 1M reads

**Service bus operations**:
* ((1 write + 1 read) operations/sent message * 1M messages) = 2M operations

**Azure Function**:
> For Gb-sec pricing calculation, please refer this formula.
(Memory Size * Execution Time * Execution Count)/1024000.
Min. memory size = 128 Mb. 
Min. execution time = 100 ms.

* Send function
    * (1 invocation * 1M messages) = 1M invocations
    * (128 Mb * 3000 * 1M messages)/1024000 = 375000 Gb-sec

## Estimated cost - 1M messages

**IMPORTANT:** This is only an estimate, based on the above usage assumptions. Your actual costs may vary.

Prices were taken from the [Azure Pricing Overview](https://azure.microsoft.com/en-us/pricing/) on June 14 2023, for the West US 2 region.

Use the [Azure Pricing Calculator](https://azure.com/e/c3bb51eeb3284a399ac2e9034883fcfa) to model different service tiers and usage patterns.

Resource                                    | Tier          | Load              | Monthly price
---                                         | ---           | ---               | --- 
Storage account (Table)                     | Standard_LRS  | < 3GB data, 9M operations | $0.14 + $0.32 = $0.46
Storage account (Blob)                      | Standard_LRS  | < 1GB data        | $0.29 + $0.02 + $0.02 = $0.34
Bot Channels Registration                   | F0            | N/A               | Free
App Service Plan                            | S2            | 730 hours         | $146.00
App Service (Bot + Tab)                     | -             |                   | (charged to App Service Plan) 
Azure Function                              | Dedicated     | 1M executions     | (free up to 1 million executions)
Service Bus                                 | Basic         | 2M executions     | $0.10
Azure Front Door                            | Azure Front Door Classic          | < 3GB data transfer, 2 routing rules        | $0.50 + $0.03 + $43.65 = $44.18
Application Insights                        | -             | < 5GB data        | $0.02
**Total**                                   |               |                   | **$191.10**

## Estimated load - 2M messages

**Number of messages sent**: 2M messages

**Data storage**: 6 GB max    
* Messages are on the order of KBs

**Table data operations**:

* Prep function
    * (3 read * 2M messages) = 6M reads

* Send function
    * (4 read * 2M messages) = 8M reads
    * (1 write * 2M messages) = 2M writes

* Data function
    * Aggregating status: (1 read * 2M messages) = 2M reads

**Service bus operations**:
* ((1 write + 1 read) operations/sent message * 2M messages) = 4M operations

**Azure Function**:
> For Gb-sec pricing calculation, please refer this formula.
(Memory Size * Execution Time * Execution Count)/1024000.
Min. memory size = 128 Mb. 
Min. execution time = 100 ms.

* Send function
    * (1 invocation * 2M messages) = 2M invocations
    * ( 128 Mb * 3000 * 2M messages)/1024000 = 750000 Gb-sec

## Estimated cost - 2M messages

**IMPORTANT:** This is only an estimate, based on the above usage assumptions. Your actual costs may vary.

Prices were taken from the [Azure Pricing Overview](https://azure.microsoft.com/en-us/pricing/) on June 14 2023, for the West US 2 region.

Use the [Azure Pricing Calculator](https://azure.com/e/c3bb51eeb3284a399ac2e9034883fcfa) to model different service tiers and usage patterns.

Resource                                    | Tier          | Load              | Monthly price
---                                         | ---           | ---               | --- 
Storage account (Table)                     | Standard_LRS  |  < 6GB data, 18M operations | $0.27 + $0.65 = $0.92
Storage account (Blob)                      | Standard_LRS  | < 1GB data        | $0.29 + $0.02 + $0.02 = $0.34
Bot Channels Registration                   | F0            | N/A               | Free
App Service Plan                            | S2            | 730 hours         | $146.00
App Service (Bot + Tab)                     | -             |                   | (charged to App Service Plan) 
Azure Function                              | Dedicated     | 2M executions     | $5.80
Service Bus                                 | Basic         | 2M executions     | $0.10
Azure Front Door                            | Azure Front Door Classic          | < 3GB data transfer, 2 routing rules        | $0.50 + $0.03 + $43.65 = $44.18
Application Insights                        | -             | < 5GB data        | $0.02
**Total**                                   |               |                   | **$197.36**