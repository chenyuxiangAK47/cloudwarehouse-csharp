# 最终报告 · 第四大段大纲（中文）— 项目路线图与迭代执行

> 对应原骨架：§5 Roadmap + §6 Milestones + §7 Sprint Hours（可合并为一章，易凑页数且回应「个人工时」）  
> 扩写后目标约 **6–10 页**（含里程碑表、Sprint 叙述、个人 Planned vs Actual 表、柱状图）  
> **Solo**：工时表必须是个人小时，不要写成三人团队（旧 sprint-plan 里有虚构团队假设，禁止沿用）

---

## 给 AI 的扩写指令（复制用）

```text
请根据下列「第四大段：项目路线图与迭代执行」中文大纲，撰写最终实习报告英文正文。

硬约束：
1. Solo Intern；工时单位 = 小时；禁止写成 3 人团队产能
2. Sprint = 1 周（不是 2 周）
3. 必须使用下列真实工时（Phase1）：
   S1 48→52；S2 44→61（+39%）；S3 56→51；S4 50→47；合计 198→211（+7%）
4. Sprint2 超支主因 = 供应商三级表头 Excel + 双格式兼容（不要写成泛泛的“合并单元格日期格式”为主因，除非同时提到三级表头）
5. Phase2 / Sprint5：Strategy、双轨运单、规则检索、报告；并并列 PDA 无订单报工投入（可分栏估时，勿编造精确到小数的假日志）
6. 禁止：微服务已上线、已与 PDA API 打通、AI 计费
7. 结构按大纲 4.1–4.8；Evidence 占位：sprint-hours-chart.html 截图、roadmap puml
8. 篇幅约 1400–2000 英文词
```

---

## 大纲正文（第四大段）

### 4. 章标题
- 中文：项目路线图与迭代执行  
- 英文：Project Roadmap and Sprint Execution

### 4.1 方法与节奏
- 敏捷短迭代；每个 Sprint **1 周**
- Phase 1 = Sprint 1–4（CloudWarehouse MVP）
- Phase 2 = Sprint 5 起（计费加深 + 运单双轨 + 检索；并列推进 PDA）
- Solo：个人看板 + 周复盘；容量按本人每周可投入小时估算

### 4.2 里程碑总览（表）
| ID | 名称 | Sprint | 状态 | 证据指向 |
|----|------|--------|------|----------|
| M1 | Foundation | S1 | Done | ERD、主数据 CRUD |
| M2 | Import Preview | S2 | Done | Excel 双格式/三级表头、预览 |
| M3 | Rules & Pricing | S3 | Done | 入库、试算 API |
| M4 | QA & CI | S4 | Done | 测试、GitHub Actions |
| M5 | Docs/Videos | S4–终期 | In progress | 报告、7 视频 |
| M6 | Strategy + Dual-track | S5 | Done | 类图13、时序14、Billing |
| M6b | Rule lookup | S5 | Done | Assistant |
| M6c | PDA No-order MVP | 并列 Phase2 | Done | PDA 演示 |
| M7 | Auth JWT/RBAC | 规划 | Planned | — |
| M8 | Service extraction | 规划 | Planned | 触发条件见 Architecture |

插图：`docs/diagrams/10-roadmap-milestones.puml`

### 4.3 Sprint 1 — Foundation
- 目标：库表、站点/目的地 CRUD、UI 壳、Dapper 通路
- 完成：schema、基础 API/UI
- 问题：本机 SQL/.NET 环境搭建
- 工时：Planned 48 / Actual 52

### 4.4 Sprint 2 — Excel Import（重点写超支）
- 目标：标准模板、解析、预览、试算挂钩
- 完成：标准表头 + **legacy 三级表头**、preview-before-commit
- **超支 +39%（44→61）**：供应商表头复杂度被低估
- 管理动作：之后外部文件任务加缓冲、拆细

### 4.5 Sprint 3 — Rules & Pricing
- 目标：事务入库、一对多 PriceRules、试算稳定
- 完成：upsert、试算 API/UI
- 工时：56→51（略低于计划）

### 4.6 Sprint 4 — QA & CI
- 目标：单测/集成/轻量压测、Actions、覆盖率
- 完成：CI 绿、coverage artifact
- 工时：50→47
- 文档/视频滚动到终期（M5）

### 4.7 Phase 2（Sprint 5+）— 深度与并列交付
**CloudWarehouse**
- Strategy Pattern（Tier/Overweight/Volumetric）
- 运单双轨 + 按发货日历史价
- 规则检索（辅助）
- 报告/图与中期反馈对齐

**PDA（并列）**
- 无订单报工 MVP：登录、选机、开工/报工、扫码、API 落库
- 工时：与云仓分栏记录；示例写法 Planned≈X / Actual≈Y（作者填实数；大纲可写 “author to finalize from timesheet”，给 AI 用占位 + 建议区间如各 40–80h 量级，**更好是你填真实数后再生成**）

强调：Phase2 回应中期「太简单」——用设计模式与结算闭环加深，而不是堆微服务。

### 4.8 个人工时 Planned vs Actual（必须有表）
| Sprint | Phase | Planned (h) | Actual (h) | Variance |
|--------|-------|-------------|------------|----------|
| 1 | P1 | 48 | 52 | +8% |
| 2 | P1 | 44 | 61 | **+39%** |
| 3 | P1 | 56 | 51 | -9% |
| 4 | P1 | 50 | 47 | -6% |
| **P1 小计** | | **198** | **211** | **+7%** |
| 5+ | P2 CW | （作者填） | （作者填） | — |
| 5+ | P2 PDA | （作者填） | （作者填） | — |

图：`docs/project-management/sprint-hours-chart.html` 截屏  

文字分析：
- 总偏差可控（+7%）
- S2 为主要噪声源；S3–4 回到 ±10%
- Solo 个人表满足导师「个人 Planned vs Actual」要求

### 4.9 Evidence
- Table：里程碑、工时  
- Figure：roadmap puml、hours chart  
- 可选：Sprint2 解析相关测试/导入失败提示截图  

### 4.10 禁区
- 禁止复制旧文档「3 members × 50h/week」假设  
- 禁止把 PDA 写成 WMS 盘点迭代  
- CD 生产自动发布若未做不要写进 Done

---

## 与第三章衔接句
第三章定义了已交付用例与 MoSCoW；本章说明这些能力如何按 Sprint/里程碑落地，并以个人工时证明执行过程可审计。
