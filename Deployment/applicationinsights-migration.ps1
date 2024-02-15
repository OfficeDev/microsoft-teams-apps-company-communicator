function CreateLogAnalyticsWorkspace {
    param(
        [Parameter(Mandatory = $true)] [string] $subscriptionId,
        [Parameter(Mandatory = $true)] [string] $resourceGroupName,
        [Parameter(Mandatory = $true)] [string] $baseResourceName
    )
        
    try{
        $workspaceName = $baseResourceName + "-log-analytics"
	
	Import-Module Az.OperationalInsights
        Write-Host "Please login with your Azure subscription account"
        az login
        az account set -s $subscriptionId
        Write-Host "Successfully logged in to Azure Subscription " -ForegroundColor Green

        Write-Host "Getting resource group location"
        $rgLocation = (Get-AzResourceGroup -Name $resourceGroupName).location
        Write-Host "Successfully got the resource group location " -ForegroundColor Green


        Write-Host "****************************************************************************************************************************************************************************************************************************"
        Write-Host "Creating Log Analytics Workspace"
        New-AzOperationalInsightsWorkspace -Location $rgLocation -Name $workspaceName -ResourceGroupName $resourceGroupName
        Write-Host "Successfully created Log Analytics Workspace : $WorkspaceName" -ForegroundColor Green

}
catch {
        $errorMessage = $_.Exception.Message
        Write-Host "Failed to create the Log Analytics Workspace. Error message: $errorMessage" -ForegroundColor Red
    }
}


function UpdateClassicAppInsights {
    param(
        [Parameter(Mandatory = $true)] [string] $resourceGroupName,
        [Parameter(Mandatory = $true)] [string] $baseResourceName
    )
        
    try{
        
        $workspaceName = $baseResourceName + "-log-analytics"

        Write-Host "****************************************************************************************************************************************************************************************************************************"        
        Write-Host "Getting resource id for log analytics workspace"
        $workspaceResourceId = (Get-AzOperationalInsightsWorkspace -ResourceGroupName $resourceGroupName -Name $workspaceName).ResourceId
        Write-Host "Successfully got the resource id for log analytics workspace " -ForegroundColor Green

        Write-Host "****************************************************************************************************************************************************************************************************************************"
        Write-Host "Updating classic application insights to workspace based application insights"
        Update-AzApplicationInsights -Name $baseResourceName -ResourceGroupName $resourceGroupName -IngestionMode LogAnalytics -WorkspaceResourceId $workspaceResourceId 
        Write-Host "Successfully migrated the classic application insights to workspace based application insights $baseResourceName" -ForegroundColor Green
}
catch {
        $errorMessage = $_.Exception.Message
        Write-Host "Failed to update the classic application insights $baseResourceName. Error message: $errorMessage" -ForegroundColor Red
    }
}

$subscriptionId = Read-Host "Please enter the subscription id of the resources where Company Communicator deployed"
$resourceGroupName = Read-Host "Please enter the resource group name"
$baseResourceName = Read-Host "Please enter the base resource name used"


CreateLogAnalyticsWorkspace -subscriptionId $subscriptionId -resourceGroupName $resourceGroupName -baseResourceName $baseResourceName
UpdateClassicAppInsights -resourceGroupName $resourceGroupName -baseResourceName $baseResourceName
