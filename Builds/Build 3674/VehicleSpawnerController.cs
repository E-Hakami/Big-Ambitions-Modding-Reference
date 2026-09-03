using System;
using System.Collections.Generic;
using BigAmbitions.PlacementSystem;
using BigAmbitions.SaveSystem;
using BigAmbitions.Tags;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using PlayerActivity;
using UI.Notification;
using UnityEngine;
using UnityEngine.AI;
using Vehicles.VehicleTypes;

public class VehicleSpawnerController : ItemController
{
	[Header("VehicleSpawner")]
	public string vehicleType;

	[SerializeField]
	public bool isStacked;

	[SerializeField]
	[HideIf("isStacked")]
	private GameObject vehicleVisuals;

	[SerializeField]
	[HideIf("isStacked")]
	private BoxCollider withVehicleCollider;

	[SerializeField]
	[HideIf("isStacked")]
	private BoxCollider withoutVehicleCollider;

	[SerializeField]
	[HideIf("isStacked")]
	private NavMeshObstacle withVehicleObstacle;

	[SerializeField]
	[HideIf("isStacked")]
	private NavMeshObstacle withoutVehicleObstacle;

	[SerializeField]
	public float cost;

	private bool _vehicleAvailability;

	public float Cost => cost;

	private VehicleType VehicleType => VehicleTypeHelper.GetVehicleType(vehicleType);

	public override void Start()
	{
		base.Start();
		if (VehicleType.HasTag(TagRef.Vehicletag.isscooter))
		{
			GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, (Action<Address>)delegate
			{
				SetVehicleAvailability(state: true);
			});
			UpdateNavMeshTargets();
		}
	}

	private void OnEnable()
	{
		SetVehicleAvailability(state: true);
	}

	public override void Show()
	{
		base.Show();
		vehicleVisuals.SetActive(_vehicleAvailability);
	}

	public override void Hide()
	{
		base.Hide();
		vehicleVisuals.SetActive(value: false);
	}

	public override bool ShouldReactToIoEnter()
	{
		return !GameManager.IsAnyMiniGameActive();
	}

	public override bool Interact()
	{
		if (PlacementSystem.IsInPlacementMode || PlayerActivityUI.IsPanelOpen || GameManager.IsAnyMiniGameActive())
		{
			return true;
		}
		if (SaveGameManager.Current.ActiveVehicleId != null)
		{
			VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
			if (currentVehicle == null || currentVehicle.VehicleType.HasTag(TagRef.Vehicletag.isscooter) || currentVehicle.vehicleTypeName != vehicleType || currentVehicle.cargoInstances.Count > 0)
			{
				return false;
			}
			VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
			selectedVehicle.ExitVehicle();
			selectedVehicle.vehicleInstance.Delete(selectedVehicle);
			SetVehicleAvailability(state: true);
			return true;
		}
		bool isHoldingBag = PlayerHelper.IsHoldingBag;
		if (VehicleType.HasTag(TagRef.Vehicletag.isscooter))
		{
			if (!isHoldingBag && PlayerHelper.ItemInstanceInHands != null)
			{
				Notifications.Show(NotificationType.Warning, "escooter_cant_carry_item");
				return true;
			}
		}
		else if (PlayerHelper.IsHoldingShoppingBasket || PlayerHelper.IsHoldingAMop)
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string>
			{
				{ "fromname", "vehicletypename_handtruck" },
				{
					"toname",
					PlayerHelper.ItemInstanceInHands.itemName
				}
			};
			Notifications.Show(NotificationType.Warning, "notification_cant_use_while_using_item", notificationData);
			return true;
		}
		if (SaveGameManager.Current.ActiveVehicleId == null)
		{
			if (cost != 0f)
			{
				LanguageChangeEventDataHolder bodyData = "vehiclespawner_rent_confirm".Localize(new
				{
					price = cost.ToShortCurrencyFormat()
				});
				HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
				{
					TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_electricscooterfee");
					if (GameManager.ChangeMoneySafe(0f - cost, transactionInfo, null, null, force: false, showNotification: true))
					{
						SpawnVehicle();
					}
				});
			}
			else
			{
				SpawnVehicle();
			}
			return true;
		}
		return base.Interact();
	}

	private void SpawnVehicle()
	{
		VehicleController vehicle = CreateVehicle(base.transform, vehicleType);
		SetVehicleAvailability(state: false);
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			vehicle.EnterVehicle();
		});
	}

	private void SetVehicleAvailability(bool state)
	{
		_vehicleAvailability = state;
		if (vehicleVisuals != null)
		{
			vehicleVisuals.SetActive(state);
		}
		if (withVehicleCollider != null)
		{
			withVehicleCollider.enabled = state;
		}
		if (withoutVehicleCollider != null)
		{
			withoutVehicleCollider.enabled = !state;
		}
		if (withVehicleObstacle != null)
		{
			withVehicleObstacle.enabled = state;
		}
		if (withoutVehicleObstacle != null)
		{
			withoutVehicleObstacle.enabled = !state;
		}
	}

	public static VehicleController CreateVehicle(Transform transform, string vehicleType)
	{
		VehicleController vehicleController = PrefabHelper.CreatePrefab<VehicleController>("Vehicles/PlayerVehicles/" + vehicleType.GetIdWithoutType(), InstanceBehavior<GameManager>.Instance.itemsContainer);
		Rigidbody component = vehicleController.GetComponent<Rigidbody>();
		if ((bool)component)
		{
			component.Move(transform.position, transform.rotation);
		}
		else
		{
			Transform obj = vehicleController.transform;
			obj.position = transform.position;
			obj.rotation = transform.rotation;
		}
		VehicleInstance vehicleInstance = new VehicleInstance(vehicleType)
		{
			position = transform.position,
			rotation = transform.rotation
		};
		SaveGameManager.Current.VehicleInstances.Add(vehicleInstance);
		vehicleController.SetVehicleInstance(vehicleInstance);
		return vehicleController;
	}
}
