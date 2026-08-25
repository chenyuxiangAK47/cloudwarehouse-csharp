# 第七章 软件设计

Software Design: Strategy Pattern and Dual-Track Billing

第六章从多视角固定了 Modular Monolith 与限界上下文落点；本章进入 **可验证的详细设计**：用 Strategy Pattern 管理计费算法变体，用类级时序说明运单预览的 **应收 / 应付双轨** 协作，并标明历史价过滤、重量取整与对比校验在链路中的位置。这直接回应中期反馈对「设计模式 + 详细设计（类图 / 时序图）」的要求。

---

## 7.1 设计范围与核心用例

本章主用例是：**运单 Excel 导入预览 + 双轨计价对比**（管理端上传账单明细后，系统计算每行应收与应付，并与表内金额比对）。

| 纳入本章 | 不纳入主路径（另章或边界说明） |
|----------|--------------------------------|
| Strategy 类结构与 Resolver | 规则检索 Assistant（只查阅，不改金额） |
| `FeeCalculationEngine` 编排 | PDA 报工流程 |
| 双轨应用服务与时序 | 微服务拆分设计 |
| 重量取整、按日过滤规则 | 认证 / RBAC |

参与者与层次对齐第六章：Browser → `BillController` → `BillImportService` → 双轨计算器 → 成本/报价计算服务 → 计费引擎 → 具体 Strategy → SQL Server。

---

## 7.2 计费变体问题与 Strategy 动机

物流价表至少包含两类基础算法，并可能扩展第三类：

| 变体 | 业务含义 | 实现状态 | 策略类 |
|------|----------|----------|--------|
| 区间计费（Tier） | ≤5kg 落在离散重量档，取档内单价 + 面单费 | Done | `TierBillingStrategy` |
| 续重计费（Overweight） | >5kg 按续重单价计算 | Done | `OverweightBillingStrategy` |
| 体积重（Volumetric） | 当 L×W×H/6000 大于实重时，按体积重再走区间或续重 | Done（引擎 + 单测） | `VolumetricBillingStrategy` |
| 阶梯 / 异形件等 | 合同扩展 | Planned | 预留类名，未实现 |

Phase 1 可用条件分支覆盖前两类；若继续堆 `if/else`，每增一种合同算法都要改核心计算路径，违背开闭原则，也难以在报告中展示「可演进设计」。Phase 2 因此将算法抽成可替换策略，由解析器按上下文选择，引擎只负责 **过滤生效规则 → 解析策略 → 计算**。

决策记录：ADR-8（Billing Strategy Pattern，Implemented）。

---

## 7.3 Strategy 类设计

类图：`docs/diagrams/13-billing-strategy-class.puml`。代码主目录：`CloudWarehouse.Pricing.Core` 下 Billing 相关类型（并经 Backend DI 注册）。

### 7.3.1 关键类型职责

| 类型 | 职责 |
|------|------|
| `BillingContext` | 携带计费重、当前有效规则列表、可选长宽高与体积重除数 |
| `IBillingStrategy` | `CanHandle(context)` + `Calculate(context)` → `PriceCalculateResult` |
| `TierBillingStrategy` | 实重（或计费重）落在 ≤5kg 区间规则时计费 |
| `OverweightBillingStrategy` | >5kg 续重规则计费 |
| `VolumetricBillingStrategy` | 有尺寸且体积重 > 实重时接管；将计费重委托给区间或续重策略 |
| `IBillingStrategyResolver` / `DefaultBillingStrategyResolver` | 按注册顺序取第一个 `CanHandle == true` 的策略 |
| `FeeCalculationEngine` | 按 `orderDate`/`BillDate` 过滤 `EffectiveDate`/`ExpiryDate`，组装 Context，调用 Resolver |
| `FeeRuleCalculator` | 静态门面，委托默认引擎，兼容旧调用方 |
| `DualTrackFeeCalculator` | 应用层：同一运单行分别算应付与应收，再汇总对比 |

### 7.3.2 解析顺序

`CreateDefault()` / DI 注册顺序为：

1. `VolumetricBillingStrategy`（有尺寸且体积重更大时优先）  
2. `TierBillingStrategy`  
3. `OverweightBillingStrategy`  

顺序本身是设计的一部分：体积重必须先于按实重选择的区间/续重，否则扩展策略会被短路。

### 7.3.3 与数据模型的衔接

策略消费的是已过滤的 `PriceRule` 列表（应付轨来自 `PriceRules`；应收轨由客户报价映射为同等结构后再进引擎）。`BillingType` 1/2 与重量上下界决定档位匹配；**版本选择发生在引擎过滤阶段**，而不是写死在某个 Strategy 内部。

---

## 7.4 开闭原则与扩展步骤

**体积重**作为已落地的扩展证据：新增策略类 + 在 Resolver/DI 注册，物理重量-only 的调用方无需改算法分支；`FeeRuleCalculator` 对仅传重量的路径保持兼容。

新增计费类型的推荐三步：

1. 实现 `IBillingStrategy`（明确 `CanHandle` 条件，避免误抢上下文）；  
2. 注册到 `DefaultBillingStrategyResolver.CreateDefault()` 与 `Program.cs` DI（**注意顺序**）；  
3. 若业务需要新输入（如长宽高、异形标记），再扩展导入列或 API 上下文——不必改双轨编排骨架。

