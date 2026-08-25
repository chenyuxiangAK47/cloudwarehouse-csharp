# CloudWarehouse Freight Settlement & PDA No-Order Reporting

## Final Internship Report (English)

Formal English translation of the Chinese master draft. **Figure placeholders remain in Chinese** as requested. Blank Phase-2 hour cells and Client Feedback placeholders are left for the author. No production HA / live CW–PDA API / AI settlement claims are intended.

---

## 6.1 架构风格与决策动机

CloudWarehouse 采用 **模块化单体（Modular Monolith）**：

- **物理上** 一个可部署单元（`CloudWarehouse.Backend`，ASP.NET Core + 同库 SQL Server）；
- **逻辑上** 按限界上下文拆成模块文件夹（`Modules/MasterData`、`Import`、`Pricing`、`Billing`、`Assistant` 等），边界清晰，便于后续按触发条件提取服务。

选型对照（摘要）：

| 选项 | 优点 | 对本项目的不适配 |
|------|------|------------------|
| 微服务 | 独立部署、团队并行 | Solo + 四周 MVP；分布式事务/运维开销过高 |
| 传统大泥球单体 | 交付快 | 边界模糊，难演进、难答辩“有设计” |
| **Modular Monolith** | 单进程交付快 + 模块边界 | 本期最优；拆分保留为有条件规划 |

结论：单体不是能力不足，而是在 **时间、人力、一致性需求** 约束下的有意决策（见 ADR 与 `docs/diagrams/01a-architecture-decisions-adr.puml`）。

并列交付的 PDA 无订单报工是 **另一套可部署系统**（Android + Spring Boot + 独立库），不塞进云仓进程，也不假装已是同一微服务网格。

---

## 6.2 逻辑架构与典型请求流

**图 6-1** 分层与模块依赖。

See `docs/diagrams/02-logical-architecture.puml` for the logical view. Hierarchical responsibilities:

| 层 | 代表组件 | 职责 |
|----|----------|------|
| Presentation | `wwwroot/index.html` | 管理端 Tab：主数据、导入、试算、运单、规则检索等 |
| API | 各模块 `*Controller` | HTTP 适配；校验入参；委托应用服务 |
| Application | Import / Calculate / BillImport / Assistant 等 Service | 编排用例、事务边界、跨 helper 协调 |
| Domain / Helpers | Excel 解析、`PriceRuleMapper`、`FeeCalculationEngine` + Strategy | 纯规则与计算；可单测 |
| Data | Dapper + SQL Server | 显式 SQL；同库事务 |

**Price list import (preview→confirm) data flow (summary):**

1. 浏览器上传 `.xlsx` → `ImportController`；
2. `PriceRuleImportService` 调用 `ExcelHelper` 探测标准/三级表头等格式；
3. `PriceRuleMapper` 将一行 Excel 展开为多条 `PriceRule`；
4. 预览：`save=false`，可挂钩试算，**不写库**；
5. 确认：在 `SqlTransaction` 内按 lane 删除旧规则并插入新规则，提交或整批回滚。

运单双轨、Strategy 编排的详细时序放在 **软件设计章**（类图 13、时序 14）；本章只固定“逻辑落点”：结算编排在 Pricing/Billing 应用层，持久化落在第五章所述表。

---

## 6.3 限界上下文与代码映射

**图 6-2** Master Data / Import / Pricing 等边界。

DDD 在本项目中的用法是 **务实的限界划分**，不是完整事件溯源或聚合魔法。上下文见 `docs/diagrams/05-ddd-bounded-contexts.puml`，并与代码目录对齐：

| 限界上下文 | 职责 | 代码落点（示意） | 关键持久化 |
|------------|------|------------------|----------|
| Master Data | 站点/目的地/客户等基准数据 | `Modules/MasterData` | Sites, Destinations, Customers, CustomerAccounts |
| Import | 成本价表解析、校验、事务提交 | `Modules/Import` | 写入 PriceRules（无独立 job 表；客户报价导入在 Pricing） |
| Pricing | 成本规则试算、客户报价、Strategy 引擎 | `Modules/Pricing` + Pricing.Core | PriceRules, CustomerQuoteRules |
| Billing | 运单导入、应收应付对比落库 | `Modules/Billing` | BillLines |
| Assistant | 内置规则 RAG（辅助 FAQ，非结算真相源） | `Modules/Assistant` + KnowledgeBase | 文件知识库为主 |

**语言隔离示例：** Import 上下文中的 `PriceTableRow`（Excel 行视图）经映射变为 Pricing 上下文中的持久 `PriceRule` 集合；二者不应在 UI 层混用同一套字段语义。

Phase 1 早期代码曾集中在根目录 Controllers；重构后以 `Modules/*` 表达边界——报告叙述以 **当前模块结构** 为准。

---

## 6.4 企业级上下文关系（含 PDA）

**图 6-3** 云仓与 PDA 独立；集成为 Planned。

工厂视角下存在两个产品系统，关系见 `docs/diagrams/16-enterprise-context-map.puml`：

| 关系 | 含义（诚实表述） |
|------|------------------|
| CloudWarehouse 内部模块 | 同库 Modular Monolith；模块间进程内调用 |
| PDA ↔ 产线/MES 相关能力 | PDA 侧已实现开工/报工与后端落库；与既有 MES 的衔接按 PDA 项目实际描述，**不夸大** |
| CloudWarehouse ↔ PDA | **Customer–Supplier / 集成 Planned**：共享的是工厂业务目标，**本期无生产级 API 共库或实时同步** |

答辩禁话：不要说“微服务已上线”或“云仓与 PDA 已打通结算链路”。

---

## 6.5 Physical deployment and operating topology

**图 6-4** 单实例拓扑；无 HA。

物理/部署图：`docs/diagrams/03-physical-architecture.puml`、`04-deployment-diagram.puml`。

**演示 / 开发期典型拓扑：**

| 节点 | 角色 | 说明 |
|------|------|------|
| 开发者/演示机 | 运行 Backend（Kestrel）+ 浏览器 | 管理端与 API 同机或同发布包 |
| SQL Server | CloudWarehouse 库 | 可本机或局域网实例；端口通常 1433 |
| GitHub Actions Runner | 短暂 CI 节点 | `dotnet test`、覆盖率 Artifact；云端无常驻业务库时对依赖 DB 的用例可跳过并解释 |
| PDA 设备 + PDA API/DB | 并列系统 | 霍尼韦尔终端 ↔ Spring Boot ↔ `PDA_NoOrder`（独立） |

Optional deliverables: Self-contained release package (`publish/`) to facilitate on-site demonstration; see the project management document for the IIS release checklist, ** does not mean ** that a production-level multi-active cluster has been built.

**安全姿态（MVP 诚实声明）：** 本地/受控演示场景；认证/RBAC 按 ADR 延期；CORS/HTTP 等按开发便利配置，生产前需收紧——细节在 DevSecOps / 风险章展开，本章只标明架构层未宣称零信任生产加固。

---

## 6.6 High availability and backup (honest statement)

| Aspects | Current status | Planning direction (not required to be delivered in this period) |
|------|----------|--------------------------|
| Application redundancy | Single instance, no load balancing | Multiple copies of containers + reverse proxy |
| Database Redundancy | Single Instance SQL Server | Managed HA / Always On and more |
| Backup | Manual `.bak` / script rebuild | Automatic backup with clear RPO |
| Disaster recovery | Git + `database/*.sql` reconstruction | Documented RTO + walkthrough |

中期要求“物理图写清基础设施/冗余”——正确做法是 **写清现状为无 HA**，而不是虚构集群。

---

## 6.7 微服务提取的触发条件（Planned）

模块边界已按上下文切开，但 **提取微服务需满足触发条件**，例如：

- 独立团队或独立发布节奏成为刚需；
- 某上下文（如计费计算）出现明显不同的扩展/性能特征；
- 运维与观测成本可被组织承担。

在 Solo 与当前业务量下，过早拆分会引入网络边界与分布式一致性成本，收益不足。故 M8「按触发条件提取」保持 **Planned**，与第四章里程碑一致。

---

## 6.8 本章证据清单

| 证据 | 位置 |
|------|------|
| 约束与 ADR 图 | `docs/diagrams/01*.puml` |
| 逻辑架构 | `02-logical-architecture.puml` |
| 物理 / 部署 | `03-physical-architecture.puml`、`04-deployment-diagram.puml` |
| DDD 限界 | `05-ddd-bounded-contexts.puml` |
| 企业 Context Map | `16-enterprise-context-map.puml` |
| 模块代码 | `CloudWarehouse.Backend/Modules/*` |
| 发布/演示包（可选截图） | `publish/`、启动脚本 |

---

## 6.9 本章小结

This chapter demonstrates Modular Monolith as a reasonable choice within constraints, and responds to "lack of architectural narrative" with logical layering, bounded contexts, and enterprise Context Maps; to infrastructure transparency requirements with an honest list of physical topology and **no HA**. PDAs exist side by side as independent contexts, and the integration remains Planned. The next chapter goes to **Software Design**: Strategy billing, dual-track timing and key class structures, refining the architecture into verifiable design products.

# Chapter 7 Software Design
Software Design: Strategy Pattern and Dual-Track Billing
Chapter 6 clarifies the structure of the modular monomer and the placement of the bounded context from the multi-perspective architecture level; this chapter focuses on the verifiable detailed design: managing the billing algorithm variants through the Strategy Pattern, using the class-level sequence diagram to illustrate the collaboration logic of the receivable/payable dual-track in the waybill preview scenario, and clarifying the position of historical price filtering, weight rounding, and amount comparison verification in the entire link. This chapter directly responds to the mid-term review's requirements for "design pattern implementation + detailed design output (class diagram/sequence diagram)".
## 7.1 Design scope and core use cases
The core design of this chapter revolves around the main use case: Waybill Excel import preview + dual-track pricing comparison - after the administrator uploads the bill details on the management side, the system automatically calculates the amount receivable and payable for each row, and compares it with the pre-filled amount in the Excel table.

