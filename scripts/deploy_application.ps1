[CmdletBinding(DefaultParameterSetName = 'Default')]
param(
    [Parameter(ParameterSetName = 'Default', Mandatory=$true)]
    [string] $AppName,
  
    [Parameter(ParameterSetName = 'Default', Mandatory=$true)]
    [string] $SubscriptionName,

    [Parameter(ParameterSetName = 'Default', Mandatory=$true)]
    [string] $Uri,

    [Parameter(ParameterSetName = 'Default', Mandatory=$true)]
    [string] $FrontEndUri,

    [Parameter(ParameterSetName = 'Default', Mandatory=$false)]
    [switch] $Upgrade
)

. ./modules/traduire_functions.ps1
. ./modules/traduire_naming.ps1 -AppName $AppName

#Connect to Azure and Log into ACR
Add-AzureCliExtensions

#Build and Push All Containers from Source 
Build-Application -AppName $AppName -AcrName $APP_ACR_NAME -SubscriptionName $SubscriptionName -Source $APP_SOURCE_DIR
return 0

# if($Upgrade) {
#     Write-Log -Message "Upgrading Traduire to ${commit_version}"
#     helm upgrade traduire helm/. --reuse-values --set commit_version=$commit_version 

#     if($?){
#         Write-Log ("Review DNS (A) Record: {0} - {1}" -f $uri, (Get-APIGatewayIP))
#         Write-Log "API successfully updated. Done"
#         return 0
#     }
    
# }
# else 
# {
#     #Get AKS Credential file
#     Get-AKSCredentials -AKSName $APP_K8S_NAME -AKSResourceGroup $APP_RG_NAME

#     #Generate Kong API secret
#     $kong_api_secret = New-APISecret -Length 25

#     #Get App Insights Key
#     $app_insights_key = (az monitor app-insights component show --app $APP_AI_NAME -g $CORE_RG_NAME --query instrumentationKey -o tsv)

#     #Get MSI Account Info
#     $app_msi  = New-MSIAccount -MSIName $APP_SERVICE_ACCT -MSIResourceGroup $APP_RG_NAME

#     #Get Cognitive Services Info
#     $cogs = New-CognitiveServicesAccount -CogsAccountName $APP_COGS_NAME -CogsResourceGroup $APP_RG_NAME

#     # Install App
#     Write-Log -Message "Deploying Traduire"
#     # helm upgrade -i traduire helm/. `
#     #     --set app_name=$AppName `
#     #     --set msi_client_id=$($app_msi.client_id) `
#     #     --set msi_selector=$APP_SERVICE_ACCT `
#     #     --set keyvault_name=$APP_KV_NAME `
#     #     --set storage_name=$APP_SA_NAME `
#     #     --set acr_name=$APP_ACR_NAME `
#     #     --set commit_version=$commit_version `
#     #     --set cogs_region=$($cogs.region) `
#     #     --set app_insights_key=$app_insights_key `
#     #     --set kong_api_secret=$kong_api_secret `
#     #     --set kong_api_uri=$Uri `
#     #     --set namespace=$APP_NAMESPACE `
#     #     --set frontend_uri="https://$FrontEndUri"

#     if($?){
#         Write-Log ("Manually create DNS (A) Record: {0} - {1}" -f $uri, (Get-APIGatewayIP))
#         Write-Log "API successfully deployed. Done"
#     }
#     else {
#         Write-Log ("Errors encountered while deploying API. Please review. Application Name: {0}" -f $AppName )
#     } 
# }