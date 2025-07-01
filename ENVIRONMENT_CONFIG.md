# Environment Configuration Examples

## Test Environment App Settings

The following app settings will be applied to the test environment via Azure CLI:

```bash
az functionapp config appsettings set \
  --name <FUNCTION_APP_NAME> \
  --resource-group <RESOURCE_GROUP> \
  --slot staging \
  --settings \
    "DOTNET_ENVIRONMENT=Development" \
    "SAMPLE_SETTING=test-sample" \
    "DEPLOYMENT_VERSION=<VERSION>" \
    "DEPLOYED_AT=<TIMESTAMP>" \
    "LOG_LEVEL=Debug" \
    "ENABLE_DETAILED_ERRORS=true"
```

## Production Environment App Settings

The following app settings will be applied to the production environment via Azure CLI:

```bash
az functionapp config appsettings set \
  --name <FUNCTION_APP_NAME> \
  --resource-group <RESOURCE_GROUP> \
  --slot staging \
  --settings \
    "DOTNET_ENVIRONMENT=Production" \
    "SAMPLE_SETTING=prod-sample" \
    "DEPLOYMENT_VERSION=<VERSION>" \
    "DEPLOYED_AT=<TIMESTAMP>" \
    "LOG_LEVEL=Information" \
    "ENABLE_DETAILED_ERRORS=false"
```

## Resource Naming Convention

### Test Environment

- Resource Group: `rg-functionapp-test`
- Function App: `func-myapp-test`
- Storage Account: `stfuncmyapptest`
- App Service Plan: `func-myapp-test-plan`

### Production Environment

- Resource Group: `rg-functionapp-prod`
- Function App: `func-myapp-prod`
- Storage Account: `stfuncmyappprod`
- App Service Plan: `func-myapp-prod-plan`

## GitHub Environment Configuration

You can also configure GitHub Environments with protection rules:

### Test Environment

- Reviewers: Optional
- Deployment branches: `develop` branch only
- Environment secrets: Test-specific Azure credentials

### Production Environment

- Reviewers: Required (add your team members)
- Deployment branches: `main` branch only
- Environment secrets: Production-specific Azure credentials
- Wait timer: Optional (e.g., 5 minutes)
