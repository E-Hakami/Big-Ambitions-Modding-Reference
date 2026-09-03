using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings;
using Buildings.Office.Headquarters;
using Entities;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using TMPro;
using UI.Components;
using UI.Elements;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.LogisticsManagers;

public class LogisticsManagerPlanUI : MonoBehaviour
{
	private const string ShowWarehouseKey = "common_show_warehouse";

	private const string ShowFactoryKey = "common_show_factory";

	private const string BusinessTargetWarehouseKey = "bizman_logisticsmanagers_min_stock_amount";

	private const string BusinessTargetFactoryKey = "bizman_logisticsmanagers_deliver_up_to";

	private const string RunsOutInKey = "bizman_logisticsmanagers_runs_out_in";

	private const string ExportPriceKey = "bizman_logisticsmanagers_export_price";

	private const int MaxTargetAmount = 9999999;

	public NoManagerAssignedPopUp noManagerAssignedPopUp;

	public Action<string> onWarehouseChanged;

	[SerializeField]
	private UI.Elements.Dropdown warehouseDropdown;

	[SerializeField]
	private Button showWarehouseButton;

	[SerializeField]
	private TextLocalizationComponent showWarehouseLabel;

	[SerializeField]
	private Button addDestinationButton;

	[SerializeField]
	private TextLocalizationComponent destinationsAmountLabel;

	[SerializeField]
	private LogisticsManagerDestinationUI destinationTemplate;

	[SerializeField]
	private ReorderableList destinationsReorderableList;

	private readonly List<LogisticsManagerDestinationUI> _destinationEntries = new List<LogisticsManagerDestinationUI>();

	private readonly List<string> _importProducts = new List<string>();

	private readonly HashSet<Address> _occupiedWarehouses = new HashSet<Address>();

	private LogisticsManagerPlan _currentPlan;

	private List<BuildingRegistration> _warehouses;

	private List<BuildingRegistration> _importExportBusinesses = new List<BuildingRegistration>();

	public List<BuildingRegistration> Destinations { get; private set; }

