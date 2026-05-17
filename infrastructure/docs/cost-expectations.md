# Cost Expectations

The target MVP operating cost is approximately under $15/month at low traffic.

Cost-minimized choices:

- Static Web Apps Free SKU for frontend hosting.
- Container Apps Consumption with `minReplicas: 0` for API scale-to-zero.
- ACR Basic SKU.
- Cosmos DB Mongo API Free Tier with one 400 RU/s database.
- StorageV2 Standard LRS for private media.
- Log Analytics 30-day retention.

Potential costs:

- Container Apps charges when the API is active.
- Log Analytics ingestion can grow with noisy logs.
- Storage grows with uploaded media.
- Cosmos costs can increase if Free Tier is unavailable in the subscription or if throughput is raised.

Avoided services:

- AKS
- App Service Premium
- Application Gateway
- Front Door
- Redis
- Service Bus
