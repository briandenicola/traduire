param(
  [Parameter(Mandatory=$true)]
  [string] $SubscriptionName,

  [Parameter(Mandatory=$true)]
  [string] $region
)

$today = (Get-Date).ToString("yyyyMMdd")
$tf_plan = "traduire.plan.{0}" -f $today

$current = $PWD.Path
$infra = Join-Path -Path ((Get-Item $PWD.Path).Parent).FullName -ChildPath "infrastructure"
Set-Location -Path $infra

az account set -s $SubscriptionName
terraform workspace new ${region}
terraform workspace select ${region}
terraform init
terraform plan -out="${tf_plan}" -var "location=${region}" -var 'zones=["1"]' #-var "branch=..."
terraform apply -auto-approve ${tf_plan}

$app_name=$(terraform output -raw APP_NAME)

if($?){
  Write-Host "------------------------------------"
  Write-Host "Infrastructure built successfully. Environment Name: ${app_name}"
  Write-Host "------------------------------------"
}
else {
  Write-Host "------------------------------------"
  Write-Host ("Errors encountered while building infrastructure. Please review. Environment Name: ${app_name}" )
  Write-Host "------------------------------------"
}

Set-Location -Path $current