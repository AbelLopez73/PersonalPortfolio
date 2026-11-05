# Publica PersonalPortfolio en Azure Container Apps.
# Requisitos: Azure CLI (az) y haber ejecutado `az login`.
# Uso: .\deploy\azure-deploy.ps1 -Name abelportfolio -Location eastus
param(
    [Parameter(Mandatory)][ValidatePattern('^[a-z0-9]{5,20}$')][string]$Name,
    [string]$Location = 'eastus',
    [string]$TelegramClientId = '',
    [string]$TelegramClientSecret = '',
    [string]$TelegramOwnerUserId = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$rg = "rg-$Name"
$acr = "acr$Name"
$cae = "cae-$Name"
$apiApp = "$Name-api"
$webApp = "$Name-web"

function Invoke-Az { & az @args; if ($LASTEXITCODE -ne 0) { throw "az $($args -join ' ') fallo" } }

Invoke-Az extension add --name containerapp --upgrade --only-show-errors
Invoke-Az provider register --namespace Microsoft.App --wait
Invoke-Az provider register --namespace Microsoft.OperationalInsights --wait
Invoke-Az provider register --namespace Microsoft.ContainerRegistry --wait

Invoke-Az group create --name $rg --location $Location --output none
Invoke-Az acr create --resource-group $rg --name $acr --sku Basic --admin-enabled true --output none
Invoke-Az containerapp env create --resource-group $rg --name $cae --location $Location --output none

# Construye las imagenes en Azure (no requiere Docker local)
Invoke-Az acr build --registry $acr --image "${apiApp}:latest" --file "$root\src\PersonalPortfolio.Api\Dockerfile" $root
Invoke-Az acr build --registry $acr --image "${webApp}:latest" --file "$root\src\frontend\Dockerfile" $root

$acrServer = az acr show --name $acr --query loginServer -o tsv
$acrUser = az acr credential show --name $acr --query username -o tsv
$acrPass = az acr credential show --name $acr --query "passwords[0].value" -o tsv

$apiEnv = @(
    'ASPNETCORE_ENVIRONMENT=Production',
    "TelegramLogin__ClientId=$TelegramClientId",
    "TelegramLogin__OwnerUserId=$TelegramOwnerUserId"
)
$apiSecrets = @()
if ($TelegramClientSecret) {
    $apiSecrets = @('--secrets', "tg-secret=$TelegramClientSecret")
    $apiEnv += 'TelegramLogin__ClientSecret=secretref:tg-secret'
}

# La API solo es accesible dentro del entorno; el frontend (nginx) le hace proxy
Invoke-Az containerapp create --resource-group $rg --name $apiApp --environment $cae `
    --image "$acrServer/${apiApp}:latest" --registry-server $acrServer `
    --registry-username $acrUser --registry-password $acrPass `
    --ingress internal --target-port 8080 --transport http --allow-insecure `
    --min-replicas 1 --max-replicas 1 @apiSecrets --env-vars @apiEnv --output none

Invoke-Az containerapp create --resource-group $rg --name $webApp --environment $cae `
    --image "$acrServer/${webApp}:latest" --registry-server $acrServer `
    --registry-username $acrUser --registry-password $acrPass `
    --ingress external --target-port 80 --min-replicas 1 --max-replicas 1 `
    --env-vars "API_UPSTREAM=$apiApp" --output none

$webFqdn = az containerapp show --resource-group $rg --name $webApp --query properties.configuration.ingress.fqdn -o tsv

Invoke-Az containerapp update --resource-group $rg --name $apiApp `
    --set-env-vars "Frontend__AllowedOrigins__0=https://$webFqdn" --output none

Write-Host ""
Write-Host "Listo: https://$webFqdn" -ForegroundColor Green
Write-Host "Redirect URI para Telegram: https://$webFqdn/signin-telegram"
