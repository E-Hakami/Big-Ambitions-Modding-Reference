using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InputSystem;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.InteriorElements;
using BigAmbitions.InteriorDesigner.Tools;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using BigAmbitions.SoundSystem;
using BigAmbitions.Tags;
using BusinessLayoutSets;
using Controllers;
using Extensions;
using Helpers;
using InteriorDesign;
using Items.SpecialItems;
using JimmysUnityUtilities;
using Localizor;
using NaughtyAttributes;
using Player.HUD.ItemInfoOverlays;
using Streets;
using UI.InteriorDesigner;
using UI.Notification;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Buildings.Indoors.InteriorDesign;

public class InteriorDesignerController : MonoBehaviour
{
	public static Action onListsUpdated;

	public static readonly List<ItemController> ItemControllersCache = new List<ItemController>();

	public static readonly List<VehicleController> VehicleControllersCache = new List<VehicleController>();

	public static readonly List<ItemController> SecurityItemsCache = new List<ItemController>();

	public static readonly List<int> SoldItemIndexes = new List<int>();

	private static readonly List<ItemController> PointOfSalesCache = new List<ItemController>();

	private static readonly List<ItemController> ProducersCache = new List<ItemController>();

	private static readonly List<ItemController> SecurityPanelItemsCache = new List<ItemController>();

	private static readonly Dictionary<ToolName, ToolSetup> ToolsCache = new Dictionary<ToolName, ToolSetup>();

	private static Camera _mainCamera;

	[BoxGroup("General")]
	[SerializeField]
	private float highlightFadeOutSecondsDuration = 0.7f;

	[BoxGroup("General")]
	[SerializeField]
	private MoneyAmountOverlay moneyAmountOverlay;

	[BoxGroup("Queue")]
	[SerializeField]
	private QueueOverlay queueOverlay;

	[BoxGroup("Queue")]
	[SerializeField]
	private QueueActionPanelUi queueActionPanelUi;

	[BoxGroup("Colors")]
	[SerializeField]
	private ColorOverlay colorOverlay;

	[BoxGroup("Colors")]
	[SerializeField]
	private EyedropperColorOverlay eyedropperColorOverlay;

	[BoxGroup("InteriorElements")]
	[SerializeField]
	private InteriorElementActionPanelUI interiorElementActionPanelUI;

	[BoxGroup("Producer")]
	[SerializeField]
	private ProducerOverlay producerOverlay;

	[BoxGroup("Producer")]
	[SerializeField]
	private ProducerActionPanelUi producerActionPanelUi;

	[BoxGroup("Furniture")]
	[SerializeField]
	private FurnitureActionPanelUi furnitureActionPanelUi;

	[BoxGroup("Package")]
	[SerializeField]
	private PackageOverlay packageOverlay;

	private readonly List<ToolSetup> _toolSetups = new List<ToolSetup>
	{
		new SellToolSetup(),
		new SaveBlueprintToolSetup(),
		new TimeOfDayToolSetup(),
		new SecurityToolSetup(),
		new HandToolSetup(),
		new ProducerToolSetup(),
		new QueueToolSetup(),
		new PaletteToolSetup(),
		new EyedropperToolSetup(),
		new FurnitureToolSetup(),
		new FloorToolSetup(),
		new WallToolSetup(),
		new DuplicateToolSetup(),
		new PackageToolSetup()
	};

	private static readonly RaycastHit[] HitResults = new RaycastHit[16];

	private EmployeeStationController _employeeStation;

	private InteriorDesignerUI _interiorDesignerUI;

	public static ToolName CurrentToolName { get; private set; }

	public static double BlueprintTotalCost { get; private set; }

	public static float InteriorScorePercentage { get; private set; }

	private static double PlayerBalanceAfterChanges { get; set; }

	public static IInteriorDesignerTool CurrentTool
	{
		get
		{
			if (CurrentToolName == ToolName.None)
			{
				return null;
			}
			return ToolsCache[CurrentToolName].Tool;
		}
	}

	public static List<ItemController> GetSecurityPanelItemControllers => SecurityPanelItemsCache?.Where((ItemController x) => x.gameObject.activeSelf).ToList();

	public static List<ItemController> GetProducerItemControllers => ProducersCache?.Where((ItemController x) => x.gameObject.activeSelf).ToList();

	public static List<ItemController> GetQueueItemControllers => PointOfSalesCache?.Where((ItemController x) => x.gameObject.activeSelf).ToList();

