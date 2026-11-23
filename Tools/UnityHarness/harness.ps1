param([Parameter(ValueFromRemainingArguments=$true)][string[]]$HarnessArgs)
$ErrorActionPreference = 'Stop'
$pythonCommand = Get-Command python -ErrorAction Stop
& $pythonCommand.Source (Join-Path $PSScriptRoot 'harness.py') @HarnessArgs
exit $LASTEXITCODE
