$env:AVALONIA_TELEMETRY_OPTOUT='1'
$env:TMP='D:\Syncora\.dsh-build'
$env:TEMP='D:\Syncora\.dsh-build'
dotnet build D:\Syncora\src\Syncora.Api\Syncora.Api.csproj --no-restore -v q --nologo 2>&1 | Select-Object -Last 30
exit $LASTEXITCODE
