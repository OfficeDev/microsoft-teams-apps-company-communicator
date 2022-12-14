function UpdateAzureFunctions {
    param(
        [Parameter(Mandatory = $true)] [string] $subscriptionId,
        [Parameter(Mandatory = $true)] [string] $resourceGroupName,
        [Parameter(Mandatory = $true)] [string] $baseResourceName
    )
        
    try{
        $prepFunctionName = $baseResourceName + "-prep-function"
        $sendFunctionName = $baseResourceName + "-function"
        $dataFunctionName = $baseResourceName + "-data-function"

        Write-Host "Please login with your Azure subscription account"
        az login
        az account set -s $subscriptionId
        Write-Host "Successfully logged in to Azure Subscription " -ForegroundColor Green

        Write-Host "****************************************************************************************************************************************************************************************************************************"
        Write-Host "Updating prep function to .NET 6 : $prepFunctionName"
        az functionapp config set --net-framework-version v6.0 -n $prepFunctionName -g $resourceGroupName
        Write-Host "Completed updating prep function to .NET 6 : $prepFunctionName" -ForegroundColor Green

       Write-Host "****************************************************************************************************************************************************************************************************************************"
        Write-Host "Updating send function to .NET 6 : $sendFunctionName"
        az functionapp config set --net-framework-version v6.0 -n $sendFunctionName -g $resourceGroupName
        Write-Host "Completed updating send function to .NET 6 : $sendFunctionName" -ForegroundColor Green

        Write-Host "****************************************************************************************************************************************************************************************************************************"
        Write-Host "Updating data function to .NET 6 : $dataFunctionName"
        az functionapp config set --net-framework-version v6.0 -n $dataFunctionName -g $resourceGroupName
        Write-Host "Completed updating data function to .NET 6 : $dataFunctionName" -ForegroundColor Green
}
catch {
        $errorMessage = $_.Exception.Message
        Write-Host "Failed to update the Azure functions. Error message: $errorMessage" -ForegroundColor Red
    }
}


function UpdateAzureAppService {
    param(
        [Parameter(Mandatory = $true)] [string] $resourceGroupName,
        [Parameter(Mandatory = $true)] [string] $baseResourceName
    )
        
    try{

        Write-Host "****************************************************************************************************************************************************************************************************************************"
        Write-Host "Updating app service to .NET 6 $baseResourceName"
        az webapp config set --net-framework-version v6.0 -n $baseResourceName -g $resourceGroupName
        Write-Host "Completed updating app service to .NET 6 $baseResourceName" -ForegroundColor Green
}
catch {
        $errorMessage = $_.Exception.Message
        Write-Host "Failed to update the app service $baseResourceName. Error message: $errorMessage" -ForegroundColor Red
    }
}

$subscriptionId = Read-Host "Please enter the subscription id of the resources where Company Communicator deployed"
$resourceGroupName = Read-Host "Please enter the resource group name"
$baseResourceName = Read-Host "Please enter the base resource name used"

UpdateAzureFunctions -subscriptionId $subscriptionId -resourceGroupName $resourceGroupName -baseResourceName $baseResourceName
UpdateAzureAppService -resourceGroupName $resourceGroupName -baseResourceName $baseResourceName

