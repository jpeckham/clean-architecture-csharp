# Prompt: Add Azure Blob Storage And Terraform Media Infrastructure

## Objective

Add Azure Blob Storage-backed media storage and Terraform infrastructure for cloud media hosting.

## Prerequisites

Complete:
- `docs/prompts/2026-05-11-add-media-02-post-component.md`
- `docs/prompts/2026-05-11-add-media-03-user-profile-image-component.md`
- `docs/prompts/2026-05-11-add-media-04-api-contracts-and-endpoints.md`

## Requirements

- Use Azure Blob Storage for binary media.
- Use managed identity and Microsoft Entra authorization where practical.
- Prefer user delegation SAS for direct browser upload when implementing direct-to-blob upload.
- Keep Azure implementation in infrastructure.
- Keep Terraform changes additive and aligned with existing `infra/terraform` style.
- Do not use Azure Media Services.

## Suggested Scope

Create if useful:
- `src/SocialApp.Infrastructure.AzureBlobStorage/`
- `src/SocialApp.Infrastructure.AzureBlobStorage/SocialApp.Infrastructure.AzureBlobStorage.csproj`
- `src/SocialApp.Infrastructure.AzureBlobStorage/DependencyInjection.cs`
- `src/SocialApp.Infrastructure.AzureBlobStorage/Options/AzureBlobMediaOptions.cs`
- Azure Blob gateway implementations for post media and profile images

Modify:
- `SocialApp.sln`
- `src/SocialApp.Api/SocialApp.Api.csproj`
- `src/SocialApp.Api/Program.cs`
- `src/SocialApp.Api/appsettings.json`
- `infra/terraform/main.tf`
- `infra/terraform/variables.tf`
- `infra/terraform/outputs.tf`
- `infra/terraform/README.md`

## Terraform Behavior

- Add a storage account for media.
- Add private blob containers such as `profile-images`, `post-media`, and `thumbnails`.
- Enable soft delete/container soft delete and versioning where supported.
- Assign the API Container App identity Blob permissions needed by the adapter.
- Add app configuration for storage account/container names.
- Prefer Key Vault references for secrets if secret changes are needed.

## Adapter Behavior

- `Media__Provider=AzureBlob` registers the Azure adapter.
- Begin upload returns short-lived upload instructions.
- Complete upload verifies blob existence and expected metadata.
- Read URL generation supports either short-lived SAS or CDN/Front Door-ready public delivery strategy.

## Out Of Scope

- Front Door custom domains and WAF unless already easy to express in existing Terraform.
- Thumbnail generation.
- Adaptive bitrate streaming or DRM.
- Azure Media Services.

## Verification

- Run `dotnet build SocialApp.sln --no-restore`.
- Run `dotnet test SocialApp.sln --no-restore`.
- Run `terraform -chdir=infra/terraform fmt`.
- Run `terraform -chdir=infra/terraform validate` when Terraform provider initialization is available.

