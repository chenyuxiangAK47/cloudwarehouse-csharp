# CloudWarehouse 云仓运费结算与 PDA 无订单报工

**最终实习报告**

# 第一章 项目概述
## 1.1 项目背景与一句话简介
本次实习项目旨在通过数字化手段解决制造业与云仓场景下的运营痛点，核心交付物包含两个并行系统：一是基于 ASP.NET Core 9 构建的 CloudWarehouse 运费结算系统，实现了运费规则导入、试算及运单应收应付双轨对账；二是基于 Spring Boot API 与霍尼韦尔 PDA 终端开发的 “MES 无订单报工” 应用，解决了夜班或无正式工单时的开工数据采集难题。作为独立实习生（Solo Intern），本人全权负责了从需求分析、系统设计、编码实现到测试与文档编写的全流程工作。需特别说明的是，虽然两套系统服务于同一工厂目标，但基于限界上下文（Bounded Context）原则，两者目前独立演进，尚未在生产环境中进行 API 级别的深度打通。
## 1.2 业务痛点分析
仓库与结算侧（CloudWarehouse 解决）：
•	数据孤岛与格式混乱： 供应商及师傅的报价与账单长期依赖 Excel 管理，格式极不统一（存在大量三级表头等复杂格式），导致数据标准化困难。
•	对账风险高： 手工对账极易出错，且缺乏版本控制。若直接使用“最新价格”计算历史账单，会导致系统性的金额偏差，缺乏可追溯性。
•	缺乏试算机制： 缺少可重复的运费试算与预览提交流程，导致正式结算前无法预估成本与收入差异。
产线侧（PDA 应用解决）：
•	无单作业盲区： 在夜班或临时插单场景下，往往没有正式的 MES 工单，传统的纸笔记录或口头报工难以追溯，数据易丢失。
•	硬件适配需求： 现场环境需要适配工业手持终端（PDA），要求具备扫码功能且流程极简，以便快速落库。
## 1.3 项目目标
CloudWarehouse 系统目标：
•	主数据管理： 实现站点、目的地、客户等基础数据的可维护性。
•	结构化 Excel 导入（自动探测表头）： 支持 Excel 成本价与客户报价的导入，包含“预览 → 校验 → 事务性入库”的完整流程。
•	双轨试算： 实现运费试算功能，运单预览支持“应收报价”与“应付成本”双轨对比，并严格遵循“按发货日取历史价”的原则。
•	架构扩展性： 引入 策略模式（Strategy Pattern） 处理复杂的计费逻辑（如区间计费、续重、体积重），提升系统扩展性。
•	工程卓越： 实施自动化测试、GitHub Actions CI 流水线及 CodeQL 静态代码分析（SAST）。
PDA 无订单报工目标：
•	极简流程： 实现“登录 → 选择产线/机群/机床 → 开工/报工/查询”的闭环。
•	硬件集成： 利用 PDA 硬件扫码，数据经 API 落库，确保无工单场景下的作业数据可追溯。
## 1.4 项目范围界定
本期已交付范围：
•	CloudWarehouse： 模块化单体（Modular Monolith）架构的 MVP 版本，包含 Phase 2 的计费引擎、双轨对账及规则检索功能。
•	PDA 应用： 无订单报工 MVP 版本。
•	工程资产： 完整的架构图、设计图、CI/CD 配置及测试证据。
明确排除范围：
•	系统集成： 云仓系统与 PDA 应用之间暂未建立生产级的集成总线。
•	微服务化： 系统目前未拆分为微服务上线，保持模块化单体架构。
•	安全认证： 完整的 JWT/RBAC 生产级认证（已记录在架构决策记录 ADR 中，规划延期）。
•	高可用集群： 暂未部署生产级的高可用（HA）集群。
•	AI 功能： 不包含“AI 智能计费”或“RAG 替代结算引擎”等实验性功能。
## 1.5 干系人分析
•	仓库管理员/结算人员： CloudWarehouse 的核心用户，关注对账效率与准确性。
•	产线操作员： PDA 应用的核心用户，关注操作便捷性与扫码响应速度。
•	企业导师/业务方： 负责需求澄清与演示反馈，其评分直接影响实习评价。
•	学术导师： 关注系统设计的深度、设计模式的应用、多视角架构图的完整性以及工作量的实质性证据。
## 1.6 针对中期反馈的回应策略
针对中期评审中指出的“系统偏简单”、“单体架构需自辩”、“计费变体需设计模式”及“缺乏多视角架构图”等反馈，本报告将在后续章节进行针对性回应（本章节仅作索引）：
•	设计深度： 在“软件设计”章节展示 策略模式（Strategy Pattern） 的类图与时序图，证明计费逻辑的复杂性处理。
•	架构合理性： 在“架构设计”章节提供逻辑视图、物理视图、部署视图及 DDD 企业上下文地图（Context Map），论证模块化单体的合理性。
•	工程证据： 在“DevSecOps 与质量保证”章节展示 CI/CD 流水线截图、测试覆盖率报告及 CodeQL 扫描结果。
•	工作量证明： 在“项目管理”章节对比计划工时与实际工时（Planned vs Actual）。
•	价值增量： 重点阐述“双轨历史价回溯”与“PDA 硬件现场闭环”带来的实际业务价值。
## 1.7 交付物快照

**图 1-1** CloudWarehouse 管理端入口，证明可运行系统。

下表展示了项目的主要交付成果状态：

| 交付物名称 | 状态 | 备注 |
| --- | --- | --- |
| CloudWarehouse 系统 | 已完成 | 包含导入、试算、运单双轨对账功能 |
| 计费策略模式实现 | 已完成 | 包含类图、时序图及详细设计 |
| CI/CD 与质量扫描 | 已完成 | 包含自动化测试套件与 CodeQL 集成 |
| 内置规则 RAG | 已完成 | 查阅 FAQ，不参与结算 |
| PDA 无订单报工 MVP | 已完成 | 霍尼韦尔 PDA 端应用 |
| 最终演示视频/报告 | 进行中 | 含 7 段演示视频 |
# 第二章 技术栈与关键技术决策
第一章界定了双系统目标与边界；本章说明支撑这些目标的技术选型，并为后文架构、软件设计、DevSecOps 提供技术上下文。
## 2.1 技术栈总览
CloudWarehouse

| 层 | 技术 |
| --- | --- |
| 语言/运行时 | C# / .NET 9、ASP.NET Core |
| API | REST Controllers（模块：MasterData / Import / Pricing / Billing / Assistant） |
| 前端 | 静态 HTML/JS（wwwroot/index.html），非 React/Angular SPA |
| 数据访问 | Dapper + 参数化 SQL |
| 数据库 | Microsoft SQL Server |
| Excel | ClosedXML |
| 计费核心 | CloudWarehouse.Pricing.Core（Strategy：Tier / Overweight / Volumetric） |
| 测试 | xUnit、WebApplicationFactory 集成测试、轻量并发/压测 |
| CI/安全 | GitHub Actions、Coverlet/ReportGenerator、CodeQL SAST |

PDA 无订单报工

| 层 | 技术 |
| --- | --- |
| 客户端 | Android（霍尼韦尔工业 PDA）+ 扫码 SDK |
| API | Spring Boot 3.x / Java 21 |
| 数据库 | SQL Server（自建库如 PDA_NoOrder） |
| 通信 | 内网 HTTP（明文，匹配车间演示环境） |

双栈是刻意选择（.NET 结算域 vs Java/Android 现场域），不是技术堆砌。
## 2.2 分层映射（仅 CloudWarehouse 细写）
按请求路径写清：
1.	Presentation：浏览器静态页
2.	API：Controllers 校验与编排
3.	Application/Services：Import / Calculate / BillImport / QuoteAssistant
4.	Domain/Helpers + Pricing.Core：Excel 解析、规则映射、FeeCalculationEngine + Strategies
5.	Infrastructure：Dapper、SqlConnection、文件流上传限制
可插：逻辑架构图引用（Figure → docs/diagrams/02-logical-architecture.puml）。
## 2.3 关键选型对比

| 决策点 | 备选 | 选择 | 理由 |
| --- | --- | --- | --- |
| 数据访问 | EF Core vs Dapper | Dapper | 导入重、要精细 SQL/事务；Solo 工期下更可控 |
| 架构风格 | Microservices vs Modular Monolith | Modular Monolith | 单人、快速交付、同库事务；用模块/DDD 留拆分缝 |
| UI | SPA vs 静态页 | 静态 HTML/JS | 精力放后端架构与计费设计，满足 MVP |
| Excel | EPPlus vs ClosedXML | ClosedXML | 读复杂表头 + 写模板/导出一体 |
| 规则维护 | 手工 CRUD 价表 vs Excel-only | Excel 导入为主 | 贴合供应商/师傅现有工作流（ADR） |
| 计费扩展 | 巨型 if/else vs Strategy | Strategy Pattern | 回应中期反馈；开闭扩展（体积重已验证） |
| CI | 仅本地测 vs GitHub Actions | Actions | 可验证证据、覆盖率 Artifact |
| SAST | 无 vs CodeQL | CodeQL | DevSecOps 证据；不声称已做完整 DAST |

PDA 客户端	手机 H5 vs Android 原生	Android 原生	车间耐用、扫码枪集成
## 2.4 开发与文档工具
•	Git + GitHub
•	PlantUML（架构/类图/时序，版本可控）
•	SSMS / sqlcmd
•	.NET CLI；Android Studio / Gradle（PDA）
•	规划文档：sprint 计划、工时 CSV、演讲稿与报告大纲
## 2.5 运行与配置要点
•	CloudWarehouse 演示默认 HTTP，端口 5001；SQL 1433
•	配置：appsettings.json + appsettings.example.json（示例脱敏）
•	上传：扩展名白名单 + 大小限制
•	认证：MVP 延期（ADR），不在本章伪装已实现
•	PDA：内网 API 地址需与现场 IP 一致；防火墙放行等运维细节可一笔带过
## 2.6 与质量/安全工具链的衔接
•	测试项目：CloudWarehouse.Tests / IntegrationTests / TestCommon
•	CI：ci.yml；SAST：codeql.yml
•	细节放到后文 QA / DevSecOps 章，本章只说明“技术栈包含这些门禁”
## 2.7 Evidence 清单
•	Table：技术栈总览、选型对比
•	Figure：逻辑分层 / 解决方案结构截图（.sln 多项目）
•	Figure：PDA 工程结构或设备实拍（可选）
•	Appendix：Program.cs DI 注册片段（Strategy 注册顺序）、CI badge/截图链接
•	
# 第三章 系统用例与业务模块
System Use Cases and Business Modules

本章在第二章技术架构的基础上，将视角从系统内部转向外部功能表现，聚焦于人类参与者与系统的交互。通过定义角色、分解业务模块、列举使用案例，并结合优先级分类与图形化表示，全面描绘CloudWarehouse与PDA两大子系统的功能边界与交付范围。所有描述均基于已上线功能与实际运行情况，为后续的上下文映射（Context Map）和详细设计（如双轨计费流程）提供明确的需求基线。

## 3.0 需求与系统分析（Analysis）

> **回应导师反馈：** 终稿须呈现 *Analysis*，而非仅从设计图倒推。本节在用例与 MoSCoW 之前，交代问题域、干系人约束、以及**分析结论如何流入设计**（满足 rubric「analysis → design」）。

### 3.0.1 现状与问题域（AS-IS）

| 领域 | 现状（AS-IS） | 痛点 | 量化/证据 |
| --- | --- | --- | --- |
| 云仓结算 | 供应商/师傅价表、客户报价、运单明细分散在 Excel | 表头格式不统一（含三级表头）、手工对账易错、历史价难追溯 | 中期 Sprint 2 因 Excel 解析超支 **+39%** 工时 |
| 运费试算 | 无统一试算与预览入库流程 | 结算前无法并列比较应收/应付 | 业务方演示反馈：需要「能解释」的预览而非黑盒 |
| 产线报工 | 夜班/插单常无 MES 工单 | 纸笔/口头难追溯 | 现场近一周 mesdb 报工以无订单路径为主（见 §11.4） |

### 3.0.2 干系人与约束

| 干系人 | 目标 | 约束 |
| --- | --- | --- |
| 仓库管理员/计费专员 | 可重复导入、试算、双轨预览 | Solo 开发、演示环境无 JWT |
| 产线操作员 | 扫码开工/报工、极简流程 | 霍尼韦尔 PDA、内网 HTTP |
| 企业导师 | 可演示 MVP、现场可用 | 不强制本期微服务/全厂 go-live |
| 学术导师 | 设计深度、可验证证据 | 类级时序、测试/安全产物 |

**架构约束（分析阶段即确定）：** 单人实习、约 20 周；优先 Modular Monolith + 双限界上下文（云仓 vs PDA），集成 **Planned**。

### 3.0.3 分析结论 → 设计输入（Analysis → Design）

| 分析结论 | 设计/实现响应 | 证据章节 |
| --- | --- | --- |
| 计费规则多变（区间/续重/体积重） | **Strategy Pattern** + `FeeCalculationEngine` | §7.3 |
| 应收与应付语义不同、须按发货日取历史价 | **双轨** `DualTrackFeeCalculator` + 类级时序图 | §7.5、`14-sequence-waybill-dual-track.puml` |
| 外部 Excel 不可控 | 预览→事务入库；标准模板 + 三级表头探测 | §7.7、`08-sequence-import.puml` |
| 规则说明与结算分离 | Assistant 词法 RAG（只读 FAQ） | §3.2 Assistant 模块 |
| 无工单场景数据要落库 | 独立 PDA 上下文 + Spring Boot API | §3.6 |

**图 3-0（建议）：** 可选补充 AS-IS / TO-BE 简图，或引用用例图（图 3-1）作为分析产出物。

