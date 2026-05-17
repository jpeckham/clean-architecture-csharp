param(
    [string]$ResourceGroupName = "rg-jdpeckham",
    [string]$StaticWebAppName = "stapp-cleansocial-prod",
    [string]$HostName = "cleansocial.azure.jdpeckham.com"
)

$ErrorActionPreference = "Stop"

az staticwebapp hostname set `
    --resource-group $ResourceGroupName `
    --name $StaticWebAppName `
    --hostname $HostName

az staticwebapp hostname show `
    --resource-group $ResourceGroupName `
    --name $StaticWebAppName `
    --hostname $HostName `
    --output table
