# DevSecOps Assessment — 中文演讲稿 + 展示清单

> 视频文件：`…Technical Assessment - DevSecOps.mp4`  
> 时长：约 **10 分钟**（可 9–11 分钟）  
> 口径：有 CI + 测试金字塔 + SAST（CodeQL）+ 依赖扫描；DAST/容器为可选或规划——**不装全链路已完成**

---

## 开场前：要准备什么（先截好图）

| 顺序 | 展示什么 | 哪里来 |
|------|----------|--------|
| 1 | 封面 | 自制：DevSecOps Assessment |
| 2 | CI/CD 活动图 | `docs/diagrams/09-cicd-pipeline.puml` → PNG |
| 3 | GitHub Actions 绿勾 | 仓库 Actions → 最新成功 run（CI） |
| 4 | `ci.yml` 关键步骤 | 打开 `.github/workflows/ci.yml` |
| 5 | 本地 / CI 测试通过 | `dotnet test` 终端 或 Actions log |
| 6 | 覆盖率 Artifact | Actions → coverage-report 下载/打开 Summary |
| 7 | CodeQL 绿勾 | Actions → CodeQL 工作流成功 |
| 8 | NuGet 漏洞扫描（若有 artifact） | CI 中 vulnerable packages 步骤/产物 |
| 9 | 安全控制一页（PPT） | 上传白名单、大小限制、无密钥进库、CORS 说明 |
| 10 | 可选：Dockerfile / Trivy | 有则录 20 秒；没有就口头 Phase 规划 |
| 11 | 收束：缺口与下一步 | PPT：DAST、JWT、密钥保管 |

录制前打开这两个页面备用：
- `https://github.com/chenyuxiangAK47/cloudwarehouse-csharp/actions`
- 本地：`dotnet test CloudWarehouse.sln`

---

## 演讲稿（照着念）

### 0. 封面 · 约 30 秒  
**【展示：封面】**

各位老师好。本视频是 **Technical Assessment — DevSecOps**。  
我将说明 CloudWarehouse 如何把质量与安全嵌入交付：持续集成流水线、自动化测试、静态应用安全测试，以及当前已知的安全控制与缺口。  
原则是：**有证据的说已做，没做的明确规划**，不宣称完整 DevSecOps 平台已经落地。

---

### 1. DevSecOps 在本项目的定义 · 约 50 秒  
**【展示：09 CI/CD 图 或 自制「DevSecOps 范围」页】**

对本实习项目，DevSecOps 落地为四层：  
1. **持续集成**：每次 push/PR 自动构建与测试；  
2. **质量门禁**：单元、集成、轻量压力/并发测试；  
3. **安全扫描**：CodeQL SAST，以及依赖脆弱性检查；  
4. **安全基线**：上传限制、配置脱敏、内网演示假设。  

PDA 报工是并列系统，本段证据以 CloudWarehouse 的 GitHub Actions 为主；PDA 侧同样强调不直连库、经 API 访问，安全演进单独跟踪。

---

### 2. CI 流水线（证据）· 约 90 秒  
**【展示：Actions 绿勾 → 点进一次成功 run → 切 `ci.yml`】**

持续集成使用 GitHub Actions。触发条件是面向 `main` 的 push 与 pull request。  

典型步骤包括：还原依赖、构建、执行 `dotnet test`、收集覆盖率并上传 Artifact；同时有依赖脆弱性相关检查产物。  
请看当前仓库最新成功运行：状态为绿色，说明主干在合并前经过自动验证。  

这解决了“只在自己电脑能过”的风险：任何人拉代码，CI 给出同一套结果。  
数据库相关集成测试在无 SQL Server 的云端 Runner 上会安全跳过，避免假红；有库的环境则跑完整路径——这在测试设计里写清楚，属于诚实的环境适配，不是隐瞒失败。

---

### 3. 测试金字塔 · 约 100 秒  
**【展示：测试通过终端 或 Actions 测试 log；可列三类】**

质量策略按测试金字塔组织：  

- **单元测试**：计价策略、Excel 解析、规则映射、规则检索评测、性能烟测等，不依赖外部库即可验证业务规则；  
- **集成测试**：用 `WebApplicationFactory` 打 API，覆盖导入、主数据、运单等关键路径；  
- **压力/并发**：例如模板下载与预览的并发用例，以及可选 k6 脚本做演示级冒烟。  

