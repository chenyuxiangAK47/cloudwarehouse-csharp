# Phase 2 结算相关表（billing + 客户报价）
param(
    [string]$Server = "localhost",
    [string]$Database = "CloudWarehouse",
    [string]$User = "sa",
    [string]$Password = "Cw@Wh2026#Sa9xK"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

function Invoke-SchemaFile($file) {
    if (-not (Test-Path $file)) { Write-Error "找不到 $file" }
    Write-Host "Applying $(Split-Path $file -Leaf) ..."
    sqlcmd -S $Server -U $User -P $Password -C -d $Database -i $file 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "sa 登录失败，尝试 Windows 身份验证 ..."
        sqlcmd -S $Server -E -C -d $Database -i $file
    }
    if ($LASTEXITCODE -ne 0) { Write-Error "sqlcmd 失败: $file" }
}

Invoke-SchemaFile (Join-Path $root "database\billing-schema.sql")
Invoke-SchemaFile (Join-Path $root "database\customer-quote-schema.sql")
Write-Host "Done. BillLines + CustomerQuoteRules 已就绪。"
