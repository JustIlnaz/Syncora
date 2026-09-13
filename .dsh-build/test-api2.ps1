$env:AVALONIA_TELEMETRY_OPTOUT='1'
$env:TMP='D:\Syncora\.dsh-build'
$env:TEMP='D:\Syncora\.dsh-build'
$out = dotnet test D:\Syncora\tests\Syncora.Api.Tests\Syncora.Api.Tests.csproj --no-restore --nologo 2>&1
$code = $LASTEXITCODE
$out | Out-File -FilePath 'D:\Syncora\.dsh-build\test-results.log' -Encoding utf8
Write-Output ("EXIT=" + $code)
Write-Output ($out | Select-String -Pattern 'passed|failed|error|Error|Прошло|Сбой|Ошибка' | Select-Object -Last 10 | ForEach-Object { $_.Line })
exit 0
