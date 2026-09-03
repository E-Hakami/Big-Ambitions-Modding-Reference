using System.Collections.Generic;
using System.Linq;
using Buildings;
using Helpers;
using Services;

namespace UI.Dialog;

public class FurnitureDeliveryContractSettings : DeliveryContractSettingsBase
{
	public const float deliveryFee = 250f;

	private const int deliveryDays = 3;

	private const int ContractMaxAmount = 100;

	private readonly int[] deliveryHours = new int[2] { 10, 14 };

	private BuildingRegistration _storeRegistration;

	public override float DeliveryFee => 250f;

	protected override int MaxContractAmount => 100;

	protected override List<BuildingRegistration> GetDeliveryDestinations()
	{
		List<BuildingRegistration> list = base.GetDeliveryDestinations();
		Building building = BuildingHelper.GetBuilding(DialogController.current.contact.Address);
		if (building != null && building.SpecialService != null && (bool)building.SpecialService.settings)
		{
			FurnitureStoreSettings settings = (FurnitureStoreSettings)building.SpecialService.settings;
			if (settings.allowedDeliveryBuildingTypes.Count > 0)
			{
				list = list.Where((BuildingRegistration x) => settings.allowedDeliveryBuildingTypes.Contains(x.GetBuildingType())).ToList();
			}
		}
		return list;
	}

	protected override List<(int day, int hour)> GenerateDeliverySlots()
	{
		List<(int, int)> list = new List<(int, int)>();
		if (TimeHelper.CurrentHour < 12)
		{
			if (TimeHelper.CurrentHour < 8)
			{
				list.Add((TimeHelper.CurrentDay, deliveryHours[0]));
			}
			list.Add((TimeHelper.CurrentDay, deliveryHours[1]));
		}
		for (int i = 1; i < 3; i++)
		{
			int[] array = deliveryHours;
			foreach (int item in array)
			{
				list.Add((TimeHelper.CurrentDay + i, item));
			}
		}
		return list;
	}

	protected override (List<string> itemsForSale, float priceMultiplier) GetItemsForSale()
	{
		_storeRegistration = BuildingHelper.GetBuildingRegistration(DialogController.current.contact.Address);
		if (ContractItemsForSaleService.TryGetItemsForContact(DialogController.current.contact.id, out var itemNames))
		{
			return (itemsForSale: itemNames, priceMultiplier: 1f);
		}
		if (_storeRegistration != null)
		{
			return (itemsForSale: _storeRegistration.GetListOfItemsForSale(), priceMultiplier: (float)_storeRegistration.GetPriceIndex() / 100f);
		}
		return (itemsForSale: new List<string>(), priceMultiplier: 1f);
	}

	protected override void UpdateItemsListTitle()
	{
		if (_storeRegistration != null)
		{
			SetItemsListTitle(_storeRegistration.BusinessName);
		}
	}
}
