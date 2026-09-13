
$p = 'D:\Syncora\src\Syncora.Api\Program.cs'
$text = [IO.File]::ReadAllText($p, [Text.UTF8Encoding]::new($false))
if ($text -notmatch 'Microsoft.AspNetCore.RateLimiting') {
  $text = $text.Replace('using System.Text;', 'using System.Text;' + [char]10 + 'using Microsoft.AspNetCore.RateLimiting;')
  [IO.File]::WriteAllText($p, $text, [Text.UTF8Encoding]::new($false))
  Write-Output 'using added'
} else { Write-Output 'already' }
