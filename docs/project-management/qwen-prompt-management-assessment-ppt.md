# 千问指令：生成 CloudWarehouse — Management Assessment PPT

> 用法：把**下面「复制给千问」整段**（从标题到文末）粘贴给千问/通义。  
> 目标产物：7 页 16:9 PPT（或先出 Marp Markdown，再导入 PPT）。  
> 视频文件名：`…Management Assessment.mp4`｜总时长约 **5 分钟**。

---

## 复制给千问（从下一行开始）

```text
请为 NUS MTech SE（SE33）实习项目 CloudWarehouse 生成「Management Assessment」演示文稿终稿。

【输出要求】
1. 先给 5 分钟时间轴（P1–P7，每页 40–50 秒）
2. 再按页输出：英文标题 + 中文标题 + 投影用短 bullet（每页 4–7 条）+ 中文口播稿（该页 40–50 秒可直接朗读）
3. 最后输出完整 Marp Markdown（marp: true, size: 16:9），可直接用 VS Code Marp 导出 PPT/PDF
4. 每页注明「建议插入」的真实资产（见下方路径）；不要虚构 Jira/现场照片文件名
5. 风格：简洁商务、适合出镜旁白；禁止花哨动画说明

【项目硬事实——必须遵守，禁止编造】
- 项目名：CloudWarehouse（云仓运费/结算）
- 技术：ASP.NET Core 9 + SQL Server + Dapper + ClosedXML + 静态前端
- 架构：Modular Monolith；模块：MasterData / Import / Pricing / Billing / Assistant
- 作者：Solo Intern（无 Peer Assessment）
- Phase1（Sprint1–4）：主数据 CRUD、成本价/客户报价 Excel 导入、运费试算、GitHub Actions CI、单测/集成/压测
- Phase2（Sprint5 起已交付）：Strategy Pattern（区间/续重/体积重）、运单双轨应收应付、按发货日历史价、计价规则检索（assistive lookup，不是结算真相源）
- Sprint 长度 = 1 周（不是 2 周）
- 工时单位 = 小时（不是人天），Solo 个人表必须使用：
  Sprint1 Planned 48 / Actual 52（+8%）
  Sprint2 Planned 44 / Actual 61（+39%）——主因：供应商 legacy 三级表头 Excel 解析与双格式兼容
  Sprint3 Planned 56 / Actual 51（-9%）
  Sprint4 Planned 50 / Actual 47（-6%）
  Phase1 合计 Planned 198 / Actual 211（+7%）
  Sprint5（Phase2）Planned ≈46 / Actual ≈46（Strategy+双轨/历史价+规则检索+报告同步；可标注“作者可微调”）
- 中期导师反馈要点：偏简单、要计费设计模式与详细设计、多架构视角、单体需自辩、工作需可验证证据
- 企业协作：与仓库师傅有需求澄清/演示反馈；Client Feedback 为评分项，需提醒准备正式反馈通道
- PDA「MES 无订单报工」仅可在 Management 一笔带过为并列能力，不展开，且不得声称已 API 打通

【禁止话术】
微服务已上线 / 完整 DDD 落地 / 生产级高可用 / RAG 智能计费 / AI 替代结算引擎 / 已与 PDA API 打通

【固定 7 页结构】
P1 Cover：CloudWarehouse — Management Assessment；Solo；日期
P2 Business Problem & Goals：痛点、用户、In Scope / Out of Scope
P3 Agile Approach：1 周 Sprint、MVP→Phase2、如何用管理动作回应中期反馈
P4 Backlog Snapshot：Must=已交付；Should=已补证据/体验优化（多数 Done）；Could=未来
P5 Sprint Effort：填入上表真实小时 + Sprint2 超支分析 + 后续缓冲措施
P6 Stakeholder Collaboration：师傅沟通、演示反馈闭环、Client Feedback 提醒
P7 Risks & Next：进度/范围/架构质疑风险与缓解；后续 Architecture/Design/DevSecOps/App/CI 视频

【建议插入的真实路径（写在每页备注里即可）】
- docs/project-management/sprint-hours-chart.html（工时柱状图截屏）
- docs/project-management/sprint-hours-chart-data.csv
- docs/diagrams/10-roadmap-milestones.puml
- docs/diagrams/02-logical-architecture.puml
- docs/diagrams/13-billing-strategy-class.puml
- log（中期反馈摘要一页）
- 作者自备：师傅演示备注或打码聊天截图（不要编路径）

请现在生成全部内容与完整 Marp 源码。
```

---

## 你本地收尾（千问出稿后）

1. 用 VS Code Marp 打开生成的 `.md` → Export PPTX/PDF  
2. 打开 `docs/project-management/sprint-hours-chart.html` 截屏插入 P5  
3. 核对 P5 数字与上表一致  
4. 出镜练习：总时长卡在 5 分钟内  
5. 导出视频命名对齐官方：`…Management Assessment.mp4`（1920×1080）
