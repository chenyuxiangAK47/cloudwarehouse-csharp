using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Helpers.Billing;

/// <summary>计费上下文：策略算法所需的重量与有效规则集。</summary>
public sealed class BillingContext
{
    public required decimal Weight { get; init; }
    public required IReadOnlyList<PriceRule> ActiveRules { get; init; }
}
