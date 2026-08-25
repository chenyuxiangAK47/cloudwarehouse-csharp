# 最终报告 · 第五章大纲（中文）— 数据库设计与 ERD

> 对应原骨架：`interim-report-writing-guide.md` **§8 Database / ERD**  
> 扩写后约 **5–8 页**  
> **务必先读：** 第四章生成稿存在严重幻觉，生成第五章前把下面「第四章红线」贴给 AI，避免继续跑偏。

---

## 第四章红线（给 AI / 给你自己改稿用）

当前第四章英文稿**不能直接交**，至少要改：

| 幻觉 | 真相 |
|------|------|
| 表：inventory / suppliers / transactions | Sites, Destinations, Customers, PriceRules, 账单/运单相关表 |
| Sprint3：JSON 规则引擎、客户折扣 | **Strategy Pattern** + PriceRules 档位/续重；客户报价 Excel |
| dual-track = 国内 vs 国际 | **应收（客户报价）vs 应付（成本）** + 发货日历史价 |
| PDA = Parallel Data Aggregator / 离线同步分析 | **霍尼韦尔 PDA 无订单报工** |
| M7 Strategy = Planned | **Done（Sprint5）** |
| 覆盖率 >80% | **勿写死**；写 “coverage report in CI artifact，见 QA 章” |
| .xls BIFF + .xlsx 双格式为主故事 | 主叙事：**xlsx + 标准/三级表头/矩阵**（ClosedXML） |
| GitHub Projects 个人看板等细节 | 若未真实使用，改成 “personal task list / markdown sprint plan” |

工时数字 48/52、44/61、56/51、50/47、198/211 **可以保留**。

---

## 给 AI 的扩写指令（第五章）

```text
请根据下列「第五章：数据库设计与 ERD」中文大纲撰写最终报告英文正文。

硬约束：
1. 只写 CloudWarehouse SQL Server 库表；不要 inventory/WMS 表
2. 核心实体：Sites, Destinations, Customers（及账户若有）, PriceRules, 运单/账单明细相关表（BillLines 等以 database/*.sql 为准的概念描述）
3. 强调：Excel 一行 → 多条 PriceRules（区间档 + 续重）；EffectiveDate/ExpiryDate 支持历史价
4. BillingType：1=区间，2=续重；体积重是策略层，不一定单独 BillingType 行
5. PDA 库（PDA_NoOrder：开工/报工等）单开一小节，注明独立库、未与云仓共库
6. 禁止：微服务拆库已完成、实时与 PDA 共享库
7. Figure 引用 docs/diagrams/07-erd.puml；Evidence 指向 database/schema.sql 等
8. 篇幅 1000–1600 英文词
```

---

## 大纲正文（第五章）

### 5. 章标题
- 中文：数据库设计与实体关系  
- 英文：Database Design and Entity-Relationship Model

### 5.1 设计目标
- 支撑主数据、多版本价格规则、导入幂等、运单双轨对比结果可追溯
- 关系型模型 + 显式 SQL（配合 Dapper）
- 与 Modular Monolith 同库（CloudWarehouse）；PDA 独立库

### 5.2 概念模型 / 限界与表分组
| 分组 | 表（概念名） | 说明 |
|------|--------------|------|
| Master Data | Sites, Destinations, Customers (+ accounts if any) | 引用完整性 |
| Pricing | PriceRules | 按站点-目的、生效日版本 |
| Billing | 运单头/行、对比字段等 | 预览/入库结算结果 |
|（可选）Import 元数据 | 若有任务表则写；没有就说文件级导入无独立 job 表 |

### 5.3 关键设计决策
1. **一对多映射**：Excel 价表一行 → 多条 PriceRules（0–0.3、0.3–0.5… + 续重）  
2. **历史价**：EffectiveDate + ExpiryDate（或等价字段）；计费按发货日过滤  
3. **导入策略**：同 lane+生效日先删后插 / upsert，保证可重复导入  
4. **BillingType**：1 tier / 2 overweight  
5. **索引**：站点+目的+日期查询路径（可提 fix-price-rules-index.sql）

### 5.4 ERD
- Figure：`docs/diagrams/07-erd.puml`  
- 叙述主要关系：Site/Destination 1—N PriceRule；Customer 与报价规则关系；Bill 与 BillLine  

若 ERD 图尚未含 BillLines/ExpiryDate：正文写清“逻辑模型含这些字段；图若滞后以 schema.sql 为准”，并 TODO 更新图。

### 5.5 完整性与事务
- FK / 非空 / 唯一约束（SiteCode 等）  
- 导入在 SqlTransaction 中：校验失败整批回滚  
- 与预览-确认工作流的关系

### 5.6 PDA 数据存储（独立）
- 库：`PDA_NoOrder`（或实际名）  
- 表概念：用户、机台、开工、报工；（标准工时/机台产线若有）  
- 与 CloudWarehouse：**物理隔离**；集成非本期共库

### 5.7 Evidence
- Figure ERD  
- Excerpt：`database/schema.sql`、`billing-schema.sql`、`customer-quote-schema.sql`  
- 可选：SSMS 表列表截图  

### 5.8 禁区
- 不写 inventory/bin/pallet  
- 不写已与 PDA 共库实时同步  

---

## 与第四章衔接句
第四章说明能力按 Sprint 交付；本章说明支撑这些能力的持久化结构，尤其是价格版本与一对多规则建模如何使历史价与双轨结算成为可能。