	private void Start()
	{
		_mainCamera = Camera.main;
	}

	private void OnEnable()
	{
		BusinessSecurityHelper.onCamerasCoverageUpdated = (Action)Delegate.Combine(BusinessSecurityHelper.onCamerasCoverageUpdated, new Action(OnCamerasCoverageUpdated));
	}

	private void OnDisable()
	{
		BusinessSecurityHelper.onCamerasCoverageUpdated = (Action)Delegate.Remove(BusinessSecurityHelper.onCamerasCoverageUpdated, new Action(OnCamerasCoverageUpdated));
	}

	public static T GetTool<T>(ToolName toolName) where T : IInteriorDesignerTool
	{
		if (!ToolsCache.TryGetValue(toolName, out var value))
		{
			return default(T);
		}
		return (T)value.Tool;
	}

	public static IInteriorDesignerTool GetTool(ToolName toolName)
	{
		if (!ToolsCache.TryGetValue(toolName, out var value))
		{
			return null;
		}
		return value.Tool;
	}

	private void Awake()
	{
		_interiorDesignerUI = UnityEngine.Object.FindObjectOfType<InteriorDesignerUI>();
		SetUpInteriorElementTools();
		foreach (ToolSetup toolSetup in _toolSetups)
		{
			var (actionPanel, overlay) = GetActionPanelAndOverlay(toolSetup.ToolName);
			toolSetup.Setup(actionPanel, overlay);
			ToolsCache[toolSetup.ToolName] = toolSetup;
		}
		IRevertibleAction.tryChangeBalance = TryChangeBalance;
		IRevertibleAction.changeBalance = ChangeBalance;
		IInteriorDesignerTool.getSelectedItemControllerIndex = GetSelectedItemControllerIndex;
		IInteriorDesignerTool.getSelectedVehicleControllerIndex = GetSelectedVehicleControllerIndex;
		IInteriorDesignerTool.getItemTransformAtIndex = GetItemTransformAtIndex;
		IInteriorDesignerTool.showErrorNotification = delegate(string str)
		{
			Notifications.ShowError(str, null, trackOnSaveGame: false);
		};
		IInteriorDesignerTool.showErrorNotificationWithItem = ShowErrorWithItem;
		IInteriorDesignerTool.showErrorNotificationWithVehicle = ShowErrorNotificationWithVehicle;
		IInteriorDesignerTool.executeActionThroughCode = _interiorDesignerUI.ExecuteRevertibleAction;
		IInteriorDesignerTool.hitUi = () => EventSystem.current.IsPointerOverGameObject() || InteriorDesignerUI.wasClickingUI;
		IInteriorDesignerTool.openTool = _interiorDesignerUI.ToggleTool;
		IInteriorDesignerTool.showMoneyAmountOverlay = delegate(Transform elementTransform, float price)
		{
			moneyAmountOverlay.Open(elementTransform, price);
		};
		IInteriorDesignerTool.hideMoneyAmountOverlay = delegate
		{
			moneyAmountOverlay.Close();
		};
		IInteriorDesignerTool.moveItemWithHandTool = StartMovingItem;
		IInteriorDesignerTool.setPositionRotationAndAttachment = delegate(int itemIndex, Vector3 position, SerializableQuaternion rotation, float yRotation, int parentItemIndex, AttachmentPoint attachmentPoint)
		{
			SetPositionRotationAndAttachment(itemIndex, position, rotation, yRotation, parentItemIndex, attachmentPoint, animate: true);
		};
		IInteriorDesignerTool.setPositionRotationAndAttachmentWithoutAnimation = delegate(int itemIndex, Vector3 position, SerializableQuaternion rotation, float yRotation, int parentItemIndex, AttachmentPoint attachmentPoint)
		{
			SetPositionRotationAndAttachment(itemIndex, position, rotation, yRotation, parentItemIndex, attachmentPoint, animate: false);
		};
		IInteriorDesignerTool.getInteriorElementClicked = GetInteriorElementClicked;
		IInteriorDesignerTool.hideCreatedItem = HideCreatedItem;
		IInteriorDesignerTool.updateCustomerCapacityPanel = delegate
		{
			InteriorDesignerInfoPanelEvents.onUpdateCustomerCapacity?.Invoke();
		};
		IInteriorDesignerTool.showOverlays = ShowOverlays;
		IInteriorDesignerTool.hideOverlays = HideOverlays;
		IInteriorDesignerTool.setAutoFillShowcaseShelvesPause = delegate(bool pause)
		{
			ItemHelper.PauseAutoFillShelves = pause;
		};
		IInteriorDesignerTool.isItemIndexValid = (int index) => index >= 0 && index < ItemControllersCache.Count;
		IInteriorDesignerTool.getDefaultItemPosition = () => InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer.position;
		ActionPanelUI.focusOnItem = FocusOnItem;
	}

