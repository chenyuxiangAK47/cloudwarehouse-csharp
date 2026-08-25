# Architectural Assessment — 中文演讲稿 + 展示清单

> 视频文件：`…Architectural Assessment.mp4`  
> 时长：约 **10 分钟**（可 9–11 分钟）  
> 核心：多视角架构图 + 单体自辩 + DDD 边界 + 安全/物理现实 +（可选）企业 Context Map（云仓+PDA）

---

## 开场前：你要准备什么（先导出 PNG）

| 顺序 | 展示什么 | 仓库路径 |
|------|----------|----------|
| 1 | 封面 | 自做 1 页 PPT：Architectural Assessment |
| 2 | 用例图 | `docs/diagrams/06-use-case-diagram.puml` |
| 3 | 逻辑架构 | `docs/diagrams/02-logical-architecture.puml` |
| 4 | DDD 限界上下文 | `docs/diagrams/05-ddd-bounded-contexts.puml` |
| 5 | 企业 Context Map（云仓+PDA） | `docs/diagrams/16-enterprise-context-map.puml` |
| 6 | 物理架构 | `docs/diagrams/03-physical-architecture.puml` |
| 7 | 部署图 | `docs/diagrams/04-deployment-diagram.puml` |
| 8 | ADR/约束（可选） | `docs/diagrams/01a-architecture-decisions-adr.puml` 或 `01b-...` |
| 9 | 安全一页（可 PPT 自制） | 无 Auth / 文件白名单 / CORS / 备份 RPO |

导出：PlantUML 插件或 https://www.plantuml.com/plantuml/uml/ → PNG，按上面顺序放进 PPT。

---

## 演讲稿（照着念）

### 0. 封面 · 约 30 秒  
**【展示：封面页】**

各位老师好。本视频是 **Architectural Assessment**。  
我将说明 CloudWarehouse 与并列交付的 PDA 报工系统，如何在架构上分工、为何选择 Modular Monolith，以及物理部署与安全边界的真实状态。重点用多张架构视图作为证据，而不是只讲功能列表。

---

### 1. 业务范围与用例视角 · 约 60 秒  
**【展示：06 用例图】**

从用例看，CloudWarehouse 的主要演员是仓库管理员：维护站点、目的地、客户；导入成本价与客户报价；试算运费；导入运单并做应收应付对比；以及查阅计价规则。  

PDA 侧演员是产线操作员：登录、选择产线/机群/机床、开工、报工、查询。  

架构含义是：两类用户、两套交互界面，但同属工厂数字化目标。因此架构上采用 **多应用、分上下文**，而不是强行塞进同一个单体进程。

---

### 2. 逻辑架构 · 约 90 秒  
**【展示：02 逻辑架构图】**

CloudWarehouse 逻辑上是 **Modular Monolith**：一个可部署的 ASP.NET Core 应用，内部分模块。  

- 表现层：静态前端 `index.html` + REST API Controllers  
- 应用/领域服务：Import、Pricing、Billing、MasterData、Assistant  
- 核心库：`CloudWarehouse.Pricing.Core`（计费策略与规则计算）  
- 基础设施：Dapper + SQL Server、ClosedXML 读写 Excel  

数据流主路径是：Excel → Import → 写入 PriceRules / 账单行 → FeeCalculationEngine 计价 → UI 预览对比。  
Assistant 规则检索是旁路辅助，不改写结算结果。  

这样划分的目的，是在单进程里保持边界清晰，为以后按模块抽取服务留下缝。

---

### 3. DDD 限界上下文 · 约 90 秒  
**【展示：05 DDD 图】**

我用 DDD 思想做边界，而不是宣称“完整落地领域框架”。  

CloudWarehouse 内：  
- **Master Data**：Site、Destination、Customer  
- **Import**：Excel 解析与事务入库  
- **Pricing**：PriceRules 与试算  
- **Billing**：运单双轨应收应付  

上下文之间 Phase 1 以进程内调用 + 共享数据库为主；这是 MVP 诚实选择。  
口头界定：聚合根在主数据侧更清晰；计费规则按站点-目的地-生效日版本管理；Billing 消费 Pricing 结果，不反向污染主数据。

---

### 4. 企业级 Context Map（含 PDA）· 约 80 秒  
**【展示：16 企业 Context Map】**

工厂数字化不是只有云仓。PDA「MES 无订单报工」是 **Shop-floor Execution** 上下文：Android + Spring Boot API + 自建库，服务夜班无工单报工。  

CloudWarehouse 是 **Billing & Pricing** 上下文。  
图上实线表示已交付系统内部能力；虚线表示规划中的集成（共享标识、文件交换、未来事件），**今天不声称已 API 打通**。  

