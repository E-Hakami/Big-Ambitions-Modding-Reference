using BaTable;
using Extensions;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.Warehouse;

public class WarehouseProductCellView : BaTableCellView<WarehouseProductModel>
{
	public TextMeshProUGUI productNameLabel;

	public TextMeshProUGUI stockLabel;

	public TextMeshProUGUI deliveriesLabel;

	public TextMeshProUGUI consumptionLabel;

	public TextMeshProUGUI balanceLabel;

	public TextMeshProUGUI cargoAmount;

	public TextLocalizationComponent daysUntilEmptyLabel;

	public override void SetData(WarehouseProductModel data)
	{
		productNameLabel.text = data.ProductName;
		stockLabel.text = data.Stock.ToFormattedNumber();
		deliveriesLabel.text = data.Deliveries.ToFormattedNumber();
		consumptionLabel.text = data.Consumption.ToFormattedNumber();
		cargoAmount.text = ((data.CargoAmount == 0) ? string.Empty : data.CargoAmount.ToString());
		balanceLabel.text = data.Balance.ToFormattedNumber();
		Color32 color = ((data.Balance >= 0) ? InstanceBehavior<GlobalReferences>.Instance.colors.darkGreen : InstanceBehavior<GlobalReferences>.Instance.colors.red);
		balanceLabel.color = color;
		if (data.DaysUntilEmpty == -1)
		{
			daysUntilEmptyLabel.SetData("bizman_inventory_run_out".Localize());
		}
		else if (data.DaysUntilEmpty == int.MaxValue)
		{
			daysUntilEmptyLabel.SetData("bizman_inventory_never_runs_out".Localize());
		}
		else
		{
			daysUntilEmptyLabel.SetData("bizman_inventory_product_days_until_empty".Localize(new
			{
				days = data.DaysUntilEmpty
			}));
		}
	}
}
