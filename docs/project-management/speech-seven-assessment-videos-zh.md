# 七段评估视频 · 演讲稿总册（对齐 Final Report EN/ZH）

> **今夜诵读（中英对照纯文本）：** `docs/speech-scripts/` — `01`…`07` 的 `.txt`；见 `README.txt`  
> 用途：NUS MTech 实习 **七段考核视频** 口播稿  
> 对齐：`Final-Report-EN - 副本.docx` / 中文终稿口径  
> 原则：有证据再说 Done；不说微服务已上线、云仓↔PDA 已打通、AI 智能计费、生产级 HA、覆盖率 >80% 口号  

| # | 视频 | 建议时长 | 对应报告章 |
|---|------|----------|------------|
| 1 | Management Assessment | ~5–5.5 min | Ch.1 / Ch.4 / Ch.9 |
| 2 | Architectural Assessment | ~10 min | Ch.6 |
| 3 | Software Design | ~5–5.5 min | Ch.7 |
| 4 | DevSecOps | ~9–10 min | Ch.8 / Ch.9 |
| 5 | Value Added | ~5.5–6 min | Ch.1 / Ch.10.6 |
| 6 | App Demo | ≤5 min（建议 4.5） | 产品证据 |
| 7 | CI/CD Demo | ≤5 min（建议 4） | Ch.8 |

**全局禁话：** 微服务已上线 · 云仓与 PDA 结算 API 已打通 · AI/RAG 智能计费 · 生产级 HA · 正文写死覆盖率 >80%

---

# 视频 1 · Management Assessment

## 开场先看：本段要展示什么

| 顺序 | 展示什么 | 怎么准备 |
|------|----------|----------|
| 1 | 封面 | **未来做 1 页 PPT**：标题 Management Assessment · CloudWarehouse & PDA |
| 2 | 双系统目标 | **未来做 1 页 PPT**：云仓痛点 vs PDA 痛点（两列） |
| 3 | 路线图 | **展示 PlantUML 导出图**：`docs/diagrams/10-roadmap-milestones.puml`（Phase1+Phase2） |
| 4 | Backlog / MoSCoW | **未来做 1 页 PPT**：Must 云仓 / Must PDA / Should / Could |
| 5 | 工时柱状图 | 打开 `docs/project-management/sprint-hours-chart.html` 截屏放 PPT，或录屏指着图 |
| 6 | 风险一页（可选） | **展示** `docs/diagrams/12-risk-management.puml` 或 PPT 三列风险 |

> 本段以 **PPT + 路线图/工时图** 为主，不要求操作业务系统。

## 口播稿

### P1 封面 · ~40s ·【展示：封面 PPT】

各位老师好。本视频是 NUS MTech SE 实习项目的 **Management Assessment**。

我是独立开发者（Solo Intern）。本实习围绕工厂数字化，交付两块可运行能力：一是 **CloudWarehouse** 云仓运费与结算系统；二是产线侧 **PDA「MES 无订单报工」**。

云仓解决 Excel 对账与运费追溯；PDA 解决夜班无正式工单时的开工、报工与现场采集。二者服务同一工厂目标，按不同限界上下文分开交付，**本期未做生产级 API 打通**。

下面从业务目标、敏捷执行、Backlog、个人工时、干系人与风险说明管理过程。

### P2 业务问题与目标 · ~50s ·【展示：双系统目标 PPT】

工厂有两类高频痛点。

仓库侧：运费长期靠 Excel，易错、难追溯、版本混乱。  
产线侧：夜班常无正式工单，但仍需登录、选机、开工报工并落库。

本期目标是交付 **可演示的双端 MVP**：  
- CloudWarehouse：主数据、成本/报价 Excel 导入、运费试算、应收/应付双轨、按发货日历史价，以及 **内置规则 RAG（仅 FAQ 查阅）**；  
- PDA：霍尼韦尔终端上登录 → 产线/机群/机床 → 开工/报工/查询。

范围上明确：两套系统是 **并列 MVP**；规则 RAG **不替代**结算引擎；不宣称微服务或生产级高可用。

### P3 敏捷与中期反馈 · ~50s ·【展示：路线图 10-roadmap】