架构决策是：先交付两个可运行 MVP，用 Context Map 管边界；集成等到业务标识与稳定性足够再做。这符合“演进式架构”，也避免过早微服务。

---

### 5. 为何 Modular Monolith（自辩）· 约 80 秒  
**【展示：01a ADR 或逻辑图再停 2 秒】**

选择单体模块化的原因有三条：  
1. Solo 开发、周期紧，微服务的部署、观测、分布式数据成本过高；  
2. 结算强一致需求适合同库事务（导入整批回滚）；  
3. 已用模块与 DDD 边界保留拆分期权。  

迁移触发条件（量化口径）：团队变大、导入与计价需独立扩缩容、发布频率冲突、或计算 QPS 明显升高。  
在此之前，Modular Monolith 是刻意选择，不是偷懒。

---

### 6. 物理架构 · 约 80 秒  
**【展示：03 物理架构图；用鼠标指端口/备份区/灰色规划框】**

物理视图补全基础设施细节，但保持诚实：  

- **节点**：客户端浏览器；单机承载 .NET 9 + Kestrel **:5001** + SQL Server **:1433**；可选 Windows 防火墙放行 5001 做局域网演示。  
- **运维检查**：进程/控制台日志 + SSMS，不做假装的监控大盘。  
- **备份落点**：独立画出 Backup landing zone——手动全量 `.bak`、仓库里的 `schema.sql`、以及发布包；重大演示或大批导入前先备一份。  
- **指标**：RPO ≈ 24h（或最近一次 bak）；RTO ≈ 30–90 分钟（装运行时 + 还原 + 启动）。  
- **HA / 冗余**：MVP **明确为 NONE**（无负载均衡、无 Always On）。图右侧灰色框是 **Planned redundancy**（IIS+TLS、独立库、自动备份/温备），只作演进说明，**不宣称已上线**。  

CI 仍在 GitHub 云端，与业务库隔离。这样既回应“物理图要看基础设施与冗余”，又不会装成生产机房。

---

### 7. 部署视图 · 约 60 秒  
**【展示：04 部署图】**

部署上，CloudWarehouse 可打成 self-contained 可执行文件，在师傅电脑或 IIS 上运行，连接 SQL Server。  
PDA 是另一条部署链：PDA 设备 → HTTP → Spring Boot API（如 8080）→ `PDA_NoOrder` 库。  

两条部署拓扑独立，降低互相拖垮风险，也解释了为何集成要分阶段。

---

### 8. 安全与威胁（架构视角）· 约 70 秒  
**【展示：自制安全一页，或停在物理图讲】**

当前安全姿态匹配内网/演示场景：  
- 云仓：无登录（ADR 延期认证）；CORS 开发期较宽；上传限制扩展名与大小；HTTP 明文。  
- PDA：内网 HTTP，登录偏简化，侧重可用性。  

威胁与缓解：未授权访问 → 网络隔离 + Phase 2 JWT/RBAC；恶意文件 → 白名单与大小限制；数据丢失 → 手动备份策略；密钥 → 本地配置，示例文件脱敏。  
架构上承认缺口，并给出演进项，而不是声称生产级安全。

---

### 9. 收束 · 约 40 秒  
**【展示：16 或 02 总览回看】**

总结：本项目用多视图说明架构——用例、逻辑、DDD、企业 Context Map、物理、部署与安全。  
CloudWarehouse 与 PDA 共同服务工厂数字化；云仓侧用 Modular Monolith 快速交付可审计结算；PDA 侧用独立应用服务现场报工。  
下一步 Technical 视频会展开计费 Strategy 与双轨时序等软件设计细节。谢谢。

---

## 时间轴（合计约 10 分）

| 段 | 内容 | 秒 |
|----|------|-----|
| 0 | 封面 | 30 |
| 1 | 用例 | 60 |
| 2 | 逻辑 | 90 |
| 3 | DDD | 90 |
| 4 | 企业 Context Map | 80 |
| 5 | 单体自辩 | 80 |
| 6 | 物理 | 70 |
| 7 | 部署 | 60 |
| 8 | 安全 | 70 |
| 9 | 收束 | 40 |
| | **合计** | **~670 秒 ≈ 11 分** |

若必须压到 10 分钟内：安全段删举例、DDD 段各砍 15 秒。

---

## 录制检查

- [ ] 出镜 + 1080P  
- [ ] 图上能指到端口 5001 / 1433、无 HA、备份  
- [ ] 明确说：DDD-informed，不是完整领域框架  
- [ ] 明确说：PDA 与云仓未 API 打通  
- [ ] 禁止：微服务已上线、生产级高可用、完整 DDD