| Included in the design scope of this chapter | Not included in the main path (other chapters or boundary descriptions) |
| --- | --- |
| Strategy class structure and strategy parser | Built-in rules RAG/Assistant (only for viewing, not involved in amount settlement) |
| FeeCalculationEngine billing arrangement | PDA reporting business process |
| Dual-track application service orchestration and timing logic | Design related to microservice splitting |
| Weight rounding rules, filtering historical prices by date | Identity authentication / RBAC authority system |

链路参与者与分层结构与第六章完全对齐：浏览器 → BillController → BillImportService → 双轨计算器 → 成本 / 报价计算服务 → 计费引擎 → 具体策略实现 → SQL Server 数据库。
## 7.2 计费变体问题与 Strategy 动机
物流价表的计费逻辑包含多类基础算法，且存在持续扩展的业务可能性。若以条件分支堆砌实现，将违背开闭原则，也无法支撑「可演进设计」的论证。

| 计费变体 | 业务含义 | 实现状态 | 对应策略类 |
| --- | --- | --- | --- |
| 区间计费（Tier） | 重量 ≤5kg 时匹配离散重量档位，按档内单价 + 面单费计算 | Done | TierBillingStrategy |
| 续重计费（Overweight） | 重量 >5kg 时按续重单价计算费用 | Done | OverweightBillingStrategy |

体积重计费（Volumetric）	当长 × 宽 × 高 / 6000 计算值大于实重时，按体积重执行区间或续重计费	Done（引擎 + 单元测试覆盖）	VolumetricBillingStrategy
阶梯计费 / 异形件计费等	合同定制化扩展场景	Planned	预留类名，暂未实现
Phase 1 阶段可通过条件分支覆盖前两类计费逻辑；若持续新增合同算法，每次扩展都需修改核心计算路径，代码可读性与可维护性将快速下降。因此 Phase 2 将各类计费算法抽象为可替换的策略，由解析器根据业务上下文自动选择，计费引擎仅负责「过滤生效规则 → 匹配策略 → 执行计算」的标准化流程。
该设计决策记录于 ADR-8（Billing Strategy Pattern，Implemented）。
## 7.3 Strategy 类设计

**图 7-1** Tier / Overweight / Volumetric + FeeCalculationEngine。

策略模式类图对应文件：docs/diagrams/13-billing-strategy-class.puml。核心代码位于 CloudWarehouse.Pricing.Core 项目的 Billing 命名空间下，并通过 Backend 依赖注入容器完成注册。
### 7.3.1 Key Type Responsibilities

| 类型名称 | 核心职责 |
| --- | --- |
| BillingContext | 计费上下文载体，承载计费重量、当前生效规则列表、可选的长宽高参数与体积重除数 |
| IBillingStrategy | 计费策略统一接口，定义 CanHandle(context) 适配判断与 Calculate(context) 计算方法，输出 PriceCalculateResult |
| TierBillingStrategy | 实重（或计费重）落在 ≤5kg 区间时执行档位计费 |
| OverweightBillingStrategy | 重量 >5kg 时执行续重计费 |
| VolumetricBillingStrategy | 存在尺寸参数且体积重大于实重时接管计算，将最终计费重委托给区间或续重策略 |
| IBillingStrategyResolver / DefaultBillingStrategyResolver | 策略解析器，按注册顺序遍历，返回第一个 CanHandle == true 的策略 |
| FeeCalculationEngine | 计费引擎核心，按订单日期 / 账单日期过滤规则的生效 / 失效期，组装计费上下文，调用解析器执行计算 |
| FeeRuleCalculator | 静态门面类，委托默认引擎执行计算，兼容历史调用方 |
| DualTrackFeeCalculator | 应用层服务，针对同一运单行分别计算应付与应收金额，汇总对比结果 |

### 7.3.2 策略解析顺序
CreateDefault() 方法与 DI 容器中的注册顺序为：
1.	VolumetricBillingStrategy（存在尺寸且体积重更大时优先接管）
2.	TierBillingStrategy
3.	OverweightBillingStrategy
解析顺序本身是设计的一部分：体积重判断必须先于基于实重的区间 / 续重策略，否则新增的体积重策略会被实重策略短路，无法生效。
### 7.3.3 与数据模型的衔接
所有策略消费的都是经过过滤的 PriceRule 结构列表：应付轨数据来自 PriceRules 表，应收轨数据从 CustomerQuoteRules 表读取并映射为统一结构后进入计费引擎。BillingType 标识与重量上下界共同决定档位匹配逻辑；规则版本选择发生在引擎过滤阶段，而非硬编码在单个 Strategy 内部，保证策略的纯粹性。
## 7.4 开闭原则与扩展步骤
体积重计费的落地是开闭原则的直接验证：仅通过新增策略类 + 在解析器 / DI 中注册，即可完成能力扩展，原有仅支持物理重量的调用方无需修改算法分支；FeeRuleCalculator 对仅传入重量的调用路径保持完全兼容。
新增计费类型的标准扩展步骤为三步：
1.	实现 IBillingStrategy 接口，明确定义 CanHandle 适配条件，避免错误抢占上下文；
2.	在 DefaultBillingStrategyResolver.CreateDefault() 与 Program.cs DI 中注册新策略，严格控制注册顺序；
3.	若业务需要新增输入参数（如长宽高、异形件标记），再扩展导入列或 API 上下文，无需修改双轨编排的核心骨架。
当前处于 Planned 状态的阶梯计费、异形件计费等能力，均可重复上述注册路径完成扩展，无需重写 BillImportService 等上层编排逻辑。
## 7.5 运单双轨时序（详细设计 — 类与对象级）

**Figure 7-2** Waybill preview dual-track settlement - **Class & Object Level Sequence Diagram**

> **回应导师反馈：** 本图不以「API 层 / 服务层 / 数据层」等组件框表示交互，而以**具体类实例**为生命线（如 `bc : BillController`、`importSvc : BillImportService`、`row : WaybillImportRow`），消息名与仓库源码方法一致，满足 *analysis → design* 中「关键用例的类级时序」要求。

源文件：`docs/diagrams/14-sequence-waybill-dual-track.puml`（PlantUML 导出 PNG 后插入 Word **图 7-2**）。

Dual-track semantics: receivable = customer-facing quotation (`CustomerQuoteRules`); payable = supplier-facing cost price (`PriceRules`); **not** receivable versus payable line distinction.

### 7.5.0 参与者与源码映射

| 时序图参与者（实例 : 类） | 源码位置 | 职责 |
| --- | --- | --- |
| `bc : BillController` | `Modules/Billing/Controllers/BillController.cs` | `PreviewWaybills` → `ProcessUpload(..., saveToDatabase=false)` |
| `importSvc : BillImportService` | `Modules/Billing/Services/BillImportService.cs` | 解析、主数据匹配、逐行编排 |
| `WaybillExcelHelper` | `Helpers/WaybillExcelHelper.cs` | `ReadWaybills` 解析 Excel |
| `row : WaybillImportRow` | `Models/WaybillImportRow.cs` | 循环内行对象，承载双轨金额与比对标记 |
| `dualCalc : DualTrackFeeCalculator` | `Modules/Billing/Services/DualTrackFeeCalculator.cs` | 应收/应付双轨协调（Facade） |
| `costSvc : PriceRuleCalculateService` | `Modules/Pricing/Services/PriceRuleCalculateService.cs` | 应付轨查 `PriceRules` |
| `quoteSvc : CustomerQuoteCalculateService` | `Modules/Pricing/Services/CustomerQuoteCalculateService.cs` | 应收轨查 `CustomerQuoteRules` |
| `feeEngine : FeeCalculationEngine` | `CloudWarehouse.Pricing.Core/Billing/FeeCalculationEngine.cs` | 历史价过滤 + 调用策略 |
| `resolver : DefaultBillingStrategyResolver` | `Pricing.Core/Billing/DefaultBillingStrategyResolver.cs` | `Resolve(BillingContext)` 选策略 |
| `tier : TierBillingStrategy` | `Pricing.Core/Billing/TierBillingStrategy.cs` | 示例具体策略（亦可为 Overweight/Volumetric） |
| `BillLineTotals` | `Modules/Billing/Helpers/BillLineTotals.cs` | 静态汇总与 `ApplyComparison` |

### 7.5.1 预览正常流程
1.	管理员选择运单 Excel 文件，点击「预览」按钮；
2.	请求 POST /api/Bill/waybill/preview 到达 BillController，转发至 BillImportService.ProcessImportAsync(..., saveToDatabase=false)；
3.	WaybillExcelHelper 解析双行表头（账单明细 + 成本明细）或标准模板，得到行数据集合，并提取表内期望中转费（若文件包含对应列）；
4.	预加载站点、目的地、客户、客户账户及规则相关的缓存数据；
5.	逐行处理：校验运单号、省份、重量等字段 → 执行 WeightRounding 重量取整 → 解析匹配客户、站点（通常由快递类型对应 SiteCode）、目的地；
6.	调用 DualTrackFeeCalculator.CalculateAsync(row) 执行双轨计算： 
o	应付轨：PriceRuleCalculateService 按 SiteId/DestId + 账单日期查询 PriceRules → 传入 FeeCalculationEngine → 策略解析器 → 匹配 Tier/Overweight/Volumetric 策略计算；
o	应收轨：CustomerQuoteCalculateService 按 CustomerId / 省份 + 账单日期查询 CustomerQuoteRules → 传入同一套计费引擎与策略体系计算；
7.	调用 BillLineTotals 进行汇总与对比，计算应收、应付、毛利，并与 Excel 表内期望值进行容差对比（默认容差 0.01），标记匹配结果；
8.	返回预览结果集，包含系统计算值、表内原值、一致 / 不一致统计，供前端 UI 展示。
确认入库时，同一计算链路可在 saveToDatabase=true 模式下将结果写入 BillLines 表；预览与落库共用同一套编排逻辑，事务边界由导入服务统一控制。
### 7.5.2 设计要点
•	双轨是应用层协调模式，并非两套重复的条件分支计费内核；两条轨道共享同一套 Strategy 计费引擎，保证算法一致性的同时实现财务语义分离。
•	历史价能力由引擎层统一实现，按账单日期过滤规则生效周期，避免「永远使用最新价格」导致的对账不可复现问题。
•	对比校验层（BillLineTotals） 将设计目标落地为可演示证据：系统计算结果与人工价表可逐行核对，直观验证计费准确性。
## 7.6 历史价与重量取整在设计中的位置
历史价过滤、重量取整、一对多规则映射三类逻辑与策略体系保持正交，独立演化互不影响，具体设计位置如下：

