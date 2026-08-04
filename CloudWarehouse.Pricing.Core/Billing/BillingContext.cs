using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Helpers.Billing;

/// <summary>计费上下文：策略算法所需的重量、可选体积尺寸与有效规则集。</summary>
public sealed class BillingContext
{
    /// <summary>实际称重（kg）。</summary>
    public required decimal Weight { get; init; }

    public required IReadOnlyList<PriceRule> ActiveRules { get; init; }

    /// <summary>长/宽/高（cm）。三者齐全时才参与体积重。</summary>
    public decimal? LengthCm { get; init; }
    public decimal? WidthCm { get; init; }
    public decimal? HeightCm { get; init; }

    /// <summary>体积重除数，默认 6000（cm³/kg，常见快递惯例）。</summary>
    public decimal VolumetricDivisor { get; init; } = 6000m;

    public bool HasVolumetricDimensions =>
        LengthCm is > 0 && WidthCm is > 0 && HeightCm is > 0 && VolumetricDivisor > 0;

    /// <summary>体积重 kg = L×W×H / divisor。</summary>
    public decimal? VolumetricWeightKg
    {
        get
        {
            if (!HasVolumetricDimensions)
                return null;
            return Math.Round(
                LengthCm!.Value * WidthCm!.Value * HeightCm!.Value / VolumetricDivisor, 3);
        }
    }
}