执行上采用一周一个 Sprint。  
**Phase 1** 约四周：CloudWarehouse MVP（主数据、导入、算价、测试/CI）。  
**Phase 2** 中期后继续：Strategy 计费加深、运单双轨、规则 RAG，并 **并列推进 PDA** 至可演示。

中期反馈要求：系统不要偏简单、计费要有设计模式与详细设计、架构要多视角、单体要能自辩、工作要有证据。  
管理响应：把 Strategy、双轨、多视角图纸、CI/测试证据列入 Must；同时用任务清单分栏管理云仓与 PDA，按周复盘，避免两头都虚。

### P4 Backlog · ~50s ·【展示：MoSCoW PPT】

**Must — CloudWarehouse 已交付：** 主数据；成本/客户报价导入；试算；Strategy（区间/续重/体积重）；运单双轨与历史价；规则 RAG（辅助查阅）。

**Must — PDA 已交付：** 登录与选机；开工/报工/查询；扫码；API 写入独立库。

**Should：** 更完整证据链、错误提示、操作手册。  
**Could：** JWT/RBAC、云仓↔PDA 集成、按触发条件再拆服务——**不计入本期 Must**。

### P5 工时 · ~55s ·【展示：sprint-hours 柱状图】

Solo 个人 Planned vs Actual（Phase 1）：  
Sprint1 48→52；Sprint2 44→61（**+39%**，三级表头 Excel）；Sprint3 56→51；Sprint4 50→47。  
合计 **198→211（+7%）**。Sprint2 之后对外部 Excel 类任务加缓冲，偏差回到约 ±10%。

Phase 2 云仓加深与 PDA 联调 **分栏记工时**（报告 Ch.4.7.3）；管理上视为同一工厂目标下的并列交付，不混成一笔糊涂账。

### P6 干系人 · ~45s ·【展示：可停封面或目标页】

仓库师傅侧：导入、试算、运单对比是否可用。  
产线侧：PDA 登录、选机、开工报工是否跑得通。  
反馈进同一任务池：能进本期的改，不能进的明确延期。  
Client Feedback 评分项：企业导师演示反馈会作为管理证据保留。

### P7 风险与收束 · ~45s ·【展示：风险图或 PPT】

风险三件：进度（Excel/真机联调易超支）、范围（双系统并行易膨胀）、叙事（不夸大已集成）。  
标准说法：**并列交付、分上下文演进、集成 Planned**。

后续视频将展开 Architecture、Software Design、DevSecOps、Value Added、App Demo 与 CI/CD Demo。谢谢。

---

# 视频 2 · Architectural Assessment

## 开场先看：本段要展示什么

| 顺序 | 展示什么 | 怎么准备 |
|------|----------|----------|
| 1 | 封面 | **未来做 PPT**：Architectural Assessment |
| 2 | 用例图 | **展示 puml**：`06-use-case-diagram.puml`（可补 `17-pda-use-case.puml` 一瞬） |
| 3 | 逻辑架构 | **展示 puml**：`02-logical-architecture.puml` |
| 4 | DDD 上下文 | **展示 puml**：`05-ddd-bounded-contexts.puml` |
| 5 | 企业 Context Map | **展示 puml**：`16-enterprise-context-map.puml` |
| 6 | PDA 逻辑栈（推荐） | **展示 puml**：`19-pda-logical-architecture.puml` |
| 7 | 物理架构 | **展示 puml**：`03-physical-architecture.puml` |
| 8 | 部署图 | **展示 puml**：`04-deployment-diagram.puml`；可选 `21-pda-deployment.puml` |
| 9 | ADR/安全一页 | **展示 puml** `01a`/`01b`，或 **未来做 PPT** 安全清单 |

## 口播稿

### 0 封面 · ~30s ·【封面 PPT】

各位老师好。本视频是 **Architectural Assessment**。  
我将用多视角架构图说明：CloudWarehouse 为何用 Modular Monolith；PDA 如何并列部署；物理与安全边界的真实状态——有证据、不装生产机房。

### 1 用例视角 · ~60s ·【06；可选 17】

