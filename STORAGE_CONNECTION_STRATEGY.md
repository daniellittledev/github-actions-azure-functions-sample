# Storage Connection String Management in CI/CD Pipeline

This document explains how the pipeline handles the Azure Storage connection string (`AzureWebJobsStorage`) in different scenarios.

## The Problem

Azure Functions require a storage connection string (`AzureWebJobsStorage`) to operate. This creates a challenge in CI/CD pipelines where:

1. Sometimes infrastructure changes and Bicep runs (new storage account or rotated keys)
2. Sometimes only the application changes and Bicep doesn't run
3. The connection string must always be available for app settings

## The Solution

### **1. Bicep Outputs the Storage Connection String**

Both `main.bicep` and `main-pipeline.bicep` now export the storage connection string:

```bicep
output webJobsStorageConnection string = webJobsStorageConnection
```

### **2. Pipeline Handles Both Scenarios**

#### **Scenario A: Infrastructure Changed (Bicep Runs)**

```yaml
- name: Deploy Bicep Infrastructure
  if: inputs.infra-changed == 'true'
  id: bicep-deploy
  run: |
    # Deploy Bicep and capture outputs
    $deploymentOutput = az deployment group create --query "properties.outputs"
    # Extract storage connection string from Bicep outputs
    $storageConnection = $deploymentOutput.webJobsStorageConnection.value
    echo "STORAGE_CONNECTION=$storageConnection" >> $env:GITHUB_OUTPUT
```

#### **Scenario B: Infrastructure Unchanged (Bicep Doesn't Run)**

```yaml
- name: Get Storage Connection String (if Bicep didn't run)
  if: inputs.infra-changed != 'true'
  id: get-storage
  run: |
    # Derive storage account name from function app name
    $storageAccountName = $functionAppName -replace '-','' + "fsa"
    # Get storage key from Azure
    $storageKey = az storage account keys list --account-name $storageAccountName
    # Construct connection string
    $storageConnection = "DefaultEndpointsProtocol=https;AccountName=$storageAccountName;AccountKey=$storageKey;EndpointSuffix=core.windows.net"
    echo "STORAGE_CONNECTION=$storageConnection" >> $env:GITHUB_OUTPUT
```

### **3. App Settings Configuration Uses the Connection String**

```yaml
- name: Configure app settings for staging slot
  run: |
    # Get storage connection string from either step
    if ("${{ steps.bicep-deploy.outputs.STORAGE_CONNECTION }}" -ne "") {
      $storageConnection = "${{ steps.bicep-deploy.outputs.STORAGE_CONNECTION }}"
    } elseif ("${{ steps.get-storage.outputs.STORAGE_CONNECTION }}" -ne "") {
      $storageConnection = "${{ steps.get-storage.outputs.STORAGE_CONNECTION }}"
    }

    # Apply all app settings including storage connection
    az functionapp config appsettings set --settings \
      "AzureWebJobsStorage=$storageConnection" \
      "FUNCTIONS_EXTENSION_VERSION=~4" \
      # ... other settings
```

## Benefits of This Approach

### **1. Reliability**

- ✅ Storage connection string is always available
- ✅ Works whether Bicep runs or not
- ✅ Handles storage key rotation automatically

### **2. Security**

- ✅ No hardcoded connection strings
- ✅ Keys are retrieved fresh from Azure
- ✅ Connection string not stored in Git or artifacts

### **3. Flexibility**

- ✅ Supports infrastructure changes (new storage accounts)
- ✅ Supports app-only deployments
- ✅ Works with any naming convention

### **4. Consistency**

- ✅ Same logic works for test and production
- ✅ Core function app settings managed via pipeline
- ✅ Environment-specific settings separated

## Flow Diagram

```
Pipeline Start
├── Infrastructure Changed?
│   ├── YES → Deploy Bicep → Extract Storage Connection from Outputs
│   └── NO → Query Existing Storage Account → Build Connection String
├── Deploy Function App to Staging Slot
├── Configure App Settings (including storage connection)
├── Run Health Checks
└── Swap to Production
```

## Storage Account Naming Convention

The pipeline assumes storage accounts follow this naming pattern:

- Function App: `my-function-app`
- Storage Account: `myfunctionappfsa` (remove hyphens + add "fsa" suffix)

This can be customized by modifying the naming logic in both Bicep and the pipeline.

## Error Handling

- **Missing Storage Account**: Pipeline fails with clear error message
- **Invalid Permissions**: Azure CLI commands fail if service principal lacks storage permissions
- **Connection String Format**: Pipeline validates format before applying settings

## Required Permissions

The Azure service principal needs these permissions:

- `Storage Account Key Operator Service Role` on the storage account
- `Contributor` on the resource group (for app settings)
- `Website Contributor` on the function app

## Testing the Setup

1. **Test with Infrastructure Changes**: Modify any file in `infra/` folder
2. **Test without Infrastructure Changes**: Modify only application code
3. **Verify Storage Connection**: Check function app logs for successful storage connection

This approach ensures your Azure Functions always have the correct storage connection string, regardless of whether the infrastructure is being deployed or just the application code.
