using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.Items;
using BigAmbitions.SaveSystem;
using BigAmbitions.Tags;
using Helpers;
using NaughtyAttributes;
using Player.HUD.ItemInfoOverlays;
using UI;
using UI.Notification;
using UnityEngine;

public class DecorativeItemHolderController : ItemController
{
	[Header("Decorative Item Holder")]
	[SerializeField]
	protected Transform itemVisualsParent;

	[SerializeField]
	private bool showVisualsAsPercentageFilled;

	public bool canSetColorToItemsHolding;

	[ShowIf("canSetColorToItemsHolding")]
	public CustomColorChannel colorChannelsThatApplyToItemsHolding;

	[ShowIf("canSetColorToItemsHolding")]
	public Renderer[] renderersToRecolor;

	public bool isWardrobe;

	[ShowIf("isWardrobe")]
	[SerializeField]
	protected AppearanceTag[] tags;

	public bool CanStoreAny(ItemInstance itemInstanceToStore)
	{
		if (itemInstanceToStore != null && itemInstanceToStore.cargoInstances.Count > 0)
		{
			return itemInstanceToStore.cargoInstances.Any((CargoInstance x) => base.ItemInstance.ItemCached.itemsWhitelist.Contains(x.ItemCached.itemName));
		}
		return false;
	}

	public bool CanStoreAny(List<CargoInstance> cargoInstancesToStore)
	{
		if (cargoInstancesToStore != null && cargoInstancesToStore.Count > 0)
		{
			return cargoInstancesToStore.Any((CargoInstance x) => base.ItemInstance.ItemCached.itemsWhitelist.Contains(x.ItemCached.itemName));
		}
		return false;
	}

	private bool CanStore(CargoInstance cargoInstanceToStore)
	{
		if (cargoInstanceToStore != null && base.ItemInstance.ItemCached.itemsWhitelist.Contains(cargoInstanceToStore.ItemCached.itemName))
		{
			if (base.ItemInstance.cargoInstances.Count >= base.ItemInstance.ItemCached.cargoCapacity)
			{
				return base.ItemInstance.cargoInstances.Any((CargoInstance x) => x.itemName == cargoInstanceToStore.itemName && x.amount < x.GetMaxStockCapacity(base.ItemInstance));
			}
			return true;
		}
		return false;
	}

	public override void Start()
	{
		base.Start();
		if (InstanceBehavior<GameManager>.Instance != null)
		{
			base.ItemInstance.AddCallToOnItemsInCargoUpdated(OnItemsInCargoUpdated);
			OnItemsInCargoUpdated();
		}
		else
		{
			FillVisualsOnly();
		}
	}

	public override void Hide()
	{
		base.Hide();
		if (!(itemVisualsParent == null))
		{
			itemVisualsParent.gameObject.SetActive(value: false);
		}
	}

	public override void Show()
	{
		base.Show();
		if (!(itemVisualsParent == null))
		{
			itemVisualsParent.gameObject.SetActive(value: true);
		}
	}

	public override bool ShouldShowDetailedOverlay()
	{
		if (PlayerIsWearingItemThatCanBeStoredInItemHolder())
		{
			return false;
		}
		return base.ItemInstance.cargoInstances.Count > 0;
	}

	public void ChangeClothes()
	{
		MoveTowardsEntity(delegate
		{
			InstanceBehavior<UIs>.Instance.changeCharacterClothesUI.Show(tags);
		});
	}

	public void Empty()
	{
		MoveTowardsEntity(delegate
		{
			if (base.ItemInstance.cargoInstances.Count != 0)
			{
				if (!PlayerHelper.IsHoldingItem && !PlayerHelper.IsUsingVehicle)
				{
					ItemInstance itemInstance = ItemHelper.InitializeNewInstance(ItemsGetter.GetRandomBag());
					ICargoHolder.TryToMergeAndMoveCargoBetweenHolders(base.ItemInstance, itemInstance);
					PlayerHelper.ItemInstanceInHands = itemInstance;
				}
				else
				{
					ICargoHolder cargoHolder;
					if (!PlayerHelper.IsHoldingItem)
					{
						ICargoHolder currentVehicle = VehicleHelper.GetCurrentVehicle();
						cargoHolder = currentVehicle;
					}
					else
					{
						ICargoHolder currentVehicle = PlayerHelper.ItemInstanceInHands;
						cargoHolder = currentVehicle;
					}
					ICargoHolder cargoHolder2 = cargoHolder;
					if (ICargoHolder.TryToMergeAndMoveCargoBetweenHolders(base.ItemInstance, cargoHolder2))
					{
						cargoHolder2.OnItemsInCargoUpdated()?.Invoke();
					}
					else
					{
						Dictionary<string, string> notificationData = new Dictionary<string, string> { 
						{
							"itemname",
							base.ItemInstance.itemName
						} };
						Notifications.Show(NotificationType.Error, "playeritempurchaser_notification_hands_full", notificationData);
					}
				}
			}
		});
	}