CloudWarehouse 演员是仓库管理员：主数据、价表导入、试算、运单双轨、规则查阅。  
PDA 演员是产线操作员：登录、选机、开工、报工、查询。  
架构含义：**两类用户、两套界面 → 多应用、分上下文**，不硬塞进同一进程。

### 2 逻辑架构 · ~90s ·【02】

CloudWarehouse 是 **Modular Monolith**：ASP.NET Core 单部署单元，内分 MasterData / Import / Pricing / Billing / Assistant。  
表现层静态 `index.html` + REST；计费核心在 `CloudWarehouse.Pricing.Core`；持久化 Dapper + SQL Server。  
主路径：Excel → Import → 规则/账单 → `FeeCalculationEngine` → UI 预览。  
**规则 RAG 是旁路查阅，不改写结算结果。**

### 3 DDD · ~90s ·【05】

用 DDD **划边界**，不宣称完整领域框架。  
上下文：Master Data、Import、Pricing、Billing、Assistant。  
MVP 阶段进程内调用 + 同库；Billing 消费 Pricing，不反向污染主数据。

### 4 企业 Context Map + PDA · ~90s ·【16 + 19】

PDA 是 Shop-floor Execution：Android + Spring Boot + 独立库 `PDA_NoOrder`。  
图上实线=已交付；虚线=规划集成。**今天不声称 API 打通。**  
先交付两个可运行 MVP，集成等标识与稳定性足够再做——演进式架构，避免过早微服务。

### 5 单体自辩 · ~80s ·【01a 或停在 02】

选 Modular Monolith 三条：Solo 与周期紧；结算需要同库事务；模块边界保留拆分期权。  
触发条件：团队变大、独立扩缩、发布冲突、量化高 QPS 等。此前是刻意选择，不是偷懒。

### 6 物理 · ~80s ·【03】

演示拓扑：浏览器 → Kestrel **:5001** → SQL Server **:1433**。  
备份：手工 `.bak` + `schema.sql`；**无负载均衡、无 DB HA**。灰色框仅 Planned redundancy。

### 7 部署 · ~60s ·【04 / 21】

云仓可 self-contained / IIS。  
PDA：设备 → 内网 HTTP → Spring Boot（如 8080）→ `PDA_NoOrder`。两条拓扑独立。

### 8 安全 · ~70s ·【安全 PPT】

云仓：认证 ADR 延期；上传白名单与大小限制；演示偏内网 HTTP。  
威胁与演进：JWT/RBAC、收紧 CORS、HTTPS——写在风险与展望，不假装已关闭。

### 9 收束 · ~40s

多视图讲清架构；双系统并列；下一步 Software Design 展开 Strategy 与双轨时序。谢谢。

---

# 视频 3 · Software Design Assessment

> 分文件诵读版（含 PDA）：`docs/speech-scripts/03-software-design.md`

## 开场先看：本段要展示什么

| 顺序 | 展示什么 | 怎么准备 |
|------|----------|----------|
| 1 | 封面 | **未来做 PPT**：Software Design · CloudWarehouse + PDA |
| 2 | 设计范围 | **未来做 PPT**：左云仓 · 右 PDA |
| 3 | Strategy 类图 | **展示 puml**：`13-billing-strategy-class.puml` |
| 4 | 双轨时序 | **展示 puml**：`14-sequence-waybill-dual-track.puml` |
| 5 | 云仓 ERD | **展示 puml**：`07-erd.puml` |
| 6 | PDA 时序 | **展示 puml**：`20-sequence-pda-start-report.puml` |
| 7 | PDA 类图 | **展示 puml**：`22-pda-class-overview.puml` |
| 8 | PDA ERD（可选） | **展示 puml**：`18-pda-erd.puml` |

## 口播稿

### 0–1 范围 · ~60s

本视频覆盖 **两套详细设计**：云仓 Strategy/双轨；PDA Start→Report。分上下文，**未 API 打通**。

### 2–4 云仓 · ~195s ·【13 / 14 / 07】

Strategy（Tier/Overweight/Volumetric + Engine）→ 双轨时序（应收/应付 + 历史价）→ ERD 规则版本。

### 5–6 PDA · ~125s ·【20 / 22】