## 3.1 角色
系统生态由两个独立运作的领域构成：基于Web的CloudWarehouse管理平台与用于车间数据采集的PDA终端系统。两者之间不存在生产级别的API集成，所有数据交换依赖人工操作或批处理作业完成。
在 CloudWarehouse 边界内：
•	主要角色：仓库管理员 / 计费专员 —— 通过Web界面直接执行主数据维护、定价规则配置及账单处理等核心任务。
•	间接角色：供应商 / 车间技术人员 —— 提供成本价格Excel文件作为输入源，但不直接访问系统；其提交的数据由管理员手动上传至系统。
在 PDA 边界内：
•	主要角色：生产线操作员 —— 使用Honeywell PDA设备，在无正式生产订单的MES环境中记录工作开始与报工事件。
•	间接角色：班组长 / MES数据使用者 —— 查阅上报的工作日志；若启用了向遗留MES系统的双写功能，则可视为“下游存储”，但该路径不具备实时双向同步能力。
需特别说明的是，系统组件如定价引擎或各类API端点，均不被视为角色。两套系统在API层面保持解耦状态，确保了各自部署与演进的独立性。
## 3.2 CloudWarehouse业务模块分解
CloudWarehouse应用按功能职责划分为六个核心模块，下表详述各模块的责任、关键接口及其对应的用户界面元素。
CloudWarehouse业务模块分解
模块	职责	主要API	UI组件
MasterData	站点、目的地、客户的增删改查及导入	/api/Site, /Destination, /Customer	专用标签页
Import	成本价Excel文件的预览、解析、模板生成与导出	/api/Import/...	成本价导入页面
Pricing	查看费率规则、模拟运费、导入客户报价	/api/PriceRule, /CustomerQuote	费率规则 / 客户报价页面
Billing	运单导入预览、数据摄取、应收（客户报价）vs 应付（成本）；两边都按发货日历史价双轨对比分析	/api/Bill/waybill...	运单导入页面
Assistant	内置规则 RAG：检索知识库并生成带引用回答（只读）	/api/Assistant/ask	规则 RAG 界面
Pricing.Core	基于策略的费用计算引擎（被Pricing与Billing模块调用的类库）	内部类库	—
值得注意的是，Assistant/规则 RAG 模块仅支持查阅检索，不影响任何实际计费结果的生成。Billing模块在结算过程中会调用FeeCalculationEngine（经由Pricing.Core实现）进行费用核算。
表 核心模块映射源自系统分析成果。
## 3.3 CloudWarehouse用例目录
以下用例目录反映了CloudWarehouse系统除第一阶段外的所有已交付功能，每项包含前置条件与主成功场景。
CloudWarehouse用例目录
ID	用例	角色	描述
UC-01	管理站点	Admin	前： 系统可访问（MVP 认证延期）。 主流程： 创建、读取、更新、删除站点记录。
UC-02	导入站点列表	Admin	前： 拥有有效Excel文件。 主流程： 上传并解析站点列表，执行格式校验。
UC-03	管理目的地	Admin	前： 系统可访问（MVP 认证延期）。 主流程： 对目的地条目执行增删改查操作。
UC-04	管理客户	Admin	前： 系统可访问（MVP 认证延期）。 主流程： 添加、编辑、删除客户档案。
UC-05	下载价格模板	Admin	前： 无。 主流程： 生成带有标准模板多为单行表头；三级表头是供应商文件兼容的标准Excel模板。
UC-06	预览成本价导入	Admin	前： Excel文件准备就绪。 主流程： 解析并验证文件格式，包括表头结构与数据类型。
UC-07	提交成本价至数据库	Admin	前： 预览成功。 主流程： 事务性地将PriceRules上插入数据库。
UC-08	模拟货运费用	Admin	前： 输入有效。 主流程： 基于站点、目的地、重量、日期等参数，利用策略驱动引擎估算费用。
UC-09	预览/导入客户报价	Admin	前： 报价文件已准备。 主流程： 验证并摄取客户专属的定价规则。
UC-10	预览双轨计费	Admin	前： 运单已上传。 主流程： 并列展示基于历史费率与当前费率计算的应收/应付金额以供比较。
UC-11	提交计费结果	Admin	前： 审核已完成。 主流程： 可选择性地持久化最终的计费输出。
UC-12	获取费率规则解释	Admin	前： 已选定规则。 主流程： 查询知识库存储的人类可读规则逻辑；此操作不影响实际计算过程。
所有列出的用例均对应真实存在的功能，且未包含微服务粒度的用例。
表 更新后的用例清单依据最终交付范围确定。
## 3.4 MoSCoW优先级划分（最终版）
根据MoSCoW方法对功能优先级进行分类，反映实际交付状态。
必须（已交付）：
•	UC-06 和 UC-07：具备预览功能的成本价导入及其数据库持久化；
•	UC-09：客户报价规则的导入；
•	UC-10：支持历史费率查询的双轨制运单计费预览；
•	UC-08：货运费用模拟；
•	主数据（站点、目的地、客户）的核心增删改查操作；
•	Pricing.Core所支持的多种策略驱动的费用计算变体。
应该（部分交付 / 待优化）：
•	UC-12：规则解释检索（基础搜索可用）；
•	Excel导入过程中的错误反馈机制有待增强；
•	测试覆盖率完善及CI证据补充。
可以（未实现）：
•	JWT身份认证与RBAC权限控制；
•	与PDA系统的集成；
•	大型Excel文件的流式处理支持。
不会做（本阶段排除）：
•	完整的WMS履约流程（如收货、上架、盘点）；
•	微服务架构部署至生产环境；
•	AI驱动的自动定价替代方案。
MoSCoW分类结果源于冲刺规划与复盘会议共识。
## 3.5 用例图

**图 3-1** CloudWarehouse 与外部参与者关系；PDA 用例见正文表。

主要用例与参与者的可视化关系见图 3.1。
图 引用自 docs/diagrams/06-use-case-diagram.puml。注：管理员位于系统边界内，供应商为外部实体。图中仅展示Phase 1核心用例；扩展用例集（UC-09 至 UC-12）详见文本及表~\ref{tab:usecases}。可选的上下文图（图 X）阐明跨系统角色关联。
## 3.6 PDA用例与模块
本节详述独立运行的PDA系统，用于支持“无生产订单”模式下的车间报工。
模块：
•	终端UI：涵盖登录、产线/机群/机床选择、开工/报工、查询等功能。
•	API端点：/api/login, /devices, /work/start, /work/report, /records 等。
•	数据实体：工作起止记录、机台-产线主数据、标准周期时间（如有）。
用例：
PDA用例摘要
ID	用例	关键点
P-UC-01	登录	支持员工ID或二维码扫描方式
P-UC-02	选择产线/机群/机床	支持扫描设备二维码
P-UC-03	启动工序	需填写批次号及其他上下文信息
P-UC-04	暂停/恢复工作	允许在规定约束下切换机器
P-UC-05	报告完工	自动关联至最近一次启动的机器
P-UC-06	查询记录	支持过往活动追溯
P-UC-07	异常检查（如已实现）	标记错配料箱或异常周期时间
MoSCoW分类：P-UC-01 至 P-UC-06 归类为“必须”（已交付）。P-UC-07（异常检查）根据实际实施完整性，归类为“应该”或“已交付”。
表 PDA操作功能总结。
## 3.7 证据概览
本章主张的真实性由以下材料支撑：
•	表格：模块映射（表~\ref{tab:modules}）、用例目录（表~\ref{tab:usecases}）、MoSCoW分类；
•	图形：用例图（图~\ref{fig:usecasediag}），可选上下文图（例如图 3.2 引用 docs/diagrams/16-enterprise-context-map.puml）；
•	截图：一张来自CloudWarehouse展示双轨运单预览的界面；一张来自 PDA 显示成功报工的截图。
一张来自PDA显示成功报工的截图。

**图 3-2** 应收/应付机器值与表内值对比。

**图 3-3** 无订单报工闭环证据。

所有证据已在文中引用，并将在最终报告附录中汇编成册。
# 第四章 项目路线图与迭代执行
上一章通过 MoSCoW 优先级方法完成交付用例梳理与优先级划分。本章阐述在单人 Solo 开发模式下，如何按照一周一个 Sprint的迭代节奏落地各项系统能力，并通过个人计划工时与实际工时（Planned vs Actual，单位：小时）实现开发过程的可审计。工时统计数据源来自项目跟踪表docs/project-management/sprint-hours-chart-data.csv以及 Phase 2 全部工作记录；本项目不引入多人团队产能假设，所有工作量均基于单人开发视角统计。
## 4.1 迭代方法与实施节奏
本项目采用短周期敏捷迭代模式，单个 Sprint 对应 1 个自然周，整体划分为 Phase1 与 Phase2 两大开发阶段。

| 阶段 | Sprint | 工作重心 |
| --- | --- | --- |

Phase 1	Sprint 1–4	CloudWarehouse MVP 开发，完成主数据、Excel 导入、运费试算、CI 持续集成
Phase 2	Sprint 5 及以后	计费策略、运单双轨、历史价格、规则检索功能开发；并行实现 PDA 无订单报工模块
单人开发场景下的项目管理规范：
1.	迭代计划：结合每周可投入工时筛选 Must 级别核心任务，拆解为可演示、可测试的小型验证项。
2.	迭代执行：以仓库任务清单、Git 提交记录作为真实进度凭证，不虚构多人团队看板流程。
3.	迭代复盘：对比计划工时与实际工时，针对 Excel 解析、硬件联调这类高不确定性任务，在后续迭代预留缓冲时间。
4.	过程治理：遵循导师要求，报告中独立展示个人维度预估工时与实际消耗工时。

### 4.1.1 Sprint 跟踪工具与 Solo 敏捷实践（含 Jira 产物）

| 问题（导师清单） | 本项目做法 | 证据 |
| --- | --- | --- |
| 是否使用 Jira 等工具？ | **已落地 Jira 兼容跟踪包。** Product Backlog / Sprint Board / Story Points Burndown 以 Jira CSV 导入格式维护，并可导入 Jira Cloud；同时用 GitHub Issue Template 绑定工程任务。配套工时 CSV + Git 历史 + Actions CI。 | `docs/project-management/jira/product-backlog.csv`、`burndown-board.html`、`.github/ISSUE_TEMPLATE/sprint-story.yml` |
| 一人团队是否仍按 Sprint 执行？ | **是。** Phase 1：**Sprint 1–4（一周一轮）**；Phase 2：**Sprint 5（里程碑 Sprint）** 覆盖 Strategy/双轨/PDA/E2E/IaC；每 Sprint 有承诺 SP、完成 SP、Planned/Actual 工时。 | §4.3–4.7；`jira/sprint-burndown-points.csv` |
| 燃尽图（Burndown）？ | **已提供。** Story Points 剩余燃尽（按 Sprint）+ 累计工时 Planned vs Actual 双曲线；报告附录截 `burndown-board.html`。 | `jira/burndown-board.html`、`sprint-burndown-cumulative.csv` |

**6 月初节奏：** Phase 1 严格周 Sprint；Phase 2 合并为 Sprint 5 里程碑迭代（仍保留 backlog、SP 燃尽与工时审计），与 Jira 导出状态一致。

## 4.2 项目里程碑总览

**图 4-1** 若图中 M6 仍为 Planned，以正文 Done 为准。

各关键里程碑、所属迭代、完成状态与产出证据如下表所示。

| ID | 名称 | Sprint | 状态 | 主要证据 |
| --- | --- | --- | --- | --- |
| M1 | Foundation | S1 | Done | database/schema.sql、站点 / 目的地 CRUD 接口 |
| M2 | Import Preview | S2 | Done | 标准表头、遗留三级表头解析逻辑，导入预览流程 |
| M3 | Rules & Pricing | S3 | Done | PriceRules 事务入库、运费试算 API 及前端界面 |
| M4 | QA & CI | S4 | Done | 单元 / 集成 / 轻量压力测试、GitHub Actions、测试覆盖率产物 |
| M5 | Documentation & Videos | S4–终期 | In progress | 本报告文档、7 组功能评估视频 |
| M6 | Billing Strategy + Dual-track | S5 | Done | Strategy 策略类图、时序图、运单双轨逻辑、历史价格能力 |
| M6b | Built-in Rule RAG | S5 | Done | /api/Assistant/ask接口、规则 RAG 前端页面（流水线可视化） |
| M6c | PDA No-order reporting MVP | Phase 2 并行 | Done | 霍尼韦尔 PDA 开工、报工功能演示 |
| M7 | Authentication (JWT/RBAC) | 规划 | Planned | ADR 文档，认证功能延期实现 |
| M8 | Microservice extraction（按触发条件） | 规划 | Planned | 详见架构章节触发条件说明 |

图示说明：可视化文件路径docs/diagrams/10-roadmap-milestones.puml；若图中 M6 状态仍标记为 Planned，以本章文字描述为准同步更新图表。

### 4.2.1 Product Backlog（Epic 级，Phase 1）

| Epic | 代表用户故事 | 优先级 | Sprint | 状态 |
| --- | --- | --- | --- | --- |
| 主数据 | US-1.1 站点 CRUD；US-1.2 目的地 CRUD | Must | S1 | Done |
| 数据库与脚手架 | US-1.3 ERD/`schema.sql`；US-1.4 ASP.NET + 静态 UI | Must | S1 | Done |
| Excel 导入 | US-2.1–2.5 模板、双表头解析、预览 | Must | S2 | Done |
| 规则入库与试算 | US-3.1–3.5 事务 upsert、`PriceRule/calculate` | Must | S3 | Done |
| 质量与 CI | US-4.1–4.3 单测/集成测/Actions | Must | S4 | Done |
| 文档与演示 | US-4.5–4.6 架构图、评估视频 | Should | S4–M5 | In progress |
| Phase 2 计费深化 | Strategy、双轨、历史价、规则 RAG | Must | S5+ | Done |
| PDA 无订单报工 | P-UC-01–06 登录/选机/开工/报工 | Must | Phase 2 并行 | Done |
| JWT/RBAC | — | Could | — | Planned |
| 云仓↔PDA 集成 | — | Won't (本期) | — | Planned |

完整故事 ID 与估时见 `docs/project-management/4-week-sprint-plan.md`。**Solo 声明：** 所有故事由同一开发者承担，无多人认领。

## 4.3 Sprint 1 — Foundation（计划 48h / 实际 52h，偏差 + 8%）
迭代目标：搭建系统可运行基础底座，完成数据库设计、主数据读写、管理端基础页面、Dapper 数据访问链路。
迭代产出
1.	SQL Server 数据库表结构与初始化脚本，完成站点、目的地、价格规则等基础主数据表设计；
2.	实现 Sites、Destinations 等模块 CRUD 接口与前端 Tab 页面；
3.	完成 Dapper 组件与数据库连接字符串配置，打通数据库访问通路。
偏差分析：实际工时 52h，相比计划超出 4 小时，偏差 + 8%。工时超支主要来源于本机 SQL Server、.NET9 开发环境部署调试，属于基础设施类一次性开销，整体偏差可控。
## 4.4 Sprint 2 — Excel Import（计划 44h / 实际 61h，偏差 + 39%）
迭代目标：实现供应商业务价表导入能力，提供模板下载、数据解析、导入预览，规避未经校验直接落库带来的数据风险。
迭代产出
1.	提供标准单行表头导入模板下载；
2.	基于 ClosedXML 组件实现遗留系统三级表头复杂格式自动识别与解析；
3.	完成 “预览–确认” 完整导入流程，预览阶段支持联动运费试算校验。
超支核心原因：真实业务场景下供应商 Excel 表头层级错乱、列对齐异常、多格式兼容的复杂度远高于前期预估，解析逻辑与回归测试工作量显著增加。本迭代实际工时 61h，偏差 + 39%，为 Phase1 阶段最大工时偏差来源。
改进措施，应用于 Sprint 3–4
1.	外部文件处理类任务进一步细化拆解，单独评估异常坏样本的回归测试工作量；
2.	对此类高不确定性任务预留约 15% 工时缓冲；
3.	严格执行预览确认后再提交入库，减少错误数据入库后的返工成本。
## 4.5 Sprint 3 — Rules & Pricing（计划 56h / 实际 51h，偏差−9%）
迭代目标：将预览校验通过的价表以事务方式写入 PriceRules 数据表，对外提供稳定的运费试算服务。
迭代产出
1.	导入提交逻辑：校验失败执行整批事务回滚；按照运输线路 + 生效日期完成规则版本更新；
2.	实现单条 Excel 记录映射生成多条 PriceRules，支持多区间档位、续重计费规则落地；
3.	完成/api/PriceRule/calculate试算接口与前端试算 UI。
迭代说明：本迭代不实现 JSON 动态规则引擎，核心为基于数据库规则行的计费数据模型与计算服务。实际工时 51h，少于计划工时，反映经过 Sprint2 复盘后工作量估算趋于收敛，导入管道复用也带来开发效率提升。
## 4.6 Sprint 4 — QA & CI（计划 50h / 实际 47h，偏差−6%）
迭代目标：质量门禁工程化，构建完整可复现的验证证据链。
迭代产出
1.	搭建 xUnit 单元测试、WebApplicationFactory集成测试，补充轻量并发压力测试用例；
2.	配置 GitHub Actions 流水线：还原依赖库→执行 dotnet 测试→生成测试报告→输出覆盖率 Artifact 产物；
3.	云端 Runner 无 SQL Server 环境下，对数据库依赖用例做可解释跳过策略，规避流水线虚假报错。
迭代说明：报告终稿、7 段评估视频归属 M5 里程碑，延后至项目末期完成，避免 Sprint4 范围膨胀。本迭代实际工时 47h，略低于计划。

