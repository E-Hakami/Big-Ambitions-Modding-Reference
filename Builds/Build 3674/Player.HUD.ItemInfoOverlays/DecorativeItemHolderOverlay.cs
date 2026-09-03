using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Extensions;
using Helpers;
using Localizor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Player.HUD.ItemInfoOverlays;

public class DecorativeItemHolderOverlay : IOverlay
{
	[Header("Fridge InfoOverlay")]
	[SerializeField]
	private Transform consumeButtonTemplate;

	[SerializeField]
	private Button emptyFridgeButton;

	[SerializeField]
	private Button changeClothesButton;

	[SerializeField]
	private Button addItemButton;

	public override bool IsValid(EntityController entityController)
	{
		if (!(entityController is FridgeController))
		{
			return entityController is DecorativeItemHolderController;
		}
		return true;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		return InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		ItemController itemController = entityController as ItemController;
		VehicleInstance vehicle = VehicleHelper.GetCurrentVehicle();
		FridgeController fridgeController = entityController as FridgeController;
		if ((object)fridgeController != null)
		{
			bool flag = itemController.ItemInstance.cargoInstances.Count > 0;
			emptyFridgeButton.gameObject.SetActive(flag);
			if (flag)
			{
				interactablePriority.Add(emptyFridgeButton.transform);
			}
			changeClothesButton.gameObject.SetActive(value: false);
			bool canAddItemFromVehicle = vehicle != null && !vehicle.VehicleType.IsMotorVehicle && vehicle.cargoInstances.Count > 0 && fridgeController.CanStoreAny(vehicle.cargoInstances);
			bool canAddItemFromHands = PlayerHelper.IsHoldingItem && fridgeController.CanStoreAny(PlayerHelper.ItemInstanceInHands);
			addItemButton.gameObject.SetActive(canAddItemFromHands | canAddItemFromVehicle);
			addItemButton.onClick.RemoveAllListeners();
			addItemButton.onClick.AddListener(delegate
			{
				InstanceBehavior<OverlayManager>.Instance?.HideDetailedOverlay();
				if (canAddItemFromVehicle)
				{
					fridgeController.WalkOverAndAddToStorage(vehicle);
				}
				else if (canAddItemFromHands)
				{
					fridgeController.WalkOverAndAddToStorage(PlayerHelper.ItemInstanceInHands);
				}
			});
			SetActionButtons(fridgeController.ItemInstance.cargoInstances, fridgeController.ConsumeItem);
			return;
		}
		DecorativeItemHolderController decorativeItemHolderController = entityController as DecorativeItemHolderController;
		if ((object)decorativeItemHolderController == null)
		{
			return;
		}
		bool flag2 = itemController.ItemInstance.cargoInstances.Count > 0;
		emptyFridgeButton.gameObject.SetActive(flag2);
		if (flag2)
		{
			interactablePriority.Add(emptyFridgeButton.transform);
		}
		changeClothesButton.gameObject.SetActive(decorativeItemHolderController.isWardrobe);
		bool canAddItemFromVehicle2 = vehicle != null && !vehicle.VehicleType.IsMotorVehicle && vehicle.cargoInstances.Count > 0 && decorativeItemHolderController.CanStoreAny(vehicle.cargoInstances);
		bool canAddItemFromHands2 = PlayerHelper.IsHoldingItem && decorativeItemHolderController.CanStoreAny(PlayerHelper.ItemInstanceInHands);
		addItemButton.gameObject.SetActive(canAddItemFromHands2 | canAddItemFromVehicle2);
		addItemButton.onClick.RemoveAllListeners();
		addItemButton.onClick.AddListener(delegate
		{
			InstanceBehavior<OverlayManager>.Instance?.HideDetailedOverlay();
			if (canAddItemFromVehicle2)
			{
				decorativeItemHolderController.WalkOverAndAddToStorage(vehicle);
			}
			else if (canAddItemFromHands2)
			{
				decorativeItemHolderController.WalkOverAndAddToStorage(PlayerHelper.ItemInstanceInHands);
			}
		});
		SetActionButtons(decorativeItemHolderController.ItemInstance.cargoInstances, decorativeItemHolderController.UseItem);
	}

	private void SetActionButtons(List<CargoInstance> cargoInstances, Action<CargoInstance> onClick)
	{
		consumeButtonTemplate.ResetTemplate();
		interactablePriority.Clear();
		List<CargoItem> list = new List<CargoItem>();
		foreach (CargoInstance cargoInstance in cargoInstances)
		{
			CargoItem cargoItem = list.FirstOrDefault((CargoItem x) => x.itemName == cargoInstance.itemName && x.paid == cargoInstance.paid);
			if (cargoItem != null)
			{
				cargoItem.AddCargoInstance(cargoInstance);
			}
			else
			{
				list.Add(new CargoItem(cargoInstance.itemName, cargoInstance.amount, cargoInstance, cargoInstance.paid));
			}
		}
		foreach (CargoItem item in list)
		{
			CargoInstance firstCargoInstance = item.cargoInstances.First();
			int num = item.cargoInstances.Sum((CargoInstance x) => x.amount);
			string arg = ((firstCargoInstance.ItemCached.saturation > 0) ? firstCargoInstance.ItemCached.GetConsumeActionLocalizationKey().GetLocalization() : "action_take".GetLocalization());
			string localization = firstCargoInstance.itemName.GetLocalization();
			string text = $"{arg} {localization} ({num})";
			Transform transform = UnityEngine.Object.Instantiate(consumeButtonTemplate, consumeButtonTemplate.parent);
			transform.name = firstCargoInstance.itemName;
			transform.GetComponentInChildren<TMP_Text>().text = text;
			Button component = transform.GetComponent<Button>();
			component.onClick.RemoveAllListeners();
			component.onClick.AddListener(delegate
			{
				onClick(firstCargoInstance);
			});
			transform.gameObject.SetActive(value: true);
			interactablePriority.Add(transform);
		}
	}

	public void OnEmptyFridgeClicked()
	{
		if (linkedController is FridgeController fridgeController)
		{
			fridgeController.EmptyFridge();
		}
		else if (linkedController is DecorativeItemHolderController decorativeItemHolderController)
		{
			decorativeItemHolderController.Empty();
		}
		InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
	}

	public void OnChangeClothesClicked()
	{
		if (linkedController is DecorativeItemHolderController decorativeItemHolderController)
		{
			decorativeItemHolderController.ChangeClothes();
		}
		InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
	}
}