	public override bool Interact()
	{
		PlayerItemPurchaserSettings obj = playerItemPurchaserSettings;
		if ((obj != null && obj.enabled) || !PlayerIsWearingItemThatCanBeStoredInItemHolder())
		{
			return base.Interact();
		}
		StorePlayerWornItemIntoItemHolder();
		return true;
	}

	public void UseItem(CargoInstance cargoInstance)
	{
		InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
		MoveTowardsEntity(delegate
		{
			if (cargoInstance.ItemCached.saturation > 0)
			{
				if (cargoInstance.ItemCached.TryToConsume())
				{
					base.ItemInstance.ReduceFromCargo(cargoInstance, 1);
				}
			}
			else if (!PlayerHelper.IsHoldingItem && !PlayerHelper.IsUsingVehicle)
			{
				ItemInstance itemInstance = ItemHelper.InitializeNewInstance(ItemsGetter.GetRandomBag());
				CargoInstance cargoInstance2 = new CargoInstance(cargoInstance.itemName, 1, cargoInstance.pricePerUnit, paid: true, GetCargoCustomColors(cargoInstance));
				base.ItemInstance.ReduceFromCargo(cargoInstance, 1);
				itemInstance.AddToCargo(cargoInstance2);
				PlayerHelper.ItemInstanceInHands = itemInstance;
			}
			else
			{
				ICargoHolder cargoHolder;
				if (!PlayerHelper.IsHoldingItem)
				{
					ICargoHolder currentVehicle = VehicleHelper.GetCurrentVehicle();
					cargoHolder = currentVehicle;
				}
				else
				{
					ICargoHolder currentVehicle = PlayerHelper.ItemInstanceInHands;
					cargoHolder = currentVehicle;
				}
				ICargoHolder cargoHolder2 = cargoHolder;
				if (cargoHolder2.GetCargoInstances().Any((CargoInstance x) => x.IsSealed))
				{
					Notifications.Show(NotificationType.Error, "notification_cant_tamper_sealed_box");
				}
				else
				{
					CargoInstance cargoInstance3 = new CargoInstance(cargoInstance.itemName, 1, cargoInstance.pricePerUnit, paid: true, GetCargoCustomColors(cargoInstance));
					cargoHolder2.MergeIntoCargo(cargoInstance3);
					if (cargoInstance3.amount > 0)
					{
						if (cargoHolder2.TryToAddToCargo(cargoInstance3))
						{
							base.ItemInstance.ReduceFromCargo(cargoInstance, 1);
						}
						else
						{
							Notifications.Show(NotificationType.Error, "playeritempurchaser_notification_hands_full");
						}
					}
					else
					{
						base.ItemInstance.ReduceFromCargo(cargoInstance, 1);
						cargoHolder2.OnItemsInCargoUpdated()?.Invoke();
					}
				}
			}
		});
	}

	private List<CustomColor> GetCargoCustomColors(CargoInstance cargoInstance)
	{
		if (canSetColorToItemsHolding && cargoInstance.ItemCached.HasTag(TagRef.Itemtag.isumbrella))
		{
			List<CustomColor> list = base.ItemInstance.customColors;
			if (list != null && list.Count > 0)
			{
				List<CustomColor> list2 = new List<CustomColor>();
				foreach (CustomColor customColor in base.ItemInstance.customColors)
				{
					if (colorChannelsThatApplyToItemsHolding.HasFlag(customColor.channel))
					{
						list2.Add(customColor.Copy());
					}
				}
				if (list2.Count <= 0)
				{
					return null;
				}
				return list2;
			}
		}
		return null;
	}

	public void TryAddToStorage()
	{
		VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
		if (currentVehicle != null && !currentVehicle.VehicleType.IsMotorVehicle)
		{
			WalkOverAndAddToStorage(currentVehicle);
		}
		else if (PlayerHelper.IsHoldingItem)
		{
			WalkOverAndAddToStorage(PlayerHelper.ItemInstanceInHands);
		}
	}

	public void WalkOverAndAddToStorage(ItemInstance itemInstance)
	{
		MoveTowardsEntity(delegate
		{
			AddToStorage(itemInstance);
		});
	}

	public void WalkOverAndAddToStorage(VehicleInstance vehicleInstance)
	{
		MoveTowardsEntity(delegate
		{
			AddToStorage(vehicleInstance);
		});
	}