### 4.6.1 Sprint Backlog 摘录（Phase 1，按 Sprint）

**Sprint 1**

| ID | 用户故事 | 计划 (h) | 实际 (h) |
| --- | --- | ---: | ---: |
| US-1.1 | 站点 CRUD | 12 | 14 |
| US-1.2 | 目的地 CRUD | 10 | 11 |
| US-1.3 | ERD + `schema.sql` | 8 | 8 |
| US-1.4 | ASP.NET Core + 静态 UI 壳 | 10 | 11 |
| US-1.5 | Dapper + SQL Server 联调 | 8 | 8 |
| **合计** | | **48** | **52** |

**Sprint 2**

| ID | 用户故事 | 计划 (h) | 实际 (h) |
| --- | --- | ---: | ---: |
| US-2.1 | 标准模板下载 API | 6 | 6 |
| US-2.2 | 单行表头解析 | 10 | 12 |
| US-2.3 | 遗留三级表头解析 | 12 | **22** |
| US-2.4 | 导入预览 API + UI | 10 | 11 |
| US-2.5 | 预览期试算校验 | 6 | 10 |
| **合计** | | **44** | **61** |

**Sprint 3**

| ID | 用户故事 | 计划 (h) | 实际 (h) |
| --- | --- | ---: | ---: |
| US-3.1 | 站点/目的地校验 | 8 | 7 |
| US-3.2 | 事务 upsert `PriceRules` | 14 | 15 |
| US-3.3 | 试算 API + UI | 12 | 11 |
| US-3.4 | 价表维护 UI 简化 | 8 | 8 |
| US-3.5 | Tier/续重映射 | 10 | 10 |
| US-3.6 | 集成测试（部分移至 S4） | 4 | 0 |
| **合计** | | **56** | **51** |

**Sprint 4**

| ID | 用户故事 | 计划 (h) | 实际 (h) |
| --- | --- | ---: | ---: |
| US-4.1 | 单元测试套件 | 14 | 13 |
| US-4.2 | WebApplicationFactory 集成测试 | 12 | 11 |
| US-4.3 | GitHub Actions + 覆盖率 | 10 | 10 |
| US-4.4 | 轻量并发/perf 冒烟 | 6 | 6 |
| US-4.5 | PlantUML 架构包 | 8 | 7 |
| US-4.6 | 评估视频（延后 M5） | 0 | 0 |
| **合计** | | **50** | **47** |

## 4.7 Phase 2（Sprint 5 起）—— 设计深化与 PDA 并行开发
中期评审反馈指出初代系统实现偏轻量化，计费逻辑需要补充设计模式与详细设计，同时架构描述与实物证据不足。Phase2 阶段没有直接开展微服务拆分，而是从系统能力深化、产线设备端开发两个方向并行迭代。
### 4.7.1 CloudWarehouse 系统能力深化（已完成）
1.	引入策略模式 Strategy Pattern，实现TierBillingStrategy、OverweightBillingStrategy、VolumetricBillingStrategy，由FeeCalculationEngine统一调度；
2.	运单双轨机制：区分应收客户报价、应付成本，以发货日期为基准读取历史生效规则，支持预览与中转费对比；
3.	内置规则 RAG（Retrieve→Augment→Generate）辅助查阅业务规则 FAQ，不作为结算引擎；
4.	配套输出类图、双轨时序图与对应测试用例，详见软件设计与质量章节。
### 4.7.2 PDA 无订单报工（并行开发，已完成）
针对产线夜班、无正式工单的业务场景，并行完成霍尼韦尔 PDA 应用开发：支持用户登录、选择产线 / 机群 / 机床，执行开工、报工、查询操作；业务数据通过 Spring Boot API 持久化存储。该模块与 CloudWarehouse 未做生产级 API 打通，属于同一工厂业务背景下两个独立上下文交付。
### 4.7.3 Phase2 个人工时统计表

| 工作包 | Planned (h) | Actual (h) | 备注 |
| --- | ---: | ---: | --- |
| CW：策略模式 + 运单双轨历史价 + 规则检索 + 类图/时序/报告同步 + Playwright E2E | 58 | 63 | 对照 Git 2026-06～08 提交 |
| PDA：后端 API + Android 应用 + 硬件联调 + 操作说明 | 72 | 76 | 与 CloudWarehouse 工时分开统计 |
| **Phase 2 合计** | **130** | **139** | |
| **全项目合计（Phase 1+2）** | **328** | **350** | Solo 个人总投入 |

## 4.8 Phase1 个人工时 Planned vs Actual 汇总
Phase1 各 Sprint 计划工时、实际工时与偏差统计如下：

| Sprint | 目标摘要 | Planned (h) | Actual (h) | Variance |
| --- | --- | --- | --- | --- |
| S1 | Foundation 基础底座 | 48 | 52 | +8% |
| S2 | Excel 导入能力 | 44 | 61 | +39% |
| S3 | 计费规则与试算 | 56 | 51 | −9% |
| S4 | QA 与 CI 流水线 | 50 | 47 | −6% |

Phase 1 合计		198	211	+7%
图表建议：截取

**图 4-2** Solo 个人工时柱状图；数据见 sprint-hours-chart-data.csv。

图表建议（若上图已贴可删本句）：截取docs/project-management/sprint-hours-chart.html工时柱状图插入报告此处。
工时分析
1.	Phase1 整体工时偏差 + 7%，整体项目计划可控；
2.	仅 Sprint2 出现显著超支，根源在于外部 Excel 文件格式的不可控性；Sprint 3–4 偏差回到 ±10% 区间，证明迭代复盘的改进手段有效；
3.	本表全部为单人 Solo 开发工时，满足中期评审对个人 Planned vs Actual 工时追踪的硬性要求。

### 4.8.1 燃尽跟踪（Burndown）— Solo 替代方案

经典 Scrum 燃尽图以 **剩余 Story Points** 为纵轴。本项目为 **单人 + 工时估算**，采用 **累计消耗工时** 对比 **累计计划工时** 作为等效跟踪（数据：`docs/project-management/sprint-burndown-cumulative.csv`）。

| 里程碑节点 | 累计计划 (h) | 累计实际 (h) | 解读 |
| --- | ---: | ---: | --- |
| Sprint 1 结束 | 48 | 52 | 环境/SQL 一次性开销 |
| Sprint 2 结束 | 92 | 113 | Excel 超支拉高曲线 |
| Sprint 3 结束 | 148 | 164 | 估算收敛 |
| Sprint 4 结束（Phase 1） | 198 | 211 | Phase 1 闭合 +7% |
| Phase 2 结束（全项目） | 328 | 350 | 设计深化 + PDA 并行 |

**图 4-3 / 4-4：** （1）打开 `docs/project-management/jira/burndown-board.html` 截取 Sprint Board + SP 燃尽；（2）工时累计曲线见 `sprint-hours-chart.html` / `sprint-burndown-cumulative.csv`。附录 **A-07 / A-07b**。

## 4.9 本章证据清单
本章所有结论均有对应的工程产物作为支撑，证据位置如下：

| 证据项 | 文件位置 |
| --- | --- |
| 工时原始数据源 | docs/project-management/sprint-hours-chart-data.csv |
| 工时统计柱状图 | sprint-hours-chart.html截图 |
| 项目路线图 | docs/diagrams/10-roadmap-milestones.puml |
| Sprint2 导入功能 | 导入预览成功 / 失败截图、ExcelHelper 单元测试 |
| Sprint4 CI 流水线 | GitHub Actions 成功记录、覆盖率 Artifact |
| Sprint5 策略模式 & 运单双轨 | 类图 13、时序图 14、运单预览截图 |

PDA 报工模块	开工报工成功截图或者简短录屏
## 4.10 本章小结
Phase1 通过四周 Sprint 迭代完成 CloudWarehouse MVP 版本交付，单人开发整体工时偏差处于可控范围。Phase2 针对中期评审反馈，基于策略模式、运单双轨历史价格完成系统深度优化；同时并行落地 PDA 无订单报工模块，覆盖工厂产线现场数据采集场景。后续章节将围绕持久化数据库模型、多维度系统架构、计费模块详细设计展开论述，全程以可验证工程证据作为支撑，避免空泛描述。
# 第五章 数据库设计与实体关系
## Database Design and Entity-Relationship Model

第四章阐述了系统功能按 Sprint 迭代落地的执行路径；本章聚焦支撑上述业务能力的持久化层设计，重点说明双轨价格规则建模、一对多规则拆解、整车道版本替换的实现思路，以及该数据模型如何为历史价查询、应收应付双轨结算提供底层能力支撑。

## 5.1 设计目标
本项目数据库设计围绕业务落地性与工程可维护性展开，核心目标如下：
1. **业务全覆盖**：支撑主数据管理、应收/应付双版本价格规则、导入操作幂等、运单结算明细可追溯四类核心场景的数据需求。
2. **技术适配性**：采用关系型数据模型，以显式 SQL 配合 Dapper 实现数据访问，兼顾执行性能与开发可控性。
3. **架构一致性**：CloudWarehouse 全部业务表部署于同一数据库，契合模块化单体（Modular Monolith）架构；PDA 无订单报工模块采用独立数据库，实现上下文物理隔离。

## 5.2 概念模型与限界表分组
基于限界上下文划分，将数据库表分为四类逻辑分组，各组职责边界清晰，通过约束机制保障跨组数据引用完整性。

| 分组 | 表（概念名称） | 说明 |
|------|--------------|------|
| Master Data（主数据） | Sites, Destinations, Customers | 存储站点、目的地、客户等基础主数据，通过唯一编码约束保障引用完整性，为价格规则与运单业务提供基准数据 |
| Pricing（计价规则） | PriceRules（应付成本）、CustomerQuoteRules（应收报价） | 分两套规则表承载双轨计价：PriceRules 存储供应商成本价，CustomerQuoteRules 存储面向客户的报价；均按站点-目的地组合、生效日期实现版本化管理 |
| Billing（结算明细） | BillLines | 无独立运单头表，所有运单结算信息以行级明细落地，存储金额、批次、计费档位等结算结果字段，完整记录双轨对比明细 |
| Import 元数据（可选） | 无独立任务表 | 当前采用文件级导入流程，未单独设计导入作业表；导入状态随预览-确认工作流流转，幂等性通过业务规则保障 |

## 5.3 关键设计决策
针对计价与导入核心场景，设计阶段做出以下关键决策，在保障业务正确性的同时兼顾性能与可维护性。

1. **一对多规则映射**
单条 Excel 价表行数据对应多条规则记录。由于价表按重量区间拆分计费档位（如 0–0.3kg、0.3–0.5kg 等）并附加续重规则，一行业务数据需拆解为多条规则行持久化，实现精细化计费匹配；该映射同时适用于 PriceRules 与 CustomerQuoteRules 两张表。

2. **历史价版本控制**
两套规则表均设置 `EffectiveDate`（生效日期）与 `ExpiryDate`（失效日期）字段，通过时间区间实现多版本价格管理。计费结算时按运单发货日期过滤匹配对应有效期的规则，确保历史订单计价可复现、可追溯。

3. **幂等导入策略**
价格规则导入采用**整车道（SiteId + DestId）维度替换**机制：针对同一运输车道（SiteId + DestId）执行“先删除该 lane 全部规则再插入”的替换逻辑（非整生效日局部 upsert），保障同一文件重复导入不会产生冗余数据，实现导入操作幂等性。

4. **计费类型标识**
设置 `BillingType` 字段区分计费模式，标识值 1 对应阶梯计费（tier）、标识值 2 对应超重计费（overweight），为策略模式的计费引擎提供数据层面的路由依据。

5. **索引优化与约束修正**
配套脚本 `database/fix-price-rules-index.sql` 的核心作用是**删除错误的唯一索引** `(SiteId, DestId, EffectiveDate)`——因同一 lane、同一生效日必须允许多条档位/续重行。当前查询以非唯一的 lane 索引（如 SiteId+DestId）为主，与一对多基数对齐。

6. **结算明细落库规则**
BillLines 中完整落地规则匹配后的结算明细，直接存储金额、批次、计费档位等业务结果字段，不冗余存储匹配到的规则行 ID，避免规则版本变更后明细追溯失效。

## 5.4 实体关系图（ERD）

**图 5-1** 以 schema.sql 为准；图若滞后于 BillLines/CustomerQuoteRules 请在 caption 说明。

系统整体实体关系可视化文件路径为：`docs/diagrams/07-erd.puml`，核心实体关联逻辑如下：
- 站点（Site）、目的地（Destination）与价格规则（PriceRules / CustomerQuoteRules）均为一对多关系：单个站点与目的地的组合可对应多条不同生效周期、不同计费档位的成本规则与报价规则。
- 客户（Customer）与 CustomerQuoteRules 为一对多关系：客户维度的专属报价通过客户编码绑定，支撑差异化定价。
- BillLines 直接关联站点、目的地与客户维度，以行级粒度承载全部结算信息，无独立运单头表。

> 说明：若当前 ERD 图尚未包含双规则表拆分、ExpiryDate 字段及无表头设计，逻辑模型已完整覆盖上述设计，数据库结构以 `database/schema.sql` 脚本为准，后续同步更新可视化图表。

## 5.5 完整性与事务机制
通过数据库约束与事务机制双重保障数据一致性与业务正确性。
- **约束体系**：通过外键（FK）、非空约束、普通唯一约束构建数据完整性防线；例如对 `SiteCode` 站点编码设置唯一约束，避免主数据重复；价格规则关联主数据外键，保障引用合法性；移除规则表错误唯一索引后，以业务逻辑保障车道+日期维度的版本唯一性。
- **导入事务控制**：价格规则导入全程包裹于 `SqlTransaction` 事务中，整车道删除与批量插入在同一事务内完成；若批量校验出现任意一条数据异常，整批导入执行回滚，不会产生部分删除、部分写入的脏数据。
- **工作流协同**：事务机制与“预览-确认”导入工作流深度配合——预览阶段仅执行内存校验、不落库；用户确认提交后才开启事务执行整车道替换写入，最大程度降低无效数据对库表的影响。

## 5.6 PDA 数据存储（独立部署）
PDA 无订单报工模块采用独立数据库 `PDA_NoOrder` 进行数据存储，与 CloudWarehouse 系统物理隔离，符合限界上下文的架构划分原则。
- 核心数据表概念包括：用户表、机台/产线表、开工记录表、报工记录表，覆盖产线现场作业全流程的数据采集需求。
- 本期交付范围内，PDA 数据库与 CloudWarehouse 数据库不做共库集成与实时同步，二者为同一工厂场景下相互独立的业务上下文。

## 5.7 本章证据清单
本章所有设计结论均有对应的工程文件与数据库产物可验证，明细如下：

| 证据项 | 文件/位置 |
|------|----------|
| 实体关系图（ERD） | `docs/diagrams/07-erd.puml` |
| 核心库表结构脚本 | `database/schema.sql` |
| 结算模块表结构脚本 | `database/billing-schema.sql` |
| 客户报价模块表结构脚本 | `database/customer-quote-schema.sql` |
| 索引修正脚本 | `database/fix-price-rules-index.sql` |
| 可选验证 | SSMS 数据库表列表截图 |

## 5.8 本章小结
本章从设计目标、概念分组、关键决策、实体关系、事务机制多个维度，完整阐述了 CloudWarehouse 与 PDA 两大模块的数据库设计方案。其中双轨规则表拆分、一对多规则映射、整车道版本替换是历史价查询、双轨结算等上层业务能力的核心底层支撑，独立库的划分则保障了不同业务上下文的架构解耦。下一章将基于上述数据模型，进一步展开系统多视角架构设计与技术选型说明。

