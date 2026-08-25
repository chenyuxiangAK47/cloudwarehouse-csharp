# 最终报告 · 第六章大纲（中文）— 系统架构

> 对应骨架：`interim-report-writing-guide.md` §9 Logical / §10 Physical / §11 相关 + DDD / Context Map  
> 扩写后约 **8–12 页**  
> 正文见：`final-report-ch6-architecture-zh.md`

## 硬约束（防幻觉）

1. 架构风格 = **Modular Monolith**（刻意选择，非“不会微服务”）
2. 同进程模块：`MasterData` / `Import` / `Pricing` / `Billing` / `Assistant`；图若仍写旧 Controllers 根目录，正文以 `Modules/` 为准
3. PDA = **独立系统 + 独立库**；企业 Context Map 用 Partnership / Shared Kernel / **Customer-Supplier Planned** 等诚实关系；**禁止**写已生产级 API 打通
4. 物理环境：本地/演示拓扑；**诚实写无 HA**、无负载均衡、认证延期（ADR）
5. 微服务拆分 = **触发条件驱动** 的 Planned，不是本期交付
6. 图：`02` 逻辑、`03` 物理、`04` 部署、`05` DDD、`16` 企业 Context Map；Strategy 类图留给 Software Design 章

## 小节结构

- 6.1 架构风格与决策动机
- 6.2 逻辑架构与请求流（导入为例）
- 6.3 限界上下文与代码映射（含 Billing / Assistant）
- 6.4 企业级 Context Map（含 PDA）
- 6.5 物理部署与运行拓扑
- 6.6 HA / 备份诚实声明与演进
- 6.7 微服务提取触发条件
- 6.8 Evidence + 小结
