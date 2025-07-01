param functionAppName string
param location string = resourceGroup().location

var functionStorageName = '${replace(functionAppName, '-', '')}fsa'

resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: functionStorageName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {}
}

resource hostingPlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: '${functionAppName}-plan'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: false
  }
}

var webJobsStorageConnection = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      ftpsState: 'Disabled'
      cors: {
        allowedOrigins: [
          'https://portal.azure.com'
          'https://functions.azure.com'
        ]
        supportCredentials: false
      }
      appSettings: [
        // Core Function App settings - these should remain in Bicep
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'WEBSITE_RUN_FROM_PACKAGE', value: '1' }
        { name: 'AzureWebJobsStorage', value: webJobsStorageConnection }
        {
          name: 'AZURE_SUBSCRIPTION_ID'
          value: subscription().subscriptionId
        }
        // Environment-specific settings will be managed via Azure CLI in the deployment pipeline
      ]
    }
    httpsOnly: true
  }
}

// Create staging slot for deployments
resource stagingSlot 'Microsoft.Web/sites/slots@2024-04-01' = {
  parent: functionApp
  name: 'staging'
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      ftpsState: 'Disabled'
      appSettings: [
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'WEBSITE_RUN_FROM_PACKAGE', value: '1' }
        { name: 'AzureWebJobsStorage', value: webJobsStorageConnection }
        {
          name: 'AZURE_SUBSCRIPTION_ID'
          value: subscription().subscriptionId
        }
      ]
    }
  }
}

// Outputs for use in deployment pipeline
output functionAppName string = functionApp.name
output resourceGroupName string = resourceGroup().name
output storageAccountName string = storage.name
output hostingPlanName string = hostingPlan.name
output webJobsStorageConnection string = webJobsStorageConnection
