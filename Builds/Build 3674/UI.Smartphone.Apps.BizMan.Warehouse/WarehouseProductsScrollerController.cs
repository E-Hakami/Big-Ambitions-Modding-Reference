using BaTable;
using EnhancedUI.EnhancedScroller;
using Entities;
using Helpers;

namespace UI.Smartphone.Apps.BizMan.Warehouse;

public class WarehouseProductsScrollerController : BaTable<WarehouseProductCellView, WarehouseProductModel>
{
	public void LoadProducts(Entities.Warehouse warehouse)
	{
		data.Clear();
		foreach (string product in warehouse.GetProducts())
		{
			data.Add(new WarehouseProductModel(product, BuildingHelper.CountResourcesInPallets(warehouse.Address, product), warehouse.GetProductDeliveries(product), warehouse.GetProductConsumption(product)));
		}
		scroller.ReloadData();
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return 100f;
	}
}
