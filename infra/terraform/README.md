# SocialApp Azure Infrastructure

This Terraform stack provisions the smallest Azure footprint for the SocialApp vertical slice:

- Azure Static Web Apps for the Blazor WebAssembly frontend
- Azure Container Apps for `SocialApp.Api`
- Azure Cosmos DB for MongoDB API
- Azure Communication Services Email with an Azure-managed sender domain
- Log Analytics for Container Apps logs

## Usage

```powershell
terraform -chdir=infra/terraform init
terraform -chdir=infra/terraform plan `
  -var "api_container_image=<registry>/socialapp-api:<tag>" `
  -var "password_reset_base_url=https://<static-web-app-host>/reset-password"
terraform -chdir=infra/terraform apply `
  -var "api_container_image=<registry>/socialapp-api:<tag>" `
  -var "password_reset_base_url=https://<static-web-app-host>/reset-password"
```

The API container receives the Cosmos Mongo and ACS connection strings through Container Apps secrets. It reads email settings from `AcsEmail__ConnectionString` and `AcsEmail__SenderAddress`.
