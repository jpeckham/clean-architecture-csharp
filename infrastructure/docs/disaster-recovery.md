# Disaster Recovery Basics

## Recovery Objectives

This MVP stack prioritizes low cost over multi-region failover. Recovery is rebuild-from-IaC plus platform backups.

## Protected State

- Cosmos DB uses periodic backups.
- Blob Storage has blob and container soft delete for 7 days.
- Source, workflow definitions, and Bicep templates live in Git.
- Container images are rebuilt from GitHub Actions and stored in ACR.

## Restore Procedure

1. Recreate infrastructure from Bicep into `rg-jdpeckham`.
2. Restore Cosmos DB data through Azure support or portal restore tooling.
3. Recover accidentally deleted blobs within the soft-delete retention window.
4. Re-run the production deployment workflow to push the current API and frontend.
5. Re-run custom domain binding if the Static Web App was recreated.

## Limits

There is no active-active region pair, private endpoint topology, or cross-region Blob replication in this cost-minimized setup.