# 第六章 系统架构设计

System Architecture and Multi-View Design

第五章给出了支撑计费与结算的持久化模型；本章从 **多视角架构** 说明这些表与能力如何被组织进可部署系统：逻辑分层与限界上下文、与 PDA 的企业关系、物理运行拓扑，以及对高可用与未来拆分的诚实边界。中期反馈要求“单体需自辩、物理图写清基础设施、DDD 讲透”——本章直接回应这些点。

---

## 6.1 架构风格与决策动机

CloudWarehouse 采用 **模块化单体（Modular Monolith）**：

- **物理上** 一个可部署单元（`CloudWarehouse.Backend`，ASP.NET Core + 同库 SQL Server）；
- **逻辑上** 按限界上下文拆成模块文件夹（`Modules/MasterData`、`Import`、`Pricing`、`Billing`、`Assistant` 等），边界清晰，便于后续按触发条件提取服务。

选型对照（摘要）：

| 选项 | 优点 | 对本项目的不适配 |
|------|------|------------------|
| 微服务 | 独立部署、团队并行 | Solo + 四周 MVP；分布式事务/运维开销过高 |
| 传统大泥球单体 | 交付快 | 边界模糊，难演进、难答辩“有设计” |
| **Modular Monolith** | 单进程交付快 + 模块边界 | 本期最优；拆分保留为有条件规划 |

结论：单体不是能力不足，而是在 **时间、人力、一致性需求** 约束下的有意决策（见 ADR 与 `docs/diagrams/01a-architecture-decisions-adr.puml`）。

并列交付的 PDA 无订单报工是 **另一套可部署系统**（Android + Spring Boot + 独立库），不塞进云仓进程，也不假装已是同一微服务网格。

---

## 6.2 逻辑架构与典型请求流

**图 6-1** 分层与模块依赖。

逻辑视图见 `docs/diagrams/02-logical-architecture.puml`。分层职责：

| 层 | 代表组件 | 职责 |
|----|----------|------|
| Presentation | `wwwroot/index.html` | 管理端 Tab：主数据、导入、试算、运单、规则检索等 |
| API | 各模块 `*Controller` | HTTP 适配；校验入参；委托应用服务 |
| Application | Import / Calculate / BillImport / Assistant 等 Service | 编排用例、事务边界、跨 helper 协调 |
| Domain / Helpers | Excel 解析、`PriceRuleMapper`、`FeeCalculationEngine` + Strategy | 纯规则与计算；可单测 |
| Data | Dapper + SQL Server | 显式 SQL；同库事务 |

**价表导入（预览→确认）数据流（摘要）：**

1. 浏览器上传 `.xlsx` → `ImportController`；
2. `PriceRuleImportService` 调用 `ExcelHelper` 探测标准/三级表头等格式；
3. `PriceRuleMapper` 将一行 Excel 展开为多条 `PriceRule`；
4. 预览：`save=false`，可挂钩试算，**不写库**；
5. 确认：在 `SqlTransaction` 内按 lane 删除旧规则并插入新规则，提交或整批回滚。

运单双轨、Strategy 编排的详细时序放在 **软件设计章**（类图 13、时序 14）；本章只固定“逻辑落点”：结算编排在 Pricing/Billing 应用层，持久化落在第五章所述表。

---

## 6.3 限界上下文与代码映射

**图 6-2** Master Data / Import / Pricing 等边界。

DDD 在本项目中的用法是 **务实的限界划分**，不是完整事件溯源或聚合魔法。上下文见 `docs/diagrams/05-ddd-bounded-contexts.puml`，并与代码目录对齐：

| 限界上下文 | 职责 | 代码落点（示意） | 关键持久化 |
|------------|------|------------------|----------|
| Master Data | 站点/目的地/客户等基准数据 | `Modules/MasterData` | Sites, Destinations, Customers, CustomerAccounts |
| Import | 成本价表解析、校验、事务提交 | `Modules/Import` | 写入 PriceRules（无独立 job 表；客户报价导入在 Pricing） |
| Pricing | 成本规则试算、客户报价、Strategy 引擎 | `Modules/Pricing` + Pricing.Core | PriceRules, CustomerQuoteRules |
| Billing | 运单导入、应收应付对比落库 | `Modules/Billing` | BillLines |
| Assistant | 内置规则 RAG（辅助 FAQ，非结算真相源） | `Modules/Assistant` + KnowledgeBase | 文件知识库为主 |

**语言隔离示例：** Import 上下文中的 `PriceTableRow`（Excel 行视图）经映射变为 Pricing 上下文中的持久 `PriceRule` 集合；二者不应在 UI 层混用同一套字段语义。

Phase 1 早期代码曾集中在根目录 Controllers；重构后以 `Modules/*` 表达边界——报告叙述以 **当前模块结构** 为准。

---

## 6.4 企业级上下文关系（含 PDA）

**图 6-3** 云仓与 PDA 独立；集成为 Planned。

工厂视角下存在两个产品系统，关系见 `docs/diagrams/16-enterprise-context-map.puml`：

| 关系 | 含义（诚实表述） |
|------|------------------|
| CloudWarehouse 内部模块 | 同库 Modular Monolith；模块间进程内调用 |
| PDA ↔ 产线/MES 相关能力 | PDA 侧已实现开工/报工与后端落库；与既有 MES 的衔接按 PDA 项目实际描述，**不夸大** |
| CloudWarehouse ↔ PDA | **Customer–Supplier / 集成 Planned**：共享的是工厂业务目标，**本期无生产级 API 共库或实时同步** |

答辩禁话：不要说“微服务已上线”或“云仓与 PDA 已打通结算链路”。

---

## 6.5 物理部署与运行拓扑

**图 6-4** 单实例拓扑；无 HA。

物理/部署图：`docs/diagrams/03-physical-architecture.puml`、`04-deployment-diagram.puml`。

**演示 / 开发期典型拓扑：**

| 节点 | 角色 | 说明 |
|------|------|------|
| 开发者/演示机 | 运行 Backend（Kestrel）+ 浏览器 | 管理端与 API 同机或同发布包 |
| SQL Server | CloudWarehouse 库 | 可本机或局域网实例；端口通常 1433 |
| GitHub Actions Runner | 短暂 CI 节点 | `dotnet test`、覆盖率 Artifact；云端无常驻业务库时对依赖 DB 的用例可跳过并解释 |
| PDA 设备 + PDA API/DB | 并列系统 | 霍尼韦尔终端 ↔ Spring Boot ↔ `PDA_NoOrder`（独立） |

可选交付物：自包含发布包（`publish/`）便于现场演示；IIS 发布检查清单见项目管理文档，**不等于**已建成生产级多活集群。

**安全姿态（MVP 诚实声明）：** 本地/受控演示场景；认证/RBAC 按 ADR 延期；CORS/HTTP 等按开发便利配置，生产前需收紧——细节在 DevSecOps / 风险章展开，本章只标明架构层未宣称零信任生产加固。

---

## 6.6 高可用与备份（诚实声明）

| 方面 | 当前状态 | 规划方向（非本期必交付） |
|------|----------|--------------------------|
| 应用冗余 | 单实例，无负载均衡 | 容器多副本 + 反向代理 |
| 数据库冗余 | 单实例 SQL Server | 托管 HA / Always On 等 |
| 备份 | 手工 `.bak` / 脚本重建 | 自动备份与明确 RPO |
| 灾难恢复 | Git + `database/*.sql` 重建 | 文档化 RTO + 演练 |

中期要求“物理图写清基础设施/冗余”——正确做法是 **写清现状为无 HA**，而不是虚构集群。

---

## 6.7 微服务提取的触发条件（Planned）

模块边界已按上下文切开，但 **提取微服务需满足触发条件**，例如：

- 独立团队或独立发布节奏成为刚需；
- 某上下文（如计费计算）出现明显不同的扩展/性能特征；
- 运维与观测成本可被组织承担。

在 Solo 与当前业务量下，过早拆分会引入网络边界与分布式一致性成本，收益不足。故 M8「按触发条件提取」保持 **Planned**，与第四章里程碑一致。

---

## 6.8 本章证据清单

| 证据 | 位置 |
|------|------|
| 约束与 ADR 图 | `docs/diagrams/01*.puml` |
| 逻辑架构 | `02-logical-architecture.puml` |
| 物理 / 部署 | `03-physical-architecture.puml`、`04-deployment-diagram.puml` |
| DDD 限界 | `05-ddd-bounded-contexts.puml` |
| 企业 Context Map | `16-enterprise-context-map.puml` |
| 模块代码 | `CloudWarehouse.Backend/Modules/*` |
| 发布/演示包（可选截图） | `publish/`、启动脚本 |

---

## 6.9 本章小结

本章论证了 Modular Monolith 作为约束下的合理选择，并用逻辑分层、限界上下文与企业 Context Map 回应“架构叙述不足”；用物理拓扑与 **无 HA** 的诚实清单回应基础设施透明度要求。PDA 作为独立上下文并列存在，集成保持 Planned。下一章进入 **软件设计**：Strategy 计费、双轨时序与关键类结构，把架构落点细化为可验证的设计产物。

# 第七章 软件设计
Software Design: Strategy Pattern and Dual-Track Billing
第六章从多视角架构层面明确了模块化单体的结构与限界上下文的落点；本章聚焦可验证的详细设计：通过策略模式（Strategy Pattern）管理计费算法变体，通过类级时序图说明运单预览场景下应收 / 应付双轨的协作逻辑，并明确历史价过滤、重量取整、金额对比校验在全链路中的位置。本章直接回应中期评审对「设计模式落地 + 详细设计产出（类图 / 时序图）」的要求。
## 7.1 设计范围与核心用例
本章核心设计围绕主用例展开：运单 Excel 导入预览 + 双轨计价对比—— 管理员在管理端上传账单明细后，系统自动计算每一行的应收金额与应付金额，并与 Excel 表内预填金额进行比对校验。

| 纳入本章设计范围 | 不纳入主路径（其他章节或边界说明） |
| --- | --- |
| Strategy 类结构与策略解析器 | 内置规则 RAG / Assistant（仅查阅，不参与金额结算） |
| FeeCalculationEngine 计费编排 | PDA 报工业务流程 |
| 双轨应用服务编排与时序逻辑 | 微服务拆分相关设计 |
| 重量取整规则、按日期过滤历史价 | 身份认证 / RBAC 权限体系 |

链路参与者与分层结构与第六章完全对齐：浏览器 → BillController → BillImportService → 双轨计算器 → 成本 / 报价计算服务 → 计费引擎 → 具体策略实现 → SQL Server 数据库。
## 7.2 计费变体问题与 Strategy 动机
物流价表的计费逻辑包含多类基础算法，且存在持续扩展的业务可能性。若以条件分支堆砌实现，将违背开闭原则，也无法支撑「可演进设计」的论证。

| 计费变体 | 业务含义 | 实现状态 | 对应策略类 |
| --- | --- | --- | --- |
| 区间计费（Tier） | 重量 ≤5kg 时匹配离散重量档位，按档内单价 + 面单费计算 | Done | TierBillingStrategy |
| 续重计费（Overweight） | 重量 >5kg 时按续重单价计算费用 | Done | OverweightBillingStrategy |

体积重计费（Volumetric）	当长 × 宽 × 高 / 6000 计算值大于实重时，按体积重执行区间或续重计费	Done（引擎 + 单元测试覆盖）	VolumetricBillingStrategy
阶梯计费 / 异形件计费等	合同定制化扩展场景	Planned	预留类名，暂未实现
Phase 1 阶段可通过条件分支覆盖前两类计费逻辑；若持续新增合同算法，每次扩展都需修改核心计算路径，代码可读性与可维护性将快速下降。因此 Phase 2 将各类计费算法抽象为可替换的策略，由解析器根据业务上下文自动选择，计费引擎仅负责「过滤生效规则 → 匹配策略 → 执行计算」的标准化流程。
该设计决策记录于 ADR-8（Billing Strategy Pattern，Implemented）。
## 7.3 Strategy 类设计

**图 7-1** Tier / Overweight / Volumetric + FeeCalculationEngine。

策略模式类图对应文件：docs/diagrams/13-billing-strategy-class.puml。核心代码位于 CloudWarehouse.Pricing.Core 项目的 Billing 命名空间下，并通过 Backend 依赖注入容器完成注册。
### 7.3.1 关键类型职责

| 类型名称 | 核心职责 |
| --- | --- |
| BillingContext | 计费上下文载体，承载计费重量、当前生效规则列表、可选的长宽高参数与体积重除数 |
| IBillingStrategy | 计费策略统一接口，定义 CanHandle(context) 适配判断与 Calculate(context) 计算方法，输出 PriceCalculateResult |
| TierBillingStrategy | 实重（或计费重）落在 ≤5kg 区间时执行档位计费 |
| OverweightBillingStrategy | 重量 >5kg 时执行续重计费 |
| VolumetricBillingStrategy | 存在尺寸参数且体积重大于实重时接管计算，将最终计费重委托给区间或续重策略 |
| IBillingStrategyResolver / DefaultBillingStrategyResolver | 策略解析器，按注册顺序遍历，返回第一个 CanHandle == true 的策略 |
| FeeCalculationEngine | 计费引擎核心，按订单日期 / 账单日期过滤规则的生效 / 失效期，组装计费上下文，调用解析器执行计算 |
| FeeRuleCalculator | 静态门面类，委托默认引擎执行计算，兼容历史调用方 |
| DualTrackFeeCalculator | 应用层服务，针对同一运单行分别计算应付与应收金额，汇总对比结果 |

### 7.3.2 策略解析顺序
CreateDefault() 方法与 DI 容器中的注册顺序为：
1.	VolumetricBillingStrategy（存在尺寸且体积重更大时优先接管）
2.	TierBillingStrategy
3.	OverweightBillingStrategy
解析顺序本身是设计的一部分：体积重判断必须先于基于实重的区间 / 续重策略，否则新增的体积重策略会被实重策略短路，无法生效。
### 7.3.3 与数据模型的衔接
所有策略消费的都是经过过滤的 PriceRule 结构列表：应付轨数据来自 PriceRules 表，应收轨数据从 CustomerQuoteRules 表读取并映射为统一结构后进入计费引擎。BillingType 标识与重量上下界共同决定档位匹配逻辑；规则版本选择发生在引擎过滤阶段，而非硬编码在单个 Strategy 内部，保证策略的纯粹性。
## 7.4 开闭原则与扩展步骤
体积重计费的落地是开闭原则的直接验证：仅通过新增策略类 + 在解析器 / DI 中注册，即可完成能力扩展，原有仅支持物理重量的调用方无需修改算法分支；FeeRuleCalculator 对仅传入重量的调用路径保持完全兼容。
新增计费类型的标准扩展步骤为三步：
1.	实现 IBillingStrategy 接口，明确定义 CanHandle 适配条件，避免错误抢占上下文；
2.	在 DefaultBillingStrategyResolver.CreateDefault() 与 Program.cs DI 中注册新策略，严格控制注册顺序；
3.	若业务需要新增输入参数（如长宽高、异形件标记），再扩展导入列或 API 上下文，无需修改双轨编排的核心骨架。
当前处于 Planned 状态的阶梯计费、异形件计费等能力，均可重复上述注册路径完成扩展，无需重写 BillImportService 等上层编排逻辑。
## 7.5 运单双轨时序（详细设计 — 类与对象级）

**图 7-2** 运单预览双轨结算 — **类与对象级时序图（Class & Object Level Sequence Diagram）**

