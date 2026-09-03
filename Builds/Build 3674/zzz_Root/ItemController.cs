using System;
using System.Collections.Generic;
using System.Linq;
using AI.Customers.CustomerEntries;
using BigAmbitions.InputSystem;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using BigAmbitions.SoundSystem;
using BigAmbitions.Tags;
using Buildings;
using Buildings.Indoors.InteriorDesign;
using Controllers;
using EmployeeStations;
using Entities;
using Extensions;
using HGAttributes;
using Helpers;
using IngameDebugConsole;
using JimmysUnityUtilities;
using NaughtyAttributes;
using Player.HUD.ItemInfoOverlays;
using Player.HUD.ItemWarningIcons;
using PlayerActivity;
using TMPro;
using UI;
using UI.Elements;
using UI.InteriorDesigner;
using UI.MiniMenu;
using UI.Notification;
using UI.Purchase;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class ItemController : EntityController, IPlaceableItem
{
	private static readonly int IsShadowCaster = Shader.PropertyToID("_IsShadowCaster");

	[Header("ItemController")]
	[AutocompleteDropdown("Items")]
	public string itemName;

	[SerializeField]
	private Transform alignmentTransform;

	public PlayerItemPurchaserSettings playerItemPurchaserSettings;

	[SerializeField]
	private Collider[] collidersToDisableForOverlapping;

	[SerializeField]
	private GameObject[] extraVisualsParentObjects;

	[SerializeField]
	private bool applyAudioMixerGroup;

	[SerializeField]
	[ShowIf("applyAudioMixerGroup")]
	private AudioSource[] audioSourcesToApplyMixerGroup;

	[Header("Seating")]
	public bool allowResting;

	[SerializeField]
	private bool ignoreSittingAngle;

	[SerializeField]
	internal Transform[] sittingPositions;

	[SerializeField]
	private SeatSpot[] seatSpots;

	[Header("Inverse Kinematics")]
	[SerializeField]
	private Transform lHandIKAttachmentPoint;

	[SerializeField]
	private Transform rHandIKAttachmentPoint;

	[SerializeField]
	private Transform headIKAttachmentPoint;

	[Header("Fields automatically set up")]
	[ReadOnly]
	[SerializeField]
	private Collider[] colliders;

	[ReadOnly]
	[SerializeField]
	private NavMeshObstacle[] navMeshObstacles;

	[ReadOnly]
	[SerializeField]
	private GroundIndicator[] groundIndicators;

	[ReadOnly]
	[SerializeField]
	private GameObject[] directionIndicators;

	[ReadOnly]
	[SerializeField]
	private AttachmentPoint[] attachmentPoints;

	[ReadOnly]
	[SerializeField]
	private ScreenVideoController screenVideoController;

	[Header("Validation")]
	[SerializeField]
	private bool checkRenderersInChildren;

	[ShowIf("checkRenderersInChildren")]
	[SerializeField]
	private int checkRenderersDepth = -1;

	[Header("Shadows")]
	[SerializeField]
	private bool shadowCasterPreserveMaterials;

	[SerializeField]
	private LightLayerEnum shadowCasterRenderingLayerMask = LightLayerEnum.LightLayerDefault | LightLayerEnum.LightLayer4;

	[HideInInspector]
	public bool initialized;

	[HideInInspector]
	public UnityEvent onItemInitialized = new UnityEvent();

	[HideInInspector]
	public List<ItemController> childItemControllers = new List<ItemController>();

	[HideInInspector]
	public ItemController parentItemController;

	[HideInInspector]
	public AttachmentPoint parentAttachmentPoint;

	[HideInInspector]
	public List<SerializableVector3> customPositions;

	[HideInInspector]
	public List<CustomColor> customColors;

	[HideInInspector]
	public bool highlightChildrenOnHover;

	[HideInInspector]
	[Tooltip("Used in special business layouts for storing specific values")]
	public string customValue;

	[NonSerialized]
	public bool loadedInHamptonsHouse;

	protected List<string> stockTypes;

	protected bool occupied;

	private Item _item;

	private ItemInstance _itemInstance;

	private PlayerItemPurchaser _playerItemPurchaser;

	private BuildingContext _buildingContext;

	private readonly List<Vector3> _navMeshTargetInitialPositions = new List<Vector3>();

	private readonly List<Renderer> _shadowCasters = new List<Renderer>();

	[HideInInspector]
	public bool blockDropdown;

	private static List<CustomColor> SavedCustomColors;

	public BuildingContext BuildingContext => GetOrCreateBuildingContext();

	public Item Item => _item ?? (_item = ItemsGetter.GetByName(itemName));

	public virtual List<string> StockTypes
	{
		get
		{
			if (stockTypes == null)
			{
				UpdateStockTypes();
			}
			return stockTypes;
		}
	}

	public PlayerItemPurchaser PlayerItemPurchaser => _playerItemPurchaser;

	public ItemInstance ItemInstance
	{
		get
		{
			return _itemInstance;
		}
		set
		{
			_itemInstance = value;
			onItemInitialized.Invoke();
		}
	}

	public Collider[] Colliders => colliders;

	public Collider[] CollidersToDisableForOverlapping => collidersToDisableForOverlapping;

	public AttachmentPoint[] AttachmentPoints => attachmentPoints;

	public Transform LHandIKAttachmentPoint => lHandIKAttachmentPoint;

	public Transform RHandIKAttachmentPoint => rHandIKAttachmentPoint;

	public Transform HeadIKAttachmentPoint => headIKAttachmentPoint;

	public SeatSpot[] SeatSpots => seatSpots;

	public virtual string CurrentStockDropdownOption => ItemInstance.GetStockInstance().itemName;

	public virtual string StockAmountText
	{
		get
		{
			CargoInstance stockInstance = ItemInstance.GetStockInstance();
			if (string.IsNullOrEmpty(stockInstance.itemName))
			{
				return ColoredTextHelper.GetAmountTextNumberOnly(0, 0);
			}
			return ColoredTextHelper.GetAmountTextNumberOnly(stockInstance.amount, stockInstance.GetMaxStockCapacity(ItemInstance));
		}
	}

	public override bool Occupied
	{
		get
		{
			if (occupied)
			{
				return true;
			}
			if (childItemControllers == null)
			{
				return false;
			}
			if (childItemControllers.Any((ItemController childItemController) => childItemController.Occupied))
			{
				return true;
			}
			if (sittingPositions.Length != 0 && sittingPositions.All((Transform x) => x.childCount > 0))
			{
				return true;
			}
			if (!seatSpots.Any((SeatSpot x) => x.occupied))
			{
				return occupied;
			}
			return true;
		}
		set
		{
			occupied = value;
			if (parentItemController != null)
			{
				parentItemController.Occupied = value;
			}
		}
	}

	public virtual Transform SittingPosition
	{
		get
		{
			if (sittingPositions.Length == 0)
			{
				return null;
			}
			List<Transform> list = sittingPositions.Where((Transform x) => x.transform.childCount == 0).ToList();
			if (list.Count == 0)
			{
				return null;
			}
			if (list.Count == 1)
			{
				return list[0];
			}
			Vector3 playerPosition = PlayerHelper.GetPosition();
			List<Transform> list2 = list.OrderBy((Transform x) => Vector3.SqrMagnitude(x.position - playerPosition)).Take(2).ToList();
			if (ignoreSittingAngle)
			{
				return list2[0];
			}
			Transform result = null;
			float num = float.MinValue;
			foreach (Transform item in list2)
			{
				Vector3 normalized = (playerPosition - item.position).normalized;
				float num2 = Vector3.Dot(item.forward, normalized);
				if (num2 > num)
				{
					num = num2;
					result = item;
				}
			}
			return result;
		}
	}

	public virtual bool CanInteractInAnyBusiness => false;

	public IReadOnlyList<Renderer> GetShadowCasters()
	{
		return _shadowCasters;
	}

	public override void Awake()
	{
		base.Awake();
		Renderer[] array = renderers;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is MeshRenderer meshRenderer)
			{
				Renderer renderer = ShadowsHelper.ExtractShadowCaster(meshRenderer, shadowCasterPreserveMaterials ? null : InstanceBehavior<GlobalReferences>.Instance.shadowOnlyMaterial, (uint)shadowCasterRenderingLayerMask);
				if (renderer != null)
				{
					_shadowCasters.Add(renderer);
				}
			}
		}
		this.SetPlacementIndicatorsVisibility(visible: false);
		HideAllAttachmentPoints();
		if (applyAudioMixerGroup)
		{
			AudioSource[] array2 = audioSourcesToApplyMixerGroup;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].outputAudioMixerGroup = InstanceBehavior<GlobalReferences>.Instance.foleyMixerGroup;
			}
		}
	}

	public override void Start()
	{
		base.Start();
		GetOrCreateBuildingContext();
		SetNavMeshTargetInitialPositions();
		if (Item == null)
		{
			return;
		}
		PlayerItemPurchaserSettings obj = playerItemPurchaserSettings;
		if (obj != null && obj.enabled)
		{
			primaryInteractionEnabled = true;
			_playerItemPurchaser = new PlayerItemPurchaser
			{
				itemController = this
			};
			_playerItemPurchaser.UpdatePriceInfo();
			if (playerItemPurchaserSettings.itemName == itemName && (BuildingContext.Building.SpecialService == null || BuildingContext.Building.SpecialService.showForSaleVisualsWhenItemsAreForSale))
			{
				Transform transform = base.transform.Find("ForSaleVisuals");
				if (transform != null)
				{
					transform.gameObject.SetActive(value: true);
				}
			}
		}
		if ((playerItemPurchaserSettings == null || !playerItemPurchaserSettings.enabled) && Item.snapToCeiling && base.transform.position.y <= 0f)
		{
			SetRoofItemYPosition();
		}
		ItemInstance itemInstance = ItemInstance;
		if (itemInstance != null && itemInstance.customColors != null)
		{
			SetCustomColors(ItemInstance.customColors);
		}
		else
		{
			List<CustomColor> list = customColors;
			if (list != null && list.Count > 0)
			{
				SetCustomColors(customColors);
			}
		}
		if (GameManager.isShadowCasterDebugViewEnabled)
		{
			UpdateDebugShadowCasterOverlay();
		}
		initialized = true;
		onItemInitialized.Invoke();
		if (ItemInstance != null)
		{
			ItemInstance itemInstance2 = ItemInstance;
			itemInstance2.onInstanceRemoved = (Action)Delegate.Combine(itemInstance2.onInstanceRemoved, new Action(OnInstanceRemoved));
		}
	}

	protected BuildingContext GetOrCreateBuildingContext()
	{
		if (_buildingContext != null)
		{
			return _buildingContext;
		}
		_buildingContext = (loadedInHamptonsHouse ? BuildingManager.CreateBuildingContextFromItemInstance(ItemInstance) : BuildingManager.CreateBuildingContextFromActiveManager());
		return _buildingContext;
	}

	private void SetRoofItemYPosition()
	{
		string buildingSize = ((BuildingPreview.isPreviewing && InstanceBehavior<UIs>.Instance != null) ? InstanceBehavior<UIs>.Instance.buildingPreview.GetCurrentBuildingSize : BuildingContext.Building.BuildingSize);
		MultipleHeightsBuildingController multipleHeightsBuildingController = ((BuildingPreview.isPreviewing && InstanceBehavior<UIs>.Instance != null) ? InstanceBehavior<UIs>.Instance.buildingPreview.GetMultipleHeightsBuildingController : BuildingContext.MultipleHeights);
		base.transform.SetPositionY((multipleHeightsBuildingController != null) ? multipleHeightsBuildingController.GetCeilingYPositionForRoofObject(base.transform.position) : BuildingSizeHelper.GetBuildingRoofPosition(buildingSize, 0));
		CoroutineUtility.RunAfterFrameDelay(UpdateNavMeshTargets, 3);
	}

	public void OnInstanceRemoved()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void OnBecameVisible()
	{
		if (!PlacementHelper.IsMovingItem(this))
		{
			InstanceBehavior<ItemWarningIconManager>.Instance?.UpdateWarningIcon(this);
		}
	}

	private void OnBecameInvisible()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			InstanceBehavior<ItemWarningIconManager>.Instance?.HideWarningIcon(this);
		}
	}

	public override void Hide()
	{
		base.Hide();
		foreach (Renderer shadowCaster in _shadowCasters)
		{
			shadowCaster.enabled = false;
		}
		GameObject[] array = extraVisualsParentObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SetActive(value: false);
		}
	}

	public override void Show()
	{
		base.Show();
		foreach (Renderer shadowCaster in _shadowCasters)
		{
			shadowCaster.enabled = true;
		}
		GameObject[] array = extraVisualsParentObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SetActive(value: true);
		}
	}

	private void SetNavMeshTargetInitialPositions()
	{
		Transform[] array = navMeshTargets;
		foreach (Transform transform in array)
		{
			_navMeshTargetInitialPositions.Add(transform.localPosition);
		}
	}

	public override bool ShouldReactToIoEnter()
	{
		if (!primaryInteractionEnabled || !base.enabled || !visible || GameManager.IsAnyMiniGameActive())
		{
			return false;
		}
		if (base.ShouldReactToIoEnter() || PlacementSystem.IsInPlacementMode || InteriorDesignerUI.IsOpen || InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness || _playerItemPurchaser != null)
		{
			return true;
		}
		if (Item.HasTag(TagRef.Itemtag.alwaysinteractable))
		{
			return true;
		}
		if (PlayerHelper.ItemInstanceInHands != null)
		{
			string text = itemName;
			if (text == "ba:itemname_angledwoodendisplaystand" || text == "ba:itemname_flatwoodendisplaystand")
			{
				return true;
			}
		}
		return false;
	}

	public override bool Interact()
	{
		if (base.Interact())
		{
			return true;
		}
		if (!primaryInteractionEnabled)
		{
			return false;
		}
		if (_playerItemPurchaser != null)
		{
			_playerItemPurchaser.GrabItem();
			return true;
		}
		if (PlacementSystem.IsInPlacementMode || EnergyHelper.goingToHospital || InstanceBehavior<BuildingManager>.Instance.enteringBuilding)
		{
			return true;
		}
		if (InstanceBehavior<UIs>.Instance.playerActivityUI?.GetCurrentActivity != null)
		{
			return false;
		}
		if (InstanceBehavior<UIs>.Instance.gameSpeed.Paused)
		{
			return true;
		}
		if (PlayerHelper.IsUsingVehicle && ItemInstance != null && _item.HasTag(TagRef.Itemtag.alwaysinteractable))
		{
			CargoInstance cargoInstance = (_item.HasTag(TagRef.Itemtag.isbox) ? ItemInstance.cargoInstances.First() : ItemInstance.ConvertToCargoInstance());
			if (VehicleHelper.GetCurrentVehicle().TryToAddToCargo(cargoInstance))
			{
				if (parentItemController != null)
				{
					RemoveFromParentPlaceableItem();
				}
				UnityEngine.Object.Destroy(base.gameObject);
				InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RemoveItemInstanceFromBuilding(ItemInstance);
				InstanceBehavior<BuildingManager>.Instance.ScheduleUpdateAvailableProducers();
				CustomerDemandHelper.ReloadCachedFulfilled(InstanceBehavior<BuildingManager>.Instance.building.Address);
				CoroutineUtility.RunAfterOneFrame(delegate
				{
					GlobalEvents.onItemGrabbed?.Invoke(ItemInstance);
				});
				return true;
			}
		}
		if (childItemControllers.Any((ItemController x) => x.Interact()))
		{
			return true;
		}
		if (_item.HasTag(TagRef.Itemtag.alwaysinteractable))
		{
			GrabItem();
			return true;
		}
		if (PlayerHelper.ItemInstanceInHands == null)
		{
			return false;
		}
		string text = itemName;
		if ((text == "ba:itemname_angledwoodendisplaystand" || text == "ba:itemname_flatwoodendisplaystand") && ItemsGetter.GetByName("ba:itemname_woodenproductcrate").itemsThatCanShowcase.Any((string x) => PlayerHelper.ItemInstanceInHands.cargoInstances.Any((CargoInstance y) => y.itemName == x)))
		{
			Notifications.ShowError("notifications_needs_wooden_product_case");
			return true;
		}
		return false;
	}

	private void GrabItem()
	{
		if (TryToGrabItem())
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public bool TryToGrabItem()
	{
		if (PlayerActivityUI.IsPanelOpen)
		{
			Notifications.ShowError("itempanelui_notification_cant_grab_while_in_activity");
			return false;
		}
		if (ItemInstance.stackedItems.Count > 0)
		{
			Notifications.ShowError("itempanelui_notification_cant_grab_with_attachments");
			return false;
		}
		if (_item.HasTag(TagRef.Itemtag.isbox) || (_item.HasTag(TagRef.Itemtag.isbag) && ItemInstance.cargoInstances.Count > 0))
		{
			if (PlayerHelper.IsHoldingItem || PlayerHelper.IsUsingVehicle)
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
				ICargoHolder toHolder = cargoHolder;
				if (!ICargoHolder.TryToMergeAndMoveCargoBetweenHolders(ItemInstance, toHolder))
				{
					Notifications.ShowError("playeritempurchaser_notification_hands_full");
					return false;
				}
				if (ItemInstance.cargoInstances.Any((CargoInstance x) => x.amount > 0))
				{
					return false;
				}
			}
			else
			{
				PlayerHelper.ItemInstanceInHands = ItemInstance;
			}
		}
		else
		{
			if (ItemInstance.cargoInstances.Count > 0 && (ItemInstance.ItemCached.type & ItemType.StorageShelf) != 0)
			{
				Dictionary<string, string> notificationData = new Dictionary<string, string> { { "itemname", ItemInstance.itemName } };
				Notifications.Show(NotificationType.Error, "itempanelui_notification_cant_grab_shelf_with_cargo", notificationData);
				return false;
			}
			if (PlayerHelper.IsHoldingItem)
			{
				CargoInstance cargoInstance = ItemInstance.ConvertToCargoInstance();
				if (!PlayerHelper.ItemInstanceInHands.TryToAddToCargo(cargoInstance))
				{
					Notifications.ShowError("playeritempurchaser_notification_hands_full");
					return false;
				}
			}
			else if (PlayerHelper.IsUsingVehicle)
			{
				CargoInstance cargoInstance2 = ItemInstance.ConvertToCargoInstance();
				if (!VehicleHelper.GetCurrentVehicle().TryToAddToCargo(cargoInstance2))
				{
					Notifications.ShowError("playeritempurchaser_notification_hands_full");
					return false;
				}
			}
			else
			{
				PlayerHelper.ItemInstanceInHands = ItemHelper.InitializeItemInHandsWithCargo(ItemInstance.ConvertToCargoInstance());
			}
		}
		if (parentItemController != null)
		{
			RemoveFromParentPlaceableItem();
		}
		InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RemoveItemInstanceFromBuilding(ItemInstance);
		ItemInstance.RemoveFromWorkShifts(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address);
		CustomerDemandHelper.ReloadCachedFulfilled(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address);
		InstanceBehavior<BuildingManager>.Instance.ScheduleUpdateAvailableProducers();
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			GlobalEvents.onItemGrabbed?.Invoke(ItemInstance);
		});
		return true;
	}

	public override void SecondaryInteract()
	{
		if (InstanceBehavior<UIs>.Instance?.playerActivityUI?.GetCurrentActivity == null && BuildingManager.IsInsideBuilding && !InstanceBehavior<BuildingManager>.Instance.enteringBuilding && !InstanceBehavior<BuildingManager>.Instance.exitingBuilding && (InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RentedByPlayer || BuildingManager.isBuildingTemporarilyEditable))
		{
			if ((!Occupied || BuildingManager.isBuildingTemporarilyEditable) && !PlacementSystem.IsInPlacementMode && !PurchaseUI.IsPanelOpen && !EnergyHelper.goingToHospital && !BuildingPreview.isPreviewing && !InteriorDesignerUI.IsOpen && !PlayerActivityUI.IsPanelOpen && !MiniMenu.IsOpen && this != InstanceBehavior<GameManager>.Instance.playerController.Character.CurrentEntityController && (!PlayerHelper.IsUsingVehicle || VehicleHelper.GetCurrentVehicleBase().vehicleType.spawnInPlayerObject))
			{
				InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
				this.StartPlacementMode();
			}
			else if (Occupied)
			{
				Notifications.ShowError("pointandclickobject_notification_cant_pick_occupied_objects");
			}
		}
	}

	public override void OnIoEnter()
	{
		if (ShouldReactToIoEnter())
		{
			InstanceBehavior<OverlayManager>.Instance.ShowSimpleOverlay(this);
			HandleHighlight();
		}
	}

	protected void HandleHighlight()
	{
		if (disableHighlightInteraction || blockOutline || !ScreenshotController.uiIsVisible || ScreenshotController.isInFreeLookMode || BuildingPreview.isPreviewing || PlayerActivityUI.IsPanelOpen)
		{
			return;
		}
		Renderer[] array = renderers;
		foreach (Renderer renderer in array)
		{
			if (!(renderer == null))
			{
				renderer.gameObject.layer = LayerHelper.InteractiveItemsOutlinedLayerIndex;
				cancelHighlightAndFadeOut = true;
			}
		}
		if (!highlightChildrenOnHover)
		{
			return;
		}
		foreach (ItemController childItemController in childItemControllers)
		{
			array = childItemController.renderers;
			foreach (Renderer renderer2 in array)
			{
				if (!(renderer2 == null))
				{
					renderer2.gameObject.layer = LayerHelper.InteractiveItemsOutlinedLayerIndex;
				}
			}
		}
	}

	public override void OnIoExit()
	{
		onIoExitEvent.Invoke();
		onIoExitEvent.RemoveAllListeners();
		if (InstanceBehavior<OverlayManager>.Instance != null && InstanceBehavior<OverlayManager>.Instance.IsShowingOverlayOverItem(this))
		{
			InstanceBehavior<OverlayManager>.Instance.HideSimpleOverlayAndClearCta();
		}
		if (disableHighlightInteraction)
		{
			return;
		}
		Renderer[] array = renderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.layer = LayerHelper.InteractiveItemsLayerIndex;
		}
		if (!highlightChildrenOnHover)
		{
			return;
		}
		foreach (ItemController childItemController in childItemControllers)
		{
			array = childItemController.renderers;
			foreach (Renderer renderer in array)
			{
				if (!(renderer == null))
				{
					renderer.gameObject.layer = LayerHelper.InteractiveItemsLayerIndex;
				}
			}
		}
	}

	public override void UpdateNavMeshTargets()
	{
		if (this == null)
		{
			return;
		}
		ResetNavMeshTargetPositionsToInitial();
		base.UpdateNavMeshTargets();
		if (!base.gameObject.activeSelf)
		{
			return;
		}
		foreach (ItemController childItemController in childItemControllers)
		{
			childItemController.UpdateNavMeshTargets();
		}
	}

	private void ResetNavMeshTargetPositionsToInitial()
	{
		if (_navMeshTargetInitialPositions.Count != 0)
		{
			for (int i = 0; i < navMeshTargets.Length; i++)
			{
				navMeshTargets[i].localPosition = _navMeshTargetInitialPositions[i];
			}
		}
	}

	protected override void DoNavMeshTargetValidation(int index)
	{
		if (Item != null && BuildingContext?.Building != null && (Item.snapToCeiling || Item.wallMounted))
		{
			MultipleHeightsBuildingController multipleHeights = BuildingContext.MultipleHeights;
			if (multipleHeights != null)
			{
				float floorHeightForPosition = multipleHeights.GetFloorHeightForPosition(navMeshTargets[index].position);
				navMeshTargets[index].SetPositionY(floorHeightForPosition);
			}
			else
			{
				navMeshTargets[index].SetPositionY(0f);
			}
		}
		else if ((bool)parentItemController)
		{
			navMeshTargets[index].SetPositionY(parentItemController.transform.position.y);
		}
		if (NavMesh.SamplePosition(navMeshTargets[index].position, out var hit, 3f, -1))
		{
			navMeshTargets[index].position = hit.position;
		}
	}

	public override Vector3 GetNavMeshTargetPosition(int index = 0)
	{
		Vector3 navMeshTargetPosition = base.GetNavMeshTargetPosition(index);
		if (!parentItemController)
		{
			return navMeshTargetPosition;
		}
		return new Vector3(navMeshTargetPosition.x, parentItemController.transform.position.y, navMeshTargetPosition.z);
	}

	public Vector3 GetNavMeshTargetPositionClosestToTheItem()
	{
		Vector3 result = default(Vector3);
		float num = float.MaxValue;
		Transform[] array = navMeshTargets;
		foreach (Transform transform in array)
		{
			float num2 = Vector3.SqrMagnitude(transform.position - base.transform.position);
			if (num2 < num)
			{
				num = num2;
				result = transform.position;
			}
		}
		if (!(parentItemController != null))
		{
			return result;
		}
		return new Vector3(result.x, parentItemController.transform.position.y, result.z);
	}

	public int GetNavMeshTargetCount()
	{
		return navMeshTargets.Length;
	}

	public override bool TryGetRandomAvailableNavMeshTargetPosition(out Vector3 navMeshPosition)
	{
		if ((bool)parentItemController)
		{
			return parentItemController.TryGetRandomAvailableNavMeshTargetPosition(out navMeshPosition);
		}
		return base.TryGetRandomAvailableNavMeshTargetPosition(out navMeshPosition);
	}

	public void PlayVideoOnScreen(VideoClipData.VideoType type)
	{
		foreach (ItemController item in ItemHelper.GetItemControllersInGroup(this))
		{
			item.PlayVideo(type);
		}
	}

	private void PlayVideo(VideoClipData.VideoType type)
	{
		screenVideoController?.Play(type);
	}

	public void StopVideoOnScreen()
	{
		foreach (ItemController item in ItemHelper.GetItemControllersInGroup(this))
		{
			item.StopVideo();
		}
	}

	private void StopVideo()
	{
		screenVideoController?.Stop();
	}

	protected virtual void UpdateStockTypes()
	{
		if (stockTypes == null)
		{
			stockTypes = new List<string>();
		}
		else
		{
			stockTypes.Clear();
		}
		stockTypes.Add("ba:itemname_undefined");
		stockTypes.AddRange(Item.itemsThatCanShowcase);
	}

	public virtual void UpdateSelectedStockOverlay()
	{
		if (!blockDropdown && InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			if (stockTypes == null)
			{
				UpdateStockTypes();
			}
			List<string> list = stockTypes;
			if (list != null && list.Count > 1)
			{
				InstanceBehavior<OverlayManager>.Instance.UpdateDynamicComponents(this, DynamicOverlayUpdateType.StockUpdate | DynamicOverlayUpdateType.CtaUpdate);
				InstanceBehavior<ItemWarningIconManager>.Instance.UpdateWarningIcon(this);
			}
		}
	}

	public virtual void OnStockOptionSelected(int stockIndex, Dropdown dropdown = null)
	{
		if (stockIndex != -1)
		{
			string stockItemName = ((StockTypes[stockIndex] == "ba:itemname_undefined") ? string.Empty : StockTypes[stockIndex]);
			OnStockOptionSelected(stockItemName, dropdown);
		}
	}

	public virtual int GetSelectedStockIndex()
	{
		string stockItemName = ItemInstance.GetStockInstance().itemName;
		if (string.IsNullOrEmpty(stockItemName))
		{
			return stockTypes.FindIndex((string x) => x == "ba:itemname_undefined");
		}
		int num = stockTypes.FindIndex((string x) => x == stockItemName);
		if (num == -1)
		{
			return stockTypes.FindIndex((string x) => x == "ba:itemname_undefined");
		}
		return num;
	}

	private void OnStockOptionSelected(string stockItemName, Dropdown dropdown = null)
	{
		CargoInstance stockInstance = ItemInstance.GetStockInstance();
		if (stockItemName == stockInstance.itemName)
		{
			return;
		}
		bool flag = false;
		if (stockInstance.amount > 0 && !string.IsNullOrEmpty(stockInstance.itemName))
		{
			CargoInstance cargoInstance = new CargoInstance(stockInstance.itemName, stockInstance.amount, stockInstance.pricePerUnit);
			flag = cargoInstance.ReturnToAShelf(ItemInstance.AddressCached, ItemInstance);
			if (!flag)
			{
				Notifications.ShowError("itemoverlay_notification_no_storage_available");
				dropdown?.ResetSelectedOption(stockTypes.FindIndex((string x) => x == stockInstance.itemName));
				stockInstance.amount = cargoInstance.amount;
				return;
			}
			stockInstance.amount = 0;
		}
		stockInstance.itemName = stockItemName;
		stockInstance.ResetItemCached();
		if (flag)
		{
			ItemInstance.OnItemsInCargoUpdated()?.Invoke();
		}
		if (!string.IsNullOrEmpty(stockItemName))
		{
			ItemInstance.FillUpShowcaseShelfOrPointOfSale();
		}
		InstanceBehavior<UIs>.Instance?.tasksUI.InstantlyCompleteListOfTasks(SaveGameManager.Current.TodoTasks.Where(delegate(TodoTask x)
		{
			if (x.itemInstanceId == ItemInstance.id)
			{
				TodoTaskType type = x.type;
				return type == TodoTaskType.LowStock || type == TodoTaskType.EmptyStock;
			}
			return false;
		}));
		GameEvent.Invoke("ba:gameevent_itemstockedup");
		InstanceBehavior<UIs>.Instance?.tasksUI.ScheduleUpdateCurrentBusinessTodoTasks();
		BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
		BusinessHelper.UpdateCustomerCapacity(buildingRegistration);
		if (buildingRegistration.HasValidAddress)
		{
			BusinessHelper.UpdatePromotion(buildingRegistration);
			CustomerEntriesHelper.UpdateCustomerEntriesForPlayerBusiness(buildingRegistration, TimeHelper.GetDayOfWeek());
			buildingRegistration.UpdateSecurityLevel();
		}
		if ((bool)InstanceBehavior<UIs>.Instance)
		{
			BusinessHelper.GenerateItemsWithoutStockTasks(InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
		}
		UpdateSelectedStockOverlay();
		InteriorDesignerInfoPanelEvents.onUpdateCustomerCapacity?.Invoke();
		GlobalEvents.onBuildingRegistrationChange?.Invoke(buildingRegistration.Address);
	}

	public void SetCustomColors(List<CustomColor> newCustomColors)
	{
		customColors = newCustomColors;
		if (newCustomColors == null)
		{
			return;
		}
		foreach (CustomColor customColor in newCustomColors)
		{
			if ((Item.customColorChannels & customColor.channel) == 0)
			{
				continue;
			}
			if (customColor.channel == CustomColorChannel.Text)
			{
				TMP_Text[] componentsInChildren = GetComponentsInChildren<TMP_Text>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].color = customColor.color;
				}
				continue;
			}
			if (customColor.channel == CustomColorChannel.Light)
			{
				IndoorLightController indoorLightController = this as IndoorLightController;
				if ((object)indoorLightController != null)
				{
					CoroutineUtility.RunAfterOneFrame(delegate
					{
						indoorLightController.SetColor(customColor.color);
					});
				}
				continue;
			}
			MaterialPropertyBlock maskColorPropertyBlock = ColorOverlay.MaskColorPropertyBlock;
			Renderer[] renderersToSetColor = GetRenderersToSetColor(customColor.channel);
			foreach (Renderer obj in renderersToSetColor)
			{
				obj.GetPropertyBlock(maskColorPropertyBlock);
				maskColorPropertyBlock.SetColor(ColorOverlay.GetMaskId(customColor.channel), customColor.color);
				obj.SetPropertyBlock(maskColorPropertyBlock);
			}
		}
	}

	public virtual Renderer[] GetRenderersToSetColor(CustomColorChannel customColorChannel)
	{
		return renderers;
	}

	public void TogglePhysics(bool physicsEnabled)
	{
		NavMeshObstacle[] array = navMeshObstacles;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = physicsEnabled;
		}
		Collider[] array2 = colliders;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = physicsEnabled;
		}
	}

	public void ForceUpdateNavMesh()
	{
		NavMeshObstacle[] array = navMeshObstacles;
		foreach (NavMeshObstacle navMeshObstacle in array)
		{
			if (navMeshObstacle.enabled && navMeshObstacle.carving)
			{
				navMeshObstacle.enabled = false;
				navMeshObstacle.enabled = true;
			}
		}
	}

	public virtual string GetProducedItemName()
	{
		PlayerItemPurchaserSettings obj = playerItemPurchaserSettings;
		if (obj != null && obj.enabled)
		{
			return playerItemPurchaserSettings.itemName;
		}
		if (ItemInstance != null && (ItemInstance.ItemCached.type & (ItemType.PointOfSale | ItemType.ShowcaseShelf)) != 0)
		{
			return ItemInstance.GetStockInstance().itemName;
		}
		Item.ItemProducerSettings producerSettings = Item.producerSettings;
		if (producerSettings != null)
		{
			string[] itemsToProduce = producerSettings.itemsToProduce;
			if (itemsToProduce != null && itemsToProduce.Length > 0)
			{
				return Item.producerSettings.itemsToProduce.GetRandom();
			}
		}
		return null;
	}

	public bool IsAmountAvailable(int amount)
	{
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			return true;
		}
		CargoInstance stockInstance = ItemInstance.GetStockInstance();
		if (!string.IsNullOrEmpty(stockInstance.itemName))
		{
			return stockInstance.amount >= amount;
		}
		return false;
	}

	public void AddShelfCount(int amount)
	{
		if (InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RentedByPlayer)
		{
			CargoInstance stockInstance = ItemInstance.GetStockInstance();
			if (!string.IsNullOrEmpty(stockInstance.itemName))
			{
				stockInstance.amount++;
			}
		}
	}

	public void RemoveStockInContent()
	{
		InstanceBehavior<GameManager>.Instance.playerController.SetGoal(this, delegate
		{
			CargoInstance stockInstance = ItemInstance.GetStockInstance();
			if (stockInstance.amount > 0)
			{
				if (PlacementSystem.IsInPlacementMode)
				{
					Notifications.ShowError("itemcontroller_notification_not_in_placementmode_to_empty");
				}
				else if (PlayerActivityUI.IsPanelOpen)
				{
					Notifications.ShowError("itempanelui_notification_cant_grab_while_in_activity");
				}
				else if (!PurchaseUI.IsPanelOpen)
				{
					ICargoHolder cargoHolder2;
					if (!PlayerHelper.IsUsingVehicle)
					{
						ICargoHolder cargoHolder = (PlayerHelper.IsHoldingItem ? PlayerHelper.ItemInstanceInHands : null);
						cargoHolder2 = cargoHolder;
					}
					else
					{
						ICargoHolder cargoHolder = VehicleHelper.GetCurrentVehicle();
						cargoHolder2 = cargoHolder;
					}
					ICargoHolder cargoHolder3 = cargoHolder2;
					if (cargoHolder3 == null)
					{
						PlayerHelper.ItemInstanceInHands = ItemHelper.InitializeItemInHandsWithCargo(new CargoInstance(stockInstance.itemName, stockInstance.amount, stockInstance.pricePerUnit));
						stockInstance.amount = 0;
						stockInstance.itemName = null;
						ItemInstance.OnItemsInCargoUpdated()?.Invoke();
					}
					else
					{
						CargoInstance cargoInstance = new CargoInstance(stockInstance.itemName, stockInstance.amount, stockInstance.pricePerUnit);
						cargoHolder3.MergeIntoCargo(cargoInstance);
						if (cargoInstance.amount > 0 && !cargoHolder3.TryToAddToCargo(cargoInstance))
						{
							stockInstance.amount = cargoInstance.amount;
							ItemInstance.OnItemsInCargoUpdated()?.Invoke();
							Notifications.ShowError("itemcontroller_notification_empty_hands_to_empty");
							return;
						}
						stockInstance.itemName = null;
						stockInstance.amount = 0;
						ItemInstance.OnItemsInCargoUpdated()?.Invoke();
					}
					InstanceBehavior<UIs>.Instance.tasksUI.InstantlyCompleteListOfTasks(SaveGameManager.Current.TodoTasks.Where(delegate(TodoTask x)
					{
						if (x.itemInstanceId == ItemInstance.id)
						{
							TodoTaskType type = x.type;
							return type == TodoTaskType.LowStock || type == TodoTaskType.EmptyStock;
						}
						return false;
					}));
					BusinessHelper.UpdateCustomerCapacity(BuildingHelper.GetBuildingRegistration(ItemInstance.AddressCached));
					GlobalEvents.onBuildingRegistrationChange?.Invoke(ItemInstance.AddressCached);
				}
			}
		});
	}

	private void HideAllAttachmentPoints()
	{
		if (attachmentPoints.Any((AttachmentPoint x) => x == null))
		{
			Debug.LogError(itemName + " has obsolete attachment points");
			return;
		}
		AttachmentPoint[] array = attachmentPoints;
		for (int num = 0; num < array.Length; num++)
		{
			array[num].Toggle(show: false);
		}
	}

	public void ToggleAttachmentPoints(AttachmentPointType typeToToggle, bool show)
	{
		AttachmentPoint[] array = attachmentPoints;
		foreach (AttachmentPoint attachmentPoint in array)
		{
			if ((attachmentPoint.AttachmentPointType & typeToToggle) == 0)
			{
				continue;
			}
			attachmentPoint.Toggle(show);
			if (show && attachmentPoint.HasVisuals)
			{
				if (attachmentPoint.transform.childCount > 1)
				{
					attachmentPoint.SetVisualizationColor(AttachmentPoint.inUseColor);
				}
				else if (IsAttachmentPointOverlappingWithSomething(attachmentPoint))
				{
					attachmentPoint.SetVisualizationColor(AttachmentPoint.invalidColor);
				}
				else
				{
					attachmentPoint.SetVisualizationColor(AttachmentPoint.validColor);
				}
			}
		}
	}

	private bool IsAttachmentPointOverlappingWithSomething(AttachmentPoint attachmentPoint)
	{
		if (attachmentPoint.colliders.Length != 1 || !(attachmentPoint.colliders[0] is BoxCollider { bounds: { center: var center, extents: var extents } }))
		{
			return false;
		}
		center.y -= extents.y;
		extents.y = 0.25f;
		center.y += extents.y;
		int num = Physics.OverlapBoxNonAlloc(center, extents, AttachmentPoint.PreAllocatedColliders, attachmentPoint.transform.rotation, AttachmentPoint.CollisionMask);
		for (int i = 0; i < num; i++)
		{
			if (!AttachmentPoint.PreAllocatedColliders[i].transform.CompareTag("AttachmentPointIndicator") && AttachmentPoint.PreAllocatedColliders[i].transform != base.transform)
			{
				ItemController component = AttachmentPoint.PreAllocatedColliders[i].GetComponent<ItemController>();
				if (component == null || (component.Item.type & ItemType.FlatDecoration) == 0)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void ToggleDirectionIndicators(bool show)
	{
		GameObject[] array = directionIndicators;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(show);
		}
	}

	public virtual void SetToParentPlaceableItem(IPlaceableItem parentPlaceableItem, AttachmentPoint attachmentPoint)
	{
		if (parentItemController != null && parentItemController.GetItemInstance() == parentPlaceableItem.GetItemInstance())
		{
			return;
		}
		if ((bool)parentItemController)
		{
			Debug.LogWarning("Item Controller " + base.gameObject.name + " already has a parent!", this);
			return;
		}
		parentItemController = parentPlaceableItem as ItemController;
		if (parentItemController == null)
		{
			Debug.LogWarning("Trying to set a non-ItemController as parent to " + base.gameObject.name, this);
			return;
		}
		parentAttachmentPoint = attachmentPoint;
		base.transform.SetParent(attachmentPoint.transform);
		if (ItemInstance != null && parentItemController.ItemInstance != null)
		{
			ItemInstance.parentId = parentItemController.ItemInstance.id;
		}
		parentItemController.childItemControllers.Add(this);
		foreach (ItemController childItemController in parentItemController.childItemControllers)
		{
			childItemController.OnParentNewChild(this);
		}
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			foreach (ItemController childItemController2 in parentItemController.childItemControllers)
			{
				InstanceBehavior<ItemWarningIconManager>.Instance.UpdateWarningIcon(childItemController2);
			}
		});
	}

	public virtual void RemoveFromParentPlaceableItem(bool updateWarningIcon = true)
	{
		if (parentItemController != null)
		{
			parentItemController.childItemControllers.Remove(this);
			foreach (ItemController childItemController in parentItemController.childItemControllers)
			{
				if (childItemController is SeatController seatController)
				{
					seatController.UpdateRestingAvailability();
				}
				if (updateWarningIcon)
				{
					InstanceBehavior<ItemWarningIconManager>.Instance.UpdateWarningIcon(childItemController);
				}
				childItemController.OnParentChildRemoved(this);
			}
			parentItemController = null;
		}
		parentAttachmentPoint = null;
		if (ItemInstance != null && ItemInstance.parentId != null && InstanceBehavior<BuildingManager>.Instance.buildingRegistration.itemInstances.TryGetValue(ItemInstance.parentId, out var value))
		{
			value.stackedItems.RemoveAll((AttachableChild x) => ItemInstance.id == x.childId);
			ItemInstance.parentId = null;
		}
		base.transform.SetParent(InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer);
	}

	protected virtual void OnParentNewChild(ItemController newChild)
	{
	}

	protected virtual void OnParentChildRemoved(ItemController removedChild)
	{
	}

	public virtual WarningIconType GetWarningIconType()
	{
		if (!ItemHelper.HasAnyMissingRequirements(ItemInstance))
		{
			return WarningIconType.None;
		}
		return WarningIconType.Danger;
	}

	public Transform GetTransform()
	{
		return base.transform;
	}

	public ItemInstance GetItemInstance()
	{
		return ItemInstance;
	}

	public Collider[] GetColliders()
	{
		return colliders;
	}

	public GroundIndicator[] GetGroundIndicators()
	{
		return groundIndicators;
	}

	public GameObject[] GetDirectionIndicators()
	{
		return directionIndicators;
	}

	public IPlaceableItem GetParentPlaceableItem()
	{
		return parentItemController;
	}

	public AttachmentPoint GetParentAttachmentPoint()
	{
		return parentAttachmentPoint;
	}

	public IPlaceableItem[] GetChildPlaceableItems()
	{
		return childItemControllers.Cast<IPlaceableItem>().ToArray();
	}

	public IEnumerable<AttachmentPoint> GetAvailableAttachmentPoints(AttachmentPointType type)
	{
		return attachmentPoints.Where((AttachmentPoint x) => (x.AttachmentPointType & type) != 0);
	}

	public IReadOnlyList<AttachmentPoint> GetAttachmentPoints()
	{
		return attachmentPoints;
	}

	public Transform GetAlignmentTransform()
	{
		return alignmentTransform;
	}

	public void Outline(Color32 outlineColor)
	{
		if (!base.IsOutlineSuppressed)
		{
			SetPermanentOutline(isOn: true);
			SetOutlineColor(outlineColor);
		}
	}

	public void RemoveOutline()
	{
		SetPermanentOutline(isOn: false);
		SetOutlineColor(InstanceBehavior<GlobalReferences>.Instance.colors.white);
	}

	public virtual void OnItemPositionUpdated()
	{
		InstanceBehavior<ItemWarningIconManager>.Instance.UpdateWarningIcon(this);
		if (Item.HasTag(TagRef.Itemtag.issecuritypanel))
		{
			ItemInstance.UpdateSecurityPanelCoverage();
		}
		if (!PlacementSystem.snapFreePlacement)
		{
			InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.ObjectSnap, base.transform.position);
		}
	}

	public void OnItemRotationUpdated()
	{
	}

	public virtual void OnItemPlacementFinished()
	{
		randomAvailableNavMeshPositionGetter?.Reset();
		CoroutineUtility.RunAfterFrameDelay(UpdateNavMeshTargets, 3);
		if (BuildingManager.isBuildingTemporarilyEditable)
		{
			return;
		}
		if (ItemInstance.cargoInstances.Count > 0 && ItemInstance.ItemCached.itemsThatCanShowcase.Length == 1 && string.IsNullOrEmpty(ItemInstance.GetStockInstance().itemName) && ((ItemInstance.ItemCached.type & ItemType.PointOfSale) == 0 || BuildingHelper.CustomersNeedPaperBagsInCurrentBuilding()))
		{
			CargoInstance stockInstance = ItemInstance.GetStockInstance();
			stockInstance.itemName = ItemInstance.ItemCached.itemsThatCanShowcase[0];
			stockInstance.ResetItemCached();
			ItemInstance.FillUpShowcaseShelfOrPointOfSale();
			GameEvent.Invoke("ba:gameevent_itemstockedup");
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(ItemInstance.AddressCached);
			if (buildingRegistration != null)
			{
				CustomerEntriesHelper.UpdateCustomerEntriesForPlayerBusiness(buildingRegistration, TimeHelper.GetDayOfWeek());
			}
		}
		if (this is SeatController seatController)
		{
			seatController.UpdateRestingAvailability();
		}
		if (parentItemController != null)
		{
			foreach (ItemController childItemController in parentItemController.childItemControllers)
			{
				if (childItemController is SeatController seatController2)
				{
					seatController2.UpdateRestingAvailability();
				}
			}
		}
		ItemInstance.dirtSpotsThatAffects = InstanceBehavior<BuildingManager>.Instance.GetDirtAffectedCells(this);
		foreach (ItemController childItemController2 in childItemControllers)
		{
			if (childItemController2.ItemInstance != null)
			{
				childItemController2.ItemInstance.dirtSpotsThatAffects = InstanceBehavior<BuildingManager>.Instance.GetDirtAffectedCells(childItemController2);
				if (childItemController2 is SeatController seatController3)
				{
					seatController3.UpdateRestingAvailability();
				}
			}
		}
		if (ItemInstance.ItemCached.hasWaitingLine)
		{
			GetComponent<IWaitingLineHolder>().GetWaitingLine().creator?.Reset();
		}
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			foreach (ItemController item in InstanceBehavior<BuildingManager>.Instance.allItemControllers.Where((ItemController x) => x is CashRegisterController))
			{
				CashRegisterController cashRegisterController = (CashRegisterController)item;
				if (childItemControllers.Contains(item))
				{
					cashRegisterController.GetWaitingLine().creator?.Reset();
				}
				else
				{
					cashRegisterController.GetWaitingLine().creator?.SetUpWaitingLine();
				}
			}
			InstanceBehavior<ItemWarningIconManager>.Instance.UpdateWarningIcon(this);
			foreach (ItemController childItemController3 in childItemControllers)
			{
				InstanceBehavior<ItemWarningIconManager>.Instance.UpdateWarningIcon(childItemController3);
			}
		});
		GameEvent.Invoke("ba:gameevent_itemdropped");
		Physics.SyncTransforms();
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			GlobalEvents.onItemDropped?.Invoke(this);
		});
		if (InstanceBehavior<BuildingManager>.Instance.buildingRegistration.HasValidAddress)
		{
			if (_item.HasTag(TagRef.Itemtag.issecuritypanel))
			{
				ItemInstance.UpdateSecurityPanelCoverage();
			}
			BusinessSecurityHelper.UpdateCamerasCoverage(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address);
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.UpdateSecurityLevel();
			CustomerDemandHelper.ReloadCachedFulfilled(InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
		}
	}

	public override void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			if (ItemInstance != null)
			{
				ItemInstance itemInstance = ItemInstance;
				itemInstance.onInstanceRemoved = (Action)Delegate.Remove(itemInstance.onInstanceRemoved, new Action(OnInstanceRemoved));
			}
			base.OnDestroy();
		}
	}

	[ConsoleMethod("SetItemForSale", "Sets the current item in hand for sale", new string[] { })]
	public static void Command_SetShelfItem()
	{
		ItemController itemController = PlacementSystem.CurrentPlaceableItemBeingPlaced as ItemController;
		if (itemController != null)
		{
			Command_SetShelfItem(itemController.itemName, 1);
		}
		else
		{
			Debug.LogError("Couldn't find an Item Controller. Ensure you have one in hand and are in placement mode");
		}
	}

	[ConsoleMethod("SetAllItemsForSale", "Sets all items in building for sale", new string[] { })]
	public static void Command_SetAllItemsForSale()
	{
		foreach (ItemController item in InstanceBehavior<BuildingManager>.Instance.allItemControllers.Where(delegate(ItemController x)
		{
			PlayerItemPurchaserSettings playerItemPurchaserSettings = x.playerItemPurchaserSettings;
			return playerItemPurchaserSettings != null && !playerItemPurchaserSettings.enabled;
		}))
		{
			if (item != null)
			{
				Command_SetShelfItem(item.itemName, 1, item);
			}
		}
	}

	[ConsoleMethod("SetItemForSale", "Sets the item for sale to the item in hand", new string[] { }, AutoCompleteMap = new string[] { "shelfItemName=Items" })]
	public static void Command_SetShelfItem(string shelfItemName)
	{
		Command_SetShelfItem(shelfItemName, 1);
	}

	[ConsoleMethod("SetItemForWholeSale", "Sets the item for whole sale to the item in hand", new string[] { }, AutoCompleteMap = new string[] { "shelfItemName=Items" })]
	public static void Command_SetShelfItemAsBox(string shelfItemName)
	{
		Command_SetShelfItem(shelfItemName, 99);
	}

	private static void Command_SetShelfItem(string shelfItemName, int quantity, ItemController itemController = null)
	{
		ItemController itemController2 = ((itemController != null) ? itemController : (PlacementSystem.CurrentPlaceableItemBeingPlaced as ItemController));
		if (itemController2 != null)
		{
			bool flag = !string.IsNullOrEmpty(shelfItemName);
			itemController2.playerItemPurchaserSettings.itemName = shelfItemName;
			itemController2.playerItemPurchaserSettings.enabled = flag;
			itemController2.playerItemPurchaserSettings.itemQuantity = quantity;
			if (shelfItemName != itemController2.itemName)
			{
				if (itemController2 is ShelfController shelfController)
				{
					shelfController.ShowItemVisuals(shelfItemName);
				}
				Transform transform = itemController2.transform.Find("ForSaleVisuals");
				if (transform != null)
				{
					transform.gameObject.SetActive(value: false);
				}
				Debug.Log("Item " + itemController2.itemName + ((!flag) ? " no longer has items in the shelf" : ((quantity > 1) ? (" has now a box of " + shelfItemName + " for sale!") : (" is now selling " + shelfItemName + "!"))));
			}
			else
			{
				Transform transform2 = itemController2.transform.Find("ForSaleVisuals");
				if (transform2 != null)
				{
					transform2.gameObject.SetActive(value: true);
				}
				Debug.Log("Item " + itemController2.itemName + " is now on sale!");
			}
			ItemController itemController3 = itemController2;
			if (itemController3._playerItemPurchaser == null)
			{
				itemController3._playerItemPurchaser = new PlayerItemPurchaser
				{
					itemController = itemController2
				};
			}
			itemController2._playerItemPurchaser.UpdatePriceInfo();
		}
		else
		{
			Debug.LogError("Couldn't find an Item Controller. Ensure you have one in hand and are in placement mode");
		}
	}

	[ConsoleMethod("UnsetItemForSale", "Unsets the current item in hand for sale", new string[] { })]
	public static void Command_UnsetShelfItem()
	{
		ItemController itemController = PlacementSystem.CurrentPlaceableItemBeingPlaced as ItemController;
		if (itemController != null)
		{
			string text = itemController.playerItemPurchaserSettings.itemName;
			itemController.playerItemPurchaserSettings.itemName = null;
			itemController.playerItemPurchaserSettings.enabled = false;
			itemController.playerItemPurchaserSettings.itemQuantity = 1;
			if (text != itemController.itemName && itemController is ShelfController shelfController)
			{
				shelfController.ToggleItemVisuals(toggle: false);
			}
			Transform transform = itemController.transform.Find("ForSaleVisuals");
			if (transform != null)
			{
				transform.gameObject.SetActive(value: true);
			}
			Debug.Log("Item " + itemController.itemName + " is now not on sale!");
			itemController._playerItemPurchaser = null;
		}
		else
		{
			Debug.LogError("Couldn't find an Item Controller. Ensure you have one in hand and are in placement mode");
		}
	}

	[ConsoleMethod("SetCustomValue", "Sets the Item custom value to the item in hand", new string[] { })]
	public static void Command_SetCustomValue(string customValue)
	{
		ItemController itemController = PlacementSystem.CurrentPlaceableItemBeingPlaced as ItemController;
		if ((bool)itemController)
		{
			itemController.customValue = customValue;
			Debug.Log("Custom value '" + customValue + "' set to item " + itemController.itemName + "!");
		}
		else
		{
			Debug.LogError("Couldn't find an Item Controller. Ensure you have one in hand and are in placement mode");
		}
	}

	[ConsoleMethod("GetCustomValue", "Shows the Item custom value of the item in hand", new string[] { })]
	public static void Command_GetCustomValue()
	{
		ItemController itemController = PlacementSystem.CurrentPlaceableItemBeingPlaced as ItemController;
		if ((bool)itemController)
		{
			Debug.Log("Custom value " + itemController.itemName + ": '" + itemController.customValue + "'");
		}
		else
		{
			Debug.LogError("Couldn't find an Item Controller. Ensure you have one in hand and are in placement mode");
		}
	}

	[ConsoleMethod("SetDiplomaNameAsCustomValue", "Sets a diploma name as a custom value to the item in hand", new string[] { })]
	public static void Command_DiplomaNameAsCustomValue(DiplomaName customValue)
	{
		ItemController itemController = PlacementSystem.CurrentPlaceableItemBeingPlaced as ItemController;
		if ((bool)itemController)
		{
			itemController.customValue = customValue.ToStringFast();
			Debug.Log($"Custom value '{customValue}' set to item {itemController.itemName}!");
		}
		else
		{
			Debug.LogError("Couldn't find an Item Controller. Ensure you have one in hand and are in placement mode");
		}
	}

	[ConsoleMethod("ForceDiscardItemInHand", "Useful for items that can't be discarded", new string[] { })]
	public static void Command_ForceDiscardItemInHand()
	{
		PlayerAction.Confirm.Reset();
		if (InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.selectedItemInstance != null)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.ClickDiscard();
		}
		else
		{
			Debug.LogError("Couldn't find an Item in hand");
		}
	}

	[ConsoleMethod("CopyColors", "Copies the colors of the current item in hand", new string[] { })]
	public static void Command_CopyColors()
	{
		ItemController itemController = PlacementSystem.CurrentPlaceableItemBeingPlaced as ItemController;
		if ((bool)itemController)
		{
			SavedCustomColors = itemController.customColors.ToList();
			Debug.Log("Custom colors from " + itemController.itemName + " copied!");
		}
		else
		{
			Debug.LogError("Couldn't find an Item Controller. Ensure you have one in hand and are in placement mode");
		}
	}

	[ConsoleMethod("PasteColors", "Pastes the copied colors from CopyColors command to the current item in hand", new string[] { })]
	public static void Command_PasteColors()
	{
		if (SavedCustomColors == null)
		{
			Debug.LogError("No colors copied, use 'CopyColors' command first!");
			return;
		}
		ItemController itemController = PlacementSystem.CurrentPlaceableItemBeingPlaced as ItemController;
		if ((bool)itemController)
		{
			itemController.SetCustomColors(SavedCustomColors.ToList());
			if (itemController.ItemInstance != null)
			{
				itemController.ItemInstance.customColors = itemController.customColors;
			}
			Debug.Log("Custom colors pasted to " + itemController.itemName + "!");
		}
		else
		{
			Debug.LogError("Couldn't find an Item Controller. Ensure you have one in hand and are in placement mode");
		}
	}

	public void UpdateDebugShadowCasterOverlay()
	{
		base.gameObject.SetLayerRecursively(LayerHelper.InteractiveItemsLayerIndex);
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>();
		foreach (Renderer obj in componentsInChildren)
		{
			bool flag = obj.shadowCastingMode != ShadowCastingMode.Off;
			Material[] materials = obj.materials;
			for (int j = 0; j < materials.Length; j++)
			{
				materials[j].SetFloat(IsShadowCaster, flag ? 1f : 0f);
			}
		}
		componentsInChildren = renderers;
		foreach (Renderer obj2 in componentsInChildren)
		{
			bool flag2 = obj2.shadowCastingMode != ShadowCastingMode.Off;
			Material[] materials = obj2.materials;
			for (int j = 0; j < materials.Length; j++)
			{
				materials[j].SetFloat(IsShadowCaster, flag2 ? 1f : 0f);
			}
		}
	}
}
