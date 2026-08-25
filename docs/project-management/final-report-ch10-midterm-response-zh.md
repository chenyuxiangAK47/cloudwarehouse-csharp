# 第十章 中期反馈逐条回应

Response to Mid-term Supervisory Feedback

中期评审明确要求：终稿与最终汇报必须回应全部意见，并附可验证证据。意见原文保存在仓库 `log`（及窗口英文摘要）。本章按优先级把每条反馈映射到 **已执行动作** 与 **本报告章节 / 产物路径**，避免只在概述里“点到为止”。

---

## 10.1 反馈来源与回应原则

| 原则 | 做法 |
|------|------|
| 有证据才写 Done | 指向图、测试、CI、UI 截图路径 |
| 未做不装做完 | DAST、完整 CD、JWT、异形件/罚款策略等保持 Planned |
| Solo 工时单独列 | 第四章个人 Planned vs Actual；无第二开发者则 N/A |
| 禁话 | 微服务已上线；AI 智能计费；云仓与 PDA 结算 API 已打通；生产级 HA 已建成 |

英文窗口要点与中文发言一致：Phase 2 增加计费复杂度与设计模式；多视角架构图；物理图写清基础设施与冗余（无则诚实写无）；单体需自辩；DDD 服务 Modular Monolith；任务需 artifacts。

---

## 10.2 总映射表（一页看清）

| 中期意见 | 本项目回应（摘要） | 主要落点 |
|----------|--------------------|----------|
| 整体偏简单 / 要有深度 | Phase 2：Strategy、双轨历史价、规则检索、多图、CI/SAST | 第 6–8、10 章 |
| 单体式架构需说明 | Modular Monolith 有意选型 + 拆分触发条件 | 第 6 章 |
| 计费变体 + 设计模式 + 类图/交互图 | Tier/Overweight/Volumetric 已实现；类图 13、双轨时序 14 | 第 7 章；ADR-8 |
| 多视角架构图 | 逻辑/物理/部署/DDD/企业 Context Map/CI 活动图等 | 第 6、8 章；`docs/diagrams/*` |
| 物理图要有基础设施与冗余 | 节点、端口、单实例；**明确无 HA** | 第 6.5–6.6 节 |
| DDD 讲透 | 限界上下文 ↔ `Modules/*`；诚实非完整领域事件框架 | 第 6.3 节；图 05、16 |
| 工作需可验证证据 | CI、CodeQL、覆盖率 Artifact、测试、截图清单 | 第 8、9 章；附录 |
| 个人 Planned vs Actual | Phase 1：198→211h；S2 +39% | 第 4 章 |
| 风险缓解要具体 | 预览/事务/白名单/索引修复；大文件流式为 Plan；认证双方案 | 第 9 章 |
| 规划要量化 | 里程碑状态 + Phase 2 工作包（Strategy 等已 Done） | 第 4、10.6 节 |

---

## 10.3 最高优先级意见 — 逐条展开

### 10.3.1 「偏简单」与计费变体要有设计模式

**意见：** 系统偏简单；第二阶段计费规则变体要重点处理；有灵活空间应使用设计模式，并展示类图、交互图。

**回应：**

- 已实现 Strategy：`TierBillingStrategy` / `OverweightBillingStrategy` / `VolumetricBillingStrategy`，由 `FeeCalculationEngine` + Resolver 编排。  
- 详细设计：`docs/diagrams/13-billing-strategy-class.puml`、`14-sequence-waybill-dual-track.puml`。  
- 双轨结算（应收客户报价 vs 应付成本）+ 按发货日/账单日历史价，提高业务深度。  
- 异形件、超时罚款等仍为 **Planned** 扩展点（证明 OCP，而非假装已全做）。

**证据：** 第七章全文；`BillingStrategyTests`；运单预览一致/不一致 UI 截图（附录）。

### 10.3.2 所有工作必须附可验证证据

**意见：** 不能只口头说做了测试/导入/CI；要截图、覆盖率、流水线成功记录等。

**回应：**

- CI：`.github/workflows/ci.yml` + Actions 绿勾；覆盖率 Artifact（不写死 >80%）。  
- SAST：`.github/workflows/codeql.yml`。  
- 测试：单元 + 集成 + 轻量并发/性能烟测。  
- 功能：导入预览成功/失败、双轨对比、PDA 开工报工等截图进附录。

**证据：** 第八章证据清单；附录索引。

### 10.3.3 工时必须拆分个人 Planned vs Actual

**意见：** 单独列出个人预估 vs 实际；新加入开发者单独统计。

**回应：**

- Solo 个人表：S1 48→52；S2 44→61；S3 56→51；S4 50→47；合计 198→211（+7%）。  
- 新加入开发者：**无**，记 N/A。  
- Phase 2 云仓/PDA 工时需定稿前填实数（第四章预留表）。

**证据：** 第四章；`sprint-hours-chart-data.csv` / HTML 截图。

---

## 10.4 高优先级意见 — 逐条展开

### 10.4.1 架构图全面升级（多视角）

