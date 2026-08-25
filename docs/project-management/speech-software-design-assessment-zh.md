# Software Design Assessment — 中文演讲稿 + 展示清单

> 视频文件：`…Technical Assessment - Software Design.mp4`  
> 时长：约 **5 分钟**（可 5–6 分钟）  
> 核心：核心用例 → Strategy 类图 → 双轨时序 → ERD；回应中期「计费要有设计模式 + 详细设计」

---

## 开场前：要准备什么（导出 PNG）

| 顺序 | 展示什么 | 仓库路径 |
|------|----------|----------|
| 1 | 封面 | 自制：Software Design Assessment |
| 2 | 核心用例提醒（可裁切） | `docs/diagrams/06-use-case-diagram.puml` 或 PPT 一页文字 |
| 3 | **Strategy 类图** | `docs/diagrams/13-billing-strategy-class.puml` |
| 4 | **运单双轨时序图** | `docs/diagrams/14-sequence-waybill-dual-track.puml` |
| 5 | 导入时序（可选，时间紧可删） | `docs/diagrams/08-sequence-import.puml` |
| 6 | ERD / 数据模型 | `docs/diagrams/07-erd.puml` |
| 7 | 扩展说明一页（可选） | PPT：新增策略三步（实现→注册 DI→可选传参） |

代码指认（录屏可闪一下，不必久留）：
- `CloudWarehouse.Pricing.Core/Billing/IBillingStrategy.cs`
- `TierBillingStrategy` / `OverweightBillingStrategy` / `VolumetricBillingStrategy`
- `FeeCalculationEngine.cs`
- `DualTrackFeeCalculator`（Billing 模块）

---

## 演讲稿（照着念）

### 0. 封面 · 约 25 秒  
**【展示：封面】**

各位老师好。本视频是 **Technical Assessment — Software Design**。  
重点说明 CloudWarehouse 计费与结算相关的软件设计：如何用 **Strategy Pattern** 管理计费变体，运单预览如何走 **双轨应收应付**，以及数据模型如何支撑历史价与规则版本。这直接回应中期反馈里对设计模式与详细设计的要求。

---

### 1. 核心用例与设计范围 · 约 40 秒  
**【展示：用例图或「核心用例」PPT 页】**

本段聚焦一条主用例：**运单导入预览与双轨计价对比**。  
管理员上传账单明细 Excel 后，系统要算出每条运单的应收（客户报价）与应付（成本价），并与表内金额对比。  

设计范围包括：重量取整、按发货日选择历史有效规则、按计费类型选择策略算法、以及结果汇总。  
旁路的规则检索助手不参与本设计主路径，结算真相源始终是计费引擎。

---

### 2. Strategy Pattern 类设计 · 约 80 秒  
**【展示：13 类图；可短暂切到 IDE 接口文件】**

Phase 1 计费主要是区间价与续重，若继续用大段 if/else，后续体积重、阶梯价会难以维护。  
因此 Phase 2 将计价重构为 Strategy Pattern：

- `IBillingStrategy`：定义 `CanHandle` 与 `Calculate`
- `TierBillingStrategy`：≤5kg 区间计费
- `OverweightBillingStrategy`：>5kg 续重计费
- `VolumetricBillingStrategy`：当体积重大于实重时，按计费重再委托区间或续重
- `IBillingStrategyResolver` / `DefaultBillingStrategyResolver`：按注册顺序选择策略
- `FeeCalculationEngine`：先按发货日过滤有效规则，再解析策略并计算
- 静态门面 `FeeRuleCalculator` 保持旧调用兼容

**开闭原则验证：** 增加体积重时，主要是新增策略类并在 DI / `CreateDefault` 注册，运单导入主流程调用方无需改算法分支。  
类图即详细设计证据；单元测试覆盖区间、续重与体积重场景。

---

### 3. 双轨计价时序 · 约 90 秒  
**【展示：14 时序图；边讲边指参与者】**

以运单预览为例，交互顺序是：

1. 前端上传 Excel → `BillController` → `BillImportService`
2. `WaybillExcelHelper` 解析双行表头账单（账单明细 + 成本明细）
3. 加载站点、目的地、客户、规则等主数据缓存
4. 对每一行：重量取整 → `DualTrackFeeCalculator`
5. 应付走成本价服务，应收走客户报价服务
6. 两者都进入 `FeeCalculationEngine` → Resolver → 具体 Strategy
7. 汇总后与 Excel 中 L/X 列中转费对比（容差如 0.01）

设计要点：  
- **双轨**是应用层协调，不是两套互相拷贝的 if/else；  
- **历史价**在引擎过滤 `EffectiveDate` / `ExpiryDate` 时完成，避免“永远用最新价”；  
- Strategy 保证计费变体可扩展，双轨保证财务对账语义清晰。

---

### 4. 数据模型（ERD）· 约 50 秒  
**【展示：07 ERD】**

数据设计支撑上述行为：  
- 主数据：Sites、Destinations、Customers  
- 规则：PriceRules（含 BillingType、重量区间、单价、面单费、生效/失效日期）  
- 结算：账单/明细相关表，保存运单行与计算/对比结果  

一页 Excel 价表行会映射为多条 PriceRules（多档区间 + 续重），这是有意的一对多建模，便于按重量匹配策略。  
历史价依赖规则版本字段，而不是覆盖写死单一价格。

---

### 5. 扩展方式与边界 · 约 35 秒  
**【展示：扩展三步 PPT，或停在类图】**

扩展新计费类型三步：实现 `IBillingStrategy` → 注册到 Resolver/DI → 如需则扩展导入或 API 上下文（例如长宽高）。  
当前诚实边界：体积重已在引擎与测试中可运行；运单 Excel 主路径仍以实重为主，尺寸字段可按业务再接入。  

软件设计结论：用 Strategy 管算法变体，用双轨时序管结算协作，用 ERD 管规则版本与追溯。谢谢。

---

## 时间轴（合计约 5.5 分钟）

| 段 | 内容 | 秒 |
|----|------|-----|
| 0 | 封面 | 25 |
| 1 | 核心用例 | 40 |
| 2 | Strategy 类图 | 80 |
| 3 | 双轨时序 | 90 |
| 4 | ERD | 50 |
| 5 | 扩展与收束 | 35 |
| | **合计** | **~320 秒 ≈ 5.3 分** |

超时就删「导入时序 08」和 IDE 切换。

---

## 录制检查

- [ ] 出镜 + 1080P  
- [ ] 类图要念到三个策略名 + Resolver + Engine  
- [ ] 时序图要念到 DualTrack、历史价、L/X 对比  
- [ ] 明确：规则检索不替代 FeeCalculationEngine  
- [ ] 禁止：已支持所有复杂计费、完整 DDD 框架、微服务计费中心
