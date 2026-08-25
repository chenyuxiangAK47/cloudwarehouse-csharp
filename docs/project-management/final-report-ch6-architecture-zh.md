# 第六章 系统架构设计

Database Design and Multi-View Architecture（接第五章）

第五章给出了支撑计费与结算的持久化模型；本章从 **多视角架构** 说明这些表与能力如何被组织进可部署系统：逻辑分层与限界上下文、与 PDA 的企业关系、物理运行拓扑，以及对高可用与未来拆分的诚实边界。中期反馈要求“单体需自辩、物理图写清基础设施、DDD 讲透”——本章直接回应这些点。

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

逻辑视图见 `docs/diagrams/02-logical-architecture.puml`。分层职责：

| 层 | 代表组件 | 职责 |
|----|----------|------|
| Presentation | `wwwroot/index.html` | 管理端 Tab：主数据、导入、试算、运单、规则检索等 |
| API | 各模块 `*Controller` | HTTP 适配；校验入参；委托应用服务 |
| Application | Import / Calculate / BillImport / Assistant 等 Service | 编排用例、事务边界、跨 helper 协调 |
| Domain / Helpers | Excel 解析、`PriceRuleMapper`、`FeeCalculationEngine` + Strategy | 纯规则与计算；可单测 |
| Data | Dapper + SQL Server | 显式 SQL；同库事务 |

**价表导入（预览→确认）数据流（摘要）：**

1. 浏览器上传 `.xlsx` → `ImportController`；
2. `PriceRuleImportService` 调用 `ExcelHelper` 探测标准/三级表头等格式；
3. `PriceRuleMapper` 将一行 Excel 展开为多条 `PriceRule`；
4. 预览：`save=false`，可挂钩试算，**不写库**；
5. 确认：在 `SqlTransaction` 内按 lane 删除旧规则并插入新规则，提交或整批回滚。

运单双轨、Strategy 编排的详细时序放在 **软件设计章**（类图 13、时序 14）；本章只固定“逻辑落点”：结算编排在 Pricing/Billing 应用层，持久化落在第五章所述表。

---

## 6.3 限界上下文与代码映射

DDD 在本项目中的用法是 **务实的限界划分**，不是完整事件溯源或聚合魔法。上下文见 `docs/diagrams/05-ddd-bounded-contexts.puml`，并与代码目录对齐：

| 限界上下文 | 职责 | 代码落点（示意） | 关键持久化 |
|------------|------|------------------|----------|
| Master Data | 站点/目的地/客户等基准数据 | `Modules/MasterData` | Sites, Destinations, Customers, CustomerAccounts |
| Import | 成本价表解析、校验、事务提交 | `Modules/Import` | 写入 PriceRules（无独立 job 表；客户报价导入在 Pricing） |
| Pricing | 成本规则试算、客户报价、Strategy 引擎 | `Modules/Pricing` + Pricing.Core | PriceRules, CustomerQuoteRules |
| Billing | 运单导入、应收应付对比落库 | `Modules/Billing` | BillLines |
| Assistant | 规则知识检索（辅助，非结算真相源） | `Modules/Assistant` + KnowledgeBase | 文件知识库为主 |

**语言隔离示例：** Import 上下文中的 `PriceTableRow`（Excel 行视图）经映射变为 Pricing 上下文中的持久 `PriceRule` 集合；二者不应在 UI 层混用同一套字段语义。

Phase 1 早期代码曾集中在根目录 Controllers；重构后以 `Modules/*` 表达边界——报告叙述以 **当前模块结构** 为准。

---

## 6.4 企业级上下文关系（含 PDA）

工厂视角下存在两个产品系统，关系见 `docs/diagrams/16-enterprise-context-map.puml`：

| 关系 | 含义（诚实表述） |
|------|------------------|
| CloudWarehouse 内部模块 | 同库 Modular Monolith；模块间进程内调用 |
| PDA ↔ 产线/MES 相关能力 | PDA 侧已实现开工/报工与后端落库；与既有 MES 的衔接按 PDA 项目实际描述，**不夸大** |
| CloudWarehouse ↔ PDA | **Customer–Supplier / 集成 Planned**：共享的是工厂业务目标，**本期无生产级 API 共库或实时同步** |

答辩禁话：不要说“微服务已上线”或“云仓与 PDA 已打通结算链路”。

---

## 6.5 物理部署与运行拓扑

物理/部署图：`docs/diagrams/03-physical-architecture.puml`、`04-deployment-diagram.puml`。

**演示 / 开发期典型拓扑：**

| 节点 | 角色 | 说明 |
|------|------|------|
| 开发者/演示机 | 运行 Backend（Kestrel）+ 浏览器 | 管理端与 API 同机或同发布包 |
| SQL Server | CloudWarehouse 库 | 可本机或局域网实例；端口通常 1433 |
| GitHub Actions Runner | 短暂 CI 节点 | `dotnet test`、覆盖率 Artifact；云端无常驻业务库时对依赖 DB 的用例可跳过并解释 |
| PDA 设备 + PDA API/DB | 并列系统 | 霍尼韦尔终端 ↔ Spring Boot ↔ `PDA_NoOrder`（独立） |

可选交付物：自包含发布包（`publish/`）便于现场演示；IIS 发布检查清单见项目管理文档，**不等于**已建成生产级多活集群。

**安全姿态（MVP 诚实声明）：** 本地/受控演示场景；认证/RBAC 按 ADR 延期；CORS/HTTP 等按开发便利配置，生产前需收紧——细节在 DevSecOps / 风险章展开，本章只标明架构层未宣称零信任生产加固。

---

## 6.6 高可用与备份（诚实声明）

| 方面 | 当前状态 | 规划方向（非本期必交付） |
|------|----------|--------------------------|
| 应用冗余 | 单实例，无负载均衡 | 容器多副本 + 反向代理 |
| 数据库冗余 | 单实例 SQL Server | 托管 HA / Always On 等 |
| 备份 | 手工 `.bak` / 脚本重建 | 自动备份与明确 RPO |
| 灾难恢复 | Git + `database/*.sql` 重建 | 文档化 RTO + 演练 |

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

本章论证了 Modular Monolith 作为约束下的合理选择，并用逻辑分层、限界上下文与企业 Context Map 回应“架构叙述不足”；用物理拓扑与 **无 HA** 的诚实清单回应基础设施透明度要求。PDA 作为独立上下文并列存在，集成保持 Planned。下一章进入 **软件设计**：Strategy 计费、双轨时序与关键类结构，把架构落点细化为可验证的设计产物。
