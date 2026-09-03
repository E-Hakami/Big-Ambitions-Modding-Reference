using System.Collections.Generic;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.Items;
using Culling;
using HGAttributes;
using Helpers;
using Localizor;
using Player.HUD.ItemInfoOverlays;
using UI.Notification;
using UnityEngine;

public class SellerStandController : EntityController, ICullable
{
	[AutocompleteDropdown("Items")]
	public string[] itemsToSell;

	[SerializeField]
	private Transform sellerPosition;

	[SerializeField]
	private EmployeePreset uniformPreset;

	private BaseHuman _seller;

	public override void Start()
	{
		base.Start();
		if ((bool)sellerPosition)
		{
			_seller = PrefabHelper.CreatePrefab<BaseHuman>("Characters/DummyHuman", base.transform);
			_seller.gameObject.SetActive(value: true);
			_seller.appearanceSetter.SetRandomAppearance();
			if (uniformPreset != null)
			{
				List<AppearanceElementData> elements = ((_seller.appearanceSetter.data.gender == Gender.Female) ? uniformPreset.femaleElements : uniformPreset.maleElements);
				_seller.appearanceSetter.UpdateElements(elements);
			}
			_seller.transform.SetPositionAndRotation(sellerPosition.position, sellerPosition.rotation);
			InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.RegisterCullable(this);
		}
	}

	public override void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			base.OnDestroy();
			InstanceBehavior<CullingManager>.Instance?.generalCullingGroupController.UnregisterCullable(this);
		}
	}

	public void OnLod0()
	{
		_seller.gameObject.SetActive(value: true);
	}

	public void OnLod2()
	{
		_seller.gameObject.SetActive(value: false);
	}

	public void OnLod1()
	{
		_seller.gameObject.SetActive(value: false);
	}

	public BoundingSphere GetCullingBoundingSphere()
	{
		return new BoundingSphere(base.transform.position + Vector3.up * 2f, 4f);
	}

	public void WalkToBuyItem(string itemToBuy)
	{
		InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
		if (VehicleHelper.IsInsideVehicle())
		{
			Notifications.ShowError("notification_must_exit_vehicle_before_action");
			return;
		}
		MoveTowardsEntity(delegate
		{
			BuyItem(itemToBuy);
		});
	}

	private void BuyItem(string itemToBuy)
	{
		if (PlayerHelper.IsHoldingItem)
		{
			Notifications.ShowError("notification_need_empty_hands_to_interact");
			return;
		}
		float defaultMarketPrice = ItemHelper.GetDefaultMarketPrice(itemToBuy);
		Dictionary<string, string> data = new Dictionary<string, string> { 
		{
			"itemName",
			itemToBuy.GetLocalization()
		} };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_purchasefromsellerstand", data);
		if (GameManager.ChangeMoneySafe(0f - defaultMarketPrice, transactionInfo, null, null, force: false, showNotification: true))
		{
			HandTruck componentInChildren = InstanceBehavior<GameManager>.Instance.playerController.GetComponentInChildren<HandTruck>();
			if ((bool)componentInChildren)
			{
				componentInChildren.ExitVehicle();
			}
			ItemInstance itemInstance = new ItemInstance(ItemsGetter.GetRandomBag());
			CargoInstance cargoInstance = new CargoInstance(itemToBuy, 1, defaultMarketPrice);
			itemInstance.AddToCargo(cargoInstance);
			PlayerHelper.ItemInstanceInHands = itemInstance;
			GameEvent.Invoke("ba:gameevent_purchasecompleted");
		}
	}
}
