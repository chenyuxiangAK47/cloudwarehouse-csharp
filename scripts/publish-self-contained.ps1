
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path "$root\CloudWarehouse.sln")) { throw "Run from repo root scripts folder." }

$outDir = Join-Path $root "publish\CloudWarehouse-win-x64"
$zipPath = Join-Path $root "publish\CloudWarehouse-win-x64-self-contained.zip"
$stamp = Get-Date -Format "yyyyMMdd"
$zipDated = Join-Path $root "publish\CloudWarehouse-演示包-$stamp.zip"

Set-Location $root

Write-Host "Building self-contained win-x64 (约 120MB, 含 .NET 运行时)..."
dotnet publish CloudWarehouse.Backend\CloudWarehouse.Backend.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $outDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# 说明与启动
Copy-Item (Join-Path $root "publish\README-交给师傅.txt") $outDir -Force
Copy-Item (Join-Path $root "scripts\启动云仓.bat") $outDir -Force

# 数据库脚本（完整 database 目录）
$dbDest = Join-Path $outDir "database"
if (Test-Path $dbDest) { Remove-Item $dbDest -Recurse -Force }
Copy-Item (Join-Path $root "database") $dbDest -Recurse -Force

# 演示 Excel
$excelDest = Join-Path $outDir "excel"
New-Item -ItemType Directory -Force -Path $excelDest | Out-Null
$demoExcel = @(
    "93-成本表-单独.xlsx",
    "93-客户报价-单独.xlsx",
    "小二小店测试sheet.xlsx"
)
foreach ($name in $demoExcel) {
    $src = Join-Path $root "excel\$name"
    if (Test-Path $src) { Copy-Item $src $excelDest -Force }
}

# 示例配置（保留 appsettings.json 由 publish 输出，另附 example）
$exampleSrc = Join-Path $outDir "appsettings.example.json"
if (-not (Test-Path $exampleSrc)) {
    @'
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CloudWarehouse;User Id=sa;Password=Cw@Wh2026#Sa9xK;TrustServerCertificate=True;Encrypt=False"
  },
  "AllowedHosts": "*"
}
'@ | Set-Content -Path $exampleSrc -Encoding UTF8
}

Write-Host "Creating zip..."
foreach ($z in @($zipPath, $zipDated)) {
    if (Test-Path $z) { Remove-Item $z -Force }
    Compress-Archive -Path $outDir -DestinationPath $z -Force
    $mb = [math]::Round((Get-Item $z).Length / 1MB, 1)
    Write-Host "  $z ($mb MB)"
}

Write-Host ""
Write-Host "Done. 交给师傅:"
Write-Host "  1. 解压 $zipDated"
Write-Host "  2. database\install-all.bat"
Write-Host "  3. 改 appsettings.json"
Write-Host "  4. 双击 启动云仓.bat → http://localhost:5001"
