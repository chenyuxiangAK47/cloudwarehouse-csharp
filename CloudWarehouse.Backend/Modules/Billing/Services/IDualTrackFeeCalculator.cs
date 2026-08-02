using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Services;

/// <summary>运单双轨计价：应付（成本）+ 应收（客户报价）+ 对比。</summary>
public interface IDualTrackFeeCalculator
{
    Task CalculateAsync(WaybillImportRow row);
}