时序：扫码去 `XK|`、防叠字；挂起续开可换机；报工对齐最近开工机台。  
类图：Activity / Controller / Service / Repository；独立库无共库 FK。

### 7 收束 · ~40s

云仓管计费变体与结算协作；PDA 管现场采集约束；集成 Planned。谢谢。

---

# 视频 4 · DevSecOps Assessment

## 开场先看：本段要展示什么

| 顺序 | 展示什么 | 怎么准备 |
|------|----------|----------|
| 1 | 封面 | **未来做 PPT**：DevSecOps |
| 2 | CI 活动图 | **展示 puml**：`09-cicd-pipeline.puml` |
| 3 | Actions 绿勾 | **浏览器**：GitHub Actions 成功 CI run |
| 4 | `ci.yml` | IDE 打开 `.github/workflows/ci.yml` |
| 5 | 测试通过 | Actions log 或本地 `dotnet test` |
| 6 | coverage Artifact | Summary 页滚到底 → 下载 `coverage-report` |
| 7 | CodeQL 绿勾 | Actions → CodeQL |
| 8 | 安全控制一页 | **未来做 PPT**：白名单/大小限制/example 配置/无密钥出镜 |
| 9 | 缺口一页 | **未来做 PPT**：DAST、JWT、完整 CD = Planned |

## 口播稿

### 0–1 定义 · ~80s

本视频讲质量与安全如何进交付：**CI + 测试 + CodeQL SAST + 依赖检查 + 基线控制**。  
有证据说已做；DAST/完整 CD/生产认证 **明确缺口**。PDA 证据以云仓 Actions 为主。

### 2 CI · ~90s ·【Actions + 09】

push/PR 到 main 触发 CI：restore → test → coverage Artifact。  
绿勾证明主干可重复验证；无 SQL 的 Runner 上 DB 集成测试按设计跳过。

### 3 测试金字塔 · ~100s

单元（策略/解析/双轨等）+ 集成（WebApplicationFactory）+ 轻量并发。  
覆盖率看 **Artifact 截图**，正文不写死 >80% 口号；强调关键路径有回归。

### 4–5 CodeQL 与依赖 · ~110s

CodeQL = SAST；另有 NuGet 脆弱性检查产物。  
不等于 DAST，也不等于生产渗透。

### 6–7 已做控制与缺口 · ~140s

已做：扩展名白名单、上传大小限制、`appsettings.example.json` 脱敏。  
未宣称：DAST 门禁、完整 CD 自动发布生产、JWT/RBAC 已上线。  
用缺口清单证明没有话术掩盖。谢谢。

---

# 视频 5 · Value Added Assessment

## 开场先看：本段要展示什么

| 顺序 | 展示什么 | 怎么准备 |
|------|----------|----------|
| 1 | 封面 | **未来做 PPT**：Value Added |
| 2 | 痛点对比 | **未来做 PPT**：Excel/口头 vs 系统 |
| 3 | Context Map | **展示 puml**：`16-enterprise-context-map.puml` |
| 4 | 双轨预览 | **浏览器**：运单 Preview 汇总（报告图 3-2 同类） |
| 5 | 规则 RAG | **浏览器**：Rule RAG Tab，点一个示例问题 |
| 6 | RAG 时序（可选） | **展示 puml**：`15-sequence-quote-assistant-rag.puml` |
| 7 | PDA 真机 | **录屏**：登录→选机→开工→报工（建议 ≥40s） |
| 8 | PDA 时序（可选） | **展示 puml**：`20-sequence-pda-start-report.puml` |
| 9 | 收束页 | **未来做 PPT**：增值三点 + 诚实边界 |

## 口播稿

### 0–2 定位 · ~105s

增值 = 两条原靠 Excel/口头的链路做成可运行系统：  
结算可审计；现场无工单可采集。  
Context Map：Billing & Pricing vs Shop-floor Execution；虚线=未打通。

### 3 云仓硬增值 · ~65s ·【运单预览】

**按发货日历史价 + 应收/应付双轨**，与表内中转费对比——对得上，对不上也可解释（退件/拦截等）。

### 4 规则 RAG · ~40s ·【Rule RAG】

内置词法检索 FAQ，可带流水线步骤；**assistive only**，不参与正式计价。