> **回应导师反馈：** 本图不以「API 层 / 服务层 / 数据层」等组件框表示交互，而以**具体类实例**为生命线（如 `bc : BillController`、`importSvc : BillImportService`、`row : WaybillImportRow`），消息名与仓库源码方法一致，满足 *analysis → design* 中「关键用例的类级时序」要求。

源文件：`docs/diagrams/14-sequence-waybill-dual-track.puml`（PlantUML 导出 PNG 后插入 Word **图 7-2**）。

双轨语义：应收 = 面向客户的报价（`CustomerQuoteRules`）；应付 = 面向供应商的成本价（`PriceRules`）；**并非**国内/国际线路区分。

### 7.5.0 参与者与源码映射

| 时序图参与者（实例 : 类） | 源码位置 | 职责 |
| --- | --- | --- |
| `bc : BillController` | `Modules/Billing/Controllers/BillController.cs` | `PreviewWaybills` → `ProcessUpload(..., saveToDatabase=false)` |
| `importSvc : BillImportService` | `Modules/Billing/Services/BillImportService.cs` | 解析、主数据匹配、逐行编排 |
| `WaybillExcelHelper` | `Helpers/WaybillExcelHelper.cs` | `ReadWaybills` 解析 Excel |
| `row : WaybillImportRow` | `Models/WaybillImportRow.cs` | 循环内行对象，承载双轨金额与比对标记 |
| `dualCalc : DualTrackFeeCalculator` | `Modules/Billing/Services/DualTrackFeeCalculator.cs` | 应收/应付双轨协调（Facade） |
| `costSvc : PriceRuleCalculateService` | `Modules/Pricing/Services/PriceRuleCalculateService.cs` | 应付轨查 `PriceRules` |
| `quoteSvc : CustomerQuoteCalculateService` | `Modules/Pricing/Services/CustomerQuoteCalculateService.cs` | 应收轨查 `CustomerQuoteRules` |
| `feeEngine : FeeCalculationEngine` | `CloudWarehouse.Pricing.Core/Billing/FeeCalculationEngine.cs` | 历史价过滤 + 调用策略 |
| `resolver : DefaultBillingStrategyResolver` | `Pricing.Core/Billing/DefaultBillingStrategyResolver.cs` | `Resolve(BillingContext)` 选策略 |
| `tier : TierBillingStrategy` | `Pricing.Core/Billing/TierBillingStrategy.cs` | 示例具体策略（亦可为 Overweight/Volumetric） |
| `BillLineTotals` | `Modules/Billing/Helpers/BillLineTotals.cs` | 静态汇总与 `ApplyComparison` |

### 7.5.1 预览正常流程
1.	管理员选择运单 Excel 文件，点击「预览」按钮；
2.	请求 POST /api/Bill/waybill/preview 到达 BillController，转发至 BillImportService.ProcessImportAsync(..., saveToDatabase=false)；
3.	WaybillExcelHelper 解析双行表头（账单明细 + 成本明细）或标准模板，得到行数据集合，并提取表内期望中转费（若文件包含对应列）；
4.	预加载站点、目的地、客户、客户账户及规则相关的缓存数据；
5.	逐行处理：校验运单号、省份、重量等字段 → 执行 WeightRounding 重量取整 → 解析匹配客户、站点（通常由快递类型对应 SiteCode）、目的地；
6.	调用 DualTrackFeeCalculator.CalculateAsync(row) 执行双轨计算： 
o	应付轨：PriceRuleCalculateService 按 SiteId/DestId + 账单日期查询 PriceRules → 传入 FeeCalculationEngine → 策略解析器 → 匹配 Tier/Overweight/Volumetric 策略计算；
o	应收轨：CustomerQuoteCalculateService 按 CustomerId / 省份 + 账单日期查询 CustomerQuoteRules → 传入同一套计费引擎与策略体系计算；
7.	调用 BillLineTotals 进行汇总与对比，计算应收、应付、毛利，并与 Excel 表内期望值进行容差对比（默认容差 0.01），标记匹配结果；
8.	返回预览结果集，包含系统计算值、表内原值、一致 / 不一致统计，供前端 UI 展示。
确认入库时，同一计算链路可在 saveToDatabase=true 模式下将结果写入 BillLines 表；预览与落库共用同一套编排逻辑，事务边界由导入服务统一控制。
### 7.5.2 设计要点
•	双轨是应用层协调模式，并非两套重复的条件分支计费内核；两条轨道共享同一套 Strategy 计费引擎，保证算法一致性的同时实现财务语义分离。
•	历史价能力由引擎层统一实现，按账单日期过滤规则生效周期，避免「永远使用最新价格」导致的对账不可复现问题。
•	对比校验层（BillLineTotals） 将设计目标落地为可演示证据：系统计算结果与人工价表可逐行核对，直观验证计费准确性。
## 7.6 历史价与重量取整在设计中的位置
历史价过滤、重量取整、一对多规则映射三类逻辑与策略体系保持正交，独立演化互不影响，具体设计位置如下：

| 关注点 | 设计位置 | 说明 |
| --- | --- | --- |

历史价过滤	FeeCalculationEngine.Calculate 规则过滤阶段	过滤条件为 EffectiveDate <= 账单日期 且未超过 ExpiryDate；导入侧可通过 MasterPriceHistoryHelper 从多版本 Excel 列展开为多段生效规则
重量取整	双轨计算前的 WeightRounding 统一处理	正向计费取整规则在试算、运单批量预览等所有路径保持一致，避免 UI 试算与批量预览口径不一致
一对多规则行映射	第五章数据模型 + Mapper 组件	单行 Excel 价表拆解为多条规则行，供 Tier 策略按重量区间匹配
## 7.7 次要用例：价表导入（简述）
成本价表导入时序对应文件：docs/diagrams/08-sequence-import.puml，第六章已说明分层数据流。从软件设计角度补充：导入流程产出的多条 PriceRules 记录正是 Strategy 计费引擎的输入数据源；Import 上下文不实现任何计费算法，仅负责格式解析、字段映射与事务化替换写入。客户报价导入遵循同类设计模式，写入 CustomerQuoteRules 表，供应收轨计费使用。
## 7.8 设计边界与后续规划

| 设计项 | 当前真实状态 |
| --- | --- |

体积重计费	引擎 API 与单元测试已完整覆盖；运单 Excel 主路径仍以实重计费为主，尺寸列尚未普遍接入业务流程
内置规则 RAG	不读写任何结算金额；正式结算以 FeeCalculationEngine 的计算结果为唯一真相源
Step 阶梯 / 异形件 / 附加费策略	Planned 状态，扩展路径已在 7.4 节说明
与 PDA 系统结算打通	非本章、非本期交付设计范围
## 7.9 本章证据清单

| 证据项 | 文件位置 |
| --- | --- |
| Strategy 计费策略类图 | docs/diagrams/13-billing-strategy-class.puml |

运单双轨结算时序图	docs/diagrams/14-sequence-waybill-dual-track.puml
价表导入时序图（可选）	docs/diagrams/08-sequence-import.puml
策略与引擎核心代码	CloudWarehouse.Pricing.Core 下 Billing 类型；Modules/Billing/Services/DualTrackFeeCalculator.cs
单元测试用例	CloudWarehouse.Tests/BillingStrategyTests.cs 等
架构决策记录	ADR-8 / 中期写作指南 §20.2–20.3
## 7.10 本章小结
本章以策略模式回应了计费变体的可扩展性要求，以双轨时序回应了结算协作的详细设计要求：应收与应付分轨管理、共享统一计费引擎、按日期获取历史价格，并通过表内金额对比形成可演示的验证证据。开闭原则通过体积重策略的增量式注册得到了实际验证。下一章将转向 DevSecOps 与质量保障体系，说明上述设计如何通过自动化测试与 CI 流水线形成质量约束，而非仅停留在设计图纸层面。
# 第八章 DevSecOps 与质量保障
DevSecOps and Quality Assurance
第七章完整呈现了策略模式与双轨结算的详细设计；本章说明上述设计如何通过自动化质量门禁与安全扫描形成刚性约束，而非仅停留在类图与时序图层面。本章遵循贯穿中期评审与答辩的统一原则：有实证支撑的表述为已落地，未实现的能力明确标注缺口与规划路径，不宣称已建成完整 DevSecOps 平台或生产级持续交付能力。
## 8.1 本章范围与本项目口径下的 DevSecOps
针对本次实习交付的项目体量，DevSecOps 落地为四个务实层级，而非营销概念：

| 层级 | 核心含义 | 本仓库落地状态 |
| --- | --- | --- |
| 持续集成（CI） | 代码提交 / PR 触发自动构建与测试 | Done（基于 GitHub Actions 实现） |
| 质量门禁 | 单元测试、集成测试、轻量并发与性能冒烟测试 | Done |
| 安全扫描 | 静态应用安全测试（CodeQL）+ NuGet 依赖脆弱性排查 | Done（依赖扫描配置为 continue-on-error，以留存证据为主，不作为硬阻断门禁） |
| 安全基线 | 文件上传限制、配置脱敏示例、演示环境安全假设 | 部分 Done；身份认证等能力为 Planned 状态 |

本章质量与安全证据主要来自 CloudWarehouse 系统的 .github/workflows/ 目录下的流水线配置。并列交付的 PDA 无订单报工系统为独立体系：其安全基于 API 访问、独立数据库、内网演示环境假设，不与 CloudWarehouse 混称为一套已打通的统一安全网格。
流水线活动图对应文件：docs/diagrams/09-cicd-pipeline.puml。
流水线活动图对应文件：`docs/diagrams/09-cicd-pipeline.puml`。

**图 8-1** CI 为主；完整 CD 未宣称。

**图 8-2** 绿勾证据。

**图 8-3** 勿在正文写死百分比口号。

## 8.2 持续集成流水线
核心工作流文件：.github/workflows/ci.yml。
触发规则：针对 main / master 分支的 push 与 pull_request 操作触发流水线。
运行环境：ubuntu-latest Runner + .NET SDK 9.0.x，与本地 Windows 开发环境形成跨平台校验。
标准执行步骤：
1.	执行 actions/checkout 拉取代码；
2.	通过 setup-dotnet 配置 .NET 运行环境；
3.	执行 dotnet restore CloudWarehouse.sln 还原项目依赖；
4.	执行 Release 模式下的 dotnet test，配合 Coverlet 与 coverlet.runsettings 采集跨平台代码覆盖率，结果输出至 ./coverage 目录；
5.	安装 ReportGenerator 工具，生成 HTML 格式与文本摘要格式的覆盖率报告，输出至 coveragereport/ 目录；
6.	打印覆盖率摘要 Summary.txt 至流水线日志；
7.	执行 dotnet list ... package --vulnerable --include-transitive 扫描依赖漏洞，结果写入 vulnerable-packages.txt，扫描失败不阻断流水线，仍正常上传产物；
8.	上传构建产物 Artifact：包含 coverage-report、coverage-cobertura、nuget-vulnerable-scan 三类文件。
该流水线解决了 “仅本地环境可运行通过” 的质量风险，保证主干代码合并前有统一、客观的验证结果。
需要明确说明：本流水线属于 CI + 质量 / 安全产物 的范畴，并未实现面向生产环境的完整持续部署（CD）。当前系统发布以自包含发布包、手工部署配合检查清单为主，相关说明见架构章节与发布文档。
## 8.3 测试金字塔与关键验证类型
本项目测试体系遵循测试金字塔原则，从下到上分为三层，验证重点与第五章数据库设计、第七章软件设计一一对应：

| 测试层级 | 实现项目与手段 | 核心验证重点 |
| --- | --- | --- |
| 单元测试 | CloudWarehouse.Tests 项目 | 覆盖策略模式（区间 / 续重 / 体积重）、Excel 解析与字段映射、历史价辅助逻辑、重量取整规则、规则检索逻辑、解析性能冒烟测试等 |
| 集成测试 | CloudWarehouse.IntegrationTests 项目 + WebApplicationFactory | 覆盖主数据管理、价表导入、客户报价、运单结算等模块的 HTTP API 全链路 |
| 轻量压力 / 并发测试 | 如 StressLoadTests 测试用例 | 针对模板下载、导入预览等高频场景做并发冒烟验证，属于演示级验证，不构成生产级 SLA 认证 |

环境适配诚实策略：GitHub Actions 云端 Runner 通常无常驻 SQL Server 实例。对于依赖真实数据库的集成测试用例，通过 DatabaseAvailability 等判断逻辑实现可解释跳过：数据库不可达时自动跳过对应用例，避免流水线出现环境导致的 “假红”；本地开发环境配备 SQL Server 时则执行完整测试链路。该设计是环境适配方案，而非隐瞒测试失败。
测试体系的设计目标优先保障计费引擎重构与双轨逻辑的可回归性，而非追求虚高的覆盖率数字。

**证据来源（可复现）：** 仓库 `https://github.com/chenyuxiangAK47/cloudwarehouse-csharp` → **Actions** → workflow **CI** → 打开最新绿色 run，查看 **Test with coverage** 步骤日志；本地复现命令为 `dotnet test CloudWarehouse.sln`（完整日志备份：`docs/project-management/artifacts/dotnet-test-full.txt`）。

### 8.3.1 单元测试与集成测试执行结果（2026-08-25 本地复现）

本项目为 **Modular Monolith**（模块化单体），**非微服务架构**；下表按**逻辑模块**分组展示测试结果，而非按虚构的 “microservice” 拆分。

**汇总（`dotnet test CloudWarehouse.sln`，Windows 本机，Release）：**

| 测试项目 | 通过 | 失败 | 跳过 | 耗时（约） |
| --- | ---: | ---: | ---: | --- |
| `CloudWarehouse.Tests`（单元） | **83** | 0 | 0 | 6.6 s |
| `CloudWarehouse.IntegrationTests`（API 集成） | **27** | 0 | 0 | 8.4 s |
| `CloudWarehouse.E2ETests`（Playwright UI） | **4** | 0 | 0 | ~2 s |
| **合计** | **114** | **0** | **0** | ~17 s |

**图 8-4（建议截图）：** GitHub Actions → CI → 绿色 run → **Test with coverage** 日志末尾 `Passed` 汇总；或本地终端同一命令的输出末尾（附录 **A-12**）。

**按模块的代表性用例（单元测试 `CloudWarehouse.Tests`）：**

| 逻辑模块 | 代表性测试类 | 验证重点 | 结果 |
| --- | --- | --- | --- |
| Pricing / Strategy | `BillingStrategyTests`, `PriceCalculatorTests`, `FeeCalculationPerfSmokeTests` | Tier / 续重 / 体积重策略、`FeeCalculationEngine`、注入解析器 | 全部通过 |
| Import / Excel | `ExcelHelperTests`, `WaybillExcelHelperTests`, `SiteExcelHelperTests`, `DestinationExcelHelperTests`, `CustomerExcelHelperTests`, `CustomerQuoteExcelHelperTests` | 价表/运单/主数据 Excel 解析与模板往返 | 全部通过 |
| Bill / 双轨 | `BillLineTotalsTests`, `Waybill93FileTests`, `BillImportServiceRegionTests` | 应收应付汇总、表内对比容差、省份归一化 | 全部通过 |
| 历史价 / 规则映射 | `MasterPriceHistoryHelperTests`, `PriceRuleMapperTests`, `MasterCostExcelTests` | 多版本价表展开、一对多规则映射、93 成本样例 | 全部通过 |
| Assistant（词法 FAQ） | `QuoteAssistantTests`, `QuoteAssistantEvalTests` | 检索命中与引用；**不替代计费引擎** | 全部通过 |