仍 Planned 的 Step / Irregular 等，只需重复上述注册路径，无需重写 `BillImportService`。

---

## 7.5 运单双轨时序（详细设计）

时序图：`docs/diagrams/14-sequence-waybill-dual-track.puml`。  
**双轨语义：应收 = 客户报价；应付 = 成本价。** 不是国内线 vs 国际线。

### 7.5.1 正常流程（预览）

1. 管理员选择运单 Excel，点击 Preview。  
2. `POST /api/Bill/waybill/preview` → `BillController` → `BillImportService.ProcessImportAsync(..., saveToDatabase=false)`。  
3. `WaybillExcelHelper` 解析双行表头（账单明细 + 成本明细）或标准模板，得到行集合及表内期望中转费（若有 L/X 列）。  
4. 加载 Sites、Destinations、Customers、CustomerAccounts 及规则相关缓存。  
5. 对每一行：校验运单号/省份/重量 → `WeightRounding` 取整 → 解析客户、站点（常由快递类型对应 SiteCode）、目的地。  
6. `DualTrackFeeCalculator.CalculateAsync(row)`：  
   - **应付轨：** `PriceRuleCalculateService` 按 SiteId/DestId + `BillDate` 查 `PriceRules` → `FeeCalculationEngine` → Resolver → Tier/Overweight（或 Volumetric）。  
   - **应收轨：** `CustomerQuoteCalculateService` 按 CustomerId/省份等 + `BillDate` 查 `CustomerQuoteRules` → 同样进入引擎与 Strategy。  
7. `BillLineTotals` 汇总应收/应付/毛利，并与 Excel 期望值对比（容差如 0.01），写入匹配标志。  
8. 返回预览结果：机器值 vs 表内值、一致/不一致计数，供 UI 展示。

确认入库时，同一计算链路可在 `saveToDatabase=true` 下将结果写入 `BillLines`（预览与落库共用编排，事务边界在导入服务）。

### 7.5.2 设计要点

- **双轨是应用层协调**，不是两套互拷的 if/else 计费核；两轨共享同一 Strategy 引擎，保证算法一致、财务语义分离。  
- **历史价**由引擎按账单日过滤规则生效期完成，避免「永远用最新价」导致对账不可复现。  
- **对比层**（`BillLineTotals`）把设计目标落到可演示证据：机器计算与师傅表可逐行核对。

---

## 7.6 历史价与重量取整在设计中的位置

| 关注点 | 设计位置 | 说明 |
|--------|----------|------|
| 历史价 | `FeeCalculationEngine.Calculate` 过滤段 | `EffectiveDate <= BillDate` 且未过 `ExpiryDate`；导入侧 `MasterPriceHistoryHelper` 可从多版本 Excel 列展开多段生效规则 |
| 重量取整 | 双轨计算前的 `WeightRounding` | 正向计费取整规则与试算/运单路径一致，避免 UI 试算与批量预览口径漂移 |
| 一对多规则行 | 第五章 + Mapper | 单行 Excel → 多条规则，供 Tier 按区间匹配 |

以上三者与 Strategy **正交**：换算法不必改版本过滤；改取整不必改 Strategy 接口。

---

## 7.7 次要用例：价表导入（简述）

成本价表导入时序见 `docs/diagrams/08-sequence-import.puml`（第六章已述分层流）。软件设计层补充一点：导入产出的多条 `PriceRules` 正是 Strategy 的输入数据面；**Import 上下文不实现计费算法**，只负责解析、映射与事务替换写入。客户报价导入由 Pricing 模块同类模式处理，写入 `CustomerQuoteRules`，供应收轨使用。

---

## 7.8 设计边界与后续

| 边界 | 诚实状态 |
|------|----------|
| 体积重 | 引擎 API 与单元测试已覆盖；运单 Excel 主路径仍以实重为主，尺寸列可按业务再接入 |
| Assistant | 不读写结算金额；正式结算以 `FeeCalculationEngine` 为准 |
| Step / Irregular / 附加费策略 | Planned，扩展路径已在 7.4 说明 |
| 与 PDA 结算打通 | 非本章、非本期设计范围 |

---

## 7.9 本章证据清单

| 证据 | 位置 |
|------|------|
| Strategy 类图 | `docs/diagrams/13-billing-strategy-class.puml` |
| 双轨时序图 | `docs/diagrams/14-sequence-waybill-dual-track.puml` |
| 导入时序（可选） | `docs/diagrams/08-sequence-import.puml` |
| 策略与引擎代码 | `CloudWarehouse.Pricing.Core` Billing 类型；`Modules/Billing/Services/DualTrackFeeCalculator.cs` |
| 单测 | `CloudWarehouse.Tests/BillingStrategyTests.cs` 等 |
| ADR | ADR-8 / 中期写作指南 §20.2–20.3 |

---

## 7.10 本章小结

本章以 Strategy Pattern 回应计费变体的可扩展性，以双轨时序回应结算协作的详细设计要求：应收与应付分轨、共享引擎、按日取历史价，并用表内金额对比形成可演示证据。开闭原则通过体积重策略的增量注册得到验证。下一章将转向 **DevSecOps / 质量与 CI**，说明这些设计如何被自动化测试与流水线约束，而不仅停留在图上。