### 5 PDA · ~80s ·【真机】

霍尼韦尔 PDA：无正式工单仍可开工报工；API 落独立库；与云仓 **未生产级打通**，但是执行域闭环。  
（有 MES 双写/异常巡检就提一句证据，没有就不要编。）

### 6 收束 · ~45s

边界：不打通、不 AI 计费、不数字孪生全家桶。  
三句：无工单变可追溯流水；硬件+规则控采集质量；与云仓构成结算—执行拼图。谢谢。

---

# 视频 6 · App Demo

## 开场先看：本段要展示什么

| 顺序 | 展示什么 | 怎么准备 |
|------|----------|----------|
| 0 | 可选封面 5s | **未来做 PPT** 或直接进浏览器 |
| 1 | 管理端首页 | `http://localhost:5001`（英文 UI 亦可） |
| 2 | 运单导入 Preview | **核心**：双轨对比汇总 |
| 3 | Rule RAG | 点 1 个预设问题即可 |
| 4 | PDA | 真机/模拟器录屏 40–70s |
| — | 导入全过程 | **可砍**：改口播“价表已预导入” |

> 本段 **几乎不要 PPT**；以真系统操作为主。

## 口播稿

### 开场 ~20s

本视频是 **App Demo**，演示可运行系统，少讲理论。

### 入口 ~25s

导航：Sites、Destinations、Customers、Price Rules、Cost/Quote Import、Waybill Import、Rule RAG。

### 主路径说明 ~20s

价表已预导入；直接演示运单预览双轨对比。

### 运单预览 ~95s

上传账单 → Preview → 指成功/差异笔数 → 解释例外行。  
历史价 + Strategy 引擎分别算应付/应收。

### Rule RAG ~25s

点示例查阅；强调只读、不改金额。

### PDA ~75s

登录 → 选机 → 开工 → 报工成功。说明与云仓未做生产级联调。

### 收束 ~15s

云仓可验证双轨预览；PDA 可演示无工单报工。谢谢。

---

# 视频 7 · CI/CD Demo

## 开场先看：本段要展示什么

| 顺序 | 展示什么 | 怎么准备 |
|------|----------|----------|
| 1 | 可选封面 | **未来做 PPT**：CI/CD Demo |
| 2 | 仓库 / Actions | 浏览器打开 GitHub Actions（CI、CodeQL 皆绿） |
| 3 | 一次 CI run | 展开 steps：test → Generate coverage |
| 4 | Artifacts | Summary **滚到底** 或点顶栏 Artifacts → `coverage-report` |
| 5 | `ci.yml` | IDE 打开工作流文件 |
| 6 | CodeQL | 另一个绿色 workflow |
| 7 | 活动图（可选） | **展示 puml**：`09-cicd-pipeline.puml` |

> **不必**现场再 push；用已成功 run 回放。不要说已完整 CD 自动发布生产。

## 口播稿

### 开场 ~20s

本视频展示 **CI 证据链**：自动测试、覆盖率产物、CodeQL SAST。

### 触发与绿勾 ~40s

main 的 push/PR 触发 CI；请看最近成功运行。

### CI 内部 ~90s

restore → test with coverage → ReportGenerator → Artifact `coverage-report`。  
打开 Summary/HTML 作为报告附录证据。

### 流水线即代码 ~40s

定义在 `ci.yml`，可审查、可回滚。

### CodeQL ~45s

SAST 与 CI 互补；DAST/完整 CD 仍属后续。

### 收束 ~20s

主干质量有云端证据链。谢谢。

---

## 录制总清单（七段共用）

- [ ] 出镜（至少片头/画中画）+ 1080P  
- [ ] 云仓演示前：SQL 有数据、`:5001` 已起、运单 Excel 路径熟  
- [ ] Actions 里 CI / CodeQL 皆绿再录 DevSecOps 与 CI/CD  
- [ ] PDA 真机或稳定录屏备用  
- [ ] 口播自检禁话表  
- [ ] 图：PlantUML 先导出 PNG 再进 PPT，避免录屏对着源码糊成一团  

分文件原稿仍在同目录 `speech-*-zh.md`；**以本总册为准**（已按终稿 Word 口径收敛）。