	private void Awake()
	{
		NoManagerAssignedPopUp obj = noManagerAssignedPopUp;
		obj.deletePlan = (Action)Delegate.Combine(obj.deletePlan, new Action(DeletePlan));
		warehouseDropdown.onOptionSelected.AddListener(OnChangedWarehouse);
		_importExportBusinesses.Clear();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.businessTypeName == "ba:businesstype_importexport")
			{
				_importExportBusinesses.Add(buildingRegistration);
			}
		}
	}

	private void OnEnable()
	{
		destinationsReorderableList.OnDragStarted += CollapseAll;
		destinationsReorderableList.OnItemReordered += OnDestinationReordered;
	}

	private void OnDisable()
	{
		destinationsReorderableList.OnDragStarted -= CollapseAll;
		destinationsReorderableList.OnItemReordered -= OnDestinationReordered;
		destinationTemplate.transform.ResetTemplate();
	}

	private void OnDestinationReordered(int fromIndex, int toIndex)
	{
		LogisticsManagerPlanDestination item = _currentPlan.destinations[fromIndex];
		_currentPlan.destinations.RemoveAt(fromIndex);
		_currentPlan.destinations.Insert(toIndex, item);
		SaveGameManager.MarkChange();
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			LoadPlan(_currentPlan);
		});
	}

	private void CollapseAll(ReorderableListItem _)
	{
		foreach (LogisticsManagerDestinationUI destinationEntry in _destinationEntries)
		{
			destinationEntry.ChangeProductsVisibility(isVisible: false);
		}
	}

	public void LoadPlan(LogisticsManagerPlan plan)
	{
		_currentPlan = plan;
		bool isFactory = plan.isFactory;
		Destinations = (isFactory ? BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilterForFactoryDestinations) : BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilterForWarehouseDestinations));
		if (isFactory)
		{
			Destinations.AddRange(BuildingHelper.GetImportExportBuildingRegistrations());
		}
		SetImportProducts();
		_destinationEntries.Clear();
		destinationTemplate.transform.ResetTemplate();
		for (int i = 0; i < _currentPlan.destinations.Count; i++)
		{
			AddDestinationEntry(i);
		}
		destinationsReorderableList.Reinitialize();
		if (plan.assignedEmployeeId == null)
		{
			noManagerAssignedPopUp.Show();
		}
		else
		{
			noManagerAssignedPopUp.Hide();
		}
		SetUpWarehouseDropdown(plan);
		SetUpWarehouseInfo(plan);
		base.gameObject.SetActive(value: true);
	}

	private bool PlayerBuildingFilterForWarehouseDestinations(BuildingRegistration buildingRegistration)
	{
		if (buildingRegistration.Address == _currentPlan.targetAddress || buildingRegistration.businessTypeName == "ba:businesstype_empty")
		{
			return false;
		}
		return BuildingTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Buildingtypetag.iswarehousedestination);
	}

	private bool PlayerBuildingFilterForFactoryDestinations(BuildingRegistration buildingRegistration)
	{
		if (buildingRegistration.Address == _currentPlan.targetAddress || buildingRegistration.businessTypeName == "ba:businesstype_empty")
		{
			return false;
		}
		return BuildingTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Buildingtypetag.isfactorydestination);
	}

	private void SetUpWarehouseDropdown(LogisticsManagerPlan plan)
	{
		_occupiedWarehouses.Clear();
		foreach (LogisticsManagerPlan logisticsManagerPlan in SaveGameManager.Current.logisticsManagerPlans)
		{
			if (logisticsManagerPlan != plan && !logisticsManagerPlan.targetAddress.IsUndefined())
			{
				_occupiedWarehouses.Add(logisticsManagerPlan.targetAddress);
			}
		}
		_warehouses = (plan.isFactory ? BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilterForFactories) : BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilterForWarehouses));
		List<string> list = new List<string> { "common_unassigned".GetLocalization() };
		list.AddRange(_warehouses.Select((BuildingRegistration x) => x.BusinessName).ToList());
		int selectedOption = _warehouses.FindIndex((BuildingRegistration x) => x.Address == plan.targetAddress) + 1;
		warehouseDropdown.SetOptions(list, localize: false, selectedOption);
	}

	private bool PlayerBuildingFilterForWarehouses(BuildingRegistration buildingRegistration)
	{
		if (buildingRegistration.businessTypeName == "ba:businesstype_warehouse" && buildingRegistration.GetBuildingType() == "ba:buildingtype_warehouse")
		{
			return !_occupiedWarehouses.Contains(buildingRegistration.Address);
		}
		return false;
	}

	private bool PlayerBuildingFilterForFactories(BuildingRegistration buildingRegistration)
	{
		if (buildingRegistration.businessTypeName == "ba:businesstype_factory" && buildingRegistration.GetBuildingType() == "ba:buildingtype_warehouse")
		{
			return !_occupiedWarehouses.Contains(buildingRegistration.Address);
		}
		return false;
	}

	private void OnChangedWarehouse(int warehouseIndex)
	{
		if (warehouseIndex == 0)
		{
			_currentPlan.targetAddress = null;
			onWarehouseChanged(null);
		}
		else
		{
			BuildingRegistration buildingRegistration = _warehouses[warehouseIndex - 1];
			_currentPlan.targetAddress = buildingRegistration.Address;
			onWarehouseChanged(buildingRegistration.BusinessName);
		}
		LoadPlan(_currentPlan);
		SaveGameManager.MarkChange();
	}

	public void AddDestination()
	{
		_currentPlan.destinations.Add(new LogisticsManagerPlanDestination
		{
			isUiCollapsed = false
		});
		AddDestinationEntry(_currentPlan.destinations.Count - 1);
		destinationsReorderableList.Reinitialize();
		addDestinationButton.interactable = _currentPlan.destinations.Count < _currentPlan.MaxDestinations;
		SaveGameManager.MarkChange();
	}

	private void DeletePlan()
	{
		if (_currentPlan == null)
		{
			Debug.LogError("No plan selected");
			return;
		}
		LogisticsManagerHelper.DeletePlan(_currentPlan.id);
		SaveGameManager.MarkChange();
	}

	private void SetUpWarehouseInfo(LogisticsManagerPlan plan)
	{
		addDestinationButton.interactable = _currentPlan.destinations.Count < _currentPlan.MaxDestinations;
		destinationsAmountLabel.SetData("bizman_logisticsmanagers_deliver_to_destinations_amount".Localize(new
		{
			amount = plan.MaxDestinations
		}));
		showWarehouseButton.interactable = !plan.targetAddress.IsUndefined();
		showWarehouseLabel.Key = (plan.isFactory ? "common_show_factory" : "common_show_warehouse");
	}

	private void AddDestinationEntry(int destinationIndex)
	{
		LogisticsManagerDestinationUI logisticsManagerDestinationUI = UnityEngine.Object.Instantiate(destinationTemplate, destinationTemplate.transform.parent);
		_destinationEntries.Add(logisticsManagerDestinationUI);
		logisticsManagerDestinationUI.SetUp(this, _currentPlan, destinationIndex, destinationIndex >= _currentPlan.MaxDestinations);
		LoadProducts(_currentPlan.destinations[destinationIndex], logisticsManagerDestinationUI);
		logisticsManagerDestinationUI.gameObject.SetActive(value: true);
	}

	private void LoadProducts(LogisticsManagerPlanDestination planDestination, LogisticsManagerDestinationUI destinationEntry)
	{
		bool isFactory = _currentPlan.isFactory;
		destinationEntry.transform.Find("ProductsHeader/BusinessTarget").GetComponent<TextLocalizationComponent>().Key = (isFactory ? "bizman_logisticsmanagers_deliver_up_to" : "bizman_logisticsmanagers_min_stock_amount");
		TextLocalizationComponent component = destinationEntry.transform.Find("ProductsHeader/RunsOutIn").GetComponent<TextLocalizationComponent>();
		if (!isFactory || planDestination.IsTargetExporter)
		{
			component.Key = (isFactory ? "bizman_logisticsmanagers_export_price" : "bizman_logisticsmanagers_runs_out_in");
			component.gameObject.SetActive(value: true);
		}
		else
		{
			component.gameObject.SetActive(value: false);
		}
		RectTransform productsElementTemplate = destinationEntry.productsElementTemplate;
		productsElementTemplate.ResetTemplate();
		List<string> listOfAvailableProducts = GetListOfAvailableProducts(planDestination);
		List<LogisticsManagerListEntryData> list = new List<LogisticsManagerListEntryData>();
		foreach (string product in listOfAvailableProducts)
		{
			Dictionary<LogisticsManagerListOption, string> productData = new Dictionary<LogisticsManagerListOption, string>();
			RectTransform rectTransform = UnityEngine.Object.Instantiate(productsElementTemplate, productsElementTemplate.parent);
			rectTransform.GetLabelByName("NameRow/ProductName").GetComponent<TextLocalizationComponent>().Key = product;
			productData.Add(LogisticsManagerListOption.ProductName, product.GetLocalization());
			bool active = IsProductInUse(planDestination.deliveryTargetAddress, product);
			rectTransform.Find("NameRow/InUseTooltip").gameObject.SetActive(active);
			int currentStock = BuildingHelper.CountResourcesInPallets(_currentPlan.targetAddress, product);
			rectTransform.GetLabelByName("WarehouseStock").text = currentStock.ToString();
			productData.Add(LogisticsManagerListOption.WarehouseStock, currentStock.ToString("000000000"));
			TextLocalizationComponent languageChangeEventByName = rectTransform.GetLanguageChangeEventByName("RunsOutIn");
			TMP_Text component2 = languageChangeEventByName.GetComponent<TMP_Text>();
			if (!isFactory)
			{
				int runsOutIn = _currentPlan.GetRunsOutIn(product, currentStock);
				LanguageChangeEventDataHolder languageChangeEventDataHolder = ((runsOutIn > 999) ? "logistics_manager_product_runs_out_in_days".Localize(new
				{
					days = "999+"
				}) : (runsOutIn switch
				{
					-1 => "logistics_manager_product_runs_out_in_empty".Localize(), 
					-2 => "logistics_manager_product_runs_out_in_never".Localize(), 
					_ => "logistics_manager_product_runs_out_in_days".Localize(new
					{
						days = runsOutIn
					}), 
				}));
				LanguageChangeEventDataHolder data = languageChangeEventDataHolder;
				component2.enabled = true;
				languageChangeEventByName.SetData(data);
				int num = ((runsOutIn > 999) ? 1000 : (runsOutIn switch
				{
					-1 => 0, 
					-2 => 1001, 
					_ => runsOutIn, 
				}));
				int num2 = num;
				productData.Add(LogisticsManagerListOption.RunsOutIn, num2.ToString("000000000"));
			}
			else if (planDestination.IsTargetExporter)
			{
				component2.gameObject.SetActive(value: true);
				float productExportPrice = ProductMarketHelper.GetProductExportPrice(product);
				string text = productExportPrice.ToCurrencyFormat();
				languageChangeEventByName.SetValue(text);
				int num3 = Mathf.RoundToInt(productExportPrice * 100f);
				productData.Add(LogisticsManagerListOption.RunsOutIn, num3.ToString("000000000000"));
			}
			else
			{
				component2.gameObject.SetActive(value: false);
			}
			UI.Components.InputField targetInput = rectTransform.Find("BusinessTarget/Amount").GetComponent<UI.Components.InputField>();
			Button buttonByName = rectTransform.GetButtonByName("BusinessTarget/PlusButton");
			Button buttonByName2 = rectTransform.GetButtonByName("BusinessTarget/MinusButton");
			ItemAmountTarget stockTarget = planDestination.stockTargets.FirstOrDefault((ItemAmountTarget x) => x.itemName == product);
			if (stockTarget == null)
			{
				stockTarget = new ItemAmountTarget(product);
			}
			productData.Add(LogisticsManagerListOption.BusinessTarget, stockTarget.targetAmount.ToString("000000000"));
			TextMeshProUGUI cargoAmount = rectTransform.GetLabelByName("CargoAmount");
			int boxSize = ItemsGetter.GetByName(product).boxSize;
			int num4 = Mathf.CeilToInt((float)stockTarget.targetAmount / (float)boxSize);
			productData.Add(LogisticsManagerListOption.CargoAmount, num4.ToString("000000000"));
			UpdateCargoAmount(cargoAmount, stockTarget.targetAmount, boxSize);
			targetInput.tmpInputField.text = stockTarget.targetAmount.ToString();
			targetInput.tmpInputField.onEndEdit.AddListener(delegate
			{
				string rawValue = targetInput.GetRawValue();
				if (!string.IsNullOrEmpty(rawValue))
				{
					if (!int.TryParse(rawValue, out stockTarget.targetAmount))
					{
						Notifications.ShowError("common_notification_invalid_amount");
					}
					else
					{
						if (stockTarget.targetAmount > 9999999)
						{
							stockTarget.targetAmount = 9999999;
						}
						else if (stockTarget.targetAmount < 0)
						{
							stockTarget.targetAmount = 0;
						}
						targetInput.tmpInputField.text = stockTarget.targetAmount.ToString();
						if (stockTarget.targetAmount == 0)
						{
							planDestination.stockTargets.Remove(stockTarget);
						}
						else if (!planDestination.stockTargets.Exists((ItemAmountTarget x) => x.itemName == product))
						{
							planDestination.stockTargets.Add(stockTarget);
						}
						UpdateCargoAmount(cargoAmount, stockTarget.targetAmount, boxSize);
						productData[LogisticsManagerListOption.BusinessTarget] = stockTarget.targetAmount.ToString("000000000");
						productData[LogisticsManagerListOption.CargoAmount] = Mathf.CeilToInt((float)stockTarget.targetAmount / (float)boxSize).ToString("000000000");
						SaveGameManager.MarkChange();
					}
				}
			});
			buttonByName.onClick.AddListener(delegate
			{
				stockTarget.targetAmount++;
				if (stockTarget.targetAmount > 9999999)
				{
					stockTarget.targetAmount = 9999999;
				}
				targetInput.tmpInputField.text = stockTarget.targetAmount.ToString();
				if (!planDestination.stockTargets.Exists((ItemAmountTarget x) => x.itemName == product))
				{
					planDestination.stockTargets.Add(stockTarget);
				}
				UpdateCargoAmount(cargoAmount, stockTarget.targetAmount, boxSize);
				productData[LogisticsManagerListOption.BusinessTarget] = stockTarget.targetAmount.ToString("000000000");
				productData[LogisticsManagerListOption.CargoAmount] = Mathf.CeilToInt((float)stockTarget.targetAmount / (float)boxSize).ToString("000000000");
				SaveGameManager.MarkChange();
			});
			buttonByName2.onClick.AddListener(delegate
			{
				stockTarget.targetAmount--;
				if (stockTarget.targetAmount < 0)
				{
					stockTarget.targetAmount = 0;
				}
				targetInput.tmpInputField.text = stockTarget.targetAmount.ToString();
				if (stockTarget.targetAmount == 0)
				{
					planDestination.stockTargets.Remove(stockTarget);
				}
				UpdateCargoAmount(cargoAmount, stockTarget.targetAmount, boxSize);
				productData[LogisticsManagerListOption.BusinessTarget] = stockTarget.targetAmount.ToString("000000000");
				productData[LogisticsManagerListOption.CargoAmount] = Mathf.CeilToInt((float)stockTarget.targetAmount / (float)boxSize).ToString("000000000");
				SaveGameManager.MarkChange();
			});
			rectTransform.gameObject.SetActive(value: true);
			list.Add(new LogisticsManagerListEntryData
			{
				entryTransform = rectTransform.transform,
				data = productData
			});
		}
		destinationEntry.productsList.SetUp(list);
		destinationEntry.UpdateProductsVisibility();
	}

	private static void UpdateCargoAmount(TextMeshProUGUI cargoAmount, int targetAmount, int boxSize)
	{
		cargoAmount.text = ((targetAmount == 0) ? string.Empty : Mathf.CeilToInt((float)targetAmount / (float)boxSize).ToString());
	}

	private static bool IsProductInUse(Address address, string product)
	{
		if (BusinessHelper.IsItemInUse(address, product))
		{
			return true;
		}
		if (!ItemsGetter.GetByName(product).HasTag(TagRef.Itemtag.isbag))
		{
			return false;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		if (!BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.customersneedpaperbags))
		{
			return false;
		}
		foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
		{
			if ((value.ItemCached.type & ItemType.PointOfSale) != 0)
			{
				return true;
			}
		}
		return false;
	}

	public void ChangeProductsVisibility(int destinationIndex, bool isVisible)
	{
		_destinationEntries[destinationIndex].ChangeProductsVisibility(isVisible);
	}

	private List<string> GetListOfAvailableProducts(LogisticsManagerPlanDestination destination)
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(destination.deliveryTargetAddress);
		if (buildingRegistration == null)
		{
			return new List<string>();
		}
		List<string> list = new List<string>();
		if (buildingRegistration.GetBuildingType() == "ba:buildingtype_warehouse")
		{
			if (_currentPlan.targetAddress != null)
			{
				if (BuildingHelper.GetBuildingRegistration(_currentPlan.targetAddress) is Entities.Warehouse warehouse)
				{
					list = warehouse.GetProducts().ToList();
				}
				AddImportProducts(list);
				AddStockTargetsWithPositiveAmounts(destination, list);
			}
		}
		else if (buildingRegistration.BuildingCached.SpecialService != null && buildingRegistration.BuildingCached.SpecialService.settings is ImportExportSettings importExportSettings)
		{
			list = new List<string>();
			foreach (string item in importExportSettings.GetItemsAvailable(null, forceAllItemsAvailable: true).Where(CanProductBeDelivered))
			{
				list.Add(item);
			}
		}
		else
		{
			BusinessType data = BusinessTypeHelper.GetData(buildingRegistration);
			list = data.GetPrimaryProducts().Where(CanProductBeDelivered).ToList();
			foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
			{
				if ((value.ItemCached.type & ItemType.ShowcaseShelf) != 0)
				{
					CargoInstance stockInstance = value.GetStockInstance();
					if (!string.IsNullOrEmpty(stockInstance.itemName))
					{
						list.Add(stockInstance.itemName);
					}
				}
			}
			if (!list.Contains("ba:itemname_paperbag") && data.HasTag(TagRef.Businesstag.customersneedpaperbags))
			{
				list.Add("ba:itemname_paperbag");
			}
			AddStockTargetsWithPositiveAmounts(destination, list);
			list = list.Distinct().ToList();
		}
		return list;
	}

	private static void AddStockTargetsWithPositiveAmounts(LogisticsManagerPlanDestination destination, List<string> suitableProducts)
	{
		foreach (ItemAmountTarget stockTarget in destination.stockTargets)
		{
			if (!suitableProducts.Contains(stockTarget.itemName) && stockTarget.targetAmount > 0)
			{
				suitableProducts.Add(stockTarget.itemName);
			}
		}
	}

	private void SetImportProducts()
	{
		_importProducts.Clear();
		foreach (ImportPartnership importPartnership in SaveGameManager.Current.importPartnerships)
		{
			foreach (ImportProduct product in importPartnership.products)
			{
				if (!(product.assignedWarehouse != _currentPlan.targetAddress) && product.amount > 0)
				{
					_importProducts.Add(product.itemName);
				}
			}
		}
	}

	private void AddImportProducts(List<string> suitableProducts)
	{
		foreach (string importProduct in _importProducts)
		{
			if (!suitableProducts.Contains(importProduct))
			{
				suitableProducts.Add(importProduct);
			}
		}
	}

	private bool CanProductBeDelivered(string itemName)
	{
		return (ItemsGetter.GetByName(itemName).type & ItemType.ServiceProduct) == 0;
	}

	public void UpdateSelectedBusiness(int destinationIndex, Address businessAddress)
	{
		if (!(_currentPlan.destinations[destinationIndex].deliveryTargetAddress == businessAddress))
		{
			_currentPlan.destinations[destinationIndex].Reset();
			_currentPlan.destinations[destinationIndex].deliveryTargetAddress = businessAddress;
			LoadProducts(_currentPlan.destinations[destinationIndex], _destinationEntries[destinationIndex]);
			SaveGameManager.MarkChange();
		}
	}

	public void ShowWarehouse()
	{
		if (_currentPlan == null)
		{
			Debug.LogError("No plan selected");
		}
		else
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(_currentPlan.targetAddress);
		}
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
		noManagerAssignedPopUp.Hide();
	}
}
