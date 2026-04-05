param(
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
Set-Location .\ElaraMacro

if ($Publish) {
    dotnet publish -c Release -r win-x64 --self-contained false
} else {
    dotnet build
}
