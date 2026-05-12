# Prompt: Harden Media Support

## Objective

Add the operational and product hardening that should follow the first working media implementation.

## Prerequisites

Complete the usable local or Azure media path first:
- Local path: phases 1-7.
- Azure path: phases 1-8.

## Requirements

- Keep hardening incremental.
- Do not block the core image/profile/post media feature on advanced video workflows.
- Continue to keep business policy in owning components and storage mechanics in infrastructure.

## Suggested Scope

Implement only the hardening items that match the current deployment target:

- Thumbnail/poster generation for images and videos.
- Background cleanup of abandoned upload reservations.
- Blob lifecycle rules for old originals.
- Cache headers for immutable media keys.
- Front Door configuration for delivery and caching.
- Stronger size/content-type validation at upload and completion.
- Optional checksum validation.
- Observability for upload failures and storage calls.
- Additional architecture tests preventing shared media-domain drift.

## Suggested Files

Likely areas:
- `src/SocialApp.Post`
- `src/SocialApp.User`
- storage infrastructure project introduced earlier
- `src/SocialApp.Api/Program.cs`
- `infra/terraform/main.tf`
- `tests/SocialApp.Architecture.Tests/ArchitectureRulesTests.cs`
- relevant component/API/infrastructure tests

## Out Of Scope

- Azure Media Services.
- DRM.
- Live streaming.
- A new shared media bounded context unless product requirements have changed enough to justify it.

## Verification

- Run narrow tests for the touched project.
- Run `dotnet test SocialApp.sln --no-restore`.
- For Terraform changes, run `terraform -chdir=infra/terraform fmt` and `terraform -chdir=infra/terraform validate` when initialized.
- Manually verify existing image/profile/post upload flows still work after hardening.

