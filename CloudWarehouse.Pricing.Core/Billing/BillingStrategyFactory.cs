namespace CloudWarehouse.Backend.Helpers.Billing;

/// <summary>
/// 静态兼容入口；内部委托 <see cref="DefaultBillingStrategyResolver"/>。
/// 生产代码优先通过 DI 注入 <see cref="IBillingStrategyResolver"/>。
/// </summary>
public static class BillingStrategyFactory
{
    private static readonly IBillingStrategyResolver DefaultResolver =
        DefaultBillingStrategyResolver.CreateDefault();

    public static IBillingStrategy? Resolve(BillingContext context) =>
        DefaultResolver.Resolve(context);

    public static IReadOnlyList<string> RegisteredStrategyNames =>
        DefaultResolver.RegisteredStrategyNames;
}