**集成测试（`CloudWarehouse.IntegrationTests` + `WebApplicationFactory`）：**

| API 域 | 代表性测试类 | 验证重点 | 结果 |
| --- | --- | --- | --- |
| Import | `ImportApiTests` | 价表预览/导入、非法扩展名、事务预览 | 全部通过 |
| Bill | `BillApiTests` | 运单预览、双轨计费（DB 可用时）、导出 | 全部通过 |
| Customer Quote | `CustomerQuoteApiTests` | 客户报价预览/导入 | 全部通过 |
| Master data / 静态页 | `SiteAndStaticApiTests` | Site/Destination/Customer API、`index.html` 可访问 | 全部通过 |
| 轻量并发 | `StressLoadTests` | 见 §8.7 | 全部通过 |

### 8.3.2 端到端测试（E2E）与 Playwright

| 方式 | 状态 | 说明 |
| --- | --- | --- |
| **Playwright UI 自动化** | **已实施** | 独立项目 `CloudWarehouse.E2ETests`：Kestrel 动态端口 + Chromium headless，类 `UiSmokeE2ETests` 共 **4** 项冒烟（首页导航、运单导入、客户报价导入、Rule RAG 面板）。本地复现：`dotnet test CloudWarehouse.E2ETests --filter Category=E2E`；日志备份 `docs/project-management/artifacts/e2e-playwright-test.txt`。 |
| **API 级 E2E** | **已实施** | `WebApplicationFactory` 对 `/api/*` 全链路发 HTTP 请求，覆盖导入、运单双轨、主数据等关键路径（§8.3.1 中 27 项集成测试）。 |
| **手工 E2E（演示）** | **已实施** | 评估视频 App Demo：运单 Preview、价表导入、PDA 开工报工；报告附录 **A-04～A-06、A-08** 为截图证据。 |
| **Planned** | 后续 | 扩展 Playwright：文件上传 + Preview 结果断言、跨浏览器矩阵、视觉回归。 |

**技术栈：** Microsoft.Playwright 1.50 + xUnit；CI 在 `ubuntu-latest` 上执行 `playwright.sh install --with-deps chromium` 后随 `dotnet test CloudWarehouse.sln` 一并运行（见 `.github/workflows/ci.yml`）。

**图 8-4b（建议截图）：** 本地或 Actions 日志中 `CloudWarehouse.E2ETests` 四项 `Passed`；或 Playwright trace（若启用）。

## 8.4 覆盖率证据（表述规范）
代码覆盖率由 Coverlet 工具采集，经 ReportGenerator 生成 HTML 报告，并作为每次 CI 构建的 Artifact 归档。报告附录可粘贴两类截图作为证据：
•	GitHub Actions 某次成功运行的绿勾截图；
•	覆盖率报告中 Summary 总览页的截图。
正文禁止写死「覆盖率 >80%」这类无法随构建动态更新的绝对化表述。规范表述为：覆盖率报告随每次 CI 构建的 Artifact 可追溯查询，核心业务模块（Pricing Core 计费引擎、Import 解析模块、Bill 双轨结算模块）均有自动化用例覆盖保护；具体覆盖率数值以附录截图对应构建日的实际结果为准。

### 8.4.1 覆盖率解读（如何读 CI Artifact）

每次 CI 成功构建会上传 **`coverage-report`**（HTML）与文本 **Summary**。阅读时应关注：

| 区域 | 预期 | 原因 |
| --- | --- | --- |
| `CloudWarehouse.Pricing.Core` / Billing | 较高 | Strategy、`FeeCalculationEngine` 有密集单元测试 |
| Import Helpers / Excel 解析 | 较高 | 多格式 Excel 单测 + 93 样例文件测试 |
| `Modules/Billing`（双轨编排） | 中等偏高 | 集成测试 + `BillLineTotals` 单测 |
| `wwwroot/index.html`、薄 Controller | 偏低 | MVP 以 API 测试为主；**Playwright 冒烟覆盖主导航与三大面板**（§8.3.2） |
| Assistant 模块 | 中等 | `QuoteAssistantTests` + Eval 黄金集 |

**图 8-3 / 附录 A-02：** 从 GitHub Actions 下载当次 `coverage-report`，截取 Summary 总览与上述模块行——**以该构建日数字为准**，正文只作定性解读。
## 8.5 SAST 与依赖供应链扫描
### 8.5.1 CodeQL 静态安全测试（SAST）
对应工作流文件：.github/workflows/codeql.yml（任务名 CodeQL SAST）。
•	触发规则：主干分支的 push / PR 操作触发，同时配置每周定时（cron）全量扫描；
•	扫描语言：C#；
•	查询规则集：security-and-quality；
•	执行步骤：初始化 CodeQL 环境 → 还原依赖并构建项目 → 执行 codeql-action/analyze 分析。
SAST 用于在代码合并前发现可自动识别的缺陷模式；不能替代人工设计评审，也不等同于动态渗透测试（DAST）。扫描发现问题的标准处理流程：修复缺陷 → 重跑流水线至通过 → 再合入主干。
### 8.5.2 NuGet 脆弱依赖扫描
CI 流水线中执行 dotnet list package --vulnerable --include-transitive 命令扫描全量依赖（含传递依赖）的已知漏洞，扫描结果随 Artifact 归档。
当前策略为可见性优先（配置 continue-on-error: true）：优先保障主干集成流程通畅，同时留存供应链风险清单供人工跟进修复；后续可根据业务要求调整为阈值门禁模式。

### 8.5.3 安全扫描结果与处置（Before → Resolution decision）

导师要求 *before with vulnerabilities … and after resolution*。本项目**不伪造「修完变 0 漏洞」截图**，而是按工程实践给出 **扫描 → 评估 → 处置决策** 闭环。

**（1）CodeQL SAST（静态代码）**

| 阶段 | 证据 | 结果 |
| --- | --- | --- |
| Before / After | GitHub Actions → workflow **CodeQL SAST** → 最近一次绿色 run（如 #7，2026-08-24） | **0 open Critical/High** 代码层告警（以 Actions Summary 为准） |
| Resolution | 无待修复 CodeQL alert 时，处置为 **monitor on each push/PR + weekly cron** | 附录 **A-03** 截图 |

**（2）NuGet 依赖扫描（供应链）**

| 阶段 | 发现 | 处置（Resolution decision） |
| --- | --- | --- |
| **Before（可见性扫描）** | 传递依赖 `Azure.Identity` 1.11.3、`Microsoft.Identity.Client` 4.60.3 在 Backend/Tests/IntegrationTests 上报告 **Moderate**（GHSA-m5vv-6r4h-3vj9） | CI 步骤 `NuGet vulnerability scan` + Artifact `nuget-vulnerable-scan`；见 QA 页 NuGet 段落 |
| **After（本期决策，非包升级）** | 未在本期强行升级传递依赖（避免牵一发动全身） | **Risk acceptance for MVP：** 系统为内网/demo、无对外暴露的生产多租户面；JWT/RBAC 未上线，攻击面以「受控演示机」为边界；项记入 **Planned**：下一迭代随 `Microsoft.Data.SqlClient`/Identity 栈统一升级后 **rescan** |
| **若导师追问「after」** | 不是「漏洞数变 0」，而是 **documented resolution**：已记录、已评估、已排期，CI 保持可见性 | 附录 **A-14** + https://chenyuxiangAK47.github.io/cloudwarehouse-csharp/ |

**（3）DAST** — 未实施常态门禁（§8.8）；无 before/after 产物。

**图 8-6（建议截图）：** CodeQL 绿勾 +（可选）Security 标签页 overview；NuGet 扫描 Moderate 列表（上半即可）。
当前已落地的应用层安全控制如下，属于 MVP 阶段务实安全基线：

| 安全控制项 | 说明 |
| --- | --- |
| 上传文件白名单 | 价表、运单等文件上传接口仅允许 .xlsx / .xlsm 等指定扩展名文件 |
| 文件大小限制 | 限制上传文件最大体积，降低超大文件导致的 DoS 攻击面，与风险登记册中大文件上传风险项对应 |
| 配置信息脱敏 | 提供 appsettings.example.json 配置模板；真实数据库连接串等敏感配置仅留存于本地或部署机，不随代码仓库传播 |
| 演示环境假设 | 系统默认运行于本机或受控内网环境；身份认证与 RBAC 权限按 ADR 决策延期实现，已纳入风险清单与里程碑 Planned 项 |

以上控制是「优先交付结算核心价值、安全能力分阶段补强」思路下的务实基线，不构成生产级零信任安全声明。
## 8.7 性能基线（轻量级负载 / 冒烟测试）

本章性能数据为**可复现的冒烟级基线**，使用 xUnit + `Stopwatch` / 并发 `Task.WhenAll` 实现，**非** k6/JMeter 生产压测，**不构成 SLA 认证**。

**实测结果（专用 filter 跑批，2026-08-25，Windows 本机；命令见下）：**

| 测试用例 | 场景 | 阈值 / 断言 | 实测 | 结果 |
| --- | --- | --- | --- | --- |
| `Import1000RowPerfTests` | 标准价表 **1000 行**纯解析（无 SQL） | &lt; 30 s | **543 ms**（`[PERF]`，2026-08-25 filter 跑批） | 通过 |
| `FeeCalculationPerfSmokeTests` | 计费引擎 **1000 次** `CalculateActive` | 循环 &lt; 200 ms | **&lt; 1 ms**（`[PERF] x1000: 0 ms`，Stopwatch 精度；断言 &lt;200 ms 通过） | 通过 |
| `StressLoadTests.TemplateDownload_30Concurrent` | **30 路并发** GET 模板 | 全部 HTTP 200，总耗时 &lt; 10 s | **986 ms** | 通过 |
| `StressLoadTests.PriceTablePreview_15Concurrent` | **15 路并发** POST 预览 | 全部 HTTP 200，总耗时 &lt; 30 s | **439 ms** | 通过 |

复现命令：

```text
dotnet test CloudWarehouse.sln --filter "FullyQualifiedName~Perf|FullyQualifiedName~Load|FullyQualifiedName~Stress" --logger "console;verbosity=detailed"
```

日志备份：`docs/project-management/artifacts/perf-load-stress-detailed.txt`

**图 8-5（建议截图）：** CI 或本地日志中含 `[PERF] ExcelHelper.ReadPriceTable 1000 rows: 114 ms` 与 `StressLoadTests` 通过行（附录 **A-13**）。

**诚实边界：** 未做长时间 soak test、未模拟万级并发、未对 SQL Server 做独立压测；云端 CI 无业务库时，部分 DB 集成用例会跳过，与本地全绿 **114** 项可能略有差异——以 Actions 日志中 **Passed/Skipped** 为准并附说明。
## 8.8 能力清单与后续规划
当前 DevSecOps / 工程化能力交付与后续项如下：

| 能力项 | 当前状态 | 说明 |
| --- | --- | --- |
| 动态应用安全测试（DAST，如 OWASP ZAP） | 规划基线 | 演示环境可跑 ZAP baseline（Planned） |
| Playwright / UI E2E 自动化 | **已实施（4 项冒烟）** | `CloudWarehouse.E2ETests`；扩展上传断言见 Planned |
| Infrastructure as Code（Terraform + Bicep + Compose） | **已实施** | 见 §8.8.1；CI workflow `iac.yml` 校验 |
| CD 至演示环境 | 部分 | CI 绿构建 + Bicep/TF 一键部署脚本；生产全自动 CD Planned |
| 容器化交付 | **已实施** | `Dockerfile` + `docker-compose.yml`（API + SQL） |
| JWT 身份认证 / RBAC 权限体系 | Planned（Backlog 已建 Story） | Jira CSV 中 To Do |
| HTTPS 强制 / CORS 收紧 | Bicep 默认 `httpsOnly=true` | 本地开发仍可用 HTTP |
| 密钥托管 | 参数文件 + 部署时注入 | 生产建议 Key Vault（Planned） |

### 8.8.1 Infrastructure as Code（IaC）——已交付

本期已落地完整 IaC 分层，满足导师清单：

| 能力 | 状态 | 路径 / 命令 |
| --- | --- | --- |
| Azure Bicep | **Done** | `infra/bicep/main.bicep`（App Service + SQL + App Insights） |
| Terraform | **Done** | `infra/terraform/main.tf`（同拓扑） |
| Docker Compose | **Done** | 根目录 `docker-compose.yml`（SQL 2022 + API） |
| 容器镜像 | **Done** | `Dockerfile`（.NET 9 多阶段构建） |
| 数据库即代码 | **Done** | `database/*.sql` |
| 流水线即代码 | **Done** | `.github/workflows/ci.yml`、`codeql.yml`、`pages.yml`、**`iac.yml`** |
| IaC 校验门禁 | **Done** | `iac.yml`：`docker compose config` + `az bicep build` + `terraform validate` |

部署示例：

```bash
az group create -n rg-cloudwarehouse-demo -l southeastasia
az deployment group create -g rg-cloudwarehouse-demo -f infra/bicep/main.bicep -p @infra/bicep/parameters.dev.json
# 或
terraform -chdir=infra/terraform init && terraform -chdir=infra/terraform apply
docker compose up -d --build
```

### 8.8.2 容器与合规范围

| 项 | 状态 |
| --- | --- |
| 容器镜像 / Compose 拓扑 | **Done**（可对接 Trivy 扫描镜像） |
| SOC2 / HIPAA / GDPR 认证申请 | 本系统为工厂内网结算/报工 MVP，**不宣称**已获第三方合规认证；安全控制见 §8.5–8.6 |

整体：CI + E2E + SAST + **IaC 校验** + Jira 跟踪包，覆盖 DevSecOps 与项目管理评分点。
## 8.9 本地开发环境 vs CI 环境
两类环境的差异本身是 DevOps 工程化的佐证，保证质量门禁不绑定单台开发电脑：

| 维度 | 本地开发环境 | CI 环境（GitHub Actions） |
| --- | --- | --- |
| 操作系统 | 通常为 Windows | ubuntu-latest |
| SQL Server | 常驻可用 | 通常无；依赖数据库的测试用例跳过或受限运行 |
| 覆盖率采集 | 可选手工执行 | 每次构建强制生成并上传 Artifact |
| 环境状态 | 有状态开发机 | 无状态临时 Runner |

跨平台、无状态的 CI 验证，有效避免了 “本地能跑、线上失败” 的环境差异问题。
## 8.10 本章证据清单

| 证据项 | 文件位置 |
| --- | --- |
| CI 核心工作流 | .github/workflows/ci.yml |
| CodeQL 安全扫描工作流 | .github/workflows/codeql.yml |
| CI/CD 流水线活动图 | docs/diagrams/09-cicd-pipeline.puml |
| Actions 构建成功记录 | GitHub Actions 成功运行截图 |
| 单元/集成/E2E 测试汇总（114 passed） | Actions **Test with coverage** 或 `artifacts/dotnet-test-full.txt` |
| 负载冒烟 `[PERF]` 输出 | 同上日志 / `artifacts/load-smoke.txt` |
| 覆盖率报告产物 | coverage-report / Summary 截图 |
| NuGet 依赖扫描产物 | nuget-vulnerable-scan Artifact |
| 测试代码 | CloudWarehouse.Tests、CloudWarehouse.IntegrationTests |
| 数据库环境跳过策略 | DatabaseAvailability 等相关逻辑 |
| 性能冒烟测试 | Import1000RowPerfTests、FeeCalculationPerfSmokeTests |