	public static void OnOpen()
	{
		PlayerBalanceAfterChanges = ((SaveGameManager.Current == null) ? double.MaxValue : ((double)Mathf.Floor(SaveGameManager.Current.Money)));
		if (InteriorDesignerHelper.BlueprintCreatorMode)
		{
			BlueprintTotalCost = BusinessLayoutSetHelper.Collect(collectDirtSpots: false).Items.Sum((BusinessLayoutSets.Item item) => ItemHelper.GetDefaultMarketPrice(item.itemName));
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				InteriorDesignerInfoPanelEvents.onBlueprintCostChanged?.Invoke(BlueprintTotalCost);
			});
		}
		UpdateInteriorScore();
		ItemControllersCache.Clear();
		VehicleControllersCache.Clear();
		SecurityItemsCache.Clear();
		SecurityPanelItemsCache.Clear();
		ProducersCache.Clear();
		PointOfSalesCache.Clear();
		SoldItemIndexes.Clear();
		foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			ItemControllersCache.Add(allItemController);
			AddItemToLists(allItemController, sort: false);
		}
		foreach (VehicleController allPlayerVehicle in VehicleHelper.AllPlayerVehicles)
		{
			if (allPlayerVehicle.vehicleInstance.Address == InstanceBehavior<BuildingManager>.Instance.building.Address)
			{
				VehicleControllersCache.Add(allPlayerVehicle);
			}
		}
		ProducersCache.Sort(delegate(ItemController x, ItemController y)
		{
			int num = x.ItemInstance.isSecured.CompareTo(y.ItemInstance.isSecured);
			return (num == 0) ? string.CompareOrdinal(x.itemName, y.itemName) : num;
		});
	}

	public static void SetTool(ToolName tool)
	{
		CurrentToolName = tool;
	}

	private static bool TryChangeBalance(float amount)
	{
		if (PlayerBalanceAfterChanges + (double)amount < 0.0 && !InteriorDesignerHelper.BlueprintCreatorMode)
		{
			Notifications.ShowInsufficientMoney();
			return false;
		}
		ChangeBalance(amount);
		return true;
	}

	private static void ChangeBalance(float amount)
	{
		if (InteriorDesignerHelper.BlueprintCreatorMode)
		{
			BlueprintTotalCost -= amount;
			InteriorDesignerInfoPanelEvents.onBlueprintCostChanged?.Invoke(BlueprintTotalCost);
			return;
		}
		PlayerBalanceAfterChanges += amount;
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			InteriorDesignerInfoPanelEvents.onPlayerBalanceChanged?.Invoke(PlayerBalanceAfterChanges);
			InteriorDesignerInfoPanelEvents.onCostBalanceChanged?.Invoke(PlayerBalanceAfterChanges - (double)SaveGameManager.Current.Money);
		});
	}

	public static void UpdateInteriorScore()
	{
		if (BuildingTypeHelper.GetData(InstanceBehavior<BuildingManager>.Instance.building).HasTag(TagRef.Buildingtypetag.hasinteriorscore))
		{
			InteriorScorePercentage = InteriorScoreCalculator.GetInteriorScorePercentage(InteriorElementsHelper.InteriorElementsCache.Select((KeyValuePair<string, InteriorElement> x) => x.Value.Serialize()).ToList());
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				InteriorDesignerInfoPanelEvents.onUpdateInteriorScore?.Invoke(InteriorScorePercentage);
			});
		}
	}

	public static void AddItemToLists(ItemController itemController, bool sort = true, bool notify = true)
	{
		if (itemController.Item.isProducer && (itemController.Item.type & ItemType.ShowcaseShelf) != 0)
		{
			ProducersCache.Add(itemController);
			SecurityPanelItemsCache.Add(itemController);
		}
		else if (itemController is ItemWithTextController || itemController is FactoryAssemblyMachineController || itemController.StockTypes.Count > 1)
		{
			ProducersCache.Add(itemController);
		}
		if ((itemController.Item.type & ItemType.Security) != 0)
		{
			SecurityItemsCache.Add(itemController);
		}
		if (itemController.Item.hasWaitingLine)
		{
			PointOfSalesCache.Add(itemController);
		}
		if (sort)
		{
			ProducersCache.Sort(delegate(ItemController x, ItemController y)
			{
				int num = x.ItemInstance.isSecured.CompareTo(y.ItemInstance.isSecured);
				return (num == 0) ? string.CompareOrdinal(x.itemName, y.itemName) : num;
			});
		}
		if (notify)
		{
			onListsUpdated?.Invoke();
		}
	}

	public static void RemoveItemFromLists(ItemController itemController, bool notify = true)
	{
		if (itemController.Item.isProducer && (itemController.Item.type & ItemType.ShowcaseShelf) != 0)
		{
			ProducersCache.Remove(itemController);
			SecurityPanelItemsCache.Remove(itemController);
		}
		if (itemController is ItemWithTextController || itemController is FactoryAssemblyMachineController || itemController.StockTypes.Count > 1)
		{
			ProducersCache.Remove(itemController);
		}
		if ((itemController.Item.type & ItemType.Security) != 0)
		{
			SecurityItemsCache.Remove(itemController);
		}
		if (itemController.Item.hasWaitingLine)
		{
			PointOfSalesCache.Remove(itemController);
		}
		if (notify)
		{
			onListsUpdated?.Invoke();
		}
	}

	public static void HandleOnClose()
	{
		if (SaveGameManager.Current != null)
		{
			double num = PlayerBalanceAfterChanges - (double)SaveGameManager.Current.Money;
			if (num >= 1.0 || num <= -1.0)
			{
				BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
				Dictionary<string, string> data = new Dictionary<string, string>();
				bool num2 = BusinessHelper.IsTaxDeductibleBusinessServiceBuilding(buildingRegistration);
				TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_interiordesigner", data);
				if (num2)
				{
					string taxDeductibleName = (string.IsNullOrEmpty(buildingRegistration.BusinessName) ? buildingRegistration.Address.ToFormattedString() : buildingRegistration.BusinessName);
					transactionInfo.SetTaxDeductibleName(taxDeductibleName);
				}
				GameManager.ChangeMoneySafe((float)num, transactionInfo);
				SaveGameManager.Current.achievementsData.totalInteriorDesignerCost += (float)num;
				GameEvent.Invoke("ba:gameevent_interiorelementschanged");
			}
		}
		if (SoldItemIndexes.Count > 0)
		{
			List<ItemController> list = new List<ItemController>();
			foreach (int soldItemIndex in SoldItemIndexes)
			{
				list.Add(ItemControllersCache[soldItemIndex]);
			}
			list.Sort(delegate(ItemController a, ItemController b)
			{
				bool value = a.parentItemController != null;
				return (b.parentItemController != null).CompareTo(value);
			});
			foreach (ItemController item in list)
			{
				item.RemoveFromParentPlaceableItem(updateWarningIcon: false);
				if (item.ItemInstance.streetNumber != -1)
				{
					item.ItemInstance.RemoveFromWorkShifts(item.ItemInstance.AddressCached);
				}
				UnityEngine.Object.Destroy(item.gameObject);
			}
			SoldItemIndexes.Clear();
		}
		InstanceBehavior<BuildingManager>.Instance.OnItemChanged(forced: true);
	}

	private static int GetSelectedItemControllerIndex()
	{
		ItemController clickedComponent = InputHelper.GetClickedComponent<ItemController>(LayerHelper.interactiveItemsAndOutlinedLayerMask, includeParent: true);
		if (!(clickedComponent == null))
		{
			return ItemControllersCache.IndexOf(clickedComponent);
		}
		return -1;
	}

	private static int GetSelectedVehicleControllerIndex()
	{
		VehicleController clickedComponent = InputHelper.GetClickedComponent<VehicleController>(LayerHelper.vehiclesAndInteractiveOutlineMask, includeParent: true);
		if (!(clickedComponent == null))
		{
			return VehicleControllersCache.IndexOf(clickedComponent);
		}
		return -1;
	}

	private static Transform GetItemTransformAtIndex(int index)
	{
		if (index != -1)
		{
			return ItemControllersCache[index]?.transform;
		}
		return null;
	}

	private static void ShowErrorWithItem(string key, int itemIndex)
	{
		Dictionary<string, string> notificationData = new Dictionary<string, string> { 
		{
			"itemname",
			ItemControllersCache[itemIndex].itemName.GetLocalization()
		} };
		Notifications.Show(NotificationType.Error, key, notificationData, 4f, null, null, notificationSound: true, trackOnSaveGame: false);
	}

	private static void ShowErrorNotificationWithVehicle(string key, int vehicleIndex)
	{
		Dictionary<string, string> notificationData = new Dictionary<string, string> { 
		{
			"itemname",
			VehicleControllersCache[vehicleIndex].vehicleInstance.vehicleTypeName
		} };
		Notifications.Show(NotificationType.Error, key, notificationData, 4f, null, null, notificationSound: true, trackOnSaveGame: false);
	}

	private static void DisableHighlights(bool disable)
	{
		foreach (ItemController item in ItemControllersCache)
		{
			item.blockOutline = disable;
		}
	}

	private static (string interiorElementId, int materialIndex) GetInteriorElementClicked(int layerMask)
	{
		if (!_mainCamera)
		{
			_mainCamera = Camera.main;
		}
		if (!_mainCamera)
		{
			return (interiorElementId: null, materialIndex: -1);
		}
		if (!TryGetInteriorElementClicked(layerMask, out var element, out var hit))
		{
			return (interiorElementId: null, materialIndex: -1);
		}
		int subMeshIndexFromRaycastHit = element.GetSubMeshIndexFromRaycastHit(hit, validDataOnly: true);
		return (interiorElementId: element.UUID, materialIndex: subMeshIndexFromRaycastHit);
	}

	private static bool TryGetInteriorElementClicked(int layerMask, out InteriorElement element, out RaycastHit hit)
	{
		element = null;
		hit = default(RaycastHit);
		int num = Physics.RaycastNonAlloc(_mainCamera.ScreenPointToRay(InputActionHelper.GetCursorPosition()), HitResults, 600f, layerMask);
		if (num == 0)
		{
			return false;
		}
		hit.distance = float.MaxValue;
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = HitResults[i];
			if (!(raycastHit.distance >= hit.distance) && raycastHit.transform.TryGetComponent<InteriorElement>(out var component) && component.elementRenderer.enabled)
			{
				element = component;
				hit = raycastHit;
			}
		}
		return element != null;
	}

	private static void SetPositionRotationAndAttachment(int itemIndex, Vector3 position, SerializableQuaternion rotation, float yRotation, int parentItemIndex, AttachmentPoint attachmentPoint, bool animate)
	{
		ItemController itemController = ItemControllersCache[itemIndex];
		ItemController parentPlaceableItem = ((parentItemIndex == -1) ? null : ItemControllersCache[parentItemIndex]);
		PlacementSystem.SetItemPosition(itemController, position, rotation, yRotation, parentPlaceableItem, attachmentPoint, animate, delegate(IPlaceableItem item)
		{
			item.OnItemPlacementFinished();
		});
		BusinessSecurityHelper.UpdateCamerasCoverage();
		InstanceBehavior<SfxManager>.Instance.PlayAudio(itemController.Item.customPlaceSound ? itemController.Item.placeSoundType : SoundType.ObjectPutDown, itemController.transform.position);
	}

	private void FocusOnItem(ItemController itemController)
	{
		MultipleHeightsBuildingController multipleHeightsBuildingController = InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController;
		if (multipleHeightsBuildingController != null)
		{
			int itemHeightIndex = multipleHeightsBuildingController.GetItemHeightIndex(itemController);
			if (itemHeightIndex != multipleHeightsBuildingController.currentHeightIndex)
			{
				_interiorDesignerUI.ChangeFloor(itemHeightIndex);
			}
		}
		InteriorDesignerHelper.PlacementCam.FocusOnPosition(itemController.transform.position);
		itemController.HighlightAndFadeOut(highlightFadeOutSecondsDuration);
	}

	private static void OnCamerasCoverageUpdated()
	{
		SecurityActionPanelUi.onCamerasCoverageUpdated?.Invoke();
	}

	private void StartMovingItem(int itemIndex, Func<HandRevertibleAction, IRevertibleAction> alternativeRevertibleAction = null, Action onCancel = null)
	{
		_interiorDesignerUI.ToggleTool(ToolName.Hand);
		HandTool obj = (HandTool)CurrentTool;
		obj.alternativeRevertibleAction = alternativeRevertibleAction;
		obj.SelectItem(ItemControllersCache.IndexOf(ItemControllersCache[itemIndex]), onCancel);
	}

	private static void HideCreatedItem(int itemIndex, bool hide, bool isChild, bool animate)
	{
		ItemController itemController = ItemControllersCache[itemIndex];
		if (hide)
		{
			if (!isChild && itemController.gameObject.activeSelf)
			{
				if (animate)
				{
					itemController.gameObject.DisableWithScaleAnim(itemController.Colliders);
				}
				else
				{
					itemController.gameObject.SetActive(value: false);
				}
			}
			SoldItemIndexes.Add(itemIndex);
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RemoveItemInstanceFromBuilding(itemController.ItemInstance, triggerAction: false);
			RemoveItemFromLists(itemController);
			InstanceBehavior<BuildingManager>.Instance.allItemControllers.Remove(itemController);
			return;
		}
		if (!isChild && !itemController.gameObject.activeSelf)
		{
			if (animate)
			{
				itemController.gameObject.EnableWithScaleAnim(itemController.Colliders);
			}
			else
			{
				itemController.gameObject.SetActive(value: true);
			}
		}
		SoldItemIndexes.Remove(itemIndex);
		InstanceBehavior<BuildingManager>.Instance.buildingRegistration.AddItemInstanceToBuilding(itemController.ItemInstance);
		InstanceBehavior<BuildingManager>.Instance.allItemControllers.Add(itemController);
		AddItemToLists(itemController);
	}

	private void ShowOverlays()
	{
		InstanceBehavior<OverlayManager>.Instance.EnableOverlays();
		InstanceBehavior<OverlayManager>.Instance.ctaDisabled = true;
	}

	private void HideOverlays()
	{
		InstanceBehavior<OverlayManager>.Instance.DisableOverlays();
		InstanceBehavior<OverlayManager>.Instance.ctaDisabled = false;
	}

	private void SetUpInteriorElementTools()
	{
		InteriorElementTool.onToolStop = MouseController.Reset;
		InteriorElementTool.onHoverElement = delegate
		{
			MouseController.SetCursor(new InteriorElementPaintCursorChangeEvent
			{
				ChangedCursor = true
			});
		};
		InteriorElementTool.onUnHoverElement = delegate
		{
			MouseController.SetCursor(null);
		};
		InteriorElementTool.disableHighlights = DisableHighlights;
		InteriorElementTool.selectButtonByPresetId = interiorElementActionPanelUI.SelectButtonByPresetId;
		InteriorElementTool.isInWarehouse = () => InstanceBehavior<BuildingManager>.Instance.building.BuildingType == "ba:buildingtype_warehouse";
		InteriorDesignerUI.OnUndoRedo.AddListener(delegate
		{
			if (CurrentToolName == ToolName.Floor)
			{
				GetTool<InteriorElementTool>(ToolName.Floor).UnHoverCurrentInteriorElement();
			}
			else if (CurrentToolName == ToolName.Wall)
			{
				GetTool<InteriorElementTool>(ToolName.Wall).UnHoverCurrentInteriorElement();
			}
		});
	}

	private (ActionPanelUI, MonoBehaviour) GetActionPanelAndOverlay(ToolName toolName)
	{
		return toolName switch
		{
			ToolName.Queue => (queueActionPanelUi, queueOverlay), 
			ToolName.Palette => (null, colorOverlay), 
			ToolName.Eyedropper => (null, eyedropperColorOverlay), 
			ToolName.Producer => (producerActionPanelUi, producerOverlay), 
			ToolName.Furniture => (furnitureActionPanelUi, null), 
			ToolName.Package => (null, packageOverlay), 
			_ => (null, null), 
		};
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		ItemControllersCache.Clear();
		VehicleControllersCache.Clear();
		SecurityItemsCache.Clear();
		SoldItemIndexes.Clear();
		PointOfSalesCache.Clear();
		ProducersCache.Clear();
		SecurityPanelItemsCache.Clear();
		ToolsCache.Clear();
		CurrentToolName = ToolName.None;
		BlueprintTotalCost = 0.0;
		InteriorScorePercentage = 0f;
		PlayerBalanceAfterChanges = 0.0;
	}
}
