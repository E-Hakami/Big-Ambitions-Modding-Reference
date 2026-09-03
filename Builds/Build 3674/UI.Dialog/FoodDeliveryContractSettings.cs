using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Special.FoodDelivery;
using Localizor;

namespace UI.Dialog;

public class FoodDeliveryContractSettings : DeliveryContractSettingsBase
{
	private static FoodDeliverySettings Settings => InstanceBehavior<GlobalReferences>.Instance.foodDeliverySettings;

	public float MinimumOrderCost => Settings.MinimumOrderCost;

	public override float DeliveryFee => Settings.DeliveryFee;

	protected override int MaxContractAmount => Settings.MaxItemsPerOrder;

	protected override bool ShouldPreselectFirstDeliverySlot => true;

	protected override List<(int day, int hour)> GenerateDeliverySlots()
	{
		return TimeHelper.GetUpcomingHourSlots(Settings.MinutesUntilEarliestDelivery, Settings.DeliverySlotsToShow);
	}

	protected override (List<string> itemsForSale, float priceMultiplier) GetItemsForSale()
	{
		return (itemsForSale: new List<string>(ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.fooddelivery)), priceMultiplier: Settings.ItemPriceMultiplier);
	}

	protected override void UpdateItemsListTitle()
	{
		SetItemsListTitle("speedy_bites".GetLocalization());
	}
}
