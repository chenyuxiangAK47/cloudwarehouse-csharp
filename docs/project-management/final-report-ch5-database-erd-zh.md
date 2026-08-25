# 第五章 数据库设计与实体关系

> 在豆包稿基础上按 `database/*.sql` 校正后的定稿稿。  
> **必改点已写入正文**（相对原稿的差异见文末「相对豆包稿的校正」）。

第四章阐述了系统功能按 Sprint 迭代落地的执行路径；本章聚焦支撑上述业务能力的持久化层设计，重点说明多版本价格规则、一对多规则建模的实现思路，以及该数据模型如何为历史价查询、运单双轨结算提供底层能力支撑。

---

## 5.1 设计目标

本项目数据库设计围绕业务落地性与工程可维护性展开，核心目标如下：

- **业务全覆盖：** 支撑主数据管理、多版本价格规则存储、导入操作可重复执行、运单双轨结算金额可追溯四类核心场景的数据需求。
- **技术适配性：** 采用关系型数据模型，以显式 SQL 配合 Dapper 实现数据访问，兼顾执行性能与开发可控性。
- **架构一致性：** CloudWarehouse 全部业务表部署于同一数据库，契合模块化单体（Modular Monolith）架构；PDA 无订单报工模块采用独立数据库，实现上下文物理隔离。

---

## 5.2 概念模型与限界表分组

基于限界上下文划分，将数据库表分为四类逻辑分组。各组职责边界清晰；主数据通过编码唯一约束保障被引用的稳定性。

| 分组 | 表（以脚本为准） | 说明 |
|------|------------------|------|
| Master Data（主数据） | `Sites`, `Destinations`, `Customers`, `CustomerAccounts` | 站点、目的地、客户及客户账户；`SiteCode` / `DestCode` / `CustomerCode` / `AccountName` 等唯一约束保障引用完整性 |
| Pricing（应付成本价） | `PriceRules` | 按站点–目的地（lane）+ 生效日版本化；`BillingType` 区分阶梯与续重 |
| Pricing（应收客户报价） | `CustomerQuoteRules` | **独立于** `PriceRules`：按客户 + 省份（及可选快递类型）+ 生效日存储应收报价，支撑差异化定价 |
| Billing（结算运单） | `BillLines` | **当前无独立运单头表**；以运单行承载主从信息与应收/应付金额、毛利、导入批次号等，满足双轨对比与追溯 |
| Import 元数据 | 无独立任务表 | 文件级导入；状态随「预览–确认」工作流流转，重复导入由业务层替换策略保障 |

---

## 5.3 关键设计决策

### 一对多规则映射

单条 Excel 价表行对应 **多条** `PriceRules` 记录。供应商价表按重量区间拆分计费档位（如 0–0.3kg、0.3–0.5kg 等）并附加续重规则，导入时由映射逻辑拆解为多条规则行持久化，实现精细化计费匹配。

### 历史价版本控制

`PriceRules` 与 `CustomerQuoteRules` 均设置 `EffectiveDate`（生效日期）与可空的 `ExpiryDate`（失效日期）。结算时按运单 **发货日 / 账单日** 过滤匹配有效期内规则，使历史订单计价可复现。

### 导入替换策略（可重复导入）

成本价导入对同一 **lane（SiteId + DestId）** 采用「先删除该 lane 下全部规则，再插入本次解析结果」的替换逻辑（见 `PriceRuleImportService`），从而同一文件可重复提交而不残留旧档位行。  
说明：替换键是 **整条 lane**，并非「lane + 同一 EffectiveDate」细粒度 upsert；正文勿写成按生效日局部 upsert，以免与代码不符。

### 计费类型标识（规则表）

`PriceRules.BillingType`（INT）：**1 = 阶梯（tier）**，**2 = 续重（overweight）**，为 Strategy 计费引擎提供数据层路由依据。  
体积重（volumetric）在 **策略层** 处理，不一定单独占用一个 BillingType 行。  
注意：`BillLines.BillingType` 为业务含义字符串（如「正向计费」），与规则表 INT 枚举 **不是同一字段语义**。

### 索引与一对多基数

