# 重量取整规则

## 正向计费取整
运单结算重量在计价前会取整：
- 实际重量 ≤ 0.3kg → 取整为 0.3kg
- 实际重量 ≤ 0.5kg → 取整为 0.5kg
- 实际重量 ≤ 5kg → 向上取整到整公斤（例如 2.19 → 3）
- 超过 5kg 按续重逻辑处理（面单费 + (重量-5)×续重单价）

## 区间计费
≤5kg 使用 BillingType=1 的公斤段单价（Strategy：TierBillingStrategy），总价 = 区间中转费 + 面单费。

## 续重计费
>5kg 使用 BillingType=2（Strategy：OverweightBillingStrategy），总价 = 面单费 + (重量-5)×续重单价。
