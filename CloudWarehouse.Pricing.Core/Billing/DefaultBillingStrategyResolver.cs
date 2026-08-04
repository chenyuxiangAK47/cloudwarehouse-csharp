namespace CloudWarehouse.Backend.Helpers.Billing;

/// <summary>按注册顺序选择第一个 CanHandle 为 true 的策略。</summary>
public sealed class DefaultBillingStrategyResolver : IBillingStrategyResolver
{
    private readonly IReadOnlyList<IBillingStrategy> _strategies;

    public DefaultBillingStrategyResolver(IEnumerable<IBillingStrategy> strategies)
    {
        _strategies = strategies.ToList();
        if (_strategies.Count == 0)
            throw new ArgumentException("At least one IBillingStrategy must be registered.", nameof(strategies));
    }

    /// <summary>体积重优先，再区间/续重——演示 Open/Closed 扩展注册顺序。</summary>
    public static DefaultBillingStrategyResolver CreateDefault() =>
        new([
            new VolumetricBillingStrategy(),
            new TierBillingStrategy(),
            new OverweightBillingStrategy()
        ]);

    public IBillingStrategy? Resolve(BillingContext context) =>
        _strategies.FirstOrDefault(s => s.CanHandle(context));

    public IReadOnlyList<string> RegisteredStrategyNames =>
        _strategies.Select(s => s.StrategyName).ToList();
}