覆盖率通过 Coverlet 与 ReportGenerator 生成，并作为 CI Artifact 保存，便于报告附录引用。  
这里的重点不是追求虚高百分比，而是：**关键计费与导入路径有自动化回归**，防止 Strategy 重构与双轨逻辑回退。

---

### 4. SAST — CodeQL · 约 70 秒  
**【展示：CodeQL workflow 绿勾】**

静态应用安全测试使用 GitHub **CodeQL**，工作流针对 C# 代码库运行。  
请看 CodeQL 工作流的成功记录：说明提交会经过静态规则扫描，用于发现常见缺陷模式。  

SAST 的价值是在合并前发现可自动识别的问题；它不能替代人工设计评审，也不等于动态渗透测试。  
若扫描发现问题，处理路径是修复后重跑至绿色，再合入主干。

---

### 5. 依赖与供应链 · 约 40 秒  
**【展示：CI 中 NuGet vulnerable / 相关 artifact 若有】**

除 CodeQL 外，流水线关注 NuGet 依赖脆弱性信息，并保留检查产物，便于追踪第三方组件风险。  
演示环境依赖版本固定在项目文件中，避免“本机能跑、别人解析到别的包”的漂移。  
这属于供应链安全的基础动作，仍需在正式上线前结合补丁策略持续看护。

---

### 6. 应用层安全控制（已做）· 约 70 秒  
**【展示：安全控制 PPT；可闪 `BillController`/`Import` 上传限制代码】**

在应用与配置层面，当前已做：  
- 上传文件扩展名白名单（如 `.xlsx` / `.xlsm`）与请求大小限制，降低恶意文件与超大上传风险；  
- 仓库中提供 `appsettings.example.json`，避免把真实密钥当模板传播；本地连接串使用开发机配置，不在演讲中展示密钥；  
- 演示部署默认内网/本机可达假设；认证在 ADR 中明确延期到后续，并写入风险登记。  

这些是 MVP 阶段的务实控制，匹配“先交付结算价值、安全按阶段加强”的决策。

---

### 7. 诚实缺口与规划 · 约 70 秒  
**【展示：缺口与下一步 PPT】**

尚未宣称完成、但已识别的项：  
- **DAST**（如 ZAP）未作为门禁常态运行——可列为后续对演示环境做基线扫描；  
- **容器镜像扫描（Trivy）**：仓库可有 Dockerfile 作演示选项，但主交付仍是 self-contained 部署，不假装已全面容器化生产；  
- **JWT/RBAC、收紧 CORS、HTTPS**：在架构与风险章节已规划，属于上线前必做，而非本期已关闭。  

DevSecOps 评分上，我用 **CI + 测试 + SAST + 依赖检查 + 明确 backlog** 证明工程化安全意识；用缺口清单证明没有用话术掩盖。

---

### 8. 收束 · 约 40 秒  
**【展示：Actions 总览回看】**

总结：CloudWarehouse 已具备可重复的 CI、自动化测试与覆盖率证据、CodeQL SAST，以及基础上传与配置安全控制。  
下一步是按优先级补 DAST 基线、认证与传输加密，并在报告附录固定截图证据。  
PDA 并列系统遵循同样原则：经 API 访问、内网部署、安全能力随正式化加强。谢谢。

---

## 时间轴（约 10 分钟）

| 段 | 内容 | 秒 |
|----|------|-----|
| 0 | 封面 | 30 |
| 1 | 范围定义 | 50 |
| 2 | CI 流水线 | 90 |
| 3 | 测试金字塔 | 100 |
| 4 | CodeQL SAST | 70 |
| 5 | 依赖扫描 | 40 |
| 6 | 应用安全控制 | 70 |
| 7 | 缺口与规划 | 70 |
| 8 | 收束 | 40 |
| | **合计** | **~560 秒 ≈ 9.3 分** |

可再加 30–60 秒现场点开 coverage HTML。

---

## 录制检查

- [ ] Actions 里 CI、CodeQL 都是绿色再录  
- [ ] 口播出现：SAST 已做、DAST 未作为门禁  
- [ ] 不说“完善 DevSecOps / 全链路安全”  
- [ ] 不展示真实密码、连接串  
- [ ] 出镜 + 1080P
