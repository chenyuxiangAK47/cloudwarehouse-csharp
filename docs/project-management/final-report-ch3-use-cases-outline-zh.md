# 最终报告 · 第三大段大纲（中文）— 系统用例与业务模块

> 对应原骨架：`interim-report-writing-guide.md` **§4 System Use Cases & Business Modules**  
> 扩写后目标约 **5–8 页**（含用例图、模块表、用例目录、MoSCoW）  
> 务必覆盖 Phase2：运单双轨、Strategy、规则检索、PDA 用例（可单列一小节）

---

## 给 AI 的扩写指令（复制用）

```text
请根据下列「第三大段：系统用例与业务模块」中文大纲，撰写最终实习报告正文。

要求：
1. 输出正式学术英文；小节编号 3.1 / 3.2…（或按全书统一为 Chapter 3）
2. 严格按大纲结构；CloudWarehouse 与 PDA 的 Actor/用例分开写，不要混成一个系统边界
3. 必须包含：Actors、模块分解表、用例目录表、MoSCoW、用例图引用、PDA 用例小节
4. 用例要反映已交付：运单预览双轨、历史价、Strategy 计费、规则检索；不要写未做的微服务用例
5. 禁止：已与 PDA API 打通、AI 自动计价、完整认证已上线
6. 每节加 Evidence 占位（Figure: 06-use-case-diagram.puml 等）
7. 篇幅约 1200–1800 英文词
```

---

## 大纲正文（第三大段）

### 3. 章标题
- 中文：系统用例与业务模块  
- 英文：System Use Cases and Business Modules

### 3.1 参与者（Actors）
**CloudWarehouse 边界内**
- 主 Actor：仓库管理员 / 结算人员（操作 Web UI）
- 间接 Actor：供应商/师傅（提供 Excel，不直接操作系统）

**PDA 边界内**
- 主 Actor：产线操作员
- 间接：班组长/MES 数据消费者（查看报工流水；若双写 MES，可写 “legacy MES as downstream store”，勿写双向实时集成）

说明：双系统 = 双边界；同一工厂不同 Actor。

### 3.2 CloudWarehouse 业务模块分解
用表：

| 模块 | 职责 | 主要 API | UI |
|------|------|----------|-----|
| MasterData | 站点/目的地/客户 CRUD 与导入 | `/api/Site` `/Destination` `/Customer` | 对应 Tab |
| Import | 成本价 Excel 预览/入库/模板/导出 | `/api/Import/...` | 成本价导入 |
| Pricing | 规则查看、试算；客户报价导入相关 | `/api/PriceRule` `/CustomerQuote` | 价格规则/客户报价 |
| Billing | 运单导入预览/入库、双轨对比 | `/api/Bill/waybill...` | 运单导入 |
| Assistant | 计价规则检索（辅助） | `/api/Assistant/ask` | 计价规则检索 |
| Pricing.Core | Strategy 计费引擎（被 Pricing/Billing 调用） | 类库 | — |

强调：Assistant **不**写入账单；Billing 调用 FeeCalculationEngine。

### 3.3 CloudWarehouse 用例目录（更新版，勿只用 Phase1 八条）
建议至少列出：

| ID | 用例 | Actor | 主成功路径要点 |
|----|------|-------|----------------|
| UC-01 | 管理站点 | Admin | CRUD |
| UC-02 | 导入站点 | Admin | Excel |
| UC-03 | 管理目的地 | Admin | CRUD |
| UC-04 | 管理客户 | Admin | CRUD |
| UC-05 | 下载价格模板 | Admin | 标准模板 |
| UC-06 | 预览成本价导入 | Admin | 格式检测/三级表头等 |
| UC-07 | 提交成本价入库 | Admin | 事务 upsert PriceRules |
| UC-08 | 运费试算 | Admin | 站点+目的+重量+日期 |
| UC-09 | 预览/导入客户报价 | Admin | 报价规则入库 |
| UC-10 | 预览运单双轨结算 | Admin | 历史价+应收应付对比 |
| UC-11 | 提交运单结算结果 | Admin | 可选入库 |
| UC-12 | 检索计价规则说明 | Admin | KB 检索，非计价 |

每个写 Preconditions + Main success（各 1–2 句即可）。

### 3.4 MoSCoW（最终版）
**Must（已交付）**
- UC-06/07 成本价导入；UC-09 客户报价；UC-10 运单双轨预览；UC-08 试算；主数据基础 CRUD；Strategy 支撑的计费变体

**Should**
- 规则检索 UC-12；更友好的导入错误提示；覆盖率/CI 证据完善

**Could**
- JWT/RBAC；与 PDA 集成；流式超大 Excel

**Won’t（本期）**
- 完整 WMS 履约；微服务拆分上线；AI 替代结算引擎

### 3.5 用例图
- Figure：`docs/diagrams/06-use-case-diagram.puml`  
- 正文说明：Admin 在边界内；Supplier 在边界外  
- 可选：另附图或文字框列出 UC-09–12（若原图未更新，诚实写 “extended catalogue in Table X；diagram shows core Phase1 set, extended in text”）

### 3.6 PDA 用例与模块（独立小节）
**模块**
- 终端 UI：登录、机群/机床、开工、报工、查询  
- API：`/api/login`、devices、work/start、work/report、records…  
- 数据：开工/报工表；机台产线主数据；标准工时（若有）

**用例（示例）**
| ID | 用例 | 要点 |
|----|------|------|
| P-UC-01 | 登录 | 工号/扫码 |
| P-UC-02 | 选择产线机群机床 | 可扫设备码 |
| P-UC-03 | 工序开工 | 批号等 |
| P-UC-04 | 挂起/续开 | 可换机，规则约束 |
| P-UC-05 | 报工 | 对齐最近开工机台 |
| P-UC-06 | 查询记录 | 可追溯 |
| P-UC-07 | 异常巡检（若做了） | 错栏/工时异常等 |

MoSCoW：P-UC-01–06 Must；巡检/双写 MES 按你真实完成度标 Should/Done。

### 3.7 Evidence
- Table：模块映射、用例目录、MoSCoW  
- Figure：用例图 06；可选 Context Map 16（跨系统 Actor 关系）  
- Screenshot：云仓运单预览、PDA 报工成功各 1  

### 3.8 禁区
- 不写跨系统一键联调用例已上线  
- 不把 Assistant 写成计费引擎  
- 用例数量与后文截图能对应

---

## 与第二章衔接句
第二章给出技术底座；本章从 Actor 与用例描述系统对外能力，并为 Architecture（上下文边界）与 Software Design（UC-10 双轨时序）提供功能锚点。
