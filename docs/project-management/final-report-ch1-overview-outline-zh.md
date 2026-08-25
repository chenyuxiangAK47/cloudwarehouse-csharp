# 最终报告 · 第一大段大纲（中文）— 项目概述 Project Overview

> 用途：把本大纲整段丢给 AI（千问/通义等）扩写成**英文终稿正文**（或先中文再译，按你学校要求）。  
> 对应原骨架：`interim-report-writing-guide.md` 的 **§2 Project Overview**（可扩成终稿第 1 章/第 2 章）。  
> 页数目标：本大段扩写后约 **4–6 页**（含 1–2 张图/表）。

---

## 给 AI 的扩写指令（复制用）

```text
请根据下列「第一大段：项目概述」中文大纲，撰写 CloudWarehouse 最终实习报告的正文。

要求：
1. 输出语言：正式学术英文（NUS MTech SE 风格），小标题可用英文
2. 结构严格按大纲的 1.1–1.7，不要合并丢节
3. 事实只能用大纲里的内容，禁止编造未列出的功能/集成/微服务已上线
4. 每小节末尾加 Evidence 提示句（Figure/Table/Appendix 占位）
5. 明确回应中期反馈：曾被指“偏简单/单体”，本段只点明“后续章节用 Strategy、多架构图、证据回应”，不在本段展开类图
6. Solo intern；CloudWarehouse + PDA 为并列交付；未 API 打通
7. 篇幅：约 1200–1800 英文词
```

---

## 大纲正文（第一大段）

### 1. 标题建议
- 中文：云仓运费结算与工厂现场报工数字化 — 项目概述  
- 英文：Project Overview — CloudWarehouse Freight Settlement and Shop-floor No-Order Reporting

### 1.1 项目一句话与背景
- 实习项目面向制造业/云仓场景的运营数字化。
- 主交付一：**CloudWarehouse**（ASP.NET Core 9）— 运费规则导入、试算、运单应收应付双轨对账。
- 并列交付二：**PDA「MES 无订单报工」**（霍尼韦尔 PDA + Spring Boot API）— 夜班/无正式工单时的开工报工采集。
- 作者角色：**Solo Intern**（独立完成需求、设计、实现、测试、文档）。
- 说明：两套系统服务同一工厂目标，按限界上下文分开演进；**当前未做生产级 API 打通**。

### 1.2 业务痛点（Why）
**仓库/结算侧**
- 供应商/师傅报价与账单长期依赖 Excel，格式不一（含三级表头等）。
- 手工对账易错；“用最新价”会导致历史账单系统性偏差。
- 缺少可重复的试算与预览提交流程。

**产线侧**
- 夜班/插单常无正式 MES 工单，纸笔或口头报工难追溯。
- 需要工业手持终端上可扫码、可落库的极简流程。

### 1.3 项目目标（What）
**CloudWarehouse 目标**
- 主数据：站点 / 目的地 / 客户可维护。
- Excel 导入成本价与客户报价（预览 → 校验 → 事务入库）。
- 运费试算；运单预览双轨（应收报价 vs 应付成本）；按发货日取历史价。
- 计费用 Strategy Pattern（区间 / 续重 / 体积重）提升扩展性。
- 辅助：计价规则检索（查阅用，不替代结算引擎）。
- 工程：自动化测试 + GitHub Actions CI + CodeQL SAST。

**PDA 目标**
- 登录 → 产线/机群/机床 → 开工/报工/查询。
- 硬件扫码；数据经 API 落库；支持无工单场景可追溯。

### 1.4 范围：In Scope / Out of Scope
**In Scope（本期已交付叙事）**
- CloudWarehouse Modular Monolith MVP + Phase2 计费/双轨/检索。
- PDA 无订单报工 MVP。
- 架构图、设计图、CI/测试证据、最终视频材料。

**Out of Scope（明确不吹）**
- 云仓与 PDA 生产级集成总线 / 已微服务拆分上线。
- 完整 JWT/RBAC 生产认证（ADR：延期，有规划）。
- 生产级高可用集群。
- “AI 智能计费 / RAG 替代结算引擎”。

### 1.5 干系人
- 仓库管理员 / 结算相关人员（CloudWarehouse 主用户）。
- 产线操作员（PDA 主用户）。
- 企业师傅/业务方（需求澄清、演示反馈；Client Feedback 评分相关）。
- 学术导师（中期反馈：深度、设计模式、多架构图、证据）。

### 1.6 中期反馈与本报告回应方式（本段只点题）
老师要点：偏简单；单体需自辩；计费变体要设计模式+详细设计；多视角架构图；工作需证据。  
本报告回应路径（指向后文，本段不展开）：
- Strategy 类图/时序 → Software Design 章  
- 逻辑/物理/部署/DDD/企业 Context Map → Architecture 章  
- CI/测试/SAST 截图 → DevSecOps / QA 章  
- 个人工时 Planned vs Actual → Management / Sprint 章  
- Value Added：双轨历史价 + PDA 硬件现场闭环  

### 1.7 交付物快照（给读者地图）
用表格列出即可：

| 交付物 | 状态 |
|--------|------|
| 可运行 CloudWarehouse（导入/试算/运单双轨） | Done |
| Strategy 计费 + 类图/时序 | Done |
| CI + 测试 + CodeQL | Done |
| 计价规则检索（辅助） | Done |
| PDA 无订单报工 MVP | Done |
| 最终 7 段视频 / 本报告 | In progress / 本交付 |

**建议插图**
- Figure：企业 Context Map（`docs/diagrams/16-enterprise-context-map.puml`）  
- Table：In/Out of Scope  
- Evidence：系统首页截图、PDA 登录/报工各 1 张（附录可再放大）

### 1.8 本大段写作禁区（给 AI）
- 禁止写：已与 PDA API 打通、微服务已上线、完整 DDD 框架、生产级 HA、AI 自动计价。  
- 单体要写成 **刻意选择的 Modular Monolith**，细节放到 Architecture 章。  
- 测试数量勿写死旧数字；可写 “comprehensive automated suite (unit + integration + stress)，详见 QA 章与 CI artifact”。

---

## 你扩写后建议自检
- [ ] 读完能回答：做什么、为谁、边界、和中期意见的关系  
- [ ] 出现 Solo、双系统并列、未打通  
- [ ] 有 Evidence 占位，方便你后贴截图