	private void AddToStorage(ICargoHolder cargoHolder)
	{
		if (!ICargoHolder.TryToMergeAndMoveCargoBetweenHolders(cargoHolder, base.ItemInstance))
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { { "itemname", itemName } };
			Notifications.Show(NotificationType.Info, "fridge_notification_cant_fit_some_items", notificationData, 4f, "fridge_notification_cant_fit_some_items");
		}
		else
		{
			OnItemsInCargoUpdated();
		}
		InstanceBehavior<OverlayManager>.Instance?.HideDetailedOverlay();
		GameEvent.Invoke("ba:gameevent_itemstockedup");
	}

	public bool PlayerIsWearingItemThatCanBeStoredInItemHolder()
	{
		if (!PlayerHelper.PlayerController.CanUnEquipItem())
		{
			return false;
		}
		if (!CanStore(SaveGameManager.Current.accessoriesData.handAccessoryCargoInstance) && !CanStore(SaveGameManager.Current.accessoriesData.headAccessoryCargoInstance))
		{
			return CanStore(SaveGameManager.Current.accessoriesData.phoneAccessoryCargoInstance);
		}
		return true;
	}

	private void StorePlayerWornItemIntoItemHolder()
	{
		CargoInstance cargoInstance = (CanStore(SaveGameManager.Current.accessoriesData.headAccessoryCargoInstance) ? SaveGameManager.Current.accessoriesData.headAccessoryCargoInstance : (CanStore(SaveGameManager.Current.accessoriesData.handAccessoryCargoInstance) ? SaveGameManager.Current.accessoriesData.handAccessoryCargoInstance : SaveGameManager.Current.accessoriesData.phoneAccessoryCargoInstance));
		base.ItemInstance.AddToCargo(cargoInstance);
		InstanceBehavior<OverlayManager>.Instance?.HideDetailedOverlay();
		GameEvent.Invoke("ba:gameevent_itemstockedup");
		PlayerHelper.PlayerController.UnEquipAccessory(cargoInstance);
	}

	protected virtual void OnItemsInCargoUpdated()
	{
		if (InstanceBehavior<OverlayManager>.Instance.IsShowingOverlayOverItem(this))
		{
			InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
		}
		if (itemVisualsParent == null)
		{
			return;
		}
		if (!showVisualsAsPercentageFilled)
		{
			foreach (Transform item in itemVisualsParent)
			{
				bool active = false;
				foreach (CargoInstance cargoInstance in base.ItemInstance.cargoInstances)
				{
					if (cargoInstance.ItemCached.itemName.GetIdWithoutType().Equals(item.name, StringComparison.InvariantCultureIgnoreCase))
					{
						active = true;
						break;
					}
				}
				item.gameObject.SetActive(active);
			}
			return;
		}
		Item byName = ItemsGetter.GetByName(itemName);
		string id = byName.itemsWhitelist[0];
		float num = Mathf.FloorToInt((float)ItemsGetter.GetByName(id).boxSize * byName.cargoCapacityMultiplier * (float)byName.cargoCapacity);
		Transform transform2 = null;
		foreach (Transform item2 in itemVisualsParent)
		{
			if (item2.name.Equals(id.GetIdWithoutType(), StringComparison.CurrentCultureIgnoreCase))
			{
				transform2 = item2;
				break;
			}
		}
		if (!(transform2 == null))
		{
			float num2 = (float)base.ItemInstance.cargoInstances.Sum((CargoInstance x) => x.amount) / num;
			for (int num3 = 0; num3 < transform2.childCount; num3++)
			{
				float num4 = (float)num3 / (float)transform2.childCount;
				transform2.GetChild(num3).gameObject.SetActive(num4 < num2);
			}
		}
	}

	public virtual void FillVisualsOnly()
	{
		if (itemVisualsParent == null)
		{
			return;
		}
		if (!showVisualsAsPercentageFilled)
		{
			foreach (Transform item in itemVisualsParent)
			{
				item.gameObject.SetActive(value: true);
			}
			return;
		}
		int childCount = itemVisualsParent.childCount;
		for (int i = 0; i < childCount; i++)
		{
			itemVisualsParent.GetChild(i).gameObject.SetActive(value: true);
		}
	}

	public override Renderer[] GetRenderersToSetColor(CustomColorChannel customColorChannel)
	{
		if (canSetColorToItemsHolding && colorChannelsThatApplyToItemsHolding.HasFlag(customColorChannel))
		{
			return renderersToRecolor;
		}
		return base.GetRenderersToSetColor(customColorChannel);
	}

	public override void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			base.OnDestroy();
			base.ItemInstance.RemoveCallFromOnItemsInCargoUpdated(OnItemsInCargoUpdated);
		}
	}

	[HideIf("showVisualsAsPercentageFilled")]
	[Button(null, EButtonEnableMode.Always)]
	private void AddWhitelistItemsToItemVisuals()
	{
		if (itemVisualsParent == null)
		{
			Debug.LogError(base.gameObject.name + " has no item visuals parent set up", base.gameObject);
			return;
		}
		Item byName = ItemsGetter.GetByName(itemName);
		string[] itemsWhitelist = byName.itemsWhitelist;
		if (itemsWhitelist == null || itemsWhitelist.Length <= 0)
		{
			Debug.LogError(base.gameObject.name + " has no items set up in the whitelist", base.gameObject);
			return;
		}
		itemsWhitelist = byName.itemsWhitelist;
		foreach (string text in itemsWhitelist)
		{
			if (!string.IsNullOrEmpty(text) && !(itemVisualsParent.Find(text)?.gameObject != null))
			{
				GameObject obj = new GameObject(text);
				obj.transform.SetParent(itemVisualsParent);
				obj.name = text;
			}
		}
	}
}
