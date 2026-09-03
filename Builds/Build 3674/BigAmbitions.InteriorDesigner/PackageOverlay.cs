using System;
using System.Collections.Generic;
using BigAmbitions.InteriorDesigner.Tools;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.Indoors.InteriorDesign;
using Extensions;
using Helpers;
using Items.SpecialItems;
using JimmysUnityUtilities;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.InteriorDesigner;
using UI.ItemPanel;
using UI.Notification;
using UI.PlayerHUD;
using UnityEngine;

namespace BigAmbitions.InteriorDesigner;

public class PackageOverlay : MonoBehaviour
{
	private const string NoInventoryKey = "interiordesigner_package_no_items";

	private const string NoInventoryWithSearchKey = "interiordesigner_package_no_items_search";

	[SerializeField]
	private TextLocalizationComponent titleText;

	[SerializeField]
	private PackageCargoItemUi cargoItemTemplate;

	[SerializeField]
	private GameObject noItemsText;

	[SerializeField]
	private TMP_InputField searchField;

	[SerializeField]
	private TextLocalizationComponent noInventoryText;

	[Header("Drag")]
	[SerializeField]
	private RectTransform dragArea;

	[HideInInspector]
	public bool isOpen;

	private ICargoHolder _currentCargoHolder;

	private int _currentClickedIndex;

	private bool _currentIsVehicle;

	private string _newItemName;

	private string _originalItemName;

	private string _newText;

	private string _originalText;

	private readonly List<PackageCargoItemUi> _cargoItemUis = new List<PackageCargoItemUi>();

	private void Awake()
	{
		InteriorDesignerUI.OnUndoRedo.AddListener(OnUndoRedo);
		PackageCargoItemUi.SetActions(OnSellClick, OnPlaceClick, OnPackClick, OnDragStart, RemoveCargoItem, RefreshList, ExecuteRevertibleAction);
	}

	private void OnUndoRedo()
	{
		if (isOpen)
		{
			ShowAllItems();
		}
	}

	public void Open(ICargoHolder cargoHolder, int clickedIndex, bool isVehicle)
	{
		if (cargoHolder != null)
		{
			titleText.Key = cargoHolder.GetCargoName(isKey: true);
			SetHeader(cargoHolder, clickedIndex, isVehicle);
			_currentCargoHolder = cargoHolder;
			_currentClickedIndex = clickedIndex;
			_currentIsVehicle = isVehicle;
			ShowAllItems();
			searchField.onValueChanged.AddListener(OnSearch);
			if (!string.IsNullOrEmpty(searchField.text))
			{
				OnSearch(searchField.text);
			}
			base.gameObject.SetActive(value: true);
			isOpen = true;
		}
	}

	public void CloseIfSelected(int itemIndex)
	{
		if (itemIndex == _currentClickedIndex)
		{
			Close();
		}
	}

	public void Close()
	{
		if (isOpen)
		{
			searchField.onValueChanged.RemoveListener(OnSearch);
			isOpen = false;
			base.gameObject.SetActive(value: false);
		}
	}

	private void SetHeader()
	{
		SetHeader(_currentCargoHolder, _currentClickedIndex, _currentIsVehicle);
	}

	private void SetHeader(ICargoHolder cargoHolder, int clickedIndex, bool isVehicle)
	{
		ItemController itemController = (isVehicle ? null : InteriorDesignerController.ItemControllersCache[clickedIndex]);
		if (itemController == null || !itemController.Item.IsStockCarrier())
		{
			SetHeader(cargoHolder);
		}
		else if (!string.IsNullOrEmpty(itemController.itemName) && !string.IsNullOrEmpty(itemController.CurrentStockDropdownOption))
		{
			CargoInstance stockInstance = itemController.ItemInstance.GetStockInstance();
			titleText.Suffix = $" ({stockInstance.amount}/{stockInstance.GetMaxStockCapacity(itemController.ItemInstance)})";
		}
	}

	private void SetHeader(ICargoHolder cargoHolder)
	{
		titleText.Suffix = $" ({cargoHolder.GetCargoInstances().Count}/{cargoHolder.GetMaxCargoSize()})";
	}

