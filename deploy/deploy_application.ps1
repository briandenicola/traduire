[CmdletBinding(DefaultParameterSetName = 'Default')]
param(
    [Parameter(ParameterSetName = 'Default', Mandatory=$true)]
    [string] $AppName,
  
    [Parameter(ParameterSetName = 'Default', Mandatory=$true)]
    [string] $SubscriptionName,

    [Parameter(ParameterSetName = 'Default', Mandatory=$true)]
    [string] $Uri,

    [Parameter(ParameterSetName = 'Default', Mandatory=$false)]
    [switch] $Upgrade
)

. .\modules\traduire_functions.ps1

Set-Variable -Name DAPR_VERSION         -Value "1.6.0"                           -Option Constant
Set-Variable -Name KEDA_VERSION         -Value "2.6.1"                           -Option Constant
Set-Variable -Name CERT_MGR_VERSION     -Value "v1.6.1"                          -Option Constant
Set-Variable -Name APP_RG_NAME          -Value ("{0}_app_rg" -f $AppName)        -Option Constant
Set-Variable -Name CORE_RG_NAME         -Value ("{0}_core_rg" -f $AppName)       -Option Constant
Set-Variable -Name APP_K8S_NAME         -Value ("{0}-aks01" -f $AppName)         -Option Constant
Set-Variable -Name APP_ACR_NAME         -Value ("{0}acr01" -f $AppName)          -Option Constant
Set-Variable -Name APP_KV_NAME          -Value ("{0}-kv01" -f $AppName)          -Option Constant
Set-Variable -Name APP_SA_NAME          -Value ("{0}files01" -f $AppName)        -Option Constant
Set-Variable -Name APP_MSI_NAME         -Value ("{0}-dapr-reader" -f $AppName)   -Option Constant
Set-Variable -Name APP_COGS_NAME        -Value ("{0}-cogs01" -f $AppName)        -Option Constant
Set-Variable -Name APP_AI_NAME          -Value ("{0}-ai01" -f $AppName)          -Option Constant
Set-Variable -Name KEDA_MSI_NAME        -Value ("{0}-keda-sb-owner" -f $AppName) -Option Constant
Set-Variable -Name KEDA_POD_BINDING     -Value "keda-podidentity"                -Option Constant

$root   = (Get-Item $PWD.Path).Parent.FullName
$source = Join-Path -Path $root -ChildPath "source"

#Start-Docker
#Start-Docker

#Connect to Azure and Log into ACR
#Connect-ToAzure -SubscriptionName $SubscriptionName
Add-AzureCliExtensions
Connect-ToAzureContainerRepo -ACRName $APP_ACR_NAME 

#Build Source
$commit_version = Get-GitCommitVersion
Build-DockerContainers -ContainerName "${APP_ACR_NAME}.azurecr.io/traduire/api:${commit_version}" -DockerFile "$source/dockerfile.api" -SourcePath $source
Build-DockerContainers -ContainerName "${APP_ACR_NAME}.azurecr.io/traduire/onstarted.handler:${commit_version}" -DockerFile "$source/dockerfile.onstarted" -SourcePath $source
Build-DockerContainers -ContainerName "${APP_ACR_NAME}.azurecr.io/traduire/onprocessing.handler:${commit_version}" -DockerFile "$source/dockerfile.onprocessing" -SourcePath $source
Build-DockerContainers -ContainerName "${APP_ACR_NAME}.azurecr.io/traduire/oncompletion.handler:${commit_version}" -DockerFile "$source/dockerfile.oncompletion" -SourcePath $source

if($Upgrade) {
    Write-Log -Message "Upgrading Traduire to ${commit_version}"
    helm upgrade traduire helm/. --reuse-values --set commit_version=$commit_version 

    if($?){
        Write-Log ("Review DNS (A) Record: {0} - {1}" -f $uri, (Get-APIGatewayIP))
        Write-Log "API successfully updated. Done"
        return 0
    }
    return -1
}

#Get AKS Credential file
Get-AKSCredentials -AKSName $APP_K8S_NAME -AKSResourceGroup $APP_RG_NAME

#Generate Kong API secret
$kong_api_secret = New-APISecret -Length 25

#Get App Insights Key
$app_insights_key = (az monitor app-insights component show --app $APP_AI_NAME -g $CORE_RG_NAME --query instrumentationKey -o tsv)

#Get MSI Account Info
$app_msi  = New-MSIAccount -MSIName $APP_MSI_NAME -MSIResourceGroup $APP_RG_NAME
$keda_msi = New-MSIAccount -MSIName $KEDA_MSI_NAME -MSIResourceGroup $APP_RG_NAME

#Get Cognitive Services Info
$cogs = New-CognitiveServicesAccount -CogsAccountName $APP_COGS_NAME -CogsResourceGroup $APP_RG_NAME

# Install Kong API Gateway 
Write-Log -Message "Deploying Kong API Gateway"
helm repo add kong https://charts.konghq.com
helm repo update
helm upgrade -i kong kong/kong --namespace kong-gateway --create-namespace --set ingressController.installCRDs=false

# Install Dapr
Write-Log -Message "Deploying Dapr"
helm repo add dapr https://dapr.github.io/helm-charts
helm repo update
helm upgrade -i dapr dapr/dapr --namespace dapr-system --create-namespace --version $DAPR_VERSION --set global.mtls.enabled=true --set global.logAsJson=true --set global.ha.enabled=true --wait

# Install Pod Identity 
Write-Log -Message "Deploying Pod Identity"
helm repo add aad-pod-identity https://raw.githubusercontent.com/Azure/aad-pod-identity/master/charts
helm repo update
helm upgrade -i aad-pod-identity aad-pod-identity/aad-pod-identity

# Install Cert Manager
Write-Log -Message "Deploying Let's Encrypt Cert Manager"
helm repo add jetstack https://charts.jetstack.io
helm repo update
helm upgrade -i cert-manager jetstack/cert-manager --namespace cert-manager --create-namespace --version $CERT_MGR_VERSION  --set installCRDs=true

# Install Keda
Write-Log -Message "Deploying Keda"
helm repo add kedacore https://kedacore.github.io/charts
helm repo update
helm upgrade -i keda kedacore/keda --namespace keda --create-namespace --version $KEDA_VERSION --set podIdentity.activeDirectory.identity=$KEDA_POD_BINDING

## Install Kured 
Write-Log -Message "Deploying Kured"
helm repo add kured https://weaveworks.github.io/kured
helm repo update
helm upgrade -i kured kured/kured --namespace kured --create-namespace

# Install App
Write-Log -Message "Deploying Traduire"
helm upgrade -i `
   --set app_name=$AppName `
   --set msi_client_id=$($app_msi.client_id) `
   --set msi_resource_id=$($app_msi.resource_id) `
   --set keyvault_name=$APP_KV_NAME `
   --set storage_name=$APP_SA_NAME `
   --set acr_name=$APP_ACR_NAME `
   --set commit_version=$commit_version `
   --set cogs_region=$($cogs.region) `
   --set app_insights_key=$app_insights_key `
   --set kong_api_secret=$kong_api_secret `
   --set kong_api_uri=$Uri `
   --set keda_msi_client_id=$($keda_msi.client_id) `
   --set keda_msi_resource_id=$($keda_msi.resource_id) `
   traduire helm/. 

if($?){
    Write-Log ("Manually create DNS (A) Record: {0} - {1}" -f $uri, (Get-APIGatewayIP))
    Write-Log "API successfully deployed. Done"
}
else {
    Write-Log ("Errors encountered while deploying API. Please review. Application Name: {0}" -f $AppName )
} 
