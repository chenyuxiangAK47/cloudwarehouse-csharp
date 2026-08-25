# 第八章 DevSecOps 与质量保障

DevSecOps and Quality Assurance

第七章给出了 Strategy 与双轨结算的详细设计；本章说明这些设计如何被 **自动化质量门禁与安全扫描** 约束，而不是只停留在类图与时序图。原则与中期、答辩一致：**有证据的写已做，没做的写缺口与规划**，不宣称完整 DevSecOps 平台或生产级 CD 已落地。

---

## 8.1 本章范围与本项目口径下的 DevSecOps

对本实习交付，DevSecOps 落地为四层（而非营销口号）：

| 层 | 含义 | 本仓库状态 |
|----|------|------------|
| 持续集成（CI） | push/PR 自动构建与测试 | Done（GitHub Actions） |
| 质量门禁 | 单元 / 集成 / 轻量并发与性能烟测 | Done |
| 安全扫描 | SAST（CodeQL）+ NuGet 依赖脆弱性列表 | Done（依赖扫描 `continue-on-error`，作证据非硬阻断） |
| 安全基线 | 上传限制、配置脱敏示例、演示环境假设 | 部分 Done；认证等 Planned |

证据主体是 **CloudWarehouse** 的 `.github/workflows/*`。PDA 无订单报工为并列系统：强调经 API 访问、独立库、内网演示假设；其流水线证据不与云仓混称为同一套已打通的安全网格。

活动图：`docs/diagrams/09-cicd-pipeline.puml`。

---

## 8.2 持续集成流水线

工作流文件：`.github/workflows/ci.yml`。

**触发：** 面向 `main` / `master` 的 `push` 与 `pull_request`。  
**Runner：** `ubuntu-latest` + .NET SDK `9.0.x`（与本地 Windows 开发形成跨平台校验）。

**典型步骤：**

1. `actions/checkout`  
2. `setup-dotnet`  
3. `dotnet restore CloudWarehouse.sln`  
4. `dotnet test`（Release）+ Coverlet（`coverlet.runsettings`，XPlat Code Coverage）→ `./coverage`  
5. 安装 ReportGenerator → 生成 HTML / TextSummary → `coveragereport/`  
6. 打印 `Summary.txt`  
7. `dotnet list ... package --vulnerable --include-transitive` → `vulnerable-packages.txt`（失败不阻断流水线，仍上传产物）  
8. 上传 Artifact：`coverage-report`、`coverage-cobertura`、`nuget-vulnerable-scan`

这解决“只在自己电脑能过”的风险：主干合并前有同一套客观结果。  
说明：本流水线是 **CI + 质量/安全产物**；**不是**已实现面向生产环境的完整 Continuous Deployment（自动发布到客户生产机）。发布目前以自包含包 / 手工或检查清单部署为主（见架构章与发布文档）。

---

## 8.3 测试金字塔与关键验证类型

| 层次 | 项目 / 手段 | 验证重点（与第七、五章对齐） |
|------|-------------|------------------------------|
| 单元 | `CloudWarehouse.Tests` | Strategy（区间/续重/体积重）、Excel 解析与映射、历史价辅助、重量取整、规则检索评测、解析性能烟测等 |
| 集成 | `CloudWarehouse.IntegrationTests` + `WebApplicationFactory` | 导入 / 主数据 / 客户报价 / 运单 Bill API 等 HTTP 路径 |
| 轻量压力/并发 | 如 `StressLoadTests` | 模板下载、预览等并发冒烟，演示级而非生产 SLA 认证 |

**环境诚实策略：** GitHub Actions 通常 **无常驻 SQL Server**。依赖真实库的集成用例通过 `DatabaseAvailability` 等逻辑在连不上库时 **可解释跳过**，避免流水线“假红”；本地有 SQL Server 时则跑完整路径。这是环境适配设计，不是隐瞒失败。

测试的设计目标优先保证：**计费引擎重构与双轨逻辑可回归**，而不是追求虚高覆盖率数字。

---

## 8.4 覆盖率证据（如何引用、如何不写错）

覆盖率由 Coverlet 采集、ReportGenerator 出 HTML，并作为每次 CI 的 Artifact 归档。报告附录应粘贴：

- Actions 某次成功 run 的绿勾截图；  
- `coverage-report` 中 Summary / 总览页截图。

**禁止在正文写死「覆盖率 >80%」之类无法随构建漂移的口号。** 正确写法是：覆盖率报告随 CI Artifact 可查，关键模块（Pricing Core Billing、Import 解析、Bill 双轨相关）有自动化用例保护；具体百分比以附录截图当日值为准。

---

## 8.5 SAST 与依赖供应链扫描

### 8.5.1 CodeQL（SAST）

工作流：`.github/workflows/codeql.yml`（`CodeQL SAST`）。

