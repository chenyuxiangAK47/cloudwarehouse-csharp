namespace CloudWarehouse.Backend.Helpers.Billing;

/// <summary>解析当前计费上下文应使用的策略（便于 DI / 单测替换）。</summary>
public interface IBillingStrategyResolver
{
    IBillingStrategy? Resolve(BillingContext context);
    IReadOnlyList<string> RegisteredStrategyNames { get; }
}
