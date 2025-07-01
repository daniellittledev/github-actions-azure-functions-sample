# GitHub Actions CI/CD Pipeline Setup Guide

This document provides a comprehensive plan for setting up a GitHub Actions CI/CD pipeline for the Azure Function project using PowerShell.

## Overview

The pipeline consists of:

1. **Build Job**: Builds, tests, and packages the application (runs on Windows with PowerShell)
2. **Deploy-Test Job**: Deploys to test environment (runs on Windows with PowerShell)
3. **Deploy-Prod Job**: Deploys to production environment (runs on Windows with PowerShell)

## Workflow Structure

### Main Workflow (`ci-cd.yml`)

- Triggers on pushes to `main`/`develop` branches and pull requests
- Runs on `windows-latest` runners
- Uses PowerShell (`pwsh`) for all script steps
- Includes build job with:
  - NuGet package caching
  - Bicep linting
  - Project build and test
  - Semantic versioning
  - Artifact publishing
- Conditional deployment jobs based on branch and change detection

### Reusable Deployment Workflow (`deploy.yml`)

- Shared by both test and production deployments
- Runs on `windows-latest` runners
- Uses PowerShell (`pwsh`) for all script steps
- Uses Azure OIDC for secure authentication
- Implements blue-green deployment with staging slots
- Separates infrastructure and application settings deployment

## Required GitHub Secrets

### For Test Environment

- `AZURE_CLIENT_ID_TEST`: Azure AD App Registration Client ID for test
- `AZURE_SUBSCRIPTION_ID_TEST`: Azure subscription ID for test environment
- `AZURE_RESOURCE_GROUP_TEST`: Resource group name for test environment
- `AZURE_FUNCTIONAPP_NAME_TEST`: Function app name for test environment

### For Production Environment

- `AZURE_CLIENT_ID_PROD`: Azure AD App Registration Client ID for production
- `AZURE_SUBSCRIPTION_ID_PROD`: Azure subscription ID for production environment
- `AZURE_RESOURCE_GROUP_PROD`: Resource group name for production environment
- `AZURE_FUNCTIONAPP_NAME_PROD`: Function app name for production environment

### Shared Secrets

- `AZURE_TENANT_ID`: Azure AD tenant ID (shared across environments)

## Azure Setup Requirements

### 1. Create Azure AD App Registrations

You'll need separate app registrations for test and production environments with OIDC federated credentials.

```bash
# Create app registration for test environment
az ad app create --display-name "github-actions-test"

# Create app registration for production environment
az ad app create --display-name "github-actions-prod"
```

### 2. Configure Federated Credentials

For each app registration, configure federated credentials for GitHub Actions:

```bash
# For test environment (adjust values accordingly)
az ad app federated-credential create \
  --id <TEST_APP_ID> \
  --parameters '{
    "name": "github-actions-test",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<YOUR_GITHUB_ORG>/<YOUR_REPO>:ref:refs/heads/develop",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# For production environment
az ad app federated-credential create \
  --id <PROD_APP_ID> \
  --parameters '{
    "name": "github-actions-prod",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<YOUR_GITHUB_ORG>/<YOUR_REPO>:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

### 3. Assign Permissions

Grant the service principals appropriate permissions:

```bash
# Get service principal IDs
TEST_SP_ID=$(az ad sp list --filter "appId eq '<TEST_APP_ID>'" --query '[0].id' -o tsv)
PROD_SP_ID=$(az ad sp list --filter "appId eq '<PROD_APP_ID>'" --query '[0].id' -o tsv)

# Assign Contributor role to test resource group
az role assignment create \
  --assignee $TEST_SP_ID \
  --role "Contributor" \
  --scope "/subscriptions/<TEST_SUBSCRIPTION_ID>/resourceGroups/<TEST_RESOURCE_GROUP>"

# Assign Contributor role to production resource group
az role assignment create \
  --assignee $PROD_SP_ID \
  --role "Contributor" \
  --scope "/subscriptions/<PROD_SUBSCRIPTION_ID>/resourceGroups/<PROD_RESOURCE_GROUP>"
```

## Key Features

### Build Job Features

- **Windows Runners**: Uses `windows-latest` for compatibility with PowerShell
- **PowerShell Scripts**: All custom scripts use PowerShell (`pwsh`) shell
- **NuGet Caching**: Speeds up builds by caching NuGet packages
- **Change Detection**: Only runs Bicep linting when infrastructure files change
- **Semantic Versioning**: Generates version numbers for deployments using PowerShell date formatting
- **Parallel Testing**: Runs unit tests with code coverage
- **Artifact Management**: Uses PowerShell `Compress-Archive` to create deployment packages

### Deployment Features

- **Windows Runners**: Uses `windows-latest` for consistency with build environment
- **PowerShell Scripts**: All deployment scripts use PowerShell for better Windows integration
- **Blue-Green Deployment**: Uses staging slots for zero-downtime deployments
- **Infrastructure Separation**: Only deploys Bicep when infrastructure changes
- **App Settings Management**: Deploys app settings via Azure CLI with PowerShell variable handling
- **Health Checks**: Uses PowerShell `Invoke-WebRequest` for deployment verification
- **Rollback Capability**: Includes cleanup steps for failed deployments

### Security Features

- **OIDC Authentication**: Uses OpenID Connect for secure Azure authentication (no stored credentials)
- **Least Privilege**: Service principals have minimal required permissions
- **Environment Isolation**: Separate credentials and resources for test/prod

## Environment-Specific Configuration

### Test Environment

- Triggered by pushes to `develop` branch or pull requests
- Uses test-specific Azure resources and credentials
- App settings configured for development/testing

### Production Environment

- Triggered only by pushes to `main` branch
- Uses production Azure resources and credentials
- App settings configured for production use

## Deployment Process

1. **Build Stage**:

   - Restore NuGet packages (with caching)
   - Lint Bicep files (if infrastructure changed)
   - Build solution
   - Run unit tests
   - Publish function app
   - Create deployment artifacts

2. **Infrastructure Deployment** (if changed):

   - Deploy Bicep templates using Azure CLI
   - Create/update Azure resources

3. **Application Deployment**:
   - Create staging slot (if not exists)
   - Deploy function app to staging slot
   - Configure environment-specific app settings
   - Run smoke tests on staging
   - Swap staging to production
   - Verify production deployment

## Files Created

1. **`.github/workflows/ci-cd.yml`**: Main CI/CD pipeline
2. **`.github/workflows/deploy.yml`**: Reusable deployment workflow
3. **`infra/main-pipeline.bicep`**: Modified Bicep template for pipeline use
4. **`PIPELINE_SETUP.md`**: This setup guide

## Next Steps

1. Set up Azure AD app registrations and federated credentials
2. Configure GitHub repository secrets
3. Create test and production resource groups in Azure
4. Test the pipeline with a small change
5. Configure branch protection rules as needed
6. Set up monitoring and alerting for deployments

## Customization Options

- **Semantic Versioning**: Replace the simple timestamp version with proper semantic-release
- **Testing Strategy**: Add integration tests, load tests, or security scans
- **Notification**: Add Slack/Teams notifications for deployment status
- **Approval Gates**: Add manual approval steps for production deployments
- **Multi-region**: Extend for multi-region deployments
- **Monitoring**: Integrate with Application Insights or other monitoring tools

## Troubleshooting

- Check Azure CLI version compatibility
- Verify OIDC federated credential configuration
- Ensure service principal permissions are correct
- Check that staging slot creation permissions are granted
- Verify function app naming constraints are met