| 关注点 | 设计位置 | 说明 |
| --- | --- | --- |

历史价过滤	FeeCalculationEngine.Calculate 规则过滤阶段	过滤条件为 EffectiveDate <= 账单日期 且未超过 ExpiryDate；导入侧可通过 MasterPriceHistoryHelper 从多版本 Excel 列展开为多段生效规则
重量取整	双轨计算前的 WeightRounding 统一处理	正向计费取整规则在试算、运单批量预览等所有路径保持一致，避免 UI 试算与批量预览口径不一致
一对多规则行映射	第五章数据模型 + Mapper 组件	单行 Excel 价表拆解为多条规则行，供 Tier 策略按重量区间匹配
## 7.7 次要用例：价表导入（简述）
成本价表导入时序对应文件：docs/diagrams/08-sequence-import.puml，第六章已说明分层数据流。从软件设计角度补充：导入流程产出的多条 PriceRules 记录正是 Strategy 计费引擎的输入数据源；Import 上下文不实现任何计费算法，仅负责格式解析、字段映射与事务化替换写入。客户报价导入遵循同类设计模式，写入 CustomerQuoteRules 表，供应收轨计费使用。
## 7.8 Design boundaries and subsequent planning

| Design items | Current real status |
| --- | --- |

体积重计费	引擎 API 与单元测试已完整覆盖；运单 Excel 主路径仍以实重计费为主，尺寸列尚未普遍接入业务流程
内置规则 RAG	不读写任何结算金额；正式结算以 FeeCalculationEngine 的计算结果为唯一真相源
Step 阶梯 / 异形件 / 附加费策略	Planned 状态，扩展路径已在 7.4 节说明
与 PDA 系统结算打通	非本章、非本期交付设计范围
## 7.9 本章证据清单

| 证据项 | 文件位置 |
| --- | --- |
| Strategy 计费策略类图 | docs/diagrams/13-billing-strategy-class.puml |

Waybill dual-track settlement sequence diagram docs/diagrams/14-sequence-waybill-dual-track.puml
Price list import sequence diagram (optional) docs/diagrams/08-sequence-import.puml
Policy and engine core code Billing type under CloudWarehouse.Pricing.Core; Modules/Billing/Services/DualTrackFeeCalculator.cs
Unit test cases CloudWarehouse.Tests/BillingStrategyTests.cs, etc.
Architectural Decision Record ADR-8/Interim Writing Guidelines §20.2–20.3
## 7.10 Summary of this chapter
This chapter responds to the scalability requirements of billing variants with a strategy model, and responds to the detailed design requirements of settlement collaboration with dual-track timing: separate track management of receivables and payables, sharing of a unified billing engine, obtaining historical prices by date, and forming demonstrable verification evidence through comparison of amounts in the table. The open-closed principle is practically verified through the incremental registration of the volumetric weight strategy. The next chapter will turn to DevSecOps and quality assurance systems to explain how the above design can form quality constraints through automated testing and CI pipelines, rather than just staying at the design drawing level.
# Chapter 8 DevSecOps and Quality Assurance
DevSecOps and Quality Assurance
第七章完整呈现了策略模式与双轨结算的详细设计；本章说明上述设计如何通过自动化质量门禁与安全扫描形成刚性约束，而非仅停留在类图与时序图层面。本章遵循贯穿中期评审与答辩的统一原则：有实证支撑的表述为已落地，未实现的能力明确标注缺口与规划路径，不宣称已建成完整 DevSecOps 平台或生产级持续交付能力。
## 8.1 本章范围与本项目口径下的 DevSecOps
针对本次实习交付的项目体量，DevSecOps 落地为四个务实层级，而非营销概念：

| 层级 | 核心含义 | 本仓库落地状态 |
| --- | --- | --- |
| 持续集成（CI） | 代码提交 / PR 触发自动构建与测试 | Done（基于 GitHub Actions 实现） |
| 质量门禁 | 单元测试、集成测试、轻量并发与性能冒烟测试 | Done |
| 安全扫描 | 静态应用安全测试（CodeQL）+ NuGet 依赖脆弱性排查 | Done（依赖扫描配置为 continue-on-error，以留存证据为主，不作为硬阻断门禁） |
| 安全基线 | 文件上传限制、配置脱敏示例、演示环境安全假设 | 部分 Done；身份认证等能力为 Planned 状态 |

本章质量与安全证据主要来自 CloudWarehouse 系统的 .github/workflows/ 目录下的流水线配置。并列交付的 PDA 无订单报工系统为独立体系：其安全基于 API 访问、独立数据库、内网演示环境假设，不与 CloudWarehouse 混称为一套已打通的统一安全网格。
流水线活动图对应文件：docs/diagrams/09-cicd-pipeline.puml。
流水线活动图对应文件：`docs/diagrams/09-cicd-pipeline.puml`。

**图 8-1** CI 为主；完整 CD 未宣称。

**图 8-2** 绿勾证据。

**图 8-3** 勿在正文写死百分比口号。

## 8.2 Continuous integration pipeline
Core workflow file: .github/workflows/ci.yml.
Trigger rules: The pipeline is triggered for push and pull_request operations of the main/master branch.
Running environment: ubuntu-latest Runner + .NET SDK 9.0.x, forming cross-platform verification with the local Windows development environment.
Standard execution steps:
1. Execute actions/checkout to pull the code;
2. Configure the .NET operating environment through setup-dotnet;
3. Execute dotnet restore CloudWarehouse.sln to restore project dependencies;
4. Execute dotnet test in Release mode, cooperate with Coverlet and coverlet.runsettings to collect cross-platform code coverage, and output the results to the ./coverage directory;
5. Install the ReportGenerator tool to generate coverage reports in HTML format and text summary format, and output them to the coveragereport/ directory;
6. Print the coverage summary Summary.txt to the pipeline log;
7. Execute dotnet list ... package --vulnerable --include-transitive to scan dependency vulnerabilities, and write the results to vulnerable-packages.txt. If the scan fails, the pipeline will not be blocked, and the product will still be uploaded normally;
8. Upload the build product Artifact: including coverage-report, coverage-cobertura, and nuget-vulnerable-scan files.
This pipeline solves the quality risk of "only the local environment can run and pass" and ensures unified and objective verification results before the main code is merged.
It needs to be clearly stated: This pipeline belongs to the category of CI + quality/security products and does not implement complete continuous deployment (CD) for production environments. The current system release is mainly based on self-contained release packages, manual deployment and checklists. For relevant instructions, please see the architecture chapter and release documents.
## 8.3 测试金字塔与关键验证类型
本项目测试体系遵循测试金字塔原则，从下到上分为三层，验证重点与第五章数据库设计、第七章软件设计一一对应：

| 测试层级 | 实现项目与手段 | 核心验证重点 |
| --- | --- | --- |
| 单元测试 | CloudWarehouse.Tests 项目 | 覆盖策略模式（区间 / 续重 / 体积重）、Excel 解析与字段映射、历史价辅助逻辑、重量取整规则、规则检索逻辑、解析性能冒烟测试等 |
| 集成测试 | CloudWarehouse.IntegrationTests 项目 + WebApplicationFactory | 覆盖主数据管理、价表导入、客户报价、运单结算等模块的 HTTP API 全链路 |
| 轻量压力 / 并发测试 | 如 StressLoadTests 测试用例 | 针对模板下载、导入预览等高频场景做并发冒烟验证，属于演示级验证，不构成生产级 SLA 认证 |

环境适配诚实策略：GitHub Actions 云端 Runner 通常无常驻 SQL Server 实例。对于依赖真实数据库的集成测试用例，通过 DatabaseAvailability 等判断逻辑实现可解释跳过：数据库不可达时自动跳过对应用例，避免流水线出现环境导致的 “假红”；本地开发环境配备 SQL Server 时则执行完整测试链路。该设计是环境适配方案，而非隐瞒测试失败。
测试体系的设计目标优先保障计费引擎重构与双轨逻辑的可回归性，而非追求虚高的覆盖率数字。

**证据来源（可复现）：** 仓库 `https://github.com/chenyuxiangAK47/cloudwarehouse-csharp` → **Actions** → workflow **CI** → 打开最新绿色 run，查看 **Test with coverage** 步骤日志；本地复现命令为 `dotnet test CloudWarehouse.sln`（完整日志备份：`docs/project-management/artifacts/dotnet-test-full.txt`）。

### 8.3.1 单元测试与集成测试执行结果（2026-08-25 本地复现）

本项目为 **Modular Monolith**（模块化单体），**非微服务架构**；下表按**逻辑模块**分组展示测试结果，而非按虚构的 “microservice” 拆分。

**汇总（`dotnet test CloudWarehouse.sln`，Windows 本机，Release）：**

| 测试项目 | 通过 | 失败 | 跳过 | 耗时（约） |
| --- | ---: | ---: | ---: | --- |
| `CloudWarehouse.Tests`（单元） | **83** | 0 | 0 | 6.6 s |
| `CloudWarehouse.IntegrationTests`（API 集成） | **27** | 0 | 0 | 8.4 s |
| `CloudWarehouse.E2ETests`（Playwright UI） | **4** | 0 | 0 | ~2 s |
| **合计** | **114** | **0** | **0** | ~17 s |