## 8.11 本章小结
本章论证了策略模式、双轨结算等核心设计变更，处于可重复执行的 CI 流水线与分层测试体系的保护之下，并通过 CodeQL 静态扫描与依赖漏洞扫描补齐了基础安全可见性；同时明确披露了动态安全测试、完整持续部署、身份认证、传输加密等能力仍为缺口或规划状态。下一章将围绕风险管理、中期评审反馈逐条回应，以及项目结论与展望展开，收束全书核心内容。
# 第九章 风险管理
## Risk Management

第八章阐述了质量与安全能力如何通过自动化流水线形成刚性约束；本章从**项目风险、技术风险、安全风险**三类维度，系统说明风险识别结果、已采取的缓解措施、仍处于规划阶段的事项，以及缓解措施有效性的验证证据。本项目的风险治理与四周Sprint迭代节奏深度绑定：每个Sprint启动前回顾风险登记册，将风险缓解动作纳入当周Must级任务，而非事后补写形式化的风险表格。

风险治理流程图示文件为

**图 9-1** 项目/技术/安全三类风险。

风险治理流程图示文件为`docs/diagrams/12-risk-management.puml`；答辩单页口述要点见`docs/project-management/risk-management-slide.md`。

## 9.1 风险管理方法
本项目采用轻量化、可落地的风险管理流程，所有环节均基于单人开发的实际场景设计，不套用重型团队管理框架：

| 管理步骤 | 本项目具体做法 |
|----------|----------------|
| 风险识别 | 从Excel导入失败、CI环境差异、演示环境暴露面等真实开发事件中抽象风险项，拒绝凭空臆造 |
| 风险评估 | 采用定性风险矩阵：以发生可能性 × 影响程度划分风险等级，详见9.5节 |
| 风险缓解 | MVP范围内可关闭的风险立即落地（如预览校验、事务回滚、上传白名单、CI流水线）；需产品决策的事项写入ADR与规划项 |
| 风险跟踪 | 与项目里程碑、个人工时偏差联动分析（如R1风险直接对应Sprint 2的+39%工时超支） |

本项目为**单人Solo**实习项目，风险登记与工时统计均为个人维度。针对导师要求中“新加入开发者从某Sprint起单独统计工时”一项，**本项目无第二名开发者加入，该项记为N/A**，不虚构团队产能与多人协作流程。

## 9.2 项目风险（Project）
项目类风险聚焦进度、范围与环境一致性三类核心问题，具体风险项与缓解措施如下：

| ID | 风险描述 | 潜在影响 | 已实施缓解措施 | 后续/Phase 2规划 |
|----|----------|----------|----------------|------------------|
| R1 | Sprint 2因遗留三级表头等Excel格式复杂度超预期，实际工时超支39% | 挤压后续Sprint的功能开发时间，导致里程碑延期 | 严格执行预览后提交的导入流程；Sprint 2后复盘重估同类任务工时；外部文件处理类任务统一预留缓冲时间 | 后续同类外部系统集成任务继续保留工时缓冲 |
| R2 | Solo开发模式下范围蔓延，如手工规则CRUD、提前实现认证、过度绘制图表等非核心需求 | 核心功能质量下降，关键里程碑延期 | 采用MoSCoW优先级方法管控需求；通过ADR锁定核心范围（如仅通过Excel维护规则、认证功能延期） | 持续执行ADR决策与待办清单纪律，严格控制范围膨胀 |
| R3 | 本地SQL Server环境与CI云端环境存在差异 | 出现“本地运行通过、流水线报错/虚假通过”的环境不一致问题 | 数据库结构全部通过`database/*.sql`脚本版本化管理；GitHub Actions强制执行`dotnet test`；数据库不可达时相关用例采用可解释跳过策略 | 持续保持schema脚本与测试用例同步更新 |

**R1风险缓解效果验证**：工时超支仅集中在Sprint 2，Sprint 3与Sprint 4工时偏差回落至±10%以内（详见第四章工时统计表），证明复盘后的纠偏措施有效。

## 9.3 技术风险（Technical）
技术类风险聚焦数据准确性、完整性与系统可用性，所有已关闭风险均有代码或脚本作为支撑证据：

| ID | 风险描述 | 潜在影响 | 已实施缓解措施 | 后续计划 |
|----|----------|----------|----------------|----------|
| T1 | 遗留三级表头错列导致解析错误，进而引发计价错误 | 报价与结算结果不准确，产生业务损失 | `ExcelHelper`实现表头行自动探测逻辑；提供标准模板下载；单元测试覆盖双格式解析场景 | 极端乱表场景可规划列映射配置UI，本期暂不实现 |
| T2 | 导入部分成功导致同一条运输车道下新旧规则混杂 | 数据完整性被破坏，计费结果混乱 | 导入全程包裹`SqlTransaction`事务，校验或写入失败则整批回滚（对应ADR-4决策） | 当前业务规模下该机制已满足需求 |
| T3 | 超大体积Excel文件导致内存溢出或请求超时 | 系统不可用，用户体验差 | 实现上传扩展名白名单 + 文件体积上限限制（如约10MB） | **规划中**：流式读取、分块处理、后台作业、断点续传——**本期未实现**，不得表述为已落地能力 |
| T4 | `(SiteId, DestId, EffectiveDate)`错误唯一索引阻断一对多档位规则入库 | 导入操作失败 | 通过`database/fix-price-rules-index.sql`脚本删除错误唯一索引，索引设计与一对多业务基数对齐 | 后续库表变更全部通过脚本执行，禁止手动直接修改数据库 |

T1与R1风险同源：外部文件的技术复杂度直接转化为进度风险。T4是索引设计与业务基数不匹配的典型工程教训，已在第五章数据库关键设计决策中交叉引用。

## 9.4 安全风险（Security）
安全风险基于MVP演示场景的实际暴露面评估，兼顾当前落地能力与远期规划，不做虚假的生产级安全承诺：

| ID | 风险描述 | 潜在影响 | 当前MVP缓解措施 | 远期规划 |
|----|----------|----------|----------------|----------|
| S1 | API与UI无认证授权机制 | 若系统暴露至公网，业务数据可被任意篡改 | 演示场景默认运行于本机或受控内网环境；ADR明确认证功能延期实现 | 实现JWT + RBAC权限体系，详见下文方案对比 |
| S2 | 开发阶段CORS配置为`AllowAll` | 跨源部署时攻击面扩大 | 文档明确标注该配置仅用于开发环境 | 生产环境收紧为明确的Origin白名单 |
| S3 | 数据库连接串等密钥泄露、恶意文件上传 | 凭证泄露、系统被入侵 | 通过`.gitignore`排除敏感配置，提供`appsettings.example.json`脱敏模板；上传文件设置白名单与大小限制；CI集成CodeQL与依赖漏洞扫描 | 接入User Secrets / 密钥托管服务；生产部署前强制启用HTTPS |

### S1 认证方案对比（回应导师“提供两种可选方案”的要求）
针对身份认证能力，设计两套落地方案，适配不同的业务集成场景：

| 评估维度 | 方案A：对接既有WMS/企业SSO | 方案B：独立JWT + RBAC |
|----------|-------------------------------|-------------------------|
| 集成成本 | 高，依赖外部身份提供商与联调窗口期 | 中等，用户与角色体系在本系统内维护 |
| 账号管理方式 | 统一由企业侧集中管理 | 本系统独立维护 |
| 演示独立性 | 依赖企业测试环境，无法独立运行 | 可脱离外部系统独立演示 |
| 适配建议 | 若云仓系统必须嵌入既有WMS生态再优先采用 | **默认推荐为Phase 2落地方案**：自主可控，与当前模块化单体架构匹配度更高 |

## 9.5 风险矩阵（缓解前定性评估）
基于发生可能性与影响程度，对所有风险项进行缓解前的定性分级：

|  | 低影响 | 中影响 | 高影响 |
|--|--------|--------|--------|
| **高可能性** |  |  | T1 遗留表头解析错误 |
| **中可能性** | S2 CORS配置宽松 | R1 进度超支；T3 大文件性能问题 | S1 无认证（外网暴露时影响升级为高） |
| **低可能性** | S3 密钥泄露（已有脱敏习惯时） | R2 范围蔓延；T2 部分导入失败（有事务后概率进一步降低） |  |

**矩阵解读**：T1是开发阶段最真实的高概率风险，已实际体现为Sprint 2的工时超支；S1在本机演示场景下发生概率可控，但**任何对外部署前必须优先关闭该风险**。

## 9.6 缓解有效性证据
所有风险缓解措施均有对应的工程产物可验证，避免空泛表述：

| 风险ID | 验证证据建议 |
|--------|--------------|
| T1 | `ExcelHelperTests`单元测试用例；导入成功/失败界面截图；标准模板文件 |
| T2 | 导入失败后UI端“未入库”提示截图；导入时序图08 |
| T3 | 上传非法扩展名文件被拒绝的截图；文件大小超限错误提示 |
| T4 | `fix-price-rules-index.sql`脚本文件；索引修复后导入流程成功运行记录 |
| R1 | 第四章工时统计表 + `sprint-hours-chart.html`工时柱状图 |
| R3 | GitHub Actions构建成功绿勾；`DatabaseAvailability`跳过策略说明 |
| S1–S3 | 架构决策记录ADR；第八章安全控制与缺口清单；不得展示真实数据库连接串等敏感信息 |

## 9.7 与PDA并列交付相关的风险（简述）
PDA无订单报工作为并列交付的独立系统，带来三类专项风险，对应缓解措施如下：

| 风险描述 | 缓解措施 |
|----------|----------|
| 双系统开发争夺单人开发带宽，导致核心功能质量下降 | 通过MoSCoW优先级管控 + 云仓/PDA分栏统计工时，避免工作量混淆不清 |
| 夸大表述，声称“已与云仓结算链路打通” | 企业Context Map中明确标注集成为Planned状态；严格遵守答辩禁语规范 |
| 硬件联调不确定性高，导致交付延期 | 对硬件联调任务预留工时缓冲；以可演示的开工/报工闭环作为完成标准 |

## 9.8 本章小结
本章表明风险管理并非报告附录的形式化装饰：R1进度风险有工时数据证明已被有效遏制，T1/T2/T4技术风险已通过代码与脚本完成闭环，S1安全风险则通过方案对比诚实承认缺口并给出落地路径。下一章将把中期导师评审意见逐条映射到已落地的设计、架构、证据与本章风险动作中，形成终稿核心的整改回应答卷。

---

# 第十章 中期反馈逐条回应
## Response to Mid-term Supervisory Feedback

中期评审明确要求：终稿与最终汇报必须全面回应全部评审意见，并附可验证的支撑证据。评审意见原文及英文摘要留存于仓库根目录 `log` 文件。本章按优先级将每条评审意见映射到已执行的改进动作，以及对应报告章节与产物路径，避免仅在概述中笼统提及而无实质支撑。

## 10.1 反馈来源与回应原则
本章所有回应遵循四项基本原则，确保内容真实可追溯，不做夸大表述：

| 原则 | 具体执行做法 |
|------|--------------|
| 有证据才标注已完成 | 所有已落地事项均指向对应的图表、测试、CI记录、UI截图等具体产物路径 |
| 未实现不虚假包装 | DAST动态扫描、完整持续部署、JWT认证、异形件/罚款策略等能力保持Planned状态 |
| 单人工时单独统计 | 第四章单独列示个人计划工时与实际工时对比；无第二开发者则标注为N/A |
| 严格遵守禁语规范 | 不得出现：微服务已上线、AI智能计费、云仓与PDA结算API已打通、生产级高可用已建成等表述 |

中期评审英文窗口要点与中文回应保持一致：Phase 2阶段深化计费复杂度并引入设计模式；补充多视角架构图；物理拓扑明确标注基础设施与冗余能力（无则如实说明）；论证单体架构的合理性；DDD理念支撑模块化单体设计；所有交付项均有对应产物。

## 10.2 总映射表（一页概览）
所有中期意见的回应落点可通过下表快速查阅：

| 中期评审意见 | 本项目回应摘要 | 主要落点章节 |
|----------|--------------------|----------|
| 整体实现偏简单，需要提升设计深度 | Phase 2新增策略模式、运单双轨历史价、规则检索、多类架构图、CI/SAST安全扫描 | 第6、7、8、10章 |
| 单体式架构需要充分说明合理性 | 模块化单体为有意选型，给出微服务拆分触发条件 | 第6章 |
| 计费变体需引入设计模式，配套类图与交互图 | 已实现区间/续重/体积重三类计费策略；配套类图13、双轨时序图14 | 第7章；ADR-8 |
| 补充多视角架构图 | 已提供逻辑、物理、部署、DDD、企业Context Map、CI活动图等多类视图 | 第6、8章；`docs/diagrams/*` |
| 物理架构图需明确基础设施与冗余能力 | 标注节点、端口、单实例部署；明确说明无高可用配置 | 第6.5–6.6节 |
| DDD理念需讲透 | 限界上下文与代码`Modules/*`目录一一映射；诚实说明非完整领域事件框架 | 第6.3节；图05、图16 |
| 所有工作需附可验证证据 | CI流水线、CodeQL扫描、覆盖率产物、测试用例、功能截图全覆盖 | 第8、9章；附录 |
| 个人计划工时与实际工时需拆分 | Phase 1合计198h→211h；Sprint 2偏差+39% | 第4章 |
| 风险缓解措施需具体落地 | 预览校验、事务回滚、上传白名单、索引修复已落地；大文件流式处理为规划项；认证给出双方案 | 第9章 |
| 未来规划需量化 | 明确各里程碑状态；Phase 2策略模式等工作包已完成 | 第4、10.6节 |

## 10.3 最高优先级意见 — 逐条展开
### 10.3.1 「系统偏简单」与计费变体需引入设计模式
**意见核心**：系统整体实现偏简单；第二阶段需重点深化计费规则变体；如有灵活空间应使用设计模式，并配套展示类图、交互图。

**逐条回应**：
1. 已落地策略模式（Strategy Pattern）：实现`TierBillingStrategy`、`OverweightBillingStrategy`、`VolumetricBillingStrategy`三类计费策略，由`FeeCalculationEngine`计费引擎与策略解析器统一编排调度。
2. 配套详细设计产物：输出`docs/diagrams/13-billing-strategy-class.puml`策略类图、`14-sequence-waybill-dual-track.puml`运单双轨时序图。
3. 深化业务能力：实现运单双轨结算（应收客户报价 vs 应付成本）+ 按发货日/账单日匹配历史价格，显著提升业务深度。
4. 诚实说明边界：异形件、超时罚款等扩展计费能力仍为**Planned**状态，通过扩展路径证明开闭原则，而非虚假宣称已全部实现。

**支撑证据**：第七章全文；`BillingStrategyTests`单元测试；运单预览一致/不一致UI截图（见附录）。

### 10.3.2 所有工作必须附可验证证据
**意见核心**：不能仅口头说明完成了测试、导入、CI等工作；需提供截图、覆盖率、流水线成功记录等实证。

**逐条回应**：
1. 持续集成：提供`.github/workflows/ci.yml`工作流文件 + GitHub Actions构建成功绿勾截图；覆盖率随Artifact归档，不写死大于80%等绝对化表述。
2. 静态安全扫描：提供`.github/workflows/codeql.yml`工作流文件与扫描成功记录。
3. 测试体系：覆盖单元测试、集成测试、轻量并发/性能冒烟测试三类用例。
4. 功能验证：价表导入成功/失败、运单双轨对比、PDA开工报工等功能截图全部纳入附录。

**支撑证据**：第八章证据清单；附录索引。

### 10.3.3 工时必须拆分个人计划值与实际值
**意见核心**：单独列出个人预估工时与实际工时；若有新加入开发者需单独统计。

