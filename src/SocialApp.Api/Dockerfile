FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY SocialApp.sln ./
COPY src/SocialApp.User/SocialApp.User.csproj src/SocialApp.User/
COPY src/SocialApp.Post/SocialApp.Post.csproj src/SocialApp.Post/
COPY src/SocialApp.Infrastructure.CosmosMongo/SocialApp.Infrastructure.CosmosMongo.csproj src/SocialApp.Infrastructure.CosmosMongo/
COPY src/SocialApp.Infrastructure.AcsEmail/SocialApp.Infrastructure.AcsEmail.csproj src/SocialApp.Infrastructure.AcsEmail/
COPY src/SocialApp.Infrastructure.LocalStorage/SocialApp.Infrastructure.LocalStorage.csproj src/SocialApp.Infrastructure.LocalStorage/
COPY src/SocialApp.Infrastructure.AzureBlobStorage/SocialApp.Infrastructure.AzureBlobStorage.csproj src/SocialApp.Infrastructure.AzureBlobStorage/
COPY src/SocialApp.Api/SocialApp.Api.csproj src/SocialApp.Api/
RUN dotnet restore src/SocialApp.Api/SocialApp.Api.csproj
COPY . .
RUN dotnet publish src/SocialApp.Api/SocialApp.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SocialApp.Api.dll"]