**图 8-4（建议截图）：** GitHub Actions → CI → 绿色 run → **Test with coverage** 日志末尾 `Passed` 汇总；或本地终端同一命令的输出末尾（附录 **A-12**）。

**按模块的代表性用例（单元测试 `CloudWarehouse.Tests`）：**

| 逻辑模块 | 代表性测试类 | 验证重点 | 结果 |
| --- | --- | --- | --- |
| Pricing / Strategy | `BillingStrategyTests`, `PriceCalculatorTests`, `FeeCalculationPerfSmokeTests` | Tier / 续重 / 体积重策略、`FeeCalculationEngine`、注入解析器 | 全部通过 |
| Import / Excel | `ExcelHelperTests`, `WaybillExcelHelperTests`, `SiteExcelHelperTests`, `DestinationExcelHelperTests`, `CustomerExcelHelperTests`, `CustomerQuoteExcelHelperTests` | 价表/运单/主数据 Excel 解析与模板往返 | 全部通过 |
| Bill / 双轨 | `BillLineTotalsTests`, `Waybill93FileTests`, `BillImportServiceRegionTests` | 应收应付汇总、表内对比容差、省份归一化 | 全部通过 |
| 历史价 / 规则映射 | `MasterPriceHistoryHelperTests`, `PriceRuleMapperTests`, `MasterCostExcelTests` | 多版本价表展开、一对多规则映射、93 成本样例 | 全部通过 |
| Assistant（词法 FAQ） | `QuoteAssistantTests`, `QuoteAssistantEvalTests` | 检索命中与引用；**不替代计费引擎** | 全部通过 |

**集成测试（`CloudWarehouse.IntegrationTests` + `WebApplicationFactory`）：**

| API 域 | 代表性测试类 | 验证重点 | 结果 |
| --- | --- | --- | --- |
| Import | `ImportApiTests` | 价表预览/导入、非法扩展名、事务预览 | 全部通过 |
| Bill | `BillApiTests` | 运单预览、双轨计费（DB 可用时）、导出 | 全部通过 |
| Customer Quote | `CustomerQuoteApiTests` | 客户报价预览/导入 | 全部通过 |
| Master data / 静态页 | `SiteAndStaticApiTests` | Site/Destination/Customer API、`index.html` 可访问 | 全部通过 |
| 轻量并发 | `StressLoadTests` | 见 §8.7 | 全部通过 |

### 8.3.2 端到端测试（E2E）与 Playwright

| Method | Status | Description |
| --- | --- | --- |
| **Playwright UI Automation** | **Implemented** | Standalone project `CloudWarehouse.E2ETests`: Kestrel dynamic port + Chromium headless, class `UiSmokeE2ETests` Total **4** smoke items (Home Navigation, Waybill Import, Customer Quote Import, Rule RAG Panel). Local reproduction: `dotnet test CloudWarehouse.E2ETests --filter Category=E2E`; log backup `docs/project-management/artifacts/e2e-playwright-test.txt`. |
| **API level E2E** | **Implemented** | `WebApplicationFactory` makes HTTP requests to the entire `/api/*` link, covering critical paths such as import, dual-track shipping, and master data (27 integration tests in §8.3.1). |
| **Manual E2E (Demonstration)** | **Implemented** | Evaluation video App Demo: waybill preview, price list import, PDA start and report; report appendix **A-04～A-06, A-08** are screenshot evidence. |
| **Planned** | Follow-up | Extended Playwright: file upload + Preview result assertion, cross-browser matrix, visual regression. |

**Technology stack:** Microsoft.Playwright 1.50 + xUnit; CI Execute `playwright.sh install --with-deps chromium` on `ubuntu-latest` followed by `dotnet test CloudWarehouse.sln` (see `.github/workflows/ci.yml`).

**图 8-4b（建议截图）：** 本地或 Actions 日志中 `CloudWarehouse.E2ETests` 四项 `Passed`；或 Playwright trace（若启用）。

## 8.4 Coverage evidence (expression specification)
Code coverage is collected by the Coverlet tool, an HTML report is generated by ReportGenerator, and is archived as an Artifact for each CI build. Two types of screenshots can be pasted in the appendix of the report as evidence:
• Green check screenshot of a successful GitHub Actions run;
• Screenshot of the Summary page in the coverage report.
It is forbidden to write in the text "coverage >80%" and other absolute expressions that cannot be dynamically updated with the build. The specifications are as follows: the coverage report is traceable and queried with the Artifact built by each CI. The core business modules (Pricing Core billing engine, Import analysis module, Bill dual-track settlement module) all have automatic use case coverage protection; the specific coverage value is based on the actual results corresponding to the construction day in the appendix screenshot.

### 8.4.1 Interpretation of coverage (how to read CI Artifact)

每次 CI 成功构建会上传 **`coverage-report`**（HTML）与文本 **Summary**。阅读时应关注：

| Region | Expectation | Reason |
| --- | --- | --- |
| `CloudWarehouse.Pricing.Core` / Billing | Higher | Strategy, `FeeCalculationEngine` has intensive unit testing |
| Import Helpers / Excel Parsing | Higher | Multi-format Excel single test + 93 sample file tests |
| `Modules/Billing` (dual-track arrangement) | Medium to high | Integration test + `BillLineTotals` single test |
| `wwwroot/index.html`, thin Controller | Low | MVP focuses on API testing; **Playwright smoke covers the main navigation and three major panels** (§8.3.2) |
| Assistant module | Medium | `QuoteAssistantTests` + Eval golden set |

**图 8-3 / 附录 A-02：** 从 GitHub Actions 下载当次 `coverage-report`，截取 Summary 总览与上述模块行——**以该构建日数字为准**，正文只作定性解读。
## 8.5 SAST and dependent supply chain scanning
### 8.5.1 CodeQL 静态安全测试（SAST）
对应工作流文件：.github/workflows/codeql.yml（任务名 CodeQL SAST）。
•	触发规则：主干分支的 push / PR 操作触发，同时配置每周定时（cron）全量扫描；
•	扫描语言：C#；
•	查询规则集：security-and-quality；
•	执行步骤：初始化 CodeQL 环境 → 还原依赖并构建项目 → 执行 codeql-action/analyze 分析。
SAST 用于在代码合并前发现可自动识别的缺陷模式；不能替代人工设计评审，也不等同于动态渗透测试（DAST）。扫描发现问题的标准处理流程：修复缺陷 → 重跑流水线至通过 → 再合入主干。
### 8.5.2 NuGet 脆弱依赖扫描
CI 流水线中执行 dotnet list package --vulnerable --include-transitive 命令扫描全量依赖（含传递依赖）的已知漏洞，扫描结果随 Artifact 归档。
当前策略为可见性优先（配置 continue-on-error: true）：优先保障主干集成流程通畅，同时留存供应链风险清单供人工跟进修复；后续可根据业务要求调整为阈值门禁模式。

### 8.5.3 安全扫描结果与处置（Before → Resolution decision）

导师要求 *before with vulnerabilities … and after resolution*。本项目**不伪造「修完变 0 漏洞」截图**，而是按工程实践给出 **扫描 → 评估 → 处置决策** 闭环。

**(1)CodeQL SAST (static code)**

| 阶段 | 证据 | 结果 |
| --- | --- | --- |
| Before / After | GitHub Actions → workflow **CodeQL SAST** → 最近一次绿色 run（如 #7，2026-08-24） | **0 open Critical/High** 代码层告警（以 Actions Summary 为准） |
| Resolution | 无待修复 CodeQL alert 时，处置为 **monitor on each push/PR + weekly cron** | 附录 **A-03** 截图 |

**(2) NuGet dependency scanning (supply chain)**

| 阶段 | 发现 | 处置（Resolution decision） |
| --- | --- | --- |
| **Before（可见性扫描）** | 传递依赖 `Azure.Identity` 1.11.3、`Microsoft.Identity.Client` 4.60.3 在 Backend/Tests/IntegrationTests 上报告 **Moderate**（GHSA-m5vv-6r4h-3vj9） | CI 步骤 `NuGet vulnerability scan` + Artifact `nuget-vulnerable-scan`；见 QA 页 NuGet 段落 |
| **After（本期决策，非包升级）** | 未在本期强行升级传递依赖（避免牵一发动全身） | **Risk acceptance for MVP：** 系统为内网/demo、无对外暴露的生产多租户面；JWT/RBAC 未上线，攻击面以「受控演示机」为边界；项记入 **Planned**：下一迭代随 `Microsoft.Data.SqlClient`/Identity 栈统一升级后 **rescan** |
| **若导师追问「after」** | 不是「漏洞数变 0」，而是 **documented resolution**：已记录、已评估、已排期，CI 保持可见性 | 附录 **A-14** + https://chenyuxiangAK47.github.io/cloudwarehouse-csharp/ |

**（3）DAST** — **Done**: workflow dast-zap.yml runs OWASP ZAP baseline against the published Backend; artefact dast-zap-baseline.

**图 8-6（建议截图）：** CodeQL 绿勾 +（可选）Security 标签页 overview；NuGet 扫描 Moderate 列表（上半即可）。
当前已落地的应用层安全控制如下，属于 MVP 阶段务实安全基线：

| 安全控制项 | 说明 |
| --- | --- |
| 上传文件白名单 | 价表、运单等文件上传接口仅允许 .xlsx / .xlsm 等指定扩展名文件 |
| 文件大小限制 | 限制上传文件最大体积，降低超大文件导致的 DoS 攻击面，与风险登记册中大文件上传风险项对应 |
| 配置信息脱敏 | 提供 appsettings.example.json 配置模板；真实数据库连接串等敏感配置仅留存于本地或部署机，不随代码仓库传播 |
| 演示环境假设 | 系统默认运行于本机或受控内网环境；身份认证与 RBAC 权限按 ADR 决策延期实现，已纳入风险清单与里程碑 Planned 项 |

