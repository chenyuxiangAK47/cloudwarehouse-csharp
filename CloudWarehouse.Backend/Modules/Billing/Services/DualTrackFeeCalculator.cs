using CloudWarehouse.Backend.Helpers;
using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Services;

/// <summary>
/// 从 BillImportService 抽出的双轨计价协调器（Facade / Application Service）。
/// 便于单独单测与答辩时讲解「应收 / 应付」两条限界上下文协作。
/// </summary>
public sealed class DualTrackFeeCalculator : IDualTrackFeeCalculator
{
    private readonly PriceRuleCalculateService _costCalculateService;
    private readonly CustomerQuoteCalculateService _customerQuoteCalculateService;

    public DualTrackFeeCalculator(
        PriceRuleCalculateService costCalculateService,
        CustomerQuoteCalculateService customerQuoteCalculateService)
    {
        _costCalculateService = costCalculateService;
        _customerQuoteCalculateService = customerQuoteCalculateService;
    }

    public async Task CalculateAsync(WaybillImportRow row)
    {
        if (row.SiteId == null || row.DestId == null || row.RoundedWeight == null)
            return;

        var billDate = row.BillDate ?? DateTime.Today;
        var costRequest = new PriceCalculateRequest
        {
            SiteId = row.SiteId.Value,
            DestId = row.DestId.Value,
            Weight = row.RoundedWeight.Value,
            OrderDate = billDate
        };

        var payable = await _costCalculateService.CalculateAsync(costRequest);
        if (payable == null)
        {
            row.ErrorMessage = $"未找到成本价格规则（站点 {row.SiteCode} → {row.Province}），请先导入成本/价格表";
            return;
        }

        row.PayableTransitFee = payable.WeightFee;
        row.PayableLabelFee = payable.BaseFee;
        row.PayableTotal = Math.Round(payable.WeightFee + payable.BaseFee, 2);

        if (row.CustomerId == null)
        {
            if (!string.IsNullOrWhiteSpace(row.CustomerCode))
                row.ErrorMessage = $"客户编号「{row.CustomerCode}」未登记，无法查询客户报价（应收）";
            else
                row.ErrorMessage = string.IsNullOrWhiteSpace(row.AccountName)
                    ? "未填写客户编号或账户名称，无法匹配客户报价（应收）"
                    : $"账户「{row.AccountName}」未登记，无法查询客户报价（应收）";
            return;
        }

        var receivable = await _customerQuoteCalculateService.CalculateAsync(new CustomerQuoteCalculateRequest
        {
            CustomerId = row.CustomerId.Value,
            Province = row.Province,
            ExpressType = row.ExpressType ?? row.SiteName,
            Weight = row.RoundedWeight.Value,
            OrderDate = billDate
        });

        if (receivable == null)
        {
            row.ErrorMessage = $"未找到客户报价（客户 → {row.Province}），请先导入客户报价表";
            return;
        }

        row.BillingType = receivable.BillingType;
        row.ReceivableTransitFee = receivable.WeightFee;
        row.ReceivableLabelFee = receivable.BaseFee;
        row.ReceivableGrandTotal = Math.Round(BillLineTotals.CalcReceivableGrandTotal(row), 2);
        row.RemainingReceivable = BillLineTotals.CalcRemainingReceivable(row);
        row.ReceivableTotal = Math.Round(
            receivable.WeightFee + receivable.BaseFee + row.Surcharge + row.Penalty, 2);

        row.PayableGrandTotal = Math.Round(BillLineTotals.CalcPayableGrandTotal(row), 2);
        row.RemainingPayable = BillLineTotals.CalcRemainingPayable(row);

        row.Profit = Math.Round(row.ReceivableTotal.Value - row.PayableTotal.Value, 2);
        BillLineTotals.ApplyComparison(row);
    }
}