	private void ShowAllItems()
	{
		cargoItemTemplate.transform.ResetTemplate();
		List<CargoInstance> cargoInstances = _currentCargoHolder.GetCargoInstances();
		bool flag = cargoInstances.Count > 0;
		noItemsText.SetActive(!flag);
		SetHeader();
		if (!flag || (cargoInstances.Count == 1 && cargoInstances[0].amount <= 0))
		{
			noInventoryText.Key = "interiordesigner_package_no_items";
			noInventoryText.gameObject.SetActive(value: true);
			return;
		}
		_cargoItemUis.Clear();
		List<CargoItem> list = CargoItem.ConvertCargoInstancesToCargoItems(cargoInstances);
		for (int i = 0; i < list.Count; i++)
		{
			CargoItem cargoItem = list[i];
			if (!string.IsNullOrEmpty(cargoItem.itemName))
			{
				bool canPlaceItem = ItemsGetter.GetByName(cargoItem.itemName).isFurniture && !ItemsGetter.GetByName(cargoItem.itemName).HasTag(TagRef.Itemtag.isbag) && BuildingManager.CanBuildOnCurrentBuilding;
				bool canPackItem = (_currentIsVehicle || InteriorDesignerController.ItemControllersCache[_currentClickedIndex].itemName != "ba:itemname_closedcardboardbox") && BuildingManager.CanBuildOnCurrentBuilding;
				PackageCargoItemUi packageCargoItemUi = UnityEngine.Object.Instantiate(cargoItemTemplate, cargoItemTemplate.transform.parent);
				packageCargoItemUi.SetUp(i, cargoItem, canPlaceItem, canPackItem);
				_cargoItemUis.Add(packageCargoItemUi);
			}
		}
		noInventoryText.gameObject.SetActive(value: false);
	}

	private void OnSearch(string newValue)
	{
		bool flag = string.IsNullOrEmpty(newValue);
		int num = 0;
		for (int i = 0; i < _cargoItemUis.Count; i++)
		{
			PackageCargoItemUi packageCargoItemUi = _cargoItemUis[i];
			bool flag2 = flag || packageCargoItemUi.SearchName.Contains(newValue, StringComparison.OrdinalIgnoreCase);
			packageCargoItemUi.gameObject.SetActive(flag2);
			if (flag2)
			{
				num++;
			}
		}
		noInventoryText.gameObject.SetActive(num == 0);
		if (num == 0)
		{
			noInventoryText.Key = (flag ? "interiordesigner_package_no_items" : "interiordesigner_package_no_items_search");
		}
	}

	private void OnSellClick(int cargoIndex, CargoItem cargoItem, CargoInstance firstCargoInstance)
	{
		if (firstCargoInstance.ItemCached.isSpecialGift)
		{
			ItemPanelUI.ConfirmDiscardingSpecialGift(OnConfirmSell);
		}
		else
		{
			OnConfirmSell();
		}
		void OnConfirmSell()
		{
			float price = firstCargoInstance.GetSellingPrice() * (float)cargoItem.cargoInstances.Count;
			IInteriorDesignerTool.executeActionThroughCode(new PackageSellRevertibleAction(_currentClickedIndex, cargoIndex, _currentIsVehicle, firstCargoInstance, price));
			ShowAllItems();
		}
	}

