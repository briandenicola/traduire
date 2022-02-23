[CmdletBinding(DefaultParameterSetName = 'Default')]
param(
    [Parameter(ParameterSetName = 'Default', Mandatory=$true)]
    [string] $AppName,
  
    [Parameter(ParameterSetName = 'Default', Mandatory=$true)]
    [string] $SubscriptionName,

    [Parameter(ParameterSetName = 'Default', Mandatory=$true)]
    [string] $AcrName
)

. .\modules\traduire_functions.ps1

$root   = (Get-Item $PWD.Path).Parent.FullName
$source = Join-Path -Path $root -ChildPath "source"

#Start-Docker
Start-Docker

Connect-ToAzure -SubscriptionName $SubscriptionName
Connect-ToAzureContainerRepo -ACRName $AcrName

#Build Source
$commit_version = Get-GitCommitVersion
Build-DockerContainers -ContainerName "${AcrName}.azurecr.io/traduire/api:${commit_version}" -DockerFile "$source/dockerfile.api" -SourcePath $source
Build-DockerContainers -ContainerName "${AcrName}.azurecr.io/traduire/onstarted.handler:${commit_version}" -DockerFile "$source/dockerfile.onstarted" -SourcePath $source
Build-DockerContainers -ContainerName "${AcrName}.azurecr.io/traduire/onprocessing.handler:${commit_version}" -DockerFile "$source/dockerfile.onprocessing" -SourcePath $source
Build-DockerContainers -ContainerName "${AcrName}.azurecr.io/traduire/oncompletion.handler:${commit_version}" -DockerFile "$source/dockerfile.oncompletion" -SourcePath $source
