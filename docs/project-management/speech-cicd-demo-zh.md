# CI/CD Demo — 中文演讲稿 + 展示清单

> 视频文件：`…Presentation Assessment CICD Demo.mp4`  
> 时长：**≤ 5 分钟**（建议 3.5–4.5 分）  
> 原则：**GitHub Actions 真画面**；证明 push/流水线/产物；可顺带 CodeQL

---

## 开场前准备

| 顺序 | 展示什么 | 说明 |
|------|----------|------|
| 1 | 可选封面 | CI/CD Demo |
| 2 | 仓库首页 | `github.com/chenyuxiangAK47/cloudwarehouse-csharp` |
| 3 | **Actions 总览** | CI 与 CodeQL 均为成功（绿） |
| 4 | 点进一次 **CI** run | 展示 jobs/steps：restore → test → coverage |
| 5 | 打开 **coverage** Artifact | 下载或展开 Summary.txt / HTML |
| 6 | 打开 `.github/workflows/ci.yml` | 指触发条件与关键 step |
| 7 | **CodeQL** run（可选 40 秒） | SAST 证据 |
| 8 | 可选：本地 `git log` / 最近 commit | 说明主干受保护于 CI |

**不必**现场真的再 push 一次（易翻车）；用**已成功的 run**回放即可。若老师爱看触发过程，可提前准备“空 commit push”但要网络稳。

---

## 演讲稿（边点边说）

### 0. 开场 · 约 20 秒  
**【画面：封面或仓库 Actions】**

各位老师好。本视频是 **CI/CD Demo**，展示 CloudWarehouse 的持续集成证据：代码进入 GitHub 后，如何自动测试、产出覆盖率，并配合 CodeQL 做静态安全扫描。

---

### 1. 仓库与触发 · 约 40 秒  
**【画面：Actions 列表】**

项目托管在 GitHub。对 `main` 的 push 与 pull request 会触发 CI 工作流。  
请看 Actions 页面：最近的 **CI** 运行状态为成功。这说明主干不是只靠本地“我觉得能过”，而是云端同一套命令验证。

---

### 2. 一次成功 CI 的内部步骤 · 约 90 秒  
**【画面：点进绿色 run → 展开 steps】**

进入一次成功的 CI run。步骤包括：检出代码、安装 .NET 9、`dotnet restore`、带覆盖率的 `dotnet test`，以及用 ReportGenerator 生成覆盖率报告并上传 Artifact。  

测试涵盖单元测试与集成测试；无 SQL Server 的 Runner 上，依赖数据库的用例会按设计跳过，避免环境假失败——有库的本地/内网仍跑完整路径。  

请看测试日志末尾为通过。再打开 Artifact：**coverage-report**，可查看覆盖率摘要。这份产物可直接贴进最终报告附录。

---

### 3. 流水线即代码 · 约 40 秒  
**【画面：ci.yml】**

CI 定义在 `.github/workflows/ci.yml`，版本进库，可审查、可回滚。  
任何人改计费或导入逻辑，合并前都要过同一门禁，降低“演示机能跑、别人拉下来挂”的风险。

---

### 4. SAST 顺带证据 · 约 45 秒  
**【画面：CodeQL 工作流绿勾】**

安全侧另有 **CodeQL SAST** 工作流，对 C# 做静态分析。  
请看最近一次 CodeQL 成功运行。它与 CI 互补：CI 偏功能回归，CodeQL 偏缺陷模式扫描。  
DAST 与完整 CD 部署到生产环境仍属后续，本视频聚焦**已落地的 CI + 测试产物 + SAST**。

---

### 5. 收束 · 约 20 秒  
**【画面：回到 Actions 总览】**

CI/CD Demo 小结：CloudWarehouse 具备可重复的 GitHub Actions 流水线、自动化测试与覆盖率 Artifact，以及 CodeQL 静态扫描记录。主干质量有云端证据链。谢谢。

---

## 时间轴（约 4 分钟）

| 段 | 秒 |
|----|-----|
| 开场 | 20 |
| 触发与绿勾 | 40 |
| CI 步骤 + Artifact | 90 |
| ci.yml | 40 |
| CodeQL | 45 |
| 收束 | 20 |
| **合计** | **~255 秒 ≈ 4.3 分** |

---

## 录制检查

- [ ] 录前确认 CI、CodeQL 都是绿的  
- [ ] 不要展示 secrets / 真实连接串  
- [ ] 1080P；鼠标移动慢一点方便老师看  
- [ ] 不说“已完整 CD 自动发布生产”  
- [ ] 出镜：片头或画中画即可
