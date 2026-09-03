using BigAmbitions.Items;
using Localizor;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.Warehouse;

public class WarehouseProductModel
{
	public string ProductName;

	public int Stock;

	public int Deliveries;

	public int Consumption;

	public int Balance;

	public int CargoAmount;

	public int DaysUntilEmpty;

	public WarehouseProductModel(string product, int stock, int deliveries, int consumption)
	{
		ProductName = product.GetLocalization();
		Stock = stock;
		Deliveries = deliveries;
		Consumption = consumption;
		Balance = Deliveries - Consumption;
		CargoAmount = Mathf.CeilToInt((float)stock / (float)ItemsGetter.GetByName(product).boxSize);
		float num = (float)Consumption / 7f;
		if (stock == 0)
		{
			DaysUntilEmpty = -1;
		}
		else if (num == 0f)
		{
			DaysUntilEmpty = int.MaxValue;
		}
		else
		{
			DaysUntilEmpty = Mathf.FloorToInt((float)Stock / num);
		}
	}
}
