param(
  [string]$OutputDirectory = (Join-Path $PSScriptRoot "release")
)

$ErrorActionPreference = "Stop"

$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path -LiteralPath $compiler)) {
  $compiler = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path -LiteralPath $compiler)) {
  throw "The .NET Framework C# compiler was not found."
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$appSource = Join-Path $PSScriptRoot "src\SnippetDropperApp.cs"
$setupSource = Join-Path $PSScriptRoot "src\SnippetDropperSetup.cs"
$appExe = Join-Path $OutputDirectory "SnippetDropper.exe"
$setupExe = Join-Path $OutputDirectory "SnippetDropper-Setup.exe"

& $compiler /nologo /target:winexe /platform:anycpu /optimize+ `
  /reference:System.dll `
  /reference:System.Core.dll `
  /reference:System.Drawing.dll `
  /reference:System.Windows.Forms.dll `
  /reference:System.Web.Extensions.dll `
  "/out:$appExe" `
  $appSource
if ($LASTEXITCODE -ne 0) {
  throw "SnippetDropper.exe compilation failed."
}

& $compiler /nologo /target:winexe /platform:anycpu /optimize+ `
  /reference:System.dll `
  /reference:System.Core.dll `
  /reference:System.Drawing.dll `
  /reference:System.Windows.Forms.dll `
  "/resource:$appExe,SnippetDropper.Payload.exe" `
  "/out:$setupExe" `
  $setupSource
if ($LASTEXITCODE -ne 0) {
  throw "SnippetDropper-Setup.exe compilation failed."
}

Get-Item -LiteralPath $appExe, $setupExe |
  Select-Object Name, Length, LastWriteTime
