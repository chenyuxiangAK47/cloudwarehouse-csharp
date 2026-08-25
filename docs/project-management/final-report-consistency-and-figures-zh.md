# 终稿一致性速查 + 插图 / 英译顺序

> 用途：Word 里 49 页中文定稿后，用本表 **全局查找** 纠一次；再插图；最后英译。  
> 原则：**先一致，再插图，再翻译**——英译不会修事实错误，只会放大错误。

---

## 1. 术语与事实唯一口径（全文必须统一）

| 概念 | 唯一正确写法 | 禁止写法 |
|------|--------------|----------|
| 双轨 | 应收=客户报价(`CustomerQuoteRules`)；应付=成本(`PriceRules`) | 国内/国际线路 |
| 运单表 | 物理上主要是 `BillLines` | 已有完整 Bill 头表（除非以后加了） |
| Import 模块 | 写 `PriceRules` | Import 写 `CustomerQuoteRules`（报价在 Pricing） |
| Strategy | Tier / Overweight / Volumetric = **Done** | JSON 规则引擎；Strategy 仍 Planned |
| 体积重 | 引擎+单测 Done；运单 Excel 主路径仍以实重为主 | “生产 Excel 已全量体积重计费” |
| Assistant | 规则查阅辅助 | AI 智能计费 / RAG 结算 |
| PDA | 霍尼韦尔无订单报工；独立库 | Parallel Data Aggregator；已与云仓结算打通 |
| 架构 | Modular Monolith；无 HA | 微服务已上线；生产多活 |
| CI/CD | CI + Artifact Done；完整 CD = Planned | 完整 DevSecOps/CD 已落地 |
| 覆盖率 | 以 Artifact 截图为准 | 正文写死 >80% |
| 工时 Phase1 | 48→52, 44→61, 56→51, 50→47；**198→211 (+7%)** | 改数字 |
| Solo | 个人 Planned/Actual；第二开发者 **N/A** | 虚构团队 |
| 大文件 | 白名单+大小限制 Done；流式/断点 = Plan | 流式上传已实现 |
| 认证 | Planned；JWT vs WMS SSO 对比在风险章 | 认证已上线 |

Word 里对上表「禁止写法」整篇搜索一遍，搜到就改。

---

## 2. 章间衔接是否通（不必重写）

当前仓库稿 **Ch4–Ch10** 口径已对齐（双轨、工时、无 HA、PDA、Strategy）。  
风险主要在：**你 Word 里的 Ch1–Ch3 / 早期 Ch5 豆包稿** 是否还留着旧幻觉。

建议只花 20 分钟：

1. 打开 Word，搜索：`inventory` `国内` `国际` `JSON` `Parallel` `微服务已` `打通` `>80` `运单头`  
2. Ch5 若仍写「头表+行表 / 客户绑 PriceRules / lane+生效日 upsert」→ 换成校正口径（见 `final-report-ch5-database-erd-zh.md`）  
3. Ch6 Import 行不要写写入 CustomerQuoteRules  

其余章节交叉引用（“详见第×章”）方向正确即可，不必合并成一篇重写。

---

## 3. 插图最小集（优先这 12 张，页数涨最快）

每张图：**Figure 标题（中英可后补）+ 1–2 句 caption**，半页起跳。

| # | 图 | 来源 | 插入章 |
|---|----|------|--------|
| 1 | 用例图 | `06-use-case-diagram.puml` → PNG | Ch3 |
| 2 | 工时柱状图 | `sprint-hours-chart.html` 截屏 | Ch4 |
| 3 | 路线图 | `10-roadmap-milestones.puml`（核对 M6=Done） | Ch4 |
| 4 | ERD | `07-erd.puml` | Ch5 |
| 5 | 逻辑架构 | `02-logical-architecture.puml` | Ch6 |
| 6 | 物理 / 部署 | `03` 或 `04` | Ch6 |
| 7 | DDD / Context Map | `05` 与/或 `16` | Ch6 |
| 8 | Strategy 类图 | `13` | Ch7 |
| 9 | 双轨时序 | `14` | Ch7 |
| 10 | CI 活动图 | `09` | Ch8 |
| 11 | Actions 绿勾 + coverage Summary | 网页截图 | Ch8 / 附录 |
| 12 | 双轨 UI 或导入预览 | 本机演示截图 | Ch7 / 附录 |

加分（再冲页数）：风险图 `12`、导入时序 `08`、PDA 截图、非法上传被拒、CodeQL 绿勾。

导出 PNG：VS Code PlantUML / 在线 plantuml，或 IDEA 插件；统一宽度放进 Word。

---

## 4. 英译策略（省事且不易跑偏）

1. **以定稿中文 + 已插图 Word 为唯一母本**（不要再让 AI 从大纲重写一版英文）。  
2. 分段喂给翻译模型，每段开头贴硬约束：

```text
Translate into academic English for NUS SE internship final report.
Do NOT add features, tables, or claims. Do NOT change numbers.
Keep: dual-track = receivable vs payable; Modular Monolith; no HA;
Strategy Tier/Overweight/Volumetric Done; PDA not API-integrated with CloudWarehouse;
hours 198→211; coverage via CI artifact only (no >80% claim).
```

3. 译完后只抽查：**工时表、里程碑 Done/Planned、双轨定义、禁话句**。  
4. Figure caption 中英对照可再加半页～1 页。

预估：正文英文化字数接近中文；再加 12+ 图，**+15 页很现实**；50 页门槛稳。

---

## 5. 建议时间盒（务实）

| 顺序 | 任务 | 建议时长 |
|------|------|----------|
| A | Word 禁词全局搜索 + Ch5/Ch6 校正 | 0.5–1h |
| B | 导出 12 张核心图并插入 + 图注 | 2–3h |
| C | 附录截图勾选（第十章 10.7） | 1h |
| D | 分段英译 + 抽查数字/禁话 | 0.5–1 天 |
| E | 短结论章（Ch11）中英各 2–3 页 | 可选，1h |

**一致性结论：** 分段写不可怕；Ch4–10 仓库稿已共口径。真正要防的是 Word 早期粘贴残留 + 英译时 AI 又编功能。按上表搜一遍就够用。