The above controls are pragmatic baselines based on the idea of ​​"prioritizing the delivery of settlement core values ​​and strengthening security capabilities in phases" and do not constitute a production-level zero trust security statement.
## 8.7 性能基线（轻量级负载 / 冒烟测试）

本章性能数据为**可复现的冒烟级基线**，使用 xUnit + `Stopwatch` / 并发 `Task.WhenAll` 实现，**非** k6/JMeter 生产压测，**不构成 SLA 认证**。

**Actual measurement results (dedicated filter running batch, 2026-08-25, Windows local machine; see the command below): **

| 测试用例 | 场景 | 阈值 / 断言 | 实测 | 结果 |
| --- | --- | --- | --- | --- |
| `Import1000RowPerfTests` | 标准价表 **1000 行**纯解析（无 SQL） | &lt; 30 s | **543 ms**（`[PERF]`，2026-08-25 filter 跑批） | 通过 |
| `FeeCalculationPerfSmokeTests` | 计费引擎 **1000 次** `CalculateActive` | 循环 &lt; 200 ms | **&lt; 1 ms**（`[PERF] x1000: 0 ms`，Stopwatch 精度；断言 &lt;200 ms 通过） | 通过 |
| `StressLoadTests.TemplateDownload_30Concurrent` | **30 路并发** GET 模板 | 全部 HTTP 200，总耗时 &lt; 10 s | **986 ms** | 通过 |
| `StressLoadTests.PriceTablePreview_15Concurrent` | **15 路并发** POST 预览 | 全部 HTTP 200，总耗时 &lt; 30 s | **439 ms** | 通过 |

Reproduction command:

```text
dotnet test CloudWarehouse.sln --filter "FullyQualifiedName~Perf|FullyQualifiedName~Load|FullyQualifiedName~Stress" --logger "console;verbosity=detailed"
```

日志备份：`docs/project-management/artifacts/perf-load-stress-detailed.txt`

**图 8-5（建议截图）：** CI 或本地日志中含 `[PERF] ExcelHelper.ReadPriceTable 1000 rows: 114 ms` 与 `StressLoadTests` 通过行（附录 **A-13**）。

**诚实边界：** 未做长时间 soak test、未模拟万级并发、未对 SQL Server 做独立压测；云端 CI 无业务库时，部分 DB 集成用例会跳过，与本地全绿 **114** 项可能略有差异——以 Actions 日志中 **Passed/Skipped** 为准并附说明。
## 8.8 DevSecOps delivery checklist (supervisor scoring map)

| Capability | Status | Evidence |
| --- | --- | --- |
| DAST (OWASP ZAP) | **Done** | `.github/workflows/dast-zap.yml`; Artifact `dast-zap-baseline` |
| Playwright UI E2E | **Done (4)** | `CloudWarehouse.E2ETests` |
| IaC (Terraform + Bicep + Compose) | **Done** | §8.8.1; `iac.yml` |
| CD / repeatable deploy | **Done** | Bicep/TF apply + `docker compose up` + CI artefacts |
| Containers | **Done** | `Dockerfile` + `docker-compose.yml` |
| JWT + Role claim (Demo) | **Done** | `POST /api/auth/token`; `Auth:DemoJwt` |
| HTTPS | **Done (Azure topology)** | Bicep `httpsOnly=true` |
| Secrets injection | **Done** | Param files / deploy vars |

### 8.8.1 Infrastructure as Code (IaC) — delivered

| Capability | Status | Path |
| --- | --- | --- |
| Azure Bicep | **Done** | `infra/bicep/main.bicep` (App Service + SQL + App Insights) |
| Terraform | **Done** | `infra/terraform/main.tf` |
| Docker Compose | **Done** | `docker-compose.yml` |
| Container image | **Done** | `Dockerfile` (.NET 9 multi-stage) |
| Database as code | **Done** | `database/*.sql` |
| Pipeline as code | **Done** | `ci.yml`, `codeql.yml`, `pages.yml`, **`iac.yml`** |
| IaC validation gate | **Done** | `docker compose config` + `bicep build` + `terraform validate` |

Deploy examples: `az deployment group create ... -f infra/bicep/main.bicep`; `terraform -chdir=infra/terraform apply`; `docker compose up -d --build`.

### 8.8.2 Containers and compliance scope

| Item | Status |
| --- | --- |
| Container image / Compose topology | **Done** (Trivy-ready) |
| SOC2 / HIPAA / GDPR certification claim | Not claimed — factory intranet MVP |

Overall: CI + E2E + SAST + **IaC validation** + Jira tracking pack cover DevSecOps and project-management scoring points.

## 8.9 本地开发环境 vs CI 环境
两类环境的差异本身是 DevOps 工程化的佐证，保证质量门禁不绑定单台开发电脑：

| 维度 | 本地开发环境 | CI 环境（GitHub Actions） |
| --- | --- | --- |
| 操作系统 | 通常为 Windows | ubuntu-latest |
| SQL Server | 常驻可用 | 通常无；依赖数据库的测试用例跳过或受限运行 |
| 覆盖率采集 | 可选手工执行 | 每次构建强制生成并上传 Artifact |
| 环境状态 | 有状态开发机 | 无状态临时 Runner |

Cross-platform, stateless CI verification effectively avoids the environmental difference problem of "can run locally but fails online".
## 8.10 List of evidence in this chapter

| 证据项 | 文件位置 |
| --- | --- |
| CI 核心工作流 | .github/workflows/ci.yml |
| CodeQL 安全扫描工作流 | .github/workflows/codeql.yml |
| CI/CD 流水线活动图 | docs/diagrams/09-cicd-pipeline.puml |
| Actions 构建成功记录 | GitHub Actions 成功运行截图 |
| 单元/集成/E2E 测试汇总（114 passed） | Actions **Test with coverage** 或 `artifacts/dotnet-test-full.txt` |
| 负载冒烟 `[PERF]` 输出 | 同上日志 / `artifacts/load-smoke.txt` |
| 覆盖率报告产物 | coverage-report / Summary 截图 |
| NuGet 依赖扫描产物 | nuget-vulnerable-scan Artifact |
| 测试代码 | CloudWarehouse.Tests、CloudWarehouse.IntegrationTests |
| 数据库环境跳过策略 | DatabaseAvailability 等相关逻辑 |
| 性能冒烟测试 | Import1000RowPerfTests、FeeCalculationPerfSmokeTests |

## 8.11 本章小结
本章论证了策略模式、双轨结算等核心设计变更，处于可重复执行的 CI 流水线与分层测试体系的保护之下，并通过 CodeQL 静态扫描与依赖漏洞扫描补齐了基础安全可见性；同时明确披露了动态安全测试、完整持续部署、身份认证、传输加密等能力仍为缺口或规划状态。下一章将围绕风险管理、中期评审反馈逐条回应，以及项目结论与展望展开，收束全书核心内容。
# Chapter 9 Risk Management
## Risk Management

第八章阐述了质量与安全能力如何通过自动化流水线形成刚性约束；本章从**项目风险、技术风险、安全风险**三类维度，系统说明风险识别结果、已采取的缓解措施、仍处于规划阶段的事项，以及缓解措施有效性的验证证据。本项目的风险治理与四周Sprint迭代节奏深度绑定：每个Sprint启动前回顾风险登记册，将风险缓解动作纳入当周Must级任务，而非事后补写形式化的风险表格。

风险治理流程图示文件为

**Figure 9-1** Three types of risks: project/technology/security.

The risk management process diagram file is `docs/diagrams/12-risk-management.puml`; the key points of the one-page oral defense are in `docs/project-management/risk-management-slide.md`.

## 9.1 风险管理方法
本项目采用轻量化、可落地的风险管理流程，所有环节均基于单人开发的实际场景设计，不套用重型团队管理框架：

| 管理步骤 | 本项目具体做法 |
|----------|----------------|
| 风险识别 | 从Excel导入失败、CI环境差异、演示环境暴露面等真实开发事件中抽象风险项，拒绝凭空臆造 |
| 风险评估 | 采用定性风险矩阵：以发生可能性 × 影响程度划分风险等级，详见9.5节 |
| 风险缓解 | MVP范围内可关闭的风险立即落地（如预览校验、事务回滚、上传白名单、CI流水线）；需产品决策的事项写入ADR与规划项 |
| 风险跟踪 | 与项目里程碑、个人工时偏差联动分析（如R1风险直接对应Sprint 2的+39%工时超支） |

本项目为**单人Solo**实习项目，风险登记与工时统计均为个人维度。针对导师要求中“新加入开发者从某Sprint起单独统计工时”一项，**本项目无第二名开发者加入，该项记为N/A**，不虚构团队产能与多人协作流程。

## 9.2 项目风险（Project）
项目类风险聚焦进度、范围与环境一致性三类核心问题，具体风险项与缓解措施如下：

| ID | 风险描述 | 潜在影响 | 已实施缓解措施 | 后续/Phase 2规划 |
|----|----------|----------|----------------|------------------|
| R1 | Sprint 2因遗留三级表头等Excel格式复杂度超预期，实际工时超支39% | 挤压后续Sprint的功能开发时间，导致里程碑延期 | 严格执行预览后提交的导入流程；Sprint 2后复盘重估同类任务工时；外部文件处理类任务统一预留缓冲时间 | 后续同类外部系统集成任务继续保留工时缓冲 |
| R2 | Solo开发模式下范围蔓延，如手工规则CRUD、提前实现认证、过度绘制图表等非核心需求 | 核心功能质量下降，关键里程碑延期 | 采用MoSCoW优先级方法管控需求；通过ADR锁定核心范围（如仅通过Excel维护规则、认证功能延期） | 持续执行ADR决策与待办清单纪律，严格控制范围膨胀 |
| R3 | 本地SQL Server环境与CI云端环境存在差异 | 出现“本地运行通过、流水线报错/虚假通过”的环境不一致问题 | 数据库结构全部通过`database/*.sql`脚本版本化管理；GitHub Actions强制执行`dotnet test`；数据库不可达时相关用例采用可解释跳过策略 | 持续保持schema脚本与测试用例同步更新 |

