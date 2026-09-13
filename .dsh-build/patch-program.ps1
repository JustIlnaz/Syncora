
$p = 'D:\Syncora\src\Syncora.Api\Program.cs'
$text = [IO.File]::ReadAllText($p, [Text.Encoding]::GetEncoding(1251))

if ($text -notmatch 'AddRateLimiter') {
  $n = [char]10
  $rate = 'builder.Services.AddRateLimiter(options =>' + $n +
          '{' + $n +
          '    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;' + $n +
          '    options.AddFixedWindowLimiter("api", window =>' + $n +
          '    {' + $n +
          '        window.PermitLimit = 100;' + $n +
          '        window.Window = TimeSpan.FromMinutes(1);' + $n +
          '        window.QueueLimit = 0;' + $n +
          '    });' + $n +
          '});' + $n + $n +
          'builder.Services.AddEndpointsApiExplorer();'

  $useRate = 'app.UseRateLimiter();' + $n + $n + 'app.UseHttpsRedirection();'
  $useMw = 'app.UseMiddleware<Syncora.Middleware.ExceptionMiddleware>();' + $n + $n + 'app.MapControllers();'

  $text = $text.Replace('builder.Services.AddEndpointsApiExplorer();', $rate)
  $text = $text.Replace('app.UseHttpsRedirection();', $useRate)
  $text = $text.Replace('app.MapControllers();', $useMw)

  [IO.File]::WriteAllText($p, $text, [Text.UTF8Encoding]::new($false))
  Write-Output 'patched'
} else {
  Write-Output 'already'
}
