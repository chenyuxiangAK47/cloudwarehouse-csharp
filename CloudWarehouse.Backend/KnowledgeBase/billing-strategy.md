# 计费策略模式（Strategy Pattern）

## 为什么用策略模式
中期评审要求对计费变体引入设计模式并展示详细设计。系统将原 if/else 计价重构为 Strategy Pattern。

## 结构
- IBillingStrategy：策略接口（CanHandle / Calculate）
- TierBillingStrategy：≤5kg 区间计费
- OverweightBillingStrategy：>5kg 续重计费
- VolumetricBillingStrategy：当体积重（L×W×H/6000）大于实际重量时，按体积重再走区间或续重
- IBillingStrategyResolver / DefaultBillingStrategyResolver：按注册顺序选择第一个 CanHandle 的策略（体积重优先）
- FeeCalculationEngine：可注入计费引擎
- DualTrackFeeCalculator：协调应收（客户报价）与应付（成本）两条限界上下文

## 扩展新计费类型
新增策略三步：实现 IBillingStrategy → 在 DI / CreateDefault 注册 →（如需）导入映射 BillingType。无需修改 BillImportService 主流程（开闭原则）。Step / 异形件 / 罚款等仍可按同样方式追加。

## 重要约束
计价结果必须可审计、可复现。内置规则检索工具只解释规则，不替代 FeeCalculationEngine 作为结算真相源。
