# Quick xlsx reader for inspection (no external deps)
param(
    [string]$XlsxPath = "$env:TEMP\cw93\wb.zip",
    [string[]]$Sheets = @('sheet7.xml','sheet8.xml','sheet9.xml','sheet10.xml','sheet5.xml','sheet2.xml')
)

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipPath = if (Test-Path $XlsxPath) { $XlsxPath } else { (Get-ChildItem -LiteralPath 'd:\tools\cloudwarehouse-csharp\excel' | Where-Object { $_.Name -like '*93*' } | Select-Object -First 1).FullName }
$temp = Join-Path $env:TEMP "cw-xlsx-read-$(Get-Random)"
New-Item -ItemType Directory -Path $temp -Force | Out-Null
if ($zipPath -like '*.xlsx') { Copy-Item -LiteralPath $zipPath "$temp\wb.zip"; $zipPath = "$temp\wb.zip" }
[System.IO.Compression.ZipFile]::ExtractToDirectory($zipPath, "$temp\x")

$ns = @{ m = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main' }
$shared = [xml](Get-Content "$temp\x\xl\sharedStrings.xml" -Encoding UTF8)
$strings = @()
$siNodes = Select-Xml -Path "$temp\x\xl\sharedStrings.xml" -XPath '//main:si' -Namespace @{ main = $ns.m }
foreach ($n in $siNodes) {
    $texts = Select-Xml -Xml $n.Node -XPath './/main:t' -Namespace @{ main = $ns.m }
    $strings += (($texts | ForEach-Object { $_.Node.InnerText }) -join '')
}

function Get-Col([string]$ref) { ($ref -replace '\d','') }
function Get-Row([string]$ref) { [int]($ref -replace '\D','') }

function Read-SheetRows($sheetFile, $maxRow = 8, $maxCol = 35) {
    $path = "$temp\x\xl\worksheets\$sheetFile"
    if (-not (Test-Path $path)) { return }
    $xml = [xml](Get-Content $path -Encoding UTF8)
    $nm = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $nm.AddNamespace('m', $ns.m)
    $grid = @{}
    foreach ($c in $xml.SelectNodes('//m:sheetData/m:row/m:c', $nm)) {
        $ref = $c.GetAttribute('r')
        $row = Get-Row $ref
        if ($row -gt $maxRow) { continue }
        $col = Get-Col $ref
        $colNum = 0
        foreach ($ch in $col.ToCharArray()) { $colNum = $colNum * 26 + ([int][char]$ch - [int][char]'A' + 1) }
        if ($colNum -gt $maxCol) { continue }
        $t = $c.GetAttribute('t')
        $v = $c.SelectSingleNode('m:v', $nm)
        $f = $c.SelectSingleNode('m:f', $nm)
        $val = ''
        if ($f) { $val = "=$($f.InnerText)" }
        elseif ($v) {
            if ($t -eq 's') { $val = $strings[[int]$v.InnerText] }
            else { $val = $v.InnerText }
        }
        if ($val) { $grid["$row,$colNum"] = $val }
    }
    Write-Host "`n=== $sheetFile ==="
    for ($r = 1; $r -le $maxRow; $r++) {
        $parts = @()
        for ($c = 1; $c -le $maxCol; $c++) {
            $k = "$r,$c"
            if ($grid.ContainsKey($k)) {
                $colLetter = [char]([int][char]'A' + $c - 1)
                if ($c -gt 26) { $colLetter = 'A' + [char]([int][char]'A' + ($c-27)) }
                $parts += "${colLetter}:$($grid[$k])"
            }
        }
        if ($parts.Count -gt 0) { Write-Host ("R{0}: {1}" -f $r, ($parts -join ' | ')) }
    }
}

$names = @{
    'sheet2.xml' = '账单统计 - 账单统计'
    'sheet5.xml' = '_参数_ - 表格 1'
    'sheet7.xml' = '_客户价格_ - 报价表'
    'sheet8.xml' = '_成本表_ - 成本表'
    'sheet9.xml' = '账单模版 - 账单明细'
    'sheet10.xml' = '2026-01 - 账单明细'
}
function Read-SheetDataRows($sheetFile, $fromRow, $toRow, $cols = 15) {
    $path = "$temp\x\xl\worksheets\$sheetFile"
    if (-not (Test-Path $path)) { return }
    $xml = [xml](Get-Content $path -Encoding UTF8)
    $nm = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $nm.AddNamespace('m', $ns.m)
    $grid = @{}
    foreach ($c in $xml.SelectNodes('//m:sheetData/m:row/m:c', $nm)) {
        $ref = $c.GetAttribute('r')
        $row = Get-Row $ref
        if ($row -lt $fromRow -or $row -gt $toRow) { continue }
        $col = Get-Col $ref
        $colNum = 0
        foreach ($ch in $col.ToCharArray()) { $colNum = $colNum * 26 + ([int][char]$ch - [int][char]'A' + 1) }
        if ($colNum -gt $cols) { continue }
        $t = $c.GetAttribute('t')
        $v = $c.SelectSingleNode('m:v', $nm)
        $val = ''
        if ($v) { if ($t -eq 's') { $val = $strings[[int]$v.InnerText] } else { $val = $v.InnerText } }
        if ($val) { $grid["$row,$colNum"] = $val }
    }
    for ($r = $fromRow; $r -le $toRow; $r++) {
        $parts = @()
        for ($c = 1; $c -le $cols; $c++) {
            $k = "$r,$c"
            if ($grid.ContainsKey($k)) { $parts += "$c=$($grid[$k])" }
        }
        if ($parts.Count -gt 0) { Write-Host ("R{0}: {1}" -f $r, ($parts -join ' | ')) }
    }
}

Write-Host "`n######## 客户报价 - 山西省 重量段样例 ########"
$shanxi = Read-SheetDataRowsOutput 'sheet7.xml' 3 200 11 | Where-Object { $_ -match '5=山西省' }
$shanxi | Select-Object -First 20

Write-Host "`n######## 客户报价 - 前35行(看重量段结构) ########"
Read-SheetDataRows 'sheet7.xml' 3 35 11

foreach ($s in $Sheets) {
    Write-Host "`n######## $($names[$s]) ########"
    if ($s -eq 'sheet10.xml') { Read-SheetRows $s 6 45; Read-SheetDataRows $s 3 8 20 }
    elseif ($s -eq 'sheet7.xml') { Read-SheetRows $s 8 12 }
    elseif ($s -eq 'sheet5.xml' -or $s -eq 'sheet2.xml') { Read-SheetRows $s 10 20 }
    else { Read-SheetRows $s 8 30 }
}

function Read-SheetDataRowsOutput($sheetFile, $fromRow, $toRow, $cols = 15) {
    $path = "$temp\x\xl\worksheets\$sheetFile"
    if (-not (Test-Path $path)) { return @() }
    $xml = [xml](Get-Content $path -Encoding UTF8)
    $nm = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $nm.AddNamespace('m', $ns.m)
    $grid = @{}
    foreach ($c in $xml.SelectNodes('//m:sheetData/m:row/m:c', $nm)) {
        $ref = $c.GetAttribute('r')
        $row = Get-Row $ref
        if ($row -lt $fromRow -or $row -gt $toRow) { continue }
        $col = Get-Col $ref
        $colNum = 0
        foreach ($ch in $col.ToCharArray()) { $colNum = $colNum * 26 + ([int][char]$ch - [int][char]'A' + 1) }
        if ($colNum -gt $cols) { continue }
        $t = $c.GetAttribute('t')
        $v = $c.SelectSingleNode('m:v', $nm)
        $val = ''
        if ($v) { if ($t -eq 's') { $val = $strings[[int]$v.InnerText] } else { $val = $v.InnerText } }
        if ($val) { $grid["$row,$colNum"] = $val }
    }
    $out = @()
    for ($r = $fromRow; $r -le $toRow; $r++) {
        $parts = @()
        for ($c = 1; $c -le $cols; $c++) {
            $k = "$r,$c"
            if ($grid.ContainsKey($k)) { $parts += "$c=$($grid[$k])" }
        }
        if ($parts.Count -gt 0) { $out += ("R{0}: {1}" -f $r, ($parts -join ' | ')) }
    }
    return $out
}
