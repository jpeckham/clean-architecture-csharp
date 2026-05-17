# Clean Social Azure Hosting Infrastructure Prompt

You are implementing Azure cloud hosting infrastructure for the Clean Social application.

## Objective

Deploy and configure a production-capable but cost-minimized Azure hosting environment for the Clean Social application using:

- Azure Static Web Apps
- Azure Container Apps
- Azure Cosmos DB Mongo API Free Tier
- Azure Blob Storage
- GitHub Actions CI/CD
- Azure Key Vault integration
- Custom DNS
- Managed HTTPS certificates

The implementation must remain aligned with:
- Clean Architecture
- Twelve-Factor App principles
- Containerized deployment
- Infrastructure as Code
- Low operational complexity
- Low monthly operating cost

---

# Azure Tenant / Subscription

## Tenant ID

```text
7f4e14a4-417c-496f-82d9-9fb7940c3d17
```

## Subscription ID

```text
6d88cea2-aec5-4d58-88c4-4830a867b3cd
```

---

# Existing Infrastructure

Use existing infrastructure where appropriate.

## Existing Resource Group

```text
rg-jdpeckham
```

## Existing DNS Zone

```text
azure.jdpeckham.com
```

## Existing Key Vault

```text
kv-jdpeckham
```

---

# Required DNS

The application must be hosted at:

```text
cleansocial.azure.jdpeckham.com
```

---

# Required Azure Resources

Create and configure the following Azure resources.

## Frontend

Use:

- Azure Static Web Apps

Requirements:

- Production environment
- GitHub Actions deployment
- Custom domain:
  - cleansocial.azure.jdpeckham.com
- HTTPS enabled
- Minimal cost SKU

---

## Backend API

Use:

- Azure Container Apps

Requirements:

- Linux container
- ASP.NET Core API container
- Consumption plan
- Scale-to-zero enabled
- Environment variables from Key Vault
- Secure ingress
- Public HTTPS endpoint
- Separate dev/prod configuration support

---

## Database

Use:

- Azure Cosmos DB Mongo API

Requirements:

- Free tier enabled
- Connection string stored in Key Vault
- Database initialized automatically if possible
- Proper partitioning guidance documented

---

## Media Storage

Use:

- Azure Blob Storage

Requirements:

- Separate containers for:
  - profile-images
  - post-images
  - post-videos
- HTTPS only
- Public access disabled by default
- SAS token or backend-mediated access strategy documented

---

## Container Registry

Use:

- Azure Container Registry Basic SKU

Requirements:

- Store backend API images
- Integrate with GitHub Actions
- Integrate with Container Apps

---

# CI/CD Requirements

Use GitHub Actions.

Implement:

- Build frontend
- Build backend container
- Push container to ACR
- Deploy frontend to Static Web Apps
- Deploy backend to Container Apps

Requirements:

- Environment secrets
- OIDC authentication preferred over static credentials
- Production deployment workflow
- Pull request validation workflow

---

# Infrastructure as Code

Prefer:

- Bicep

Acceptable alternatives:

- Terraform
- Azure CLI scripts

Requirements:

- Idempotent deployment
- Modular structure
- Clear documentation
- Minimal manual steps

---

# Security Requirements

Implement:

- HTTPS everywhere
- Managed identities where possible
- Secrets stored in Key Vault
- No secrets committed to source control
- Least privilege access

---

# Repository Tasks

Perform the following tasks.

## Add Infrastructure Folder Structure

Create a structure similar to:

```text
/infrastructure
    /bicep
    /github
    /scripts
    /docs
```

---

## Add Documentation

Create markdown documentation for:

- Azure architecture
- Deployment process
- DNS configuration
- Local development setup
- Cost expectations
- Disaster recovery basics

---

## Add GitHub Actions

Create workflows for:

- pull-request validation
- production deployment

---

## Add Environment Configuration

Create examples for:

- local development
- production hosting
- GitHub secrets
- Key Vault secrets

---

# Cost Optimization

Prioritize minimizing monthly costs.

Target:

- MVP hosting under approximately $15/month

Use:
- Consumption/serverless plans
- Free tiers where possible
- Smallest practical SKUs

Avoid:
- AKS
- App Service Premium
- Application Gateway
- Front Door
- Redis
- Service Bus
- Any unnecessary paid infrastructure

---

# Deliverables

Generate:

- Infrastructure as Code
- GitHub Actions workflows
- Azure deployment scripts
- DNS setup documentation
- Architecture documentation
- Environment configuration templates

Do not leave placeholders unless absolutely necessary.

Prefer working implementations over theoretical examples.