**R1 Risk Mitigation Effect Verification**: Man-hour overruns were concentrated only in Sprint 2, and the man-hour deviation between Sprint 3 and Sprint 4 fell back to within ±10% (see the man-hour statistics table in Chapter 4 for details), proving that the corrective measures after review were effective.

## 9.3 技术风险（Technical）
技术类风险聚焦数据准确性、完整性与系统可用性，所有已关闭风险均有代码或脚本作为支撑证据：

| ID | 风险描述 | 潜在影响 | 已实施缓解措施 | 后续计划 |
|----|----------|----------|----------------|----------|
| T1 | 遗留三级表头错列导致解析错误，进而引发计价错误 | 报价与结算结果不准确，产生业务损失 | `ExcelHelper`实现表头行自动探测逻辑；提供标准模板下载；单元测试覆盖双格式解析场景 | 极端乱表场景可规划列映射配置UI，本期暂不实现 |
| T2 | 导入部分成功导致同一条运输车道下新旧规则混杂 | 数据完整性被破坏，计费结果混乱 | 导入全程包裹`SqlTransaction`事务，校验或写入失败则整批回滚（对应ADR-4决策） | 当前业务规模下该机制已满足需求 |
| T3 | 超大体积Excel文件导致内存溢出或请求超时 | 系统不可用，用户体验差 | 实现上传扩展名白名单 + 文件体积上限限制（如约10MB） | **规划中**：流式读取、分块处理、后台作业、断点续传——**本期未实现**，不得表述为已落地能力 |
| T4 | `(SiteId, DestId, EffectiveDate)`错误唯一索引阻断一对多档位规则入库 | 导入操作失败 | 通过`database/fix-price-rules-index.sql`脚本删除错误唯一索引，索引设计与一对多业务基数对齐 | 后续库表变更全部通过脚本执行，禁止手动直接修改数据库 |

T1 and R1 risks have the same origin: the technical complexity of external files directly translates into schedule risks. T4 is a typical engineering lesson that the index design does not match the business base, and has been cross-referenced in Chapter 5 Database Key Design Decisions.

## 9.4 Security Risk (Security)
Security risks are assessed based on the actual exposure of the MVP demonstration scenario, taking into account current implementation capabilities and long-term planning, and do not make false production-level security promises:

| ID | 风险描述 | 潜在影响 | 当前MVP缓解措施 | 远期规划 |
|----|----------|----------|----------------|----------|
| S1 | API与UI无认证授权机制 | 若系统暴露至公网，业务数据可被任意篡改 | 演示场景默认运行于本机或受控内网环境；ADR明确认证功能延期实现 | 实现JWT + RBAC权限体系，详见下文方案对比 |
| S2 | 开发阶段CORS配置为`AllowAll` | 跨源部署时攻击面扩大 | 文档明确标注该配置仅用于开发环境 | 生产环境收紧为明确的Origin白名单 |
| S3 | 数据库连接串等密钥泄露、恶意文件上传 | 凭证泄露、系统被入侵 | 通过`.gitignore`排除敏感配置，提供`appsettings.example.json`脱敏模板；上传文件设置白名单与大小限制；CI集成CodeQL与依赖漏洞扫描 | 接入User Secrets / 密钥托管服务；生产部署前强制启用HTTPS |

### S1 认证方案对比（回应导师“提供两种可选方案”的要求）
针对身份认证能力，设计两套落地方案，适配不同的业务集成场景：

| 评估维度 | 方案A：对接既有WMS/企业SSO | 方案B：独立JWT + RBAC |
|----------|-------------------------------|-------------------------|
| 集成成本 | 高，依赖外部身份提供商与联调窗口期 | 中等，用户与角色体系在本系统内维护 |
| 账号管理方式 | 统一由企业侧集中管理 | 本系统独立维护 |
| 演示独立性 | 依赖企业测试环境，无法独立运行 | 可脱离外部系统独立演示 |
| 适配建议 | 若云仓系统必须嵌入既有WMS生态再优先采用 | **默认推荐为Phase 2落地方案**：自主可控，与当前模块化单体架构匹配度更高 |

## 9.5 风险矩阵（缓解前定性评估）
基于发生可能性与影响程度，对所有风险项进行缓解前的定性分级：

|  | 低影响 | 中影响 | 高影响 |
|--|--------|--------|--------|
| **高可能性** |  |  | T1 遗留表头解析错误 |
| **中可能性** | S2 CORS配置宽松 | R1 进度超支；T3 大文件性能问题 | S1 无认证（外网暴露时影响升级为高） |
| **低可能性** | S3 密钥泄露（已有脱敏习惯时） | R2 范围蔓延；T2 部分导入失败（有事务后概率进一步降低） |  |

**矩阵解读**：T1是开发阶段最真实的高概率风险，已实际体现为Sprint 2的工时超支；S1在本机演示场景下发生概率可控，但**任何对外部署前必须优先关闭该风险**。

## 9.6 缓解有效性证据
所有风险缓解措施均有对应的工程产物可验证，避免空泛表述：

| 风险ID | 验证证据建议 |
|--------|--------------|
| T1 | `ExcelHelperTests`单元测试用例；导入成功/失败界面截图；标准模板文件 |
| T2 | 导入失败后UI端“未入库”提示截图；导入时序图08 |
| T3 | 上传非法扩展名文件被拒绝的截图；文件大小超限错误提示 |
| T4 | `fix-price-rules-index.sql`脚本文件；索引修复后导入流程成功运行记录 |
| R1 | 第四章工时统计表 + `sprint-hours-chart.html`工时柱状图 |
| R3 | GitHub Actions构建成功绿勾；`DatabaseAvailability`跳过策略说明 |
| S1–S3 | 架构决策记录ADR；第八章安全控制与缺口清单；不得展示真实数据库连接串等敏感信息 |

## 9.7 与PDA并列交付相关的风险（简述）
PDA无订单报工作为并列交付的独立系统，带来三类专项风险，对应缓解措施如下：

| 风险描述 | 缓解措施 |
|----------|----------|
| 双系统开发争夺单人开发带宽，导致核心功能质量下降 | 通过MoSCoW优先级管控 + 云仓/PDA分栏统计工时，避免工作量混淆不清 |
| 夸大表述，声称“已与云仓结算链路打通” | 企业Context Map中明确标注集成为Planned状态；严格遵守答辩禁语规范 |
| 硬件联调不确定性高，导致交付延期 | 对硬件联调任务预留工时缓冲；以可演示的开工/报工闭环作为完成标准 |

## 9.8 本章小结
本章表明风险管理并非报告附录的形式化装饰：R1进度风险有工时数据证明已被有效遏制，T1/T2/T4技术风险已通过代码与脚本完成闭环，S1安全风险则通过方案对比诚实承认缺口并给出落地路径。下一章将把中期导师评审意见逐条映射到已落地的设计、架构、证据与本章风险动作中，形成终稿核心的整改回应答卷。

---

# Chapter 10 Response to Mid-term Feedback
## Response to Mid-term Supervisory Feedback

中期评审明确要求：终稿与最终汇报必须全面回应全部评审意见，并附可验证的支撑证据。评审意见原文及英文摘要留存于仓库根目录 `log` 文件。本章按优先级将每条评审意见映射到已执行的改进动作，以及对应报告章节与产物路径，避免仅在概述中笼统提及而无实质支撑。

## 10.1 反馈来源与回应原则
本章所有回应遵循四项基本原则，确保内容真实可追溯，不做夸大表述：

| 原则 | 具体执行做法 |
|------|--------------|
| 有证据才标注已完成 | 所有已落地事项均指向对应的图表、测试、CI记录、UI截图等具体产物路径 |
| 未实现不虚假包装 | DAST动态扫描、完整持续部署、JWT认证、异形件/罚款策略等能力保持Planned状态 |
| 单人工时单独统计 | 第四章单独列示个人计划工时与实际工时对比；无第二开发者则标注为N/A |
| 严格遵守禁语规范 | 不得出现：微服务已上线、AI智能计费、云仓与PDA结算API已打通、生产级高可用已建成等表述 |

The main points of the mid-term review English window are consistent with the Chinese responses: Phase 2 deepens the complexity of billing and introduces design patterns; supplements the multi-perspective architecture diagram; the physical topology clearly indicates the infrastructure and redundancy capabilities (if not, explain it truthfully); demonstrates the rationality of the monolithic architecture; the DDD concept supports modular monolithic design; all deliverables have corresponding products.

## 10.2 总映射表（一页概览）
所有中期意见的回应落点可通过下表快速查阅：

| 中期评审意见 | 本项目回应摘要 | 主要落点章节 |
|----------|--------------------|----------|
| 整体实现偏简单，需要提升设计深度 | Phase 2新增策略模式、运单双轨历史价、规则检索、多类架构图、CI/SAST安全扫描 | 第6、7、8、10章 |
| 单体式架构需要充分说明合理性 | 模块化单体为有意选型，给出微服务拆分触发条件 | 第6章 |
| 计费变体需引入设计模式，配套类图与交互图 | 已实现区间/续重/体积重三类计费策略；配套类图13、双轨时序图14 | 第7章；ADR-8 |
| 补充多视角架构图 | 已提供逻辑、物理、部署、DDD、企业Context Map、CI活动图等多类视图 | 第6、8章；`docs/diagrams/*` |
| 物理架构图需明确基础设施与冗余能力 | 标注节点、端口、单实例部署；明确说明无高可用配置 | 第6.5–6.6节 |
| DDD理念需讲透 | 限界上下文与代码`Modules/*`目录一一映射；诚实说明非完整领域事件框架 | 第6.3节；图05、图16 |
| 所有工作需附可验证证据 | CI流水线、CodeQL扫描、覆盖率产物、测试用例、功能截图全覆盖 | 第8、9章；附录 |
| 个人计划工时与实际工时需拆分 | Phase 1合计198h→211h；Sprint 2偏差+39% | 第4章 |
| 风险缓解措施需具体落地 | 预览校验、事务回滚、上传白名单、索引修复已落地；大文件流式处理为规划项；认证给出双方案 | 第9章 |
| 未来规划需量化 | 明确各里程碑状态；Phase 2策略模式等工作包已完成 | 第4、10.6节 |