**已提供视角（PlantUML 源文件可追溯）：**

| 视角 | 文件 |
|------|------|
| 约束 / ADR | `01*.puml` |
| 逻辑架构 | `02-logical-architecture.puml` |
| 物理架构 | `03-physical-architecture.puml` |
| 部署 | `04-deployment-diagram.puml` |
| DDD 限界 | `05-ddd-bounded-contexts.puml` |
| 用例 | `06-use-case-diagram.puml` |
| ERD | `07-erd.puml` |
| 导入 / 双轨时序 | `08`、`14` |
| CI 活动 | `09-cicd-pipeline.puml` |
| 企业 Context Map | `16-enterprise-context-map.puml` |
| Strategy 类图 | `13-billing-strategy-class.puml` |

逻辑图强调分层与依赖；物理/部署图给出节点与端口量级信息。

### 10.4.2 物理架构：基础设施与冗余

**回应：** 写清演示拓扑（Kestrel、SQL Server 1433、CI Runner、PDA 并列节点）；备份为手工/脚本重建；**明确无负载均衡、无 DB HA**。用诚实清单满足“如果有冗余就写、没有就写没有”，而非虚构集群。

### 10.4.3 单体合理性 + 向微服务演进

**回应：** 第六章对比微服务 / 泥球 / Modular Monolith；给出拆分触发条件（团队规模、独立扩缩、发布节奏冲突、技术异构、量化 QPS 等）。M8 保持 Planned。候选上下文：Import / Pricing / Master Data（按触发再拆，不先拆）。

### 10.4.4 DDD 必须讲透

**回应：**

- 上下文：Master Data、Import、Pricing、Billing、Assistant（及 PDA 独立上下文）。  
- 代码映射：`Modules/*`。  
- 交互：同进程同步调用 + 同库事务（非已上线的事件总线）。  
- **诚实边界：** DDD-informed 模块化，不是完整领域事件/聚合框架教材复刻。

---

## 10.5 中优先级意见 — 逐条展开

### 10.5.1 风险缓解要具体

| 导师点名项 | 本报告落点 |
|------------|------------|
| 大文件上传 | 已做白名单+大小限制；流式/分块/断点续传 = **Plan**（第九章 T3） |
| 三级表头 | 自动探测算法 + 模板 + 测试（T1）；与 S2 超支联动 |
| 无登录 | JWT/RBAC vs WMS SSO 对比表（第九章 S1） |

### 10.5.2 未来规划要量化

| 工作包 | 状态 | 说明 |
|--------|------|------|
| Strategy + 体积重 | Done | 约 Sprint 5；详见第七、四章 |
| 规则检索 | Done | 辅助查阅，非结算真相源 |
| 运单双轨 + 历史价 | Done | 时序图 14 |
| 性能基线（1000 行解析等） | 部分 Done | 烟测已有；附录补截图 |
| JWT/RBAC | Planned | 估时见规划表，负责人=作者（Solo） |
| Import 微服务 spike | Planned | 依赖稳定边界与触发条件 |
| 完整 CD | Planned | 现为 CI + 发布包/检查清单 |

（Phase 2 个人小时定稿前填入第四章空表，与上表一致。）

### 10.5.3 加分项落实情况

| 建议 | 状态 |
|------|------|
| 计费复杂度分析 + 模式如何扩展 | 第七、十章变体表 + OCP 三步 |
| 性能：1000 行等 | `Import1000RowPerfTests` 等；附录贴数字 |
| 设计决策对比（Dapper vs EF、单体 vs 微服务） | 第二章技术栈 + 第六章架构 |

---

## 10.6 额外交付：并列 PDA 与价值边界

中期焦点在云仓深度；Phase 2 另并列交付 **霍尼韦尔 PDA 无订单报工**，用现场数据采集回应“工厂数字化”叙事，但：

- 与 CloudWarehouse **未做生产级结算 API 打通**；  
- 在 Context Map 中标为集成 Planned；  
- 工时与证据与云仓分列，避免用 PDA 掩盖云仓设计深度不足，也避免用云仓话术夸大 PDA 集成。

---

## 10.7 仍待附录补齐的截图清单（作者执行）

定稿前建议逐项勾选：

- [ ] GitHub Actions CI 绿勾  
- [ ] coverage Summary  
- [ ] CodeQL 成功  
- [ ] 价表导入成功 / 失败  
- [ ] 运单双轨预览一致/不一致  
- [ ] Strategy 相关测试通过或类图 PNG  
- [ ] 工时柱状图  
- [ ] PDA 开工/报工  
- [ ]（可选）上传非法扩展名被拒  

---

## 10.8 本章小结

中期意见已从“提醒”转化为可核对的工程与文档动作：计费用 Strategy 与双轨做深，架构用多视角与诚实无 HA 说清，质量用 CI/SAST/测试证明，管理用个人工时与风险具体措施闭环。下一章给出 **结论、已知限制与展望**，并简要触及 Client Feedback 与提交清单，结束正文。
