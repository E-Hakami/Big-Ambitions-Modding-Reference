using System;
using BigAmbitions.InputSystem;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using BigAmbitions.Tags;
using Boats;
using Helpers;
using JimmysUnityUtilities;
using PlayerActivity;
using UI.InteriorDesigner;
using UI.PurchaseVehicle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Player.HUD.ItemInfoOverlays;

public class OverlayManager : InstanceBehavior<OverlayManager>
{
	[SerializeField]
	private bool isBlueprintMode;

	[SerializeField]
	private DetailedOverlay detailedOverlay;

	[SerializeField]
	private SimpleOverlay simpleOverlay;

	[SerializeField]
	private double navigationBlockGracePeriod;

	[HideInInspector]
	public bool blockPlayerNavigation;

	[HideInInspector]
	public bool ctaDisabled;

	private bool _preventSimpleOverlay;

	private bool _wasClickingUI;

	public static bool IsBlueprintMode => InstanceBehavior<OverlayManager>.Instance.isBlueprintMode;

	public bool IsDetailedOverlayActive => detailedOverlay.isActiveAndEnabled;

	private void Start()
	{
		if ((bool)detailedOverlay)
		{
			detailedOverlay.gameObject.SetActive(value: false);
		}
		if ((bool)simpleOverlay)
		{
			simpleOverlay.gameObject.SetActive(value: false);
		}
		InteriorDesignerUI.onOpenInteriorDesigner = (Action)Delegate.Combine(InteriorDesignerUI.onOpenInteriorDesigner, new Action(HideOverlays));
		if (!isBlueprintMode)
		{
			GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, (Action<Address>)delegate
			{
				HideOverlays();
			});
			GlobalEvents.onItemDiscarded = (Action<ItemInstance>)Delegate.Combine(GlobalEvents.onItemDiscarded, (Action<ItemInstance>)delegate
			{
				UpdateDynamicComponents(null, DynamicOverlayUpdateType.StockUpdate);
			});
			GlobalEvents.onItemGrabbed = (Action<ItemInstance>)Delegate.Combine(GlobalEvents.onItemGrabbed, (Action<ItemInstance>)delegate
			{
				UpdateDynamicComponents(null, DynamicOverlayUpdateType.StockUpdate);
			});
			GlobalEvents.onItemSelected = (Action<ItemInstance>)Delegate.Combine(GlobalEvents.onItemSelected, (Action<ItemInstance>)delegate
			{
				UpdateDynamicComponents(null, DynamicOverlayUpdateType.StockUpdate);
			});
			GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, (Action<VehicleController>)delegate
			{
				HideDetailedOverlay();
				UpdateDynamicComponents(null, DynamicOverlayUpdateType.CtaUpdate);
			});
		}
	}

	protected override void OnDestroy()
	{
		InteriorDesignerUI.onOpenInteriorDesigner = (Action)Delegate.Remove(InteriorDesignerUI.onOpenInteriorDesigner, new Action(HideOverlays));
		base.OnDestroy();
	}

	private void Update()
	{
		if ((!simpleOverlay || !simpleOverlay.isActiveAndEnabled) && (!detailedOverlay || !detailedOverlay.isActiveAndEnabled))
		{
			return;
		}
		if (PlayerAction.Click.Pressed() && EventSystem.current.IsPointerOverGameObject())
		{
			_wasClickingUI = true;
		}
		if (PlayerAction.Click.Released())
		{
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				_wasClickingUI = false;
			});
		}
		if (PlayerAction.Click.Pressed() && !_wasClickingUI && detailedOverlay.isActiveAndEnabled)
		{
			HideDetailedOverlay();
		}
	}

	private void LateUpdate()
	{
		if ((bool)simpleOverlay && simpleOverlay.isActiveAndEnabled)
		{
			simpleOverlay.UpdatePosition();
		}
		if ((bool)detailedOverlay && detailedOverlay.isActiveAndEnabled)
		{
			detailedOverlay.UpdatePosition();
		}
	}

	public void EnableOverlays()
	{
		_preventSimpleOverlay = false;
	}

	public void DisableOverlays()
	{
		HideOverlays();
		_preventSimpleOverlay = true;
	}

	public bool IsShowingOverlayOverItem(EntityController entityController)
	{
		(EntityController, bool) relevantEntity = OverlayHelper.GetRelevantEntity(entityController);
		EntityController item = relevantEntity.Item1;
		EntityController entityController2 = (relevantEntity.Item2 ? entityController : item);
		if (!simpleOverlay.isActiveAndEnabled || !(simpleOverlay.linkedController == entityController2))
		{
			if (detailedOverlay.isActiveAndEnabled)
			{
				return detailedOverlay.linkedController == entityController2;
			}
			return false;
		}
		return true;
	}

	public void ShowSimpleOverlay(EntityController entityController, Vector3 customPosition = default(Vector3))
	{
		CtaManager.UpdateCta(entityController);
		if (!_preventSimpleOverlay)
		{
			var (entityController2, flag) = OverlayHelper.GetRelevantEntity(entityController);
			if (ShouldShowSimpleOverlay(entityController2))
			{
				string overlayHeaderText = OverlayHelper.GetOverlayHeaderText(flag ? entityController : entityController2);
				simpleOverlay.linkedController = (flag ? entityController : entityController2);
				simpleOverlay.relevantController = entityController2;
				simpleOverlay.customPosition = customPosition;
				simpleOverlay.UpdateOverlay(entityController2, overlayHeaderText, ctaDisabled, isBlueprintMode);
				simpleOverlay.UpdatePosition();
				simpleOverlay.gameObject.SetActive(value: true);
			}
		}
	}

	public bool ShowDetailedOverlay(EntityController entityController)
	{
		var (entityController2, flag) = OverlayHelper.GetRelevantEntity(entityController);
		if (entityController2.detailedOverlayType == (DetailedOverlayType)0 || !ShouldShowDetailedOverlay(entityController2))
		{
			return false;
		}
		HideSimpleOverlayAndClearCta();
		_preventSimpleOverlay = true;
		string overlayHeaderText = OverlayHelper.GetOverlayHeaderText(flag ? entityController : entityController2);
		detailedOverlay.linkedController = (flag ? entityController : entityController2);
		detailedOverlay.relevantController = entityController2;
		if (detailedOverlay.UpdateOverlay(entityController2, overlayHeaderText) == 0)
		{
			_preventSimpleOverlay = false;
			ShowSimpleOverlay(entityController);
			return false;
		}
		detailedOverlay.gameObject.SetActive(value: true);
		LayoutRebuilder.ForceRebuildLayoutImmediate(detailedOverlay.rectTransform);
		detailedOverlay.UpdateOffsets();
		detailedOverlay.UpdatePosition();
		blockPlayerNavigation = true;
		return true;
	}

	public void HideOverlays()
	{
		HideSimpleOverlayAndClearCta();
		HideDetailedOverlay();
	}

	public void HideSimpleOverlayAndClearCta()
	{
		if ((bool)simpleOverlay && simpleOverlay.isActiveAndEnabled)
		{
			simpleOverlay.gameObject.SetActive(value: false);
		}
		CtaManager.Clear();
	}

	public void HideDetailedOverlay()
	{
		if ((bool)detailedOverlay && detailedOverlay.isActiveAndEnabled)
		{
			detailedOverlay.gameObject.SetActive(value: false);
			_preventSimpleOverlay = false;
			CoroutineUtility.RunAfterDelay(delegate
			{
				blockPlayerNavigation = false;
			}, TimeSpan.FromSeconds(navigationBlockGracePeriod));
		}
	}

	public void UpdateDynamicComponents(EntityController entityController, DynamicOverlayUpdateType updateType)
	{
		if ((bool)simpleOverlay && simpleOverlay.isActiveAndEnabled && (entityController == null || simpleOverlay.linkedController == entityController))
		{
			simpleOverlay.UpdateDynamicComponents(updateType);
		}
		if ((bool)detailedOverlay && detailedOverlay.isActiveAndEnabled && (entityController == null || detailedOverlay.linkedController == entityController))
		{
			detailedOverlay.UpdateDynamicComponents(updateType);
		}
	}

	private static bool ShouldShowDetailedOverlay(EntityController entityController)
	{
		bool flag = entityController is ItemController itemController && itemController.CanInteractInAnyBusiness;
		if (!CityMap.IsOpen && !PurchaseVehicleUI.IsShowcaseAnimationRunning && !VehicleHelper.IsInsideMotorVehicle())
		{
			VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
			if ((currentVehicle == null || !currentVehicle.VehicleType.HasTag(TagRef.Vehicletag.isscooter)) && !PlayerActivityUI.IsPanelOpen && (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness || !(entityController is ItemController) || flag))
			{
				if (PlayerHelper.IsHoldingShoppingBasket && !flag)
				{
					return false;
				}
				return entityController.ShouldShowDetailedOverlay();
			}
		}
		return false;
	}

	private static bool ShouldShowSimpleOverlay(EntityController entityController)
	{
		if (PurchaseVehicleUI.IsShowcaseAnimationRunning)
		{
			return false;
		}
		if (CityMap.IsOpen)
		{
			if (!(entityController is VehicleController) && !(entityController is BoatController))
			{
				return entityController is SubwayStation;
			}
			return true;
		}
		if (!PlacementSystem.IsInPlacementMode)
		{
			return !PlayerActivityUI.IsPanelOpen;
		}
		return false;
	}
}
