# GitHub Actions CI/CD Pipeline Setup Guide

This document provides a setup guide for the GitHub Actions CI/CD pipeline implementing a three-step workflow: **Build**, **Deploy-Test**, and **Deploy-Prod** with shared deployment logic.

## 🚀 First Time Setup

### 1. Azure Prerequisites

#### Create Azure AD App Registrations

Create separate app registrations for test and production environments:

```pwsh
# Create app registration for test environment
az ad app create --display-name "github-actions-test"

# Create app registration for production environment
az ad app create --display-name "github-actions-prod"
```

#### Configure Federated Credentials

Set up OIDC federated credentials for GitHub Actions (replace placeholders with your values):

```pwsh
# For test environment
az ad app federated-credential create `
  --id <TEST_APP_ID> `
  --parameters '{
    "name": "github-actions-test",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<YOUR_GITHUB_ORG>/<YOUR_REPO>:ref:refs/heads/develop",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# For production environment
az ad app federated-credential create `
  --id <PROD_APP_ID> `
  --parameters '{
    "name": "github-actions-prod",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<YOUR_GITHUB_ORG>/<YOUR_REPO>:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

#### Assign Azure Permissions

Grant the service principals Contributor access to their respective resource groups:

```pwsh
# Get service principal IDs
$TestSpId = az ad sp list --filter "appId eq '<TEST_APP_ID>'" --query '[0].id' -o tsv
$ProdSpId = az ad sp list --filter "appId eq '<PROD_APP_ID>'" --query '[0].id' -o tsv

# Assign Contributor role to test resource group
az role assignment create `
  --assignee $TestSpId `
  --role "Contributor" `
  --scope "/subscriptions/<TEST_SUBSCRIPTION_ID>/resourceGroups/<TEST_RESOURCE_GROUP>"

# Assign Contributor role to production resource group
az role assignment create `
  --assignee $ProdSpId `
  --role "Contributor" `
  --scope "/subscriptions/<PROD_SUBSCRIPTION_ID>/resourceGroups/<PROD_RESOURCE_GROUP>"
```

### 2. GitHub Repository Configuration

#### Required GitHub Secrets

Configure these secrets in your GitHub repository settings:

**Test Environment:**

- `AZURE_CLIENT_ID_TEST`: Azure AD App Registration Client ID for test
- `AZURE_SUBSCRIPTION_ID_TEST`: Azure subscription ID for test environment
- `AZURE_RESOURCE_GROUP_TEST`: Resource group name for test environment
- `AZURE_FUNCTIONAPP_NAME_TEST`: Function app name for test environment

**Production Environment:**

- `AZURE_CLIENT_ID_PROD`: Azure AD App Registration Client ID for production
- `AZURE_SUBSCRIPTION_ID_PROD`: Azure subscription ID for production environment
- `AZURE_RESOURCE_GROUP_PROD`: Resource group name for production environment
- `AZURE_FUNCTIONAPP_NAME_PROD`: Function app name for production environment

**Shared:**

- `AZURE_TENANT_ID`: Azure AD tenant ID (shared across environments)

### 3. Required Files

Ensure these workflow files exist in your repository:

1. **`.github/workflows/ci-cd.yml`**: Main CI/CD pipeline
2. **`.github/workflows/deploy.yml`**: Reusable deployment workflow
3. **`infra/main.bicep`**: Infrastructure template

## 📖 Usage

### Triggering Deployments

#### Automatic Triggers

- **Test Environment**: Push to `develop` branch or create pull requests to `main`
- **Production Environment**: Push to `main` branch only

#### Manual Deployment

Use the GitHub Actions UI to manually trigger deployments via workflow dispatch.

### Branch Strategy

- **`develop`** → Deploys to **Test Environment**
- **`main`** → Deploys to **Production Environment**
- **Pull Requests to `main`** → Deploys to **Test Environment** for validation

### Deployment Process

1. **Build Stage**: Compiles code, runs tests, creates artifacts
2. **Test Deployment**: Automatically deploys to test environment
3. **Production Deployment**: Deploys to production (main branch only)

### Monitoring Deployments

- Check the **Actions** tab in GitHub for deployment status
- Review staging slot health before production promotion
- Monitor Azure Function logs for any issues

## Architecture

### Pipeline Overview

The CI/CD pipeline consists of three main components:

1. **Build Job** (`ci-cd.yml`): Code compilation, testing, and artifact creation
2. **Deploy-Test Job** (calls `deploy.yml`): Test environment deployment
3. **Deploy-Prod Job** (calls `deploy.yml`): Production environment deployment

### Build Job Features

- **NuGet Package Caching**: Caches packages for faster builds
- **Infrastructure Change Detection**: Only processes infrastructure when files change
- **Bicep Linting**: Validates infrastructure templates
- **Build & Test**: Compiles code and runs unit tests with coverage
- **Artifact Creation**: Packages function app and infrastructure templates
- **Semantic Versioning**: Generates timestamp-based versions

### Deployment Workflow Features

- **Azure OIDC Authentication**: Secure, passwordless authentication
- **Blue-Green Deployment**: Zero-downtime deployments using staging slots
- **Conditional Infrastructure**: Only deploys infrastructure when changed
- **Storage Connection Management**: Automatically handles storage connections
- **Health Validation**: Checks deployment health before promoting to production

### Key Technical Decisions

#### Smart Infrastructure Deployment

- **Bicep templates** only deploy when `infra/**` files change
- **App settings** managed separately from infrastructure for flexibility
- **Storage connections** extracted from Bicep outputs or queried from existing resources

#### Staging Slot Strategy

1. Deploy to staging slot first
2. Run health checks on staging
3. Swap to production only after validation
4. Quick rollback capability through slot swap

#### Security Model

- **OIDC Authentication**: No stored credentials, uses federated identity
- **Environment Isolation**: Separate service principals for test/production
- **Least Privilege**: Minimal required permissions for each environment
