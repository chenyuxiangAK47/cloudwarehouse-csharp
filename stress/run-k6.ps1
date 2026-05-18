# 用法: 先启动 API (dotnet run)，再执行 .\stress\run-k6.ps1
$base = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5001" }
Write-Host "Stress test against $base"
k6 run -e BASE_URL=$base "$PSScriptRoot\k6-smoke.js"