**逐条回应**：
1. 单人开发工时表：Sprint 1–48h→52h；Sprint 2 44h→61h；Sprint 3 56h→51h；Sprint 4 50h→47h；Phase 1合计198h→211h，整体偏差+7%。
2. 新增开发者说明：本项目无第二名开发者加入，该项记为N/A。
3. Phase 2工时：第四章已预留云仓与PDA分栏工时表，定稿前需填入真实数值。

**支撑证据**：第四章；`sprint-hours-chart-data.csv` / 工时柱状图截图。

## 10.4 高优先级意见 — 逐条展开
### 10.4.1 架构图全面升级（多视角）
针对“补充多视角架构图”的要求，已输出全套可追溯的PlantUML源文件，覆盖架构全维度：

| 架构视角 | 对应文件 |
|------|------|
| 约束与架构决策（ADR） | `01*.puml` |
| 逻辑架构 | `02-logical-architecture.puml` |
| 物理架构 | `03-physical-architecture.puml` |
| 部署视图 | `04-deployment-diagram.puml` |
| DDD限界上下文 | `05-ddd-bounded-contexts.puml` |
| 用例视图 | `06-use-case-diagram.puml` |
| 实体关系图（ERD） | `07-erd.puml` |
| 导入/双轨时序 | `08`、`14` |
| CI流水线活动图 | `09-cicd-pipeline.puml` |
| 企业Context Map | `16-enterprise-context-map.puml` |
| 策略模式类图 | `13-billing-strategy-class.puml` |

其中逻辑架构图重点体现分层与依赖关系；物理与部署图给出节点、端口等基础设施量级信息。

### 10.4.2 物理架构：基础设施与冗余能力
**回应**：明确写清演示环境拓扑（Kestrel服务、SQL Server 1433端口、CI Runner、PDA并列节点）；数据备份采用手工备份与脚本重建方式；**明确说明无负载均衡、无数据库高可用配置**。通过诚实披露现状满足“有冗余就写、没有就写没有”的评审要求，而非虚构集群能力。

### 10.4.3 单体架构合理性 + 向微服务演进路径
**回应**：第六章通过对比微服务、大泥球单体、模块化单体三类选型，论证模块化单体为当前约束下的最优解；给出明确的微服务拆分触发条件，包括团队规模变化、独立扩缩容需求、发布节奏冲突、技术异构需求、量化QPS压力等。里程碑M8「按触发条件提取微服务」保持Planned状态，候选拆分上下文包括Import、Pricing、Master Data，满足触发条件后再实施拆分，不为拆分而拆分。

### 10.4.4 DDD理念必须讲透
**回应**：
1. 明确划分Master Data、Import、Pricing、Billing、Assistant五大限界上下文，以及PDA独立上下文。
2. 上下文与代码结构一一映射：对应`Modules/*`目录下的模块划分。
3. 诚实说明交互方式：同进程同步调用 + 同库事务，并非已上线的事件总线架构。
4. 明确能力边界：本项目为DDD理念指导的模块化设计，并非完整复刻领域事件、聚合根等重型DDD框架。

## 10.5 中优先级意见 — 逐条展开
### 10.5.1 风险缓解措施要具体
针对导师重点点名的三类风险，本报告均给出了已落地措施与规划路径的明确区分：

| 导师点名风险项 | 本报告落点 |
|------------|------------|
| 大文件上传风险 | 已落地扩展名白名单+文件大小限制；流式读取、分块处理、断点续传为**规划项**（第九章T3） |
| 三级表头解析风险 | 已落地自动探测算法+标准模板+单元测试覆盖（第九章T1）；与Sprint 2工时超支联动分析 |
| 无登录认证风险 | 给出JWT/RBAC与WMS SSO两套方案对比（第九章S1） |

### 10.5.2 未来规划要量化
各规划工作包的状态与说明如下，所有已完成项均对应明确的迭代周期：

| 工作包 | 状态 | 说明 |
|--------|------|------|
| 策略模式 + 体积重计费 | Done | 约Sprint 5完成；详见第七、四章 |
| 规则知识库检索 | Done | 辅助查阅功能，不作为结算真相源 |
| 运单双轨 + 历史价格 | Done | 对应时序图14 |
| 性能基线（1000行解析等） | 部分Done | 冒烟测试已完成，具体数值截图补充至附录 |
| JWT/RBAC认证体系 | Planned | 预估工时见规划表，负责人为项目作者（单人开发） |
| Import模块微服务调研 | Planned | 依赖稳定的模块边界与拆分触发条件 |
| 完整持续部署（CD） | Planned | 当前为CI + 发布包/部署检查清单模式 |

> Phase 2个人工时定稿前需填入第四章预留表格，与本规划表保持一致。

### 10.5.3 加分项落实情况
| 导师建议加分项 | 落实状态 |
|------|------|
| 计费复杂度分析 + 模式扩展方式 | 第七、十章变体表 + 开闭原则扩展三步法 |
| 性能基线：1000行解析等 | `Import1000RowPerfTests`等测试用例；具体数值贴入附录 |
| 设计决策对比（Dapper vs EF、单体 vs 微服务） | 第二章技术选型 + 第六章架构选型对比 |

## 10.6 额外交付：并列PDA与价值边界
中期评审核心聚焦于云仓系统的深度优化；Phase 2阶段额外并列交付了**霍尼韦尔PDA无订单报工**模块，从工厂现场数据采集维度补充了“工厂数字化”的业务叙事，但严格遵守以下边界：
- 与CloudWarehouse系统**未实现生产级结算API打通**；
- 企业Context Map中明确标注跨系统集成为Planned状态；
- PDA的工时统计与证据材料与云仓系统分列，避免用PDA交付掩盖云仓设计深度不足，也避免用云仓话术夸大PDA集成程度。

## 10.7 仍待附录补齐的截图清单（作者执行）

定稿前建议逐项核对：

- [ ] GitHub Actions CI 绿勾
- [ ] 覆盖率 Summary
- [ ] CodeQL 成功
- [ ] 价表导入成功 / 失败
- [ ] 运单双轨预览
- [ ] 工时柱状图
- [ ] PDA 开工/报工
- [ ]（可选）非法扩展名被拒

## 10.8 本章小结
中期评审意见已从“提醒建议”转化为可核对的工程动作与文档产出：计费能力通过策略模式与双轨结算实现深度提升，架构设计通过多视角视图与诚实的无高可用声明实现清晰透明，质量体系通过CI、SAST、分层测试形成可验证证据，项目管理通过个人工时追踪与具体风险措施形成管理闭环。下一章将给出项目结论、已知限制与未来展望，并简要说明客户反馈与提交清单，结束正文内容。

# 第十一章 结论与展望

### 11.1 结论

本实习在 Solo 条件下交付了两套并列系统：CloudWarehouse（Modular Monolith 运费结算 MVP，含 Phase 2 的 Strategy 计费、应收/应付双轨与历史价、**内置规则 RAG** 辅助查阅）与霍尼韦尔 PDA 无订单报工 MVP。二者服务同一工厂目标，但按限界上下文独立演进，**本期未做生产级 API 打通**。

中期反馈已通过可验证产物回应：多视角架构图与诚实无 HA 声明、Strategy 类图与双轨时序、CI/CodeQL/测试证据、个人 Planned vs Actual（Phase 1：198→211 小时）。规则 RAG 仅作 FAQ 检索增强，**不替代** FeeCalculationEngine。

### 11.2 已知限制

- 无 JWT/RBAC；CORS/HTTP 为演示配置
- 无生产级 HA / 完整 CD / DAST 常态门禁
- 体积重引擎已通，运单 Excel 主路径仍以实重为主
- 异形件/罚款等计费变体仍为 Planned
- 规则 RAG 为词法检索（非生产级向量语义 RAG）；未配置 ApiKey 时为摘录生成

### 11.3 展望（量化方向）

| 项 | 状态 | 依赖 |
|----|------|------|
| JWT + RBAC | Planned | ADR |
| 演示环境 DAST 基线 | Planned | 稳定演示部署 |
| 完整 CD | Planned | 认证与发布目标环境 |
| 微服务提取 | Planned | 触发条件（见第六章） |
| 云仓↔PDA 集成 | Planned | 稳定 ID/文件交换约定 |

### 11.4 Client Feedback

#### 11.4.1 企业导师反馈（业务价值）

从企业侧看，这次实习同时对准了工厂里两块真实痛点：仓库运费核对，以及没有正式工单时的产线报工。

云仓这条线，把结算从“对着 Excel 猜”推进到可重复路径：主数据、成本/报价导入、试算，以及按发货日的应收应付双轨预览。对业务来说，值钱的不是口号，而是预览结果能核对、对不上的也能解释，而不是黑盒。做成模块化单体也符合约束：一个人、时间紧，先要能跑的系统，而不是一上来拆微服务。

产线这条线，PDA 无订单报工对准夜班常见情况——活在干，MES 却没有工单。工人可以在工业手持机上登录、选机、开工、报工。现场反馈明显偏正面：大家更愿意扫码落库，而不是纸笔或口头，因为事后能追查。近一周报工量也侧面说明：无订单路径（含 PDA 双写）在 mesdb 报工里占绝对主流，有工单号的正式路径很少有人用。也就是说，现场真正在用的，正是这个项目加强的那条路。

总评：实习生以 Solo 身份把需求、设计、实现、测试/CI 证据和缺口说明（如登录鉴权、完整 CD、两套系统以后再对接）串下来了。承诺为 MVP 的部分可以演示；延期的部分是有意排期，不是没管。本期接受该交付，云仓与 PDA 先并列演进，等标识与对接时机成熟再谈打通。

#### 11.4.2 Sponsor 正式验收状态（Formal Acceptance）

| 问题 | 答复 |
| --- | --- |
| 是否已有 **书面正式 sign-off**（签字邮件/验收单）？ | **无。** 截至终稿提交日，企业方未出具正式盖章验收文件或邮件归档。 |
| 实际接受程度 | **演示接受 + 现场使用：** 企业导师在演示与车间走访中确认 MVP 可演示、PDA 路径在现场被操作员使用；云仓双轨预览用于核对场景获口头认可。 |
| 是否等于全厂生产 go-live？ | **否。** 云仓仍为受控演示/内网部署；JWT、HA、云仓↔PDA API 集成均为 Planned。 |
| 学术评分用表述 | Sponsor **接受本期实习交付物**（报告+演示+可运行 MVP），**不等于**企业级生产系统正式上线验收。 |

证据建议：附录保留演示会议记录/微信反馈截图（如有）；无正式邮件则如实写「oral acceptance only」。

### 11.5 提交物

- 本报告（中文定稿）
- 英文版
- 评估演示视频
- 附录证据截图（见附录 A）

# 附录 A 证据与截图清单

附录证据一览：

| 编号 | 内容 | 建议来源 |
|------|------|----------|
| A-01 | GitHub Actions CI 绿勾 | Actions 网页 |
| A-02 | coverage Summary | CI Artifact |
| A-03 | CodeQL 成功 | Actions |
| A-04 | 价表导入成功 | 管理端 UI |
| A-05 | 价表导入失败/未入库 | 管理端 UI |
| A-06 | 运单双轨预览一致/不一致 | 管理端 UI |
| A-07 | 工时柱状图 | sprint-hours-chart.html |
| A-08 | PDA 开工/报工 | 设备或模拟器 |
| A-09 | 规则 RAG 查询结果（含三步流水线） | 管理端「规则 RAG」Tab |
| A-10 | 非法扩展名上传被拒（可选） | UI |
| A-11 | 解决方案结构 / Modules 目录（可选） | IDE |
| A-12 | 测试通过汇总（114 passed，含 E2E） | GitHub Actions → CI → Test with coverage |
| A-13 | 负载冒烟 `[PERF]` / StressLoadTests | `perf-load-stress-detailed.txt` 或 QA 页 |
| A-14 | NuGet Moderate 扫描 + 处置说明 | QA 页 / CI Artifact |
| A-15 | 公开 QA 报告首页 | https://chenyuxiangAK47.github.io/cloudwarehouse-csharp/ |
| A-16 | Playwright E2E 四项通过 | `artifacts/e2e-playwright-test.txt` 或 CI 日志 |
| A-07b | 累计工时燃尽（Planned vs Actual 折线） | `sprint-burndown-cumulative.csv` |

# 第十二章 Reflection Questions（反思问题）

> **导师邮件要求：** 报告须包含独立章节 *Reflection Questions*。以下按实习全过程作答（Solo、双系统、中期反馈后整改）。

**Q1. 本实习中你最大的收获是什么？**  
从「能跑的功能」升级到「能证明的工程」：计费用 Strategy 与双轨时序落地设计模式；用 114 项自动化测试（含 Playwright 冒烟）、CI 覆盖率、CodeQL 与 QA 页把主张变成可点击证据。并学会在单人约束下用 Modular Monolith 而非过早微服务。

**Q2. 最大的困难是什么？如何克服？**  
Sprint 2 的 Excel 三级表头解析超支 39%——外部文件格式不可控。通过预览入库、标准模板、密集单测样例与复盘缓冲，把后续 Sprint 偏差压回 ±10%。Phase 2 并行 PDA 硬件联调则靠时间盒与明确「云仓/PDA 不强行 API 打通」控制范围。

**Q3. 中期导师反馈后，你改变了什么？**  
补全 Analysis（§3.0）、类级时序图、Sprint backlog 表、安全 before→resolution 叙事、Playwright E2E、公开 QA 站点；删除「未做 Playwright」等不准确表述；Client Feedback 区分演示接受与正式 sign-off。

**Q4. 一人团队如何实践 Sprint/敏捷？是否有效？**  
有效且可审计：已维护 **Jira 兼容 Product Backlog + Sprint Board + SP 燃尽**（`docs/project-management/jira/`，可导入 Jira Cloud），并以 GitHub Issue Template 绑定工程任务；Phase 1 一周一 Sprint，Phase 2 为 Sprint 5 里程碑迭代。复盘文档化（Sprint 2 retro）保留。

**Q5. 若重来，会如何安排？**  
更早锁定 Excel 黄金样例库；Phase 1 末即引入 Playwright 冒烟；安全扫描在依赖选型阶段就记录 baseline；报告母稿与 Word 同步频率提高，避免终期集中补图。

**Q6. AI 工具如何使用？哪些仍由本人负责？**  
AI 用于 PlantUML 草稿、测试脚手架、文档润色与 CI 脚本；**计费规则、双轨语义、工时数据、验收边界、答辩禁语**由本人核对源码与业务方反馈后定稿。见 `docs/project-management/ai-assistance-disclosure.md`。

**Q7. 对赞助企业与客户关系的反思？**  
企业价值在「可核对的双轨预览」与「PDA 无订单落库」，不在技术堆砌。诚实说明无正式 sign-off、无生产 go-live，反而有利于下一阶段谈集成范围。

**Q8. 下一步个人成长方向？**  
深化 .NET 性能与 SQL 调优；完成 JWT/RBAC 与 DAST 基线；IaC（Bicep/Terraform/Compose）已落地，下一步把演示环境稳定跑在 `terraform apply` / `az deployment` 上。

# 附录 B 术语与禁话速查

| 正确 | 禁止 |
|------|------|
| 双轨=应收报价 vs 应付成本 | 国内/国际线路 |
| Modular Monolith；无 HA | 微服务已上线；生产多活已建成 |
| Strategy Tier/Overweight/Volumetric Done | JSON 规则引擎；AI 智能计费 |
| 内置规则 RAG（词法检索 FAQ） | AI 结算 / 向量 RAG 已生产上线 |
| PDA 未与云仓结算 API 打通 | 已打通 / Parallel Data Aggregator |
| 覆盖率以 Artifact 为准 | 正文写死 >80% |
