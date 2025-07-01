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
      netFrameworkVersion: 'v9.0'
      ftpsState: 'Disabled'
      cors: {
        allowedOrigins: [
          'https://portal.azure.com'
          'https://functions.azure.com'
        ]
        supportCredentials: false
      }
      appSettings: [
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'WEBSITE_RUN_FROM_PACKAGE', value: '1' }
        { name: 'DOTNET_ENVIRONMENT', value: 'Production' }
        { name: 'AzureWebJobsStorage', value: webJobsStorageConnection }
        {
          name: 'AZURE_SUBSCRIPTION_ID'
          value: subscription().subscriptionId
        }
        {
          name: 'SAMPLE_SETTING'
          value: 'prod-sample'
        }
      ]
    }
    httpsOnly: true
  }
}
