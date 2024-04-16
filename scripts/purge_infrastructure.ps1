<#
.SYNOPSIS
This PowerShell Script will tear the infrastructure for the Traduire appplication.

.DESCRIPTION
Version - 1.0.0
This PowerShell Script will tear the infrastructure for the Traduire appplication..

.EXAMPLE
.\purge_environment.ps1 -AppName cheetah-37209 -SubscriptionName my_subscription

.PARAMETER AppNmame
Specifies the Application Name as outputed by the create_core_infrastructure.ps1 script

.PARAMETER SubscriptionName
The Subscription Name to deploy the Azure Resources. Mandatory parameter

#>
param(
  [Parameter(ParameterSetName = 'Default', Mandatory=$true)]
  [string] $AppName,

  [Parameter(Mandatory=$true)]
  [String] $SubscriptionName
)

. ./modules/traduire_functions.ps1
. ./modules/traduire_naming.ps1 -AppName $AppName

Connect-ToAzure -SubscriptionName $SubscriptionName
az group list --tag Application="${AppName}" --query "[].name" -o tsv | xargs -ot -n 1 az group delete -y --verbose --no-wait -n 

$current = $PWD.Path
$infra = Join-Path -Path ((Get-Item $PWD.Path).Parent).FullName -ChildPath "infrastructure"

Set-Location -Path $infra; Remove-TerraformState
Set-Location -Path $current