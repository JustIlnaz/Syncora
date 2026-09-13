$env:AVALONIA_TELEMETRY_OPTOUT='1'
$env:TMP='D:\Syncora\.dsh-build'
$env:TEMP='D:\Syncora\.dsh-build'
$env:VSTEST_CONNECTION_TIMEOUT='300'
$out = dotnet test D:\Syncora\tests\Syncora.Api.Tests\Syncora.Api.Tests.csproj --no-restore --nologo --logger "console;verbosity=normal" 2>&1
$code = $LASTEXITCODE
$out | Out-File -FilePath 'D:\Syncora\.dsh-build\test-results.log' -Encoding utf8
Write-Output ("EXIT=" + $code)
exit 0
