FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/SocialApp.Web/SocialApp.Web.csproj src/SocialApp.Web/
RUN dotnet restore src/SocialApp.Web/SocialApp.Web.csproj
COPY src/SocialApp.Web/ src/SocialApp.Web/
RUN dotnet publish src/SocialApp.Web/SocialApp.Web.csproj -c Release -o /app/publish

FROM nginx:1.27-alpine AS final
COPY src/SocialApp.Web/nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY src/SocialApp.Web/docker-entrypoint.d/10-write-appsettings.sh /docker-entrypoint.d/10-write-appsettings.sh
RUN chmod +x /docker-entrypoint.d/10-write-appsettings.sh
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 80