- 触发：`push` / `PR` 到主干，以及每周定时（`cron`）；  
- 语言：C#；查询集：`security-and-quality`；  
- 步骤：初始化 CodeQL → restore/build → `codeql-action/analyze`。

SAST 用于合并前发现可自动识别的缺陷模式；**不能替代**设计评审，也 **不等于** 动态渗透（DAST）。发现问题的处理路径：修复 → 重跑至绿 → 再合入。

### 8.5.2 NuGet 脆弱依赖列表

CI 中执行 `dotnet list package --vulnerable --include-transitive`，结果写入 Artifact。当前策略为 **可见性优先**（`continue-on-error: true`）：先保证主干可集成，同时留下供应链风险清单，便于人工跟进补丁；若答辩被问，可说明后续可改为门禁阈值。

---

## 8.6 应用层安全控制（已做）

| 控制 | 说明 |
|------|------|
| 上传白名单 | 价表/运单等接口限制 `.xlsx` / `.xlsm` 等扩展名 |
| 大小限制 | 降低超大文件 DoS 面（与风险登记中的大文件上传项对应） |
| 配置脱敏 | 提供 `appsettings.example.json`；真实连接串留在本地/部署机，不把密钥当模板传播 |
| 演示假设 | 本机/受控内网可达；**认证/RBAC 按 ADR 延期**（写入风险与里程碑 Planned） |

这些是 MVP「先交付结算价值、安全分阶段加强」下的务实基线，不是零信任生产声明。

---

## 8.7 性能基线（轻量）

为报告提供可复现数量级，而非生产压测证书：

- **解析烟测：** 如 `Import1000RowPerfTests`——标准价表约 1000 行、无 SQL 的纯解析路径，阈值例如 &lt;30s（实际本地/CI 常见为秒级，以测试输出与附录为准）；  
- **计费烟测：** `FeeCalculationPerfSmokeTests` 等对引擎循环调用的基线；  
- 可选工具：`tools/PerfExcelGen` 生成大表样本。

表述建议：给出方法、环境（CI 无库 / 本地有库）与一次实测数字截图；避免虚构「支持百万单/日」类 SLA。

---

## 8.8 诚实缺口与规划

| 项 | 状态 | 说明 |
|----|------|------|
| DAST（如 OWASP ZAP） | 未作常态门禁 | 可规划对演示环境做基线扫描 |
| 完整 CD 自动发布生产 | 未做 | 现有为 CI + 手工/检查清单发布包 |
| 容器镜像扫描（Trivy 等） | 非主路径 | 主交付偏 self-contained；不假装已全面容器化生产 |
| JWT / RBAC | Planned | ADR 延期认证 |
| HTTPS / 收紧 CORS | 上线前必做 | 开发期便利配置需收紧 |
| 密钥保管（Vault 等） | 未做 | 示例配置 + 部署机本地配置 |

评分叙事：**CI + 测试 + SAST + 依赖可见性 + 明确 backlog** = 工程化安全意识；缺口清单 = 未用话术掩盖。

---

## 8.9 本地开发 vs CI 环境

| 方面 | 本地 | CI（GitHub Actions） |
|------|------|----------------------|
| OS | 通常 Windows | `ubuntu-latest` |
| SQL Server | 常驻可用 | 通常无；依赖库测试跳过或受限 |
| 覆盖率 | 可选手工跑 | 每次强制生成并上传 Artifact |
| 状态 | 有状态开发机 | 无状态临时 Runner |

跨环境差异本身是 DevOps 证据：质量门禁不绑定某一台开发电脑。

---

## 8.10 本章证据清单

| 证据 | 位置 |
|------|------|
| CI 工作流 | `.github/workflows/ci.yml` |
| CodeQL 工作流 | `.github/workflows/codeql.yml` |
| CI/CD 活动图 | `docs/diagrams/09-cicd-pipeline.puml` |
| Actions 绿勾 | GitHub Actions 成功 run 截图 |
| 覆盖率 Artifact | `coverage-report` / Summary 截图 |
| NuGet 扫描产物 | `nuget-vulnerable-scan` Artifact |
| 测试代码 | `CloudWarehouse.Tests`、`CloudWarehouse.IntegrationTests` |
| 跳过策略 | `DatabaseAvailability` 等 |
| 性能烟测 | `Import1000RowPerfTests`、`FeeCalculationPerfSmokeTests` |

---

## 8.11 本章小结

本章表明：Strategy 与双轨等设计变更处在可重复的 CI 与测试金字塔保护下，并以 CodeQL 与依赖扫描补齐基础安全可见性；同时明确 DAST、完整 CD、认证与传输加密仍为缺口或规划项。下一章可将 **风险管理、中期反馈逐条回应与结论/展望** 收束全书（或按学校目录拆为独立章）。
