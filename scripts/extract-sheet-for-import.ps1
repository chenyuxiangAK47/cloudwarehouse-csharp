# 从师傅整本账单 Excel 抽出单个 sheet，供云仓导入（整本文件 ClosedXML 可能无法打开）
param(
    [string]$SourcePath = "",
    [string]$SheetName = "_客户价格_ - 报价表",
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not $SourcePath) {
    $SourcePath = (Get-ChildItem -LiteralPath (Join-Path $root "excel") -Filter "*93*" |
        Where-Object { $_.Extension -eq ".xlsx" } | Select-Object -First 1).FullName
}
if (-not $OutputPath) {
    $OutputPath = Join-Path $root "excel\93-客户报价-单独.xlsx"
}

if (-not (Test-Path -LiteralPath $SourcePath)) {
    Write-Error "找不到源文件: $SourcePath"
}

$py = @"
from openpyxl import load_workbook, Workbook
import sys
src_path, sheet_name, out_path = sys.argv[1], sys.argv[2], sys.argv[3]
src = load_workbook(src_path, data_only=True)
if sheet_name not in src.sheetnames:
    for n in src.sheetnames:
        if '报价' in n or '客户价格' in n:
            sheet_name = n
            break
ws = src[sheet_name]
dest = Workbook()
d = dest.active
d.title = '客户报价'
for row in ws.iter_rows(values_only=True):
    d.append(list(row))
dest.save(out_path)
print(f'OK: {out_path} (from sheet {sheet_name})')
"@

$tempPy = Join-Path $env:TEMP "cw-extract-sheet.py"
Set-Content -Path $tempPy -Value $py -Encoding UTF8
python $tempPy $SourcePath $SheetName $OutputPath
Write-Host "请上传: $OutputPath"
