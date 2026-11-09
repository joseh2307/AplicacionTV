# Compila el APK de Android usando el SDK portátil instalado en D:\DevTools
# (el disco C: no tiene espacio para las cargas de trabajo de MAUI).
# Uso:  powershell -ExecutionPolicy Bypass -File .\build-apk.ps1

$ErrorActionPreference = 'Stop'
$tools = 'D:\DevTools'
$env:DOTNET_ROOT = "$tools\dotnet"
$env:PATH = "$tools\dotnet;$env:PATH"
$env:NUGET_PACKAGES = "$tools\nuget"
$env:DOTNET_CLI_HOME = "$tools\clihome"
$env:TEMP = "$tools\tmp"; $env:TMP = "$tools\tmp"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$jdk = (Get-ChildItem "$tools\jdk" -Directory | Select-Object -First 1).FullName

Set-Location $PSScriptRoot
& "$tools\dotnet\dotnet.exe" publish AplicacionTV.csproj -f net10.0-android -c Release `
    "-p:JavaSdkDirectory=$jdk" `
    "-p:AndroidSdkDirectory=C:\Program Files (x86)\Android\android-sdk" `
    -o .\apk
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Get-ChildItem .\apk\*-Signed.apk | ForEach-Object {
    Copy-Item $_.FullName .\TVMundial.apk -Force
    Write-Host "APK listo: $(Resolve-Path .\TVMundial.apk)"
}
