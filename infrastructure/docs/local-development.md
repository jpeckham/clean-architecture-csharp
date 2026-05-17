# Local Development

Docker Compose remains the default local runtime:

```powershell
docker compose up -d --build
```

Local endpoints:

```text
API: http://localhost:8080
Web: http://localhost:8081
Mongo: localhost:27017
```

Local Compose uses:

```text
CosmosMongo__ConnectionString=mongodb://mongo:27017
CosmosMongo__DatabaseName=socialapp
Media__Provider=FileSystem
LocalMedia__RootPath=/var/socialapp/media
API_BASE_ADDRESS=http://127.0.0.1:8080
```

Cloud hosting uses the same application settings pattern, replacing local Mongo and filesystem media with Cosmos DB Mongo API and Azure Blob Storage.
