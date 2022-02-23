param(
  [Parameter(Mandatory=$true)]
  [string] $AppName,

  [Parameter(Mandatory=$true)]
  [string] $StorageAccountKey
)

Set-Location ./terraform
terraform init -backend=true -backend-config="access_key=$StorageAccountKey" -backend-config="key=trad8b43xz.terraform.tfstate"
terraform plan -out="$tfPlanFileName" -var-file="shared-cluster.tfvars"
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