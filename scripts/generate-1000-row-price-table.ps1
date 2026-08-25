# Generate excel/perf-1000-rows-price-table.xlsx for UI stopwatch demos.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root "excel\perf-1000-rows-price-table.xlsx"
New-Item -ItemType Directory -Force -Path (Split-Path $out) | Out-Null
dotnet run --project (Join-Path $root "tools\PerfExcelGen\PerfExcelGen.csproj") -- $out
Write-Host "Next: open http://localhost:5001 → 成本价导入 → Preview this file → stopwatch for appendix."
