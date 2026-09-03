using BigAmbitions.Items;
using Controllers;
using Items.SpecialItems;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace Player.HUD.ItemInfoOverlays;

public class ButtonOverlay : IOverlay
{
	[Header("Cash Register")]
	[SerializeField]
	private Button workButton;

	[SerializeField]
	private Button orderButton;

	[SerializeField]
	private TextLocalizationComponent orderText;

	[SerializeField]
	private Button acceptFoodDeliveryButton;

	[Header("Drinks fridge")]
	[SerializeField]
	private Button takeDrinkButton;

	public override bool IsValid(EntityController entityController)
	{
		if ((entityController is ShowcaseShelfController { itemName: var itemName } && (itemName == "ba:itemname_drinksfridge" || itemName == "ba:itemname_largedrinksfridge")) ? true : false)
		{
			return true;
		}
		return entityController is CashRegisterController;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		if (entityController is CashRegisterController cashRegisterController)
		{
			if (!cashRegisterController.CanWork() && !cashRegisterController.CanOrder())
			{
				return cashRegisterController.CanAcceptFoodDelivery();
			}
			return true;
		}
		if (!(entityController is ShowcaseShelfController showcaseShelfController) || InstanceBehavior<BuildingManager>.Instance.businessType.businessTypeName != "ba:businesstype_gym")
		{
			return false;
		}
		return !string.IsNullOrEmpty(showcaseShelfController.CurrentStockDropdownOption);
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		HideAllButtons();
		CashRegisterController cashRegisterController = entityController as CashRegisterController;
		if ((object)cashRegisterController != null)
		{
			bool active = cashRegisterController.CanWork();
			bool flag = cashRegisterController.CanOrder();
			bool active2 = cashRegisterController.CanAcceptFoodDelivery();
			workButton.gameObject.SetActive(active);
			orderButton.gameObject.SetActive(flag);
			acceptFoodDeliveryButton.gameObject.SetActive(active2);
			workButton.onClick.RemoveAllListeners();
			orderButton.onClick.RemoveAllListeners();
			acceptFoodDeliveryButton.onClick.RemoveAllListeners();
			workButton.onClick.AddListener(delegate
			{
				cashRegisterController.Work();
				InstanceBehavior<OverlayManager>.Instance.HideOverlays();
			});
			orderButton.onClick.AddListener(delegate
			{
				cashRegisterController.Order();
				InstanceBehavior<OverlayManager>.Instance.HideOverlays();
			});
			acceptFoodDeliveryButton.onClick.AddListener(delegate
			{
				cashRegisterController.AcceptFoodDelivery();
				InstanceBehavior<OverlayManager>.Instance.HideOverlays();
			});
			if (flag)
			{
				CustomerType customerType = InstanceBehavior<BuildingManager>.Instance.businessType.customerType;
				TextLocalizationComponent textLocalizationComponent = orderText;
				string key = ((customerType != CustomerType.SelfService && customerType != CustomerType.CinemaTheater) ? "bizman_purchasingagents_order" : "common_purchase");
				textLocalizationComponent.Key = key;
			}
			return;
		}
		ShowcaseShelfController showcaseShelfController = entityController as ShowcaseShelfController;
		if ((object)showcaseShelfController == null)
		{
			return;
		}
		takeDrinkButton.gameObject.SetActive(value: true);
		CargoInstance stockInstance = showcaseShelfController.ItemInstance.GetStockInstance();
		bool flag2 = stockInstance != null && stockInstance.amount > 0;
		takeDrinkButton.interactable = flag2;
		if (flag2)
		{
			takeDrinkButton.onClick.RemoveAllListeners();
			takeDrinkButton.onClick.AddListener(delegate
			{
				showcaseShelfController.WalkToTakeItem(stockInstance.itemName);
				InstanceBehavior<OverlayManager>.Instance.HideOverlays();
			});
		}
	}

	private void HideAllButtons()
	{
		orderButton.gameObject.SetActive(value: false);
		workButton.gameObject.SetActive(value: false);
		acceptFoodDeliveryButton.gameObject.SetActive(value: false);
		takeDrinkButton.gameObject.SetActive(value: false);
	}
}
