using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings;
using Extensions;
using Helpers;
using Localizor;
using TMPro;
using UnityEngine;

namespace Player.HUD.ItemInfoOverlays;

public class StockOverlay : IOverlay
{
	[Header("Stock")]
	[SerializeField]
	private TMP_Text contentField;

	[SerializeField]
	private TMP_Text amountField;

	public override bool IsValid(EntityController entityController)
	{
		if (entityController is VehicleController vehicleController)
		{
			return vehicleController.vehicleType.maxCargoCapacity > 0;
		}
		if (!(entityController is ItemController itemController))
		{
			return false;
		}
		if (itemController.Item.itemsThatCanShowcase.Length == 0)
		{
			if (itemController is ShelfController shelfController)
			{
				return !(shelfController is DeliverySpot);
			}
			return false;
		}
		return true;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		if (entityController is VehicleController)
		{
			return true;
		}
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness || !BuildingTypeHelper.GetData(InstanceBehavior<BuildingManager>.Instance.building).HasTag(TagRef.Buildingtypetag.sellsphysicalproducts))
		{
			return false;
		}
		if (!(entityController is ItemController itemController))
		{
			return true;
		}
		if (itemController.Item.HasTag(TagRef.Itemtag.iscashregister))
		{
			if (InstanceBehavior<BuildingManager>.Instance.businessType != null)
			{
				return InstanceBehavior<BuildingManager>.Instance.businessType.HasTag(TagRef.Businesstag.customersneedpaperbags);
			}
			return false;
		}
		return true;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		SetContent(entityController);
		SetAmount(entityController);
	}

	private void SetContent(EntityController entityController)
	{
		bool flag = !(entityController is StorageShelfController) && !(entityController is PalletController) && !(entityController is DeliverySpot) && !(entityController is VehicleController);
		contentField.gameObject.SetActive(flag);
		if (flag)
		{
			string itemName = (entityController as ItemController).ItemInstance.GetStockInstance().itemName;
			contentField.text = string.Format("{0}: {1}", "itemoverlay_contents_headline".Localize(), LocalizationHelper.GetItemLabel(itemName));
		}
	}

	private void SetAmount(EntityController entityController)
	{
		if (entityController is VehicleController vehicleController)
		{
			amountField.text = "common_storage_capacity_status".Localize(new
			{
				amount = $"{vehicleController.vehicleInstance.cargoInstances.Count}/{vehicleController.vehicleType.maxCargoCapacity}"
			}).ToString();
			amountField.gameObject.SetActive(value: true);
		}
		else
		{
			if (!(entityController is ItemController itemController))
			{
				return;
			}
			bool flag = entityController is StorageShelfController || entityController is PalletController || entityController is DeliverySpot;
			bool flag2 = flag || !string.IsNullOrEmpty(itemController.ItemInstance.GetStockInstance().itemName);
			amountField.gameObject.SetActive(flag2);
			if (flag2)
			{
				if (flag)
				{
					amountField.text = "common_storage_capacity_status".Localize(new
					{
						amount = $"{itemController.ItemInstance.cargoInstances.Count}/{itemController.Item.cargoCapacity}"
					}).ToString();
					return;
				}
				CargoInstance stockInstance = itemController.ItemInstance.GetStockInstance();
				amountField.text = ColoredTextHelper.GetAmountText(stockInstance.amount, stockInstance.GetMaxStockCapacity(itemController.ItemInstance));
			}
		}
	}
}
