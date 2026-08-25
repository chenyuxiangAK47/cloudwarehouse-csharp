# 最终报告 · 第二大段大纲（中文）— 技术栈 Technology Stack

> 对应原骨架：`interim-report-writing-guide.md` **§3 Technology Stack**  
> 扩写后目标约 **4–6 页**（含选型对比表 + 分层示意/截图占位）  
> 用法：复制文首「给 AI 指令」+ 下方大纲，让 AI 生成正文（建议英文终稿）

---

## 给 AI 的扩写指令（复制用）

```text
请根据下列「第二大段：技术栈」中文大纲，撰写 CloudWarehouse（及并列 PDA）最终实习报告正文。

要求：
1. 输出：正式学术英文；小标题英文（3.1 / 3.2…）
2. 严格按大纲 2.1–2.7 结构，不要编造未列出的技术（如 Kubernetes、Kafka、EF Core 已全面采用等）
3. 每个重要选型必须有「Alternatives → Chosen → Rationale」；可做成表格
4. CloudWarehouse 与 PDA 分小节写清技术栈，避免混成一个系统
5. 禁止：微服务已上线、AI 计费引擎、生产级 HA、已与 PDA API 打通
6. 每节末尾加 Evidence 占位（Figure/Table/Appendix）
7. 篇幅约 1000–1600 英文词
8. 与第一章一致：Solo、Modular Monolith、Strategy 在 Pricing.Core、CI+CodeQL
```

---

## 大纲正文（第二大段）

### 2. 章标题建议
- 中文：技术栈与关键技术决策  
- 英文：Technology Stack and Key Technical Decisions

### 2.1 总览（Stack Overview）
用一张总表列出两套系统：

**CloudWarehouse**
| 层 | 技术 |
|----|------|
| 语言/运行时 | C# / .NET 9、ASP.NET Core |
| API | REST Controllers（模块：MasterData / Import / Pricing / Billing / Assistant） |
| 前端 | 静态 HTML/JS（`wwwroot/index.html`），非 React/Angular SPA |
| 数据访问 | Dapper + 参数化 SQL |
| 数据库 | Microsoft SQL Server |
| Excel | ClosedXML |
| 计费核心 | `CloudWarehouse.Pricing.Core`（Strategy：Tier / Overweight / Volumetric） |
| 测试 | xUnit、WebApplicationFactory 集成测试、轻量并发/压测 |
| CI/安全 | GitHub Actions、Coverlet/ReportGenerator、CodeQL SAST |

**PDA 无订单报工**
| 层 | 技术 |
|----|------|
| 客户端 | Android（霍尼韦尔工业 PDA）+ 扫码 SDK |
| API | Spring Boot 3.x / Java 21 |
| 数据库 | SQL Server（自建库如 `PDA_NoOrder`；可双写 MES 报工表——按你真实情况写，勿夸大未做的） |
| 通信 | 内网 HTTP（明文，匹配车间演示环境） |

点明：双栈是刻意选择（.NET 结算域 vs Java/Android 现场域），不是技术堆砌。

### 2.2 分层映射（仅 CloudWarehouse 细写）
按请求路径写清：
1. **Presentation**：浏览器静态页  
2. **API**：Controllers 校验与编排  
3. **Application/Services**：Import / Calculate / BillImport / QuoteAssistant  
4. **Domain/Helpers + Pricing.Core**：Excel 解析、规则映射、FeeCalculationEngine + Strategies  
5. **Infrastructure**：Dapper、SqlConnection、文件流上传限制  

可插：逻辑架构图引用（Figure → `docs/diagrams/02-logical-architecture.puml`）。

### 2.3 关键选型对比（必须有表）
至少覆盖这些行（选项 / 选择 / 理由）：

| 决策点 | 备选 | 选择 | 理由（写进大纲给 AI） |
|--------|------|------|------------------------|
| 数据访问 | EF Core vs Dapper | **Dapper** | 导入重、要精细 SQL/事务；Solo 工期下更可控 |
| 架构风格 | Microservices vs Modular Monolith | **Modular Monolith** | 单人、快速交付、同库事务；用模块/DDD 留拆分缝 |
| UI | SPA vs 静态页 | **静态 HTML/JS** | 精力放后端架构与计费设计，满足 MVP |
| Excel | EPPlus vs ClosedXML | **ClosedXML** | 读复杂表头 + 写模板/导出一体 |
| 规则维护 | 手工 CRUD 价表 vs Excel-only | **Excel 导入为主** | 贴合供应商/师傅现有工作流（ADR） |
| 计费扩展 | 巨型 if/else vs Strategy | **Strategy Pattern** | 回应中期反馈；开闭扩展（体积重已验证） |
| CI | 仅本地测 vs GitHub Actions | **Actions** | 可验证证据、覆盖率 Artifact |
| SAST | 无 vs CodeQL | **CodeQL** | DevSecOps 证据；不声称已做完整 DAST |

PDA 侧可补一行：为何 Android 原生/工业 PDA 而非纯手机 H5（车间耐用、扫码枪集成）。

### 2.4 开发与文档工具
- Git + GitHub  
- PlantUML（架构/类图/时序，版本可控）  
- SSMS / sqlcmd  
- .NET CLI；Android Studio / Gradle（PDA）  
- 规划文档：sprint 计划、工时 CSV、演讲稿与报告大纲  

### 2.5 运行与配置要点（诚实）
- CloudWarehouse 演示默认 HTTP，端口 **5001**；SQL **1433**  
- 配置：`appsettings.json` + `appsettings.example.json`（示例脱敏）  
- 上传：扩展名白名单 + 大小限制  
- 认证：MVP 延期（ADR），不在本章伪装已实现  
- PDA：内网 API 地址需与现场 IP 一致；防火墙放行等运维细节可一笔带过  

### 2.6 与质量/安全工具链的衔接（点到为止）
- 测试项目：`CloudWarehouse.Tests` / `IntegrationTests` / `TestCommon`  
- CI：`ci.yml`；SAST：`codeql.yml`  
- 细节放到后文 QA / DevSecOps 章，本章只说明“技术栈包含这些门禁”

### 2.7 Evidence 清单（扩写时插入占位）
- Table：技术栈总览、选型对比  
- Figure：逻辑分层 / 解决方案结构截图（.sln 多项目）  
- Figure：PDA 工程结构或设备实拍（可选）  
- Appendix：`Program.cs` DI 注册片段（Strategy 注册顺序）、CI badge/截图链接  

### 2.8 写作禁区
- 不写 EF Core 已全面替换 Dapper  
- 不写已上 K8s / 服务网格  
- 不写 RAG/LLM 是结算核心  
- “CI/CD” 可写 CI 扎实；CD 自动发布生产若未做就写 “CI-focused，CD planned”

---

## 与第一章衔接句（给 AI 放在 2.1 开头）
第一章界定了双系统目标与边界；本章说明支撑这些目标的技术选型，并为后文 Architecture、Software Design、DevSecOps 提供技术上下文。

---

## 你扩写后自检
- [ ] 云仓与 PDA 技术栈分开写清  
- [ ] 至少一张 Alternatives 对比表  
- [ ] Modular Monolith + Dapper + Strategy 理由齐全  
- [ ] 无吹微服务/打通/AI 计费