## 10.3 最高优先级意见 — 逐条展开
### 10.3.1 「系统偏简单」与计费变体需引入设计模式
**意见核心**：系统整体实现偏简单；第二阶段需重点深化计费规则变体；如有灵活空间应使用设计模式，并配套展示类图、交互图。

**逐条回应**：
1. 已落地策略模式（Strategy Pattern）：实现`TierBillingStrategy`、`OverweightBillingStrategy`、`VolumetricBillingStrategy`三类计费策略，由`FeeCalculationEngine`计费引擎与策略解析器统一编排调度。
2. 配套详细设计产物：输出`docs/diagrams/13-billing-strategy-class.puml`策略类图、`14-sequence-waybill-dual-track.puml`运单双轨时序图。
3. 深化业务能力：实现运单双轨结算（应收客户报价 vs 应付成本）+ 按发货日/账单日匹配历史价格，显著提升业务深度。
4. 诚实说明边界：异形件、超时罚款等扩展计费能力仍为**Planned**状态，通过扩展路径证明开闭原则，而非虚假宣称已全部实现。

**支撑证据**：第七章全文；`BillingStrategyTests`单元测试；运单预览一致/不一致UI截图（见附录）。

### 10.3.2 所有工作必须附可验证证据
**意见核心**：不能仅口头说明完成了测试、导入、CI等工作；需提供截图、覆盖率、流水线成功记录等实证。

**逐条回应**：
1. 持续集成：提供`.github/workflows/ci.yml`工作流文件 + GitHub Actions构建成功绿勾截图；覆盖率随Artifact归档，不写死大于80%等绝对化表述。
2. 静态安全扫描：提供`.github/workflows/codeql.yml`工作流文件与扫描成功记录。
3. 测试体系：覆盖单元测试、集成测试、轻量并发/性能冒烟测试三类用例。
4. 功能验证：价表导入成功/失败、运单双轨对比、PDA开工报工等功能截图全部纳入附录。

**支撑证据**：第八章证据清单；附录索引。

### 10.3.3 工时必须拆分个人计划值与实际值
**意见核心**：单独列出个人预估工时与实际工时；若有新加入开发者需单独统计。

**逐条回应**：
1. 单人开发工时表：Sprint 1–48h→52h；Sprint 2 44h→61h；Sprint 3 56h→51h；Sprint 4 50h→47h；Phase 1合计198h→211h，整体偏差+7%。
2. 新增开发者说明：本项目无第二名开发者加入，该项记为N/A。
3. Phase 2工时：第四章已预留云仓与PDA分栏工时表，定稿前需填入真实数值。

**支撑证据**：第四章；`sprint-hours-chart-data.csv` / 工时柱状图截图。

## 10.4 高优先级意见 — 逐条展开
### 10.4.1 Comprehensive upgrade of architecture diagram (multiple perspectives)
In response to the requirement of "supplementing the multi-perspective architecture diagram", a full set of traceable PlantUML source files has been output, covering the entire dimension of the architecture:

| 架构视角 | 对应文件 |
|------|------|
| 约束与架构决策（ADR） | `01*.puml` |
| 逻辑架构 | `02-logical-architecture.puml` |
| 物理架构 | `03-physical-architecture.puml` |
| 部署视图 | `04-deployment-diagram.puml` |
| DDD限界上下文 | `05-ddd-bounded-contexts.puml` |
| 用例视图 | `06-use-case-diagram.puml` |
| 实体关系图（ERD） | `07-erd.puml` |
| 导入/双轨时序 | `08`、`14` |
| CI流水线活动图 | `09-cicd-pipeline.puml` |
| 企业Context Map | `16-enterprise-context-map.puml` |
| 策略模式类图 | `13-billing-strategy-class.puml` |

The logical architecture diagram focuses on reflecting layering and dependency relationships; the physical and deployment diagram gives infrastructure level information such as nodes and ports.

### 10.4.2 物理架构：基础设施与冗余能力
**回应**：明确写清演示环境拓扑（Kestrel服务、SQL Server 1433端口、CI Runner、PDA并列节点）；数据备份采用手工备份与脚本重建方式；**明确说明无负载均衡、无数据库高可用配置**。通过诚实披露现状满足“有冗余就写、没有就写没有”的评审要求，而非虚构集群能力。

### 10.4.3 Rationality of monolithic architecture + evolution path to microservices
**Response**: Chapter 6 compares the three types of selection: microservices, big mud ball monomers, and modular monoliths, demonstrating that modular monoliths are the optimal solution under current constraints; clear trigger conditions for microservice splitting are given, including changes in team size, independent expansion and contraction requirements, release rhythm conflicts, technical heterogeneous requirements, quantitative QPS pressure, etc. Milestone M8 "Extract microservices based on trigger conditions" remains in the Planned state. Candidate split contexts include Import, Pricing, and Master Data. Split will be implemented after the trigger conditions are met, and the split will not be split for the sake of splitting.

### 10.4.4 DDD理念必须讲透
**回应**：
1. 明确划分Master Data、Import、Pricing、Billing、Assistant五大限界上下文，以及PDA独立上下文。
2. 上下文与代码结构一一映射：对应`Modules/*`目录下的模块划分。
3. 诚实说明交互方式：同进程同步调用 + 同库事务，并非已上线的事件总线架构。
4. 明确能力边界：本项目为DDD理念指导的模块化设计，并非完整复刻领域事件、聚合根等重型DDD框架。

## 10.5 Medium priority opinions - expand one by one
### 10.5.1 Risk mitigation measures should be specific
For the three types of risks highlighted by the instructor, this report provides a clear distinction between implemented measures and planned paths:

| 导师点名风险项 | 本报告落点 |
|------------|------------|
| 大文件上传风险 | 已落地扩展名白名单+文件大小限制；流式读取、分块处理、断点续传为**规划项**（第九章T3） |
| 三级表头解析风险 | 已落地自动探测算法+标准模板+单元测试覆盖（第九章T1）；与Sprint 2工时超支联动分析 |
| 无登录认证风险 | 给出JWT/RBAC与WMS SSO两套方案对比（第九章S1） |

### 10.5.2 Future planning must be quantified
The status and description of each planning work package are as follows, and all completed items correspond to clear iteration cycles:

| 工作包 | 状态 | 说明 |
|--------|------|------|
| 策略模式 + 体积重计费 | Done | 约Sprint 5完成；详见第七、四章 |
| 规则知识库检索 | Done | 辅助查阅功能，不作为结算真相源 |
| 运单双轨 + 历史价格 | Done | 对应时序图14 |
| 性能基线（1000行解析等） | 部分Done | 冒烟测试已完成，具体数值截图补充至附录 |
| JWT/RBAC认证体系 | Planned | 预估工时见规划表，负责人为项目作者（单人开发） |
| Import模块微服务调研 | Planned | 依赖稳定的模块边界与拆分触发条件 |
| 完整持续部署（CD） | Planned | 当前为CI + 发布包/部署检查清单模式 |

> Phase 2 man-hours must be filled in before finalizing the reservation form in Chapter 4, consistent with this planning form.

### 10.5.3 加分项落实情况
| 导师建议加分项 | 落实状态 |
|------|------|
| 计费复杂度分析 + 模式扩展方式 | 第七、十章变体表 + 开闭原则扩展三步法 |
| 性能基线：1000行解析等 | `Import1000RowPerfTests`等测试用例；具体数值贴入附录 |
| 设计决策对比（Dapper vs EF、单体 vs 微服务） | 第二章技术选型 + 第六章架构选型对比 |

## 10.6 Additional Delivery: Juxtaposing PDA and Value Frontiers
The core of the mid-term review focuses on the in-depth optimization of the cloud warehouse system; Phase 2 additionally delivered the **Honeywell PDA no-order work reporting** module in parallel, which supplemented the business narrative of "factory digitalization" from the perspective of factory on-site data collection, but strictly adhered to the following boundaries:
- The production-level settlement API is not connected to the CloudWarehouse system**;
- Cross-system integration is clearly marked as Planned in the enterprise Context Map;
- PDA's working hours statistics and evidence materials are separated from the cloud warehouse system to avoid using PDA delivery to cover up the lack of depth of the cloud warehouse design, and avoid using cloud warehouse rhetoric to exaggerate the degree of PDA integration.

## 10.7 仍待附录补齐的截图清单（作者执行）

定稿前建议逐项核对：

- [ ] GitHub Actions CI 绿勾
- [ ] 覆盖率 Summary
- [ ] CodeQL 成功
- [ ] 价表导入成功 / 失败
- [ ] 运单双轨预览
- [ ] 工时柱状图
- [ ] PDA 开工/报工
- [ ]（可选）非法扩展名被拒

## 10.8 本章小结
中期评审意见已从“提醒建议”转化为可核对的工程动作与文档产出：计费能力通过策略模式与双轨结算实现深度提升，架构设计通过多视角视图与诚实的无高可用声明实现清晰透明，质量体系通过CI、SAST、分层测试形成可验证证据，项目管理通过个人工时追踪与具体风险措施形成管理闭环。下一章将给出项目结论、已知限制与未来展望，并简要说明客户反馈与提交清单，结束正文内容。

# Chapter 11 Conclusion and Outlook

### 11.1 结论

本实习在 Solo 条件下交付了两套并列系统：CloudWarehouse（Modular Monolith 运费结算 MVP，含 Phase 2 的 Strategy 计费、应收/应付双轨与历史价、**内置规则 RAG** 辅助查阅）与霍尼韦尔 PDA 无订单报工 MVP。二者服务同一工厂目标，但按限界上下文独立演进，**本期未做生产级 API 打通**。

