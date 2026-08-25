# 最终报告 · 第七章大纲（中文）— 软件设计

> 对应骨架：`interim-report-writing-guide.md` §14 核心用例流、§20.2–20.3 Strategy、ADR-8  
> 图：`13-billing-strategy-class.puml`、`14-sequence-waybill-dual-track.puml`（可选 `08-sequence-import.puml`）  
> 正文：`final-report-ch7-software-design-zh.md`

## 硬约束

1. **双轨 = 应收（CustomerQuoteRules）vs 应付（PriceRules）**，按 BillDate/发货日取历史价；不是国内/国际
2. Strategy **已实现**：Tier / Overweight / Volumetric；Resolver 顺序 Volumetric → Tier → Overweight
3. 结算真相源 = `FeeCalculationEngine`；Assistant **不在**本章主路径
4. 体积重：引擎 + 单测已通；运单 Excel 主路径仍以实重为主（尺寸未普遍接入）——诚实写
5. 禁止：JSON 规则引擎、已与 PDA API 结算打通
6. 扩展仍 Planned：Step / Irregular 等，只写注册方式证明 OCP

## 小节

- 7.1 设计范围与核心用例
- 7.2 计费变体问题与 Strategy 动机
- 7.3 Strategy 类设计（对图 13）
- 7.4 开闭原则与扩展步骤
- 7.5 运单双轨时序（对图 14）
- 7.6 历史价与重量取整在设计中的位置
- 7.7 次要用例：价表导入（可选简述 + 图 08）
- 7.8 设计边界与后续
- 7.9 Evidence + 小结