	private void OnPlaceClick(int cargoIndex, CargoInstance cargoInstance)
	{
		if (!ItemHelper.FitsInBuilding(cargoInstance.itemName))
		{
			Notifications.ShowError("itempanelui_notification_item_too_tall");
			return;
		}
		ItemInstance itemInstance = cargoInstance.InitializeNewInstance();
		ItemController controller = PrefabHelper.CreatePrefabItem(cargoInstance.itemName, InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer);
		controller.ItemInstance = itemInstance;
		controller.transform.rotation = Quaternion.identity;
		int index = InteriorDesignerController.ItemControllersCache.Count;
		InteriorDesignerController.ItemControllersCache.Add(controller);
		InstanceBehavior<BuildingManager>.Instance.allItemControllers.Add(controller);
		IInteriorDesignerTool.toolToOpenAfterUsage = ToolName.Package;
		IInteriorDesignerTool.toolToOpenAfterUsageArguments = new
		{
			_currentClickedIndex = _currentClickedIndex,
			_isVehicle = _currentIsVehicle
		};
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if (controller.StockTypes.Count == 2)
			{
				controller.OnStockOptionSelected(1);
				if (controller is ShowcaseShelfController showcaseShelfController)
				{
					showcaseShelfController.ShowItemVisuals();
				}
			}
		});
		IInteriorDesignerTool.moveItemWithHandTool(index, (HandRevertibleAction handRevertibleAction) => new PackagePlaceRevertibleAction(_currentClickedIndex, cargoIndex, _currentIsVehicle, cargoInstance, index, handRevertibleAction), null);
	}

	private void OnPackClick(int cargoIndex, CargoInstance cargoInstance)
	{
		ItemController itemController = (_currentIsVehicle ? null : InteriorDesignerController.ItemControllersCache[_currentClickedIndex]);
		CargoInstance cargoInstanceToPack;
		if (itemController != null && itemController.Item.IsStockCarrier())
		{
			cargoInstanceToPack = cargoInstance.Copy();
		}
		else
		{
			cargoInstanceToPack = cargoInstance;
		}
		ItemInstance itemInstance = ItemHelper.InitializeNewInstance("ba:itemname_closedcardboardbox");
		itemInstance.AddToCargo(cargoInstanceToPack);
		ItemController itemController2 = PrefabHelper.CreatePrefabItem("ba:itemname_closedcardboardbox", InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer);
		itemController2.ItemInstance = itemInstance;
		itemController2.transform.rotation = Quaternion.identity;
		int index = InteriorDesignerController.ItemControllersCache.Count;
		InteriorDesignerController.ItemControllersCache.Add(itemController2);
		InstanceBehavior<BuildingManager>.Instance.allItemControllers.Add(itemController2);
		IInteriorDesignerTool.toolToOpenAfterUsage = ToolName.Package;
		IInteriorDesignerTool.toolToOpenAfterUsageArguments = new
		{
			_currentClickedIndex = _currentClickedIndex,
			_isVehicle = _currentIsVehicle
		};
		IInteriorDesignerTool.moveItemWithHandTool(index, (HandRevertibleAction handRevertibleAction) => new PackagePackCargoRevertibleAction(_currentClickedIndex, cargoIndex, _currentIsVehicle, cargoInstanceToPack, index, handRevertibleAction), null);
	}

	private void OnDragStart(int cargoIndex, CargoItem cargoItem)
	{
		PackageCargoItemUi packageCargoItemUi = _cargoItemUis[cargoIndex];
		if (cargoItem.cargoInstances.Count > 1)
		{
			CargoItem cargoItem2 = cargoItem.Copy();
			cargoItem2.cargoInstances.RemoveAt(0);
			PackageCargoItemUi packageCargoItemUi2 = UnityEngine.Object.Instantiate(packageCargoItemUi, packageCargoItemUi.transform.parent);
			packageCargoItemUi2.SetUp(cargoIndex, cargoItem2, packageCargoItemUi.CanPlaceItem, packageCargoItemUi.CanPackItem);
			packageCargoItemUi2.transform.SetSiblingIndex(packageCargoItemUi.transform.GetSiblingIndex());
			_cargoItemUis.RemoveAt(cargoIndex);
			_cargoItemUis.Insert(cargoIndex, packageCargoItemUi2);
		}
		packageCargoItemUi.transform.SetParent(dragArea);
	}

	private void RemoveCargoItem(int cargoIndex)
	{
		_cargoItemUis.RemoveAt(cargoIndex);
		SetHeader();
	}

	private void RefreshList()
	{
		if (isOpen)
		{
			ShowAllItems();
			if (!string.IsNullOrEmpty(searchField.text))
			{
				OnSearch(searchField.text);
			}
		}
	}

	private void ExecuteRevertibleAction(int cargoIndex, int hitIndex, bool hitVehicle)
	{
		CargoInstance cargoInstance = CargoItem.ConvertCargoInstancesToCargoItems(_currentCargoHolder.GetCargoInstances())[cargoIndex].cargoInstances[0];
		IInteriorDesignerTool.executeActionThroughCode?.Invoke(new PackageMoveRevertibleAction(_currentClickedIndex, cargoIndex, _currentIsVehicle, cargoInstance, hitIndex, hitVehicle, preventFirstAnimation: false));
	}
}