中期反馈已通过可验证产物回应：多视角架构图与诚实无 HA 声明、Strategy 类图与双轨时序、CI/CodeQL/测试证据、个人 Planned vs Actual（Phase 1：198→211 小时）。规则 RAG 仅作 FAQ 检索增强，**不替代** FeeCalculationEngine。

### 11.2 已知限制

- 无 JWT/RBAC；CORS/HTTP 为演示配置
- 无生产级 HA / 完整 CD / DAST 常态门禁
- 体积重引擎已通，运单 Excel 主路径仍以实重为主
- 异形件/罚款等计费变体仍为 Planned
- 规则 RAG 为词法检索（非生产级向量语义 RAG）；未配置 ApiKey 时为摘录生成

### 11.3 展望（量化方向）

| 项 | 状态 | 依赖 |
|----|------|------|
| JWT + RBAC | Planned | ADR |
| 演示环境 DAST 基线 | Planned | 稳定演示部署 |
| 完整 CD | Planned | 认证与发布目标环境 |
| 微服务提取 | Planned | 触发条件（见第六章） |
| 云仓↔PDA 集成 | Planned | 稳定 ID/文件交换约定 |

### 11.4 Client Feedback

#### 11.4.1 企业导师反馈（业务价值）

From the perspective of the company, this internship targeted two real pain points in the factory: warehouse freight verification, and production line reporting when there is no formal work order.

云仓这条线，把结算从“对着 Excel 猜”推进到可重复路径：主数据、成本/报价导入、试算，以及按发货日的应收应付双轨预览。对业务来说，值钱的不是口号，而是预览结果能核对、对不上的也能解释，而不是黑盒。做成模块化单体也符合约束：一个人、时间紧，先要能跑的系统，而不是一上来拆微服务。

产线这条线，PDA 无订单报工对准夜班常见情况——活在干，MES 却没有工单。工人可以在工业手持机上登录、选机、开工、报工。现场反馈明显偏正面：大家更愿意扫码落库，而不是纸笔或口头，因为事后能追查。近一周报工量也侧面说明：无订单路径（含 PDA 双写）在 mesdb 报工里占绝对主流，有工单号的正式路径很少有人用。也就是说，现场真正在用的，正是这个项目加强的那条路。

Overall comments: The intern, as a Solo, strings together the requirements, design, implementation, testing/CI evidence and gap descriptions (such as login authentication, complete CD, and the two systems will be connected later). The parts promised as MVP can be demonstrated; the delayed parts are intentionally scheduled, not ignored. Accepting the delivery in this issue, Cloud Warehouse and PDA will evolve side by side first, and we will talk about opening them up when the time for identification and docking is mature.

#### 11.4.2 Sponsor 正式验收状态（Formal Acceptance）

| 问题 | 答复 |
| --- | --- |
| 是否已有 **书面正式 sign-off**（签字邮件/验收单）？ | **无。** 截至终稿提交日，企业方未出具正式盖章验收文件或邮件归档。 |
| 实际接受程度 | **演示接受 + 现场使用：** 企业导师在演示与车间走访中确认 MVP 可演示、PDA 路径在现场被操作员使用；云仓双轨预览用于核对场景获口头认可。 |
| 是否等于全厂生产 go-live？ | **否。** 云仓仍为受控演示/内网部署；JWT、HA、云仓↔PDA API 集成均为 Planned。 |
| 学术评分用表述 | Sponsor **接受本期实习交付物**（报告+演示+可运行 MVP），**不等于**企业级生产系统正式上线验收。 |

Evidence suggestions: Keep the demo meeting minutes/WeChat feedback screenshots (if any) in the appendix; if there is no formal email, write "oral acceptance only" truthfully.

### 11.5 提交物

- 本报告（中文定稿）
- 英文版
- 评估演示视频
- 附录证据截图（见附录 A）

# Appendix A Evidence and Screenshot Checklist

附录证据一览：

| 编号 | 内容 | 建议来源 |
|------|------|----------|
| A-01 | GitHub Actions CI 绿勾 | Actions 网页 |
| A-02 | coverage Summary | CI Artifact |
| A-03 | CodeQL 成功 | Actions |
| A-04 | 价表导入成功 | 管理端 UI |
| A-05 | 价表导入失败/未入库 | 管理端 UI |
| A-06 | 运单双轨预览一致/不一致 | 管理端 UI |
| A-07 | 工时柱状图 | sprint-hours-chart.html |
| A-08 | PDA 开工/报工 | 设备或模拟器 |
| A-09 | 规则 RAG 查询结果（含三步流水线） | 管理端「规则 RAG」Tab |
| A-10 | 非法扩展名上传被拒（可选） | UI |
| A-11 | 解决方案结构 / Modules 目录（可选） | IDE |
| A-12 | 测试通过汇总（114 passed，含 E2E） | GitHub Actions → CI → Test with coverage |
| A-13 | 负载冒烟 `[PERF]` / StressLoadTests | `perf-load-stress-detailed.txt` 或 QA 页 |
| A-14 | NuGet Moderate 扫描 + 处置说明 | QA 页 / CI Artifact |
| A-15 | 公开 QA 报告首页 | https://chenyuxiangAK47.github.io/cloudwarehouse-csharp/ |
| A-16 | Playwright E2E 四项通过 | `artifacts/e2e-playwright-test.txt` 或 CI 日志 |
| A-07b | 累计工时燃尽（Planned vs Actual 折线） | `sprint-burndown-cumulative.csv` |

### 4.1.1 Sprint tracking tools (incl. Jira artefacts)

| Supervisor question | Practice | Evidence |
| --- | --- | --- |
| Jira / tracking tool? | **Jira-compatible tracking pack delivered.** Product backlog / sprint board / SP burndown as Jira CSV import format (importable to Jira Cloud) + GitHub Issue templates. Hours CSV + Git history + Actions CI. | `docs/project-management/jira/product-backlog.csv`, `burndown-board.html`, `.github/ISSUE_TEMPLATE/sprint-story.yml` |
| Solo sprints? | **Yes.** Phase 1: Sprint 1–4 weekly; Phase 2: Sprint 5 milestone (Strategy/dual-track/PDA/E2E/IaC). | §4.3–4.7; `jira/sprint-burndown-points.csv` |
| Burndown? | **Yes.** Remaining SP burndown per sprint + cumulative Planned vs Actual hours. Screenshot `burndown-board.html` for appendix. | `jira/burndown-board.html` |


# Chapter 12 Reflection Questions

> **导师邮件要求：** 报告须包含独立章节 *Reflection Questions*。以下按实习全过程作答（Solo、双系统、中期反馈后整改）。

**Q1. 本实习中你最大的收获是什么？**  
从「能跑的功能」升级到「能证明的工程」：计费用 Strategy 与双轨时序落地设计模式；用 114 项自动化测试（含 Playwright 冒烟）、CI 覆盖率、CodeQL 与 QA 页把主张变成可点击证据。并学会在单人约束下用 Modular Monolith 而非过早微服务。

**Q2. 最大的困难是什么？如何克服？**  
Sprint 2 的 Excel 三级表头解析超支 39%——外部文件格式不可控。通过预览入库、标准模板、密集单测样例与复盘缓冲，把后续 Sprint 偏差压回 ±10%。Phase 2 并行 PDA 硬件联调则靠时间盒与明确「云仓/PDA 不强行 API 打通」控制范围。

**Q3. What have you changed after the mid-term mentor’s feedback? **
Complete Analysis (§3.0), class-level sequence diagram, Sprint backlog table, security before→resolution narrative, Playwright E2E, public QA site; delete inaccurate statements such as "Playwright is not done"; Client Feedback distinguishes demo acceptance from formal sign-off.

**Q4. 一人团队如何实践 Sprint/敏捷？是否有效？**  
Delivered **Jira-compatible Product Backlog + Sprint Board + SP burndown** (`docs/project-management/jira/`, importable to Jira Cloud) plus GitHub Issue templates; Phase 1 weekly sprints; Phase 2 = Sprint 5 milestone. Sprint 2 retro retained.

**Q5. 若重来，会如何安排？**  
更早锁定 Excel 黄金样例库；Phase 1 末即引入 Playwright 冒烟；安全扫描在依赖选型阶段就记录 baseline；报告母稿与 Word 同步频率提高，避免终期集中补图。

**Q6. AI 工具如何使用？哪些仍由本人负责？**  
AI 用于 PlantUML 草稿、测试脚手架、文档润色与 CI 脚本；**计费规则、双轨语义、工时数据、验收边界、答辩禁语**由本人核对源码与业务方反馈后定稿。见 `docs/project-management/ai-assistance-disclosure.md`。

**Q7. Reflection on the relationship between sponsoring companies and customers? **
The corporate value lies in "verifiable dual-track preview" and "PDA-free order placement", not in technology stacking. Honestly stating that there is no formal sign-off and no production go-live will help discuss the integration scope in the next stage.

**Q8. What is the next step for personal growth? **
Deepen .NET performance and SQL tuning; complete JWT/RBAC and DAST baselines; IaC (Bicep/Terraform/Compose) is already delivered — next stabilise demo deploys via terraform apply / az deployment.

# Appendix B Terminology and Forbidden Phrases

| Correct | Forbidden |
|------|------|
| Dual Track = Quotation Receivable vs Cost Payable | receivable versus payable Routes |
| Modular Monolith; no HA | Microservices are online; production and multi-activity have been built |
| Strategy Tier/Overweight/Volumetric Done | JSON rules engine; AI smart billing |
| Built-in rules RAG (lexical search FAQ) | AI settlement/vector RAG has been produced and launched |
| PDA has not been connected with the cloud warehouse settlement API | Already connected / Parallel Data Aggregator |
| Coverage is subject to Artifact | Text should be hard-coded >80% |
