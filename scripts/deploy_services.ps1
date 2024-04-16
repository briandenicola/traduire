[CmdletBinding(DefaultParameterSetName = 'Default')]
param(
    [Parameter(ParameterSetName = 'Default', Mandatory=$true)]
    [string] $AppName,
  
    [Parameter(ParameterSetName = 'Default', Mandatory=$true)]
    [string] $SubscriptionName,

    [Parameter(ParameterSetName = 'Default', Mandatory=$true)]
    [string] $DomainName,

    [Parameter(ParameterSetName = 'Default', Mandatory=$false)]
    [switch] $Upgrade,

    [Parameter(ParameterSetName = 'Default', Mandatory=$false)]
    [switch] $SkipBuild,

    [Parameter(ParameterSetName = 'Default', Mandatory=$false)]
    [switch] $BuildOnly
)

. ./modules/traduire_functions.ps1
. ./modules/traduire_naming.ps1 -AppName $AppName

#Connect to Azure and Log into ACR
Add-AzureCliExtensions

#Build and Push All Containers from Source 
$commit_version = Get-GitCommitVersion
if(-not $SkipBuild) {
    Build-Application -AppName $AppName -AcrName $APP_ACR_NAME -SubscriptionName $SubscriptionName -Source $APP_SOURCE_DIR -Version $commit_version
}

if($BuildOnly) { exit(0) }

if($Upgrade) {
    $msg = "Review DNS (A) Record:"
    Write-Log -Message "Upgrading Traduire to ${commit_version}"
    helm upgrade traduire ../chart/. --reuse-values --set GIT_COMMIT_VERSION=$commit_version   
}
else 
{

    $msg = "Manually create DNS (A) Record:"

    Get-AKSCredentials -AKSName $APP_K8S_NAME -AKSResourceGroup $APP_RG_NAME

    $kong_api_secret = New-APISecret -Length 25
    $app_insights_key = Get-AppInsightsKey -AppInsightsAccountName $APP_AI_NAME -AppInsightsResourceGroup $APP_RG_NAME
    $app_msi  = Get-MSIAccountInfo -MSIName $APP_SERVICE_ACCT -MSIResourceGroup $APP_RG_NAME
    $cogs = Get-CognitiveServicesAccount -CogsAccountName $APP_COGS_NAME -CogsResourceGroup $APP_RG_NAME

    # Install App using Helm Chart
    Write-Log -Message "Deploying Traduire to ${commit_version}"
    helm upgrade -i traduire ../chart/. `
        --set APP_NAME=$AppName `
        --set NAMESPACE=$APP_NAMESPACE `
        --set GIT_COMMIT_VERSION=$commit_version `
        --set WORKLOAD_ID.CLIENT_ID=$($app_msi.client_id) `
        --set WORKLOAD_ID.TENANT_ID=$($app_msi.tenant_id) `
        --set WORKLOAD_ID.NAME=$APP_SERVICE_ACCT `
        --set KEYVAULT.NAME=$APP_KV_NAME `
        --set STORAGE.NAME=$APP_SA_NAME `
        --set ACR.NAME=$APP_ACR_NAME `
        --set REGION=$($cogs.region) `
        --set APP_INSIGHTS.CONNECTION_STRING=$($app_insights_key.connection_string) `
        --set URIS.KONG.API_SECRET=$kong_api_secret `
        --set URIS.KONG.API_ENDPOINT=$APP_API_URI `
        --set URIS.FRONTEND_ENDPOINT=$APP_FE_URI
}

if($?){
    Write-Log ("{0} {1} @ {2}" -f $msg,$APP_API_URI, (Get-APIGatewayIP))
    Write-Log "Application successfully deployed. . ."
}
else {
    Write-Log ("Errors encountered while deploying application. Please review. Application Name: {0}" -f $AppName )
} 