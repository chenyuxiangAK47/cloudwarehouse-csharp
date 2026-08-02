# CloudWarehouse.Pricing.Api

**用途：** NUS 学术演示 — 展示 Pricing 边界可独立部署为 HTTP 服务。  
**不进入** `publish-self-contained.ps1` / 师傅发布包。  
**主生产系统**仍为 `CloudWarehouse.Backend` 单体。

## 启动

```powershell
dotnet run --project CloudWarehouse.Pricing.Api
```

默认监听 `http://localhost:5002`。

## 示例

```powershell
$body = @{
  weight = 1
  row = @{
    price_0_0_3 = 1.6
    price_0_3_0_5 = 1.7
    price_0_5_1 = 2.1
    price_1_2 = 3.3
    baseFee = 2.5
    additionalUnitPrice = 0.5
  }
} | ConvertTo-Json -Depth 5

Invoke-RestMethod -Uri http://localhost:5002/api/calculate/preview -Method Post -Body $body -ContentType application/json
```

预期：`totalPrice` 与单体 `PriceCalculator` 一致（1kg → 2.1 + 2.5 = 4.6）。

## 依赖

仅引用 `CloudWarehouse.Pricing.Core`（无 SQL、无 ClosedXML）。
