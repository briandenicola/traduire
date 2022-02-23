param(
  [Parameter(Mandatory=$true)]
  [string] $AppName,

  [Parameter(Mandatory=$true)]
  [string] $SubscriptionName
)

az account set -s $SubscriptionName

$tfPlanFileName = "{0}.plan" -f $AppName

Set-Location ./terraform
terraform init
terraform plan -out="$tfPlanFileName" -var-file="shared_cluster.tfvars"
terraform apply -auto-approve $tfPlanFileName

# echo Application name
if($?){
  Write-Host "------------------------------------"
  Write-Host ("Infrastructure built successfully. Application Name: {0}" -f $AppName)
  Write-Host "------------------------------------"
}
else {
  Write-Host "------------------------------------"
  Write-Host ("Errors encountered while building infrastructure. Please review. Application Name: {0}" -f $AppName )
  Write-Host "------------------------------------"
}
Set-Location ..