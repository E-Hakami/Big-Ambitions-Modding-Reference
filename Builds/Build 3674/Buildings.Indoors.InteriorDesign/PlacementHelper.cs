using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InputSystem;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using BigAmbitions.SoundSystem;
using BigAmbitions.Tags;
using CameraControllers;
using Cinemachine;
using Extensions;
using Helpers;
using IngameDebugConsole;
using JimmysUnityUtilities;
using OdinSerializer.Utilities;
using UI;
using UI.InteriorDesigner;
using UI.Notification;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Buildings.Indoors.InteriorDesign;

public static class PlacementHelper
{
	private const float MaxCameraTransitionDuration = 0.3f;

	public static bool wasPausedBeforePlacementMode;

	public static bool wasCargoPanelOpenBeforePlacementMode;

	private static PedestrianCam IndoorCam;

	private static PedestrianCam OutdoorCam;

	private static PlacementCam PlacementCam;

	public static void Init(PlacementCam placementCam, PedestrianCam indoorCam, PedestrianCam outdoorCam = null)
	{
		PlacementCam = placementCam;
		IndoorCam = indoorCam;
		OutdoorCam = outdoorCam;
		PlacementSystem.onTriedToPlaceInBlockedArea = delegate
		{
			Notifications.ShowError("notification_cant_place_in_blocked_area", "placingInBlockedArea");
		};
		PlacementSystem.onItemPlaced = delegate
		{
			InstanceBehavior<SfxManager>.Instance.PlayAudio(PlacementSystem.CurrentPlaceableItemBeingPlaced.GetItemInstance().ItemCached.customPlaceSound ? PlacementSystem.CurrentPlaceableItemBeingPlaced.GetItemInstance().ItemCached.placeSoundType : SoundType.ObjectPutDown, PlacementSystem.CurrentPlaceableItemBeingPlaced.GetTransform().position);
		};
		PlacementSystem.onItemRotated = delegate
		{
			InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.ObjectRotate, PlacementSystem.CurrentPlaceableItemBeingPlaced.GetTransform().position);
		};
		GlobalEvents.RegisterOnGameLoadedCallback(delegate
		{
			PlacementSystem.mainCamera = GameManager.GetMainCamera();
		});
	}

	public static bool StartPlacementMode(this ItemController itemControllerToPlace, bool warpCursor = true, bool setCamera = true)
	{
		if (itemControllerToPlace == null)
		{
			return false;
		}
		if (ItemNotMovable(itemControllerToPlace))
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { { "itemname", itemControllerToPlace.itemName } };
			Notifications.Show(NotificationType.Warning, "item_location_cannot_be_changed_in_building_notification", notificationData);
			return false;
		}
		if (itemControllerToPlace.Occupied && !BuildingManager.isBuildingTemporarilyEditable)
		{
			Notifications.ShowError("pointandclickobject_notification_cant_pick_occupied_objects");
			return false;
		}
		GameManager.preventAutoSave = true;
		if (setCamera)
		{
			wasPausedBeforePlacementMode = InstanceBehavior<UIs>.Instance.gameSpeed.Paused;
			if (!wasPausedBeforePlacementMode)
			{
				InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: true);
			}
			wasCargoPanelOpenBeforePlacementMode = InstanceBehavior<UIs>.Instance.playerHUD.manageCargoUI.isPanelOpen;
			if (wasCargoPanelOpenBeforePlacementMode)
			{
				InstanceBehavior<UIs>.Instance.playerHUD.manageCargoUI.Close();
			}
		}
		if (warpCursor)
		{
			Vector3 position = itemControllerToPlace.transform.position;
			if (!itemControllerToPlace.ItemInstance.ItemCached.wallMounted)
			{
				position.y = 0f;
			}
			Vector3 vector = GameManager.GetMainCamera().WorldToScreenPoint(position);
			if (MathHelper.DistanceSqr(vector, InputActionHelper.GetCursorPosition()) >= (float)(Screen.width + Screen.height * Screen.width + Screen.height))
			{
				vector = new Vector3(Screen.width, Screen.height, 0f) * 0.5f;
			}
			Mouse.current.WarpCursorPosition(vector);
			InputState.Change(Mouse.current.position, vector);
		}
		MouseController.SetRestrictedObjectTypes(default(InteractiveObjectType));
		if (setCamera)
		{
			InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: true);
			PlacementCam.UpdateBounds();
			PlacementCam.distance = IndoorCam.distance;
			PlacementCam.transform.position = InstanceBehavior<GameManager>.Instance.playerController.transform.position;
			PlacementCam.ForceUpdateCameraPosition();
			PlacementCam.RotateCamera(IndoorCam.angle - PlacementCam.currentAngle);
			InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
			InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.PlacementMode);
			CameraHelper.SetCamera(InstanceBehavior<GameManager>.Instance.indoorPlacementCamera);
		}
		if (itemControllerToPlace.ItemInstance.ItemCached.HasTag(TagRef.Itemtag.issecuritycamera))
		{
			foreach (ItemController item in InstanceBehavior<BuildingManager>.Instance.allItemControllers.Where((ItemController x) => x.Item.HasTag(TagRef.Itemtag.issecuritycamera)))
			{
				if (item.ItemInstance != itemControllerToPlace.ItemInstance)
				{
					item.transform.Find("SecondaryCoverageRadiusIndicator").gameObject.SetActive(value: true);
				}
			}
		}
		bool flag = (itemControllerToPlace.ItemInstance.ItemCached.type & ItemType.FactoryMachine) != 0;
		foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			allItemController.ToggleAttachmentPoints(itemControllerToPlace.ItemInstance.ItemCached.stackType, show: true);
			if (flag && (allItemController.Item.type & ItemType.FactoryMachine) != 0)
			{
				allItemController.ToggleDirectionIndicators(show: true);
			}
		}
		ToggleItemsCollidersThatCanBeOverlapped(toggled: false, itemControllerToPlace.ItemInstance);
		InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.ObjectPickup, itemControllerToPlace.transform.position);
		PlacementSystem.StartPlacingItem(itemControllerToPlace);
		PlacementSystem.onPlacementModeStart?.Invoke();
		if (setCamera)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetItemInstance(itemControllerToPlace.ItemInstance, spawnIntoPlayerHands: false);
		}
		return true;
	}

	public static void Run()
	{
		if (!PlacementSystem.IsInPlacementMode || (DebugLogManager.Instance != null && DebugLogManager.Instance.IsLogWindowVisible) || GameManager.HasInputSelected() || EventSystem.current.IsPointerOverGameObject())
		{
			return;
		}
		PlacementSystem.UpdateItemPosition();
		if (!InteriorDesignerUI.IsOpen && PlacementSystem.PlaceInputReleased && PlacementSystem.CanPlaceItem())
		{
			PlayerAction.Interact.Reset();
			PlacementSystem.SaveCurrentItemBeingMovedPosition();
			CancelPlacementMode();
			if (PlayerHelper.IsHoldingItem)
			{
				InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetItemInstance(PlayerHelper.ItemInstanceInHands);
			}
		}
	}

	public static void CancelPlacementMode(bool setCamera = true)
	{
		ItemInstance itemInstance = PlacementSystem.CurrentPlaceableItemBeingPlaced.GetItemInstance();
		bool flag = (itemInstance.ItemCached.type & ItemType.FactoryMachine) != 0;
		foreach (ItemController item in InstanceBehavior<BuildingManager>.Instance.allItemControllers.Where((ItemController x) => x != null))
		{
			item.ToggleAttachmentPoints(AttachmentPointType.All, show: false);
			if (flag && (item.Item.type & ItemType.FactoryMachine) != 0)
			{
				item.ToggleDirectionIndicators(show: false);
			}
		}
		if (itemInstance.ItemCached.HasTag(TagRef.Itemtag.issecuritycamera))
		{
			foreach (ItemController item2 in InstanceBehavior<BuildingManager>.Instance.allItemControllers.Where((ItemController x) => x.Item.HasTag(TagRef.Itemtag.issecuritycamera)))
			{
				if (item2.GetItemInstance() != itemInstance)
				{
					item2.transform.Find("SecondaryCoverageRadiusIndicator").gameObject.SetActive(value: false);
				}
			}
		}
		ToggleItemsCollidersThatCanBeOverlapped(toggled: true, itemInstance);
		MouseController.Reset();
		PlacementSystem.StopPlacingItem();
		GameManager.SetPreventAutoSave(newPreventAutosaveValue: false);
		PlacementSystem.onPlacementModeEnd?.Invoke();
		if (setCamera)
		{
			if (!wasPausedBeforePlacementMode)
			{
				InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: false);
			}
			InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: false);
			CoroutineUtility.Run(ResetCameraCoroutine());
			if (PlayerHelper.IsHoldingItem)
			{
				InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetItemInstance(PlayerHelper.ItemInstanceInHands, spawnIntoPlayerHands: false);
			}
			else if (PlayerHelper.IsUsingVehicle)
			{
				InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVehicle(VehicleHelper.GetCurrentVehicleBase());
			}
			if (wasCargoPanelOpenBeforePlacementMode)
			{
				InstanceBehavior<UIs>.Instance.playerHUD.manageCargoUI.Show(InstanceBehavior<UIs>.Instance.playerHUD.manageCargoUI.currentCargoHolder);
				wasCargoPanelOpenBeforePlacementMode = false;
			}
		}
	}

	private static void ToggleItemsCollidersThatCanBeOverlapped(bool toggled, ItemInstance itemInstance)
	{
		if (itemInstance.ItemCached.itemsThatCanOverlapWith == null || itemInstance.ItemCached.itemsThatCanOverlapWith.Length == 0)
		{
			return;
		}
		foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			if (allItemController.ItemInstance != itemInstance && itemInstance.ItemCached.itemsThatCanOverlapWith.Contains(allItemController.itemName))
			{
				LinqExtensions.ForEach(allItemController.CollidersToDisableForOverlapping, delegate(Collider x)
				{
					x.enabled = toggled;
				});
			}
		}
	}

	private static bool ItemNotMovable(IPlaceableItem target)
	{
		if (!BuildingManager.isBuildingTemporarilyEditable && !GameManager.IsDevMode && target.GetItemInstance().ItemCached.HasTag(TagRef.Itemtag.cannotmoveinresidential) && InstanceBehavior<BuildingManager>.Instance.building.BuildingType == "ba:buildingtype_residential")
		{
			return !InstanceBehavior<BuildingManager>.Instance.building.IsHamptonsHouse();
		}
		return false;
	}

	private static IEnumerator ResetCameraCoroutine()
	{
		IndoorCam.angle = PlacementCam.currentAngle;
		if ((bool)OutdoorCam)
		{
			OutdoorCam.angle = IndoorCam.angle;
		}
		IndoorCam.distance = PlacementCam.distance;
		CinemachineVirtualCameraBase indoorCamera = InstanceBehavior<GameManager>.Instance.indoorCamera;
		CinemachineVirtualCameraBase indoorPlacementCamera = InstanceBehavior<GameManager>.Instance.indoorPlacementCamera;
		CameraHelper.SetBlendDuration(Mathf.Min(Vector3.Distance(indoorCamera.transform.position, indoorPlacementCamera.transform.position) * 0.3f, 0.3f), indoorPlacementCamera.Name, indoorCamera.Name);
		yield return CameraHelper.SetCameraRoutine(indoorCamera);
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.PlacementMode);
	}

	public static bool IsMovingItem(ItemController itemController)
	{
		IPlaceableItem currentPlaceableItemBeingPlaced = PlacementSystem.CurrentPlaceableItemBeingPlaced;
		if (currentPlaceableItemBeingPlaced == null || itemController == null)
		{
			return false;
		}
		ItemInstance itemInstance = currentPlaceableItemBeingPlaced.GetItemInstance();
		if (itemInstance == null || itemController.ItemInstance == null)
		{
			return false;
		}
		if (itemInstance.id == itemController.ItemInstance.id)
		{
			return true;
		}
		IPlaceableItem[] childPlaceableItems = currentPlaceableItemBeingPlaced.GetChildPlaceableItems();
		for (int i = 0; i < childPlaceableItems.Length; i++)
		{
			if (childPlaceableItems[i].GetItemInstance().id == itemController.ItemInstance.id)
			{
				return true;
			}
		}
		return false;
	}

	public static bool CancelPlacementModeIfIsActive()
	{
		if (!PlacementSystem.IsInPlacementMode)
		{
			return false;
		}
		if (PlacementSystem.initialPlacementPosition == InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer.transform.position)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.ClickGrab();
		}
		else
		{
			PlacementSystem.RevertPlacement();
			CancelPlacementMode();
		}
		return true;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		wasPausedBeforePlacementMode = false;
		wasCargoPanelOpenBeforePlacementMode = false;
		IndoorCam = null;
		OutdoorCam = null;
		PlacementCam = null;
	}
}