`PriceRules` **不能**对 `(SiteId, DestId, EffectiveDate)` 建唯一索引——同一 lane、同一生效日必须允许多条档位/续重行共存。早期错误唯一索引导致导入失败，已由 `database/fix-price-rules-index.sql` **删除错误唯一索引**；当前脚本侧以非唯一的 lane 查询索引（如 `SiteId + DestId`）支撑匹配。该经历本身是索引设计与业务基数对齐的证据。

---

## 5.4 实体关系图（ERD）

可视化文件：`docs/diagrams/07-erd.puml`（当前图以 Sites / Destinations / PriceRules 为核心）。

核心关联逻辑：

- **Site、Destination 与 PriceRule：** 一对多；同一站点–目的地组合可对应多条不同档位、不同生效周期的价格规则。
- **Customer 与报价：** 通过 `CustomerQuoteRules.CustomerId` 外键关联；**不是**把客户直接绑在 `PriceRules` 上。应收走客户报价表，应付走成本 `PriceRules`。
- **Customer 与账户：** `CustomerAccounts` 一对多挂在客户下，运单行可通过账户名关联客户。
- **运单持久化：** 以 `BillLines` 为结算明细事实表（唯一运单号等）；逻辑上可叙述「头–行」概念，但物理库 **本期无单独 Bill 头表**。

若 ERD 图尚未画出 `Customers` / `CustomerQuoteRules` / `BillLines` / `ExpiryDate`：以 `database/schema.sql`、`billing-schema.sql`、`customer-quote-schema.sql` 为准；报告中注明图可滞后于脚本并计划同步更新。

---

## 5.5 完整性与事务机制

- **约束：** FK（如 `PriceRules` → Sites/Destinations）、非空与唯一约束（如 `UQ_Sites_SiteCode`）构成完整性防线。
- **导入事务：** 价格规则提交包裹在 `SqlTransaction` 中；校验或写入失败则整批回滚，避免部分写入。
- **与预览工作流配合：** 预览阶段内存解析与校验、不落库；用户确认后才开事务写入，降低无效数据冲击。

---

## 5.6 PDA 数据存储（独立部署）

PDA 无订单报工使用独立库（如 `PDA_NoOrder`），与 CloudWarehouse **物理隔离**。概念表包括用户、机台/产线、开工记录、报工记录等。本期不做共库与实时同步，二者为同一工厂场景下相互独立的业务上下文。

---

## 5.7 本章证据清单

| 证据项 | 文件 / 位置 |
|--------|-------------|
| ERD | `docs/diagrams/07-erd.puml` |
| 核心库表 | `database/schema.sql` |
| 结算表 | `database/billing-schema.sql` |
| 客户报价表 | `database/customer-quote-schema.sql` |
| 错误唯一索引修复 | `database/fix-price-rules-index.sql` |
| 可选 | SSMS 表列表截图 |

---

## 5.8 本章小结

本章从设计目标、表分组、关键决策、实体关系与事务机制说明了 CloudWarehouse 与 PDA 的持久化方案。多版本价格规则与「一行 Excel → 多条 PriceRules」是历史价与双轨结算的底层支撑；应收与应付分表、运单以 `BillLines` 落库、PDA 独立库则分别对应差异化定价、结算追溯与上下文解耦。下一章将基于该数据模型展开多视角架构设计。

---

## 相对豆包稿的校正（作者自检）

| 原稿表述 | 校正 |
|----------|------|
| 运单头表 + 运单行表 | 仅有 `BillLines`，无独立 Bill 头表 |
| 客户报价绑在 PriceRules | 应收为 `CustomerQuoteRules`；应付为 `PriceRules` |
| lane+生效日先删后插 | 实际按 **整条 lane（SiteId+DestId）** 先删后插 |
| 组合索引覆盖站点+目的+生效日以提速 | `fix-*-index.sql` 主要是 **删掉错误唯一索引**；查询索引以 lane 为主 |
| 「规则匹配明细」完整落库 | `BillLines` 存金额与批次等；一般不存所匹配规则行 ID 明细 |
| Customers「含账户信息」 | 账户在独立表 `CustomerAccounts` |
