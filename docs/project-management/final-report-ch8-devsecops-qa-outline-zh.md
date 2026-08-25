# 最终报告 · 第八章大纲（中文）— DevSecOps 与质量保障

> 对应骨架：`interim-report-writing-guide.md` §15–16 CI、§18 QA；演讲稿 `speech-devsecops-assessment-zh.md`  
> 图：`docs/diagrams/09-cicd-pipeline.puml`  
> 正文：`final-report-ch8-devsecops-qa-zh.md`

## 硬约束

1. **有证据才写 Done**：CI、xUnit+集成、Coverlet/ReportGenerator Artifact、CodeQL、NuGet `--vulnerable`（continue-on-error）
2. **禁止**：覆盖率 >80% 写死；完整 CD 已上线；DAST/ZAP 已作门禁；生产级密钥管理已完成
3. CI Runner 无 SQL Server → 依赖库用例可解释跳过（`DatabaseAvailability`）
4. 性能：1000 行解析烟测等基线，勿吹生产压测 SLA
5. PDA：本段以 CloudWarehouse Actions 为主；PDA 安全另述一句即可
6. 完整 CD / 容器生产 / JWT：Planned 或缺口清单

## 小节

- 8.1 本章范围与 DevSecOps 定义（本项目口径）
- 8.2 CI 流水线（对 ci.yml + 图 09）
- 8.3 测试金字塔与关键用例类型
- 8.4 覆盖率证据（不写死百分比口号）
- 8.5 SAST（CodeQL）与依赖扫描
- 8.6 应用层安全控制（已做）
- 8.7 性能基线（轻量）
- 8.8 诚实缺口：DAST / CD / Auth / HTTPS
- 8.9 Local vs CI
- 8.10 Evidence + 小结 → 下一章（风险/中期回应/结论视全书结构）
