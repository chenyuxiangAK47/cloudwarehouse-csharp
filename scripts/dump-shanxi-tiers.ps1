$temp = "$env:TEMP\cw93\x"
$ns = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'
$shared = [xml](Get-Content "$temp\xl\sharedStrings.xml" -Encoding UTF8)
$nm2 = New-Object System.Xml.XmlNamespaceManager($shared.NameTable)
$nm2.AddNamespace('m', $ns) | Out-Null
$strings = @()
foreach ($si in $shared.SelectNodes('//m:si', $nm2)) {
    $strings += (($si.SelectNodes('.//m:t', $nm2) | ForEach-Object { $_.InnerText }) -join '')
}
$xml = [xml](Get-Content "$temp\xl\worksheets\sheet7.xml" -Encoding UTF8)
$nm = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$nm.AddNamespace('m', $ns) | Out-Null
$grid = @{}
foreach ($c in $xml.SelectNodes('//m:sheetData/m:row/m:c', $nm)) {
    $ref = $c.GetAttribute('r')
    $row = [int]($ref -replace '\D', '')
    if ($row -lt 3 -or $row -gt 500) { continue }
    $col = ($ref -replace '\d', '')
    $cn = 0
    foreach ($ch in $col.ToCharArray()) { $cn = $cn * 26 + [int][char]$ch - [int][char]'A' + 1 }
    if ($cn -gt 11) { continue }
    $t = $c.GetAttribute('t')
    $v = $c.SelectSingleNode('m:v', $nm)
    if ($v) {
        $val = if ($t -eq 's') { $strings[[int]$v.InnerText] } else { $v.InnerText }
        $grid["$row,$cn"] = $val
    }
}
Write-Host '=== 山西省 全部重量段 ==='
for ($r = 3; $r -le 500; $r++) {
    if ($grid["$r,5"] -eq '山西省') {
        Write-Host ("R{0}: {1}-{2}kg 首重/中转={3} 续重={4}" -f $r, $grid["$r,6"], $grid["$r,7"], $grid["$r,10"], $grid["$r,11"])
    }
}
Write-Host '=== 样例：2.19kg 场景（取整后3kg）应匹配的重量段 ==='
for ($r = 3; $r -le 500; $r++) {
    if ($grid["$r,5"] -eq '山西省' -and $grid["$r,7"] -eq '3') {
        Write-Host ("匹配 R{0}: {1}-{2} 价格={3}" -f $r, $grid["$r,6"], $grid["$r,7"], $grid["$r,10"])
    }
}
