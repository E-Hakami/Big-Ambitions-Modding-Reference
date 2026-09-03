using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using AI.Customers.CustomerEntries;
using AI.Employees.SalaryNegotiation;
using BigAmbitions.DayNightCycle;
using BigAmbitions.InteriorDesigner.InteriorElements;
using BigAmbitions.Items;
using Blueprints;
using Buildings;
using Buildings.BuildingTypes.Shared;
using Buildings.Factory;
using Buildings.Office.Headquarters;
using Buildings.Retail.Businesses.CinemaTheater;
using BusinessLayoutSets;
using Dialogs;
using Entities;
using Extensions;
using HGAttributes;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using PlayerActivity;
using Seasons;
using Services;
using Streets;
using UI;
using UI.Smartphone.Apps.Contacts;
using UI.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class BuildingRegistration
{
	public string StreetName;

	public int StreetNumber;

	public bool AvailableForRent;

	public bool RentedByPlayer;

	public float RentPerDay;

	public List<ScheduleDay> scheduleDays;

	public bool temporarilyClosed;

	public string BusinessName;

	public string BusinessDescription;

	[FormerlySerializedAs("businessType")]
	public string businessTypeName = "ba:businesstype_empty";

	public string Layout;

	public Dictionary<string, ItemInstance> itemInstances = new Dictionary<string, ItemInstance>();

	public List<Order> unprocessedCompletedOrders;

	public List<OrderHistoryEntry> orderHistory;

	public List<DirtSpot> dirtSpots;

	public List<RetailPrice> retailPrices;

	public List<RetailPrice> storedRetailPrices;

	public float lastDeposit;

	public Satisfaction satisfaction;

	public Promotion promotion;

	public SignAppearanceSettings signAppearanceSettings;

	public List<SerializedInteriorDesign> interiorDesigns;

	public List<MarketingCampaign> marketingCampaigns;

	public int customerCapacity;

	public LogoSettings logoSettings;

	public float takeoverOfferAcceptRate;

	public bool takenOver;

	[AutocompleteDropdown("Items")]
	public List<string> cachedAvailableProducts;

	public List<string> cachedFulfilledCustomerDemands;

	public int creationDay = -1;

	public int lastDayOnSale;

	public float stolenItemsCost;

	public float securityLevelPercentage;

	public RadioStation radioStation;

	public bool warnedLastHourAboutNoEmployee;

	public float radioVolume;

	public string blueprintName;

	public string buildingOwnerRivalId;

	public string businessOwnerRivalId;

	public List<AiBusinessEmployeeData> aiEmployees;

	public List<EmployeeInstance> poachedEmployees;

	public List<float> dailyIncomes;

	public Dictionary<string, string> uniformsBySkill = new Dictionary<string, string>();

	public List<FactoryExport> factoryExports = new List<FactoryExport>();

	[NonSerialized]
	private Building _buildingCached;

	[Obsolete("delivered items now belong to delivery spots")]
	public List<string> deliveredItems = new List<string>();

	[Obsolete("Since EA 0.8")]
	public List<string> itemsInBuilding;

	[Obsolete]
	public HandTruckSpawnerData handTruckSpawnerData;

	private Dictionary<string, SerializedInteriorDesign> _interiorLookup = new Dictionary<string, SerializedInteriorDesign>();

	[IgnoreDataMember]
	public Building BuildingCached
	{
		get
		{
			if (_buildingCached == null && !string.IsNullOrEmpty(StreetName))
			{
				_buildingCached = BuildingHelper.GetBuilding(new Address(StreetName, StreetNumber));
			}
			return _buildingCached;
		}
		set
		{
			_buildingCached = value;
		}
	}

	[IgnoreDataMember]
	public Address Address
	{
		get
		{
			if (!(BuildingCached == null))
			{
				return BuildingCached.Address;
			}
			return null;
		}
	}

	[IgnoreDataMember]
	public string Neighborhood => BuildingCached.Neighbourhood;

	[IgnoreDataMember]
	public int Alerts
	{
		get
		{
			int num = 0;
			Address address = BuildingCached.Address;
			List<TodoTask> todoTasks = SaveGameManager.Current.TodoTasks;
			for (int i = 0; i < todoTasks.Count; i++)
			{
				if (todoTasks[i].address == address)
				{
					num++;
				}
			}
			return num;
		}
	}

	[IgnoreDataMember]
	public bool BuildingOwnedByPlayer => SaveGameManager.Current.realEstate.Exists((RealEstate x) => x.address == BuildingCached.Address);

	[IgnoreDataMember]
	public RealEstate RealEstate => SaveGameManager.Current.realEstate.Find((RealEstate x) => x.address == BuildingCached.Address);

	public bool HasValidAddress => !string.IsNullOrEmpty(StreetName);

	public bool HasEstablishedBusiness => !string.IsNullOrEmpty(BusinessName);

	public string GetBuildingType()
	{
		return BuildingCached.BuildingType;
	}

	public string GetDisplayName()
	{
		if (!string.IsNullOrEmpty(BusinessName))
		{
			return BusinessName;
		}
		return Address.ToFormattedString();
	}

	public BuildingRegistration()
	{
		Reset(resetFields: false);
	}

	public void Reset(bool resetFields = true)
	{
		Address address = Address;
		if (resetFields)
		{
			FieldInfo[] fields = GetType().GetFields();
			for (int i = 0; i < fields.Length; i++)
			{
				fields[i].SetValue(this, null);
			}
		}
		scheduleDays = new List<ScheduleDay>();
		unprocessedCompletedOrders = new List<Order>();
		orderHistory = new List<OrderHistoryEntry>();
		dirtSpots = new List<DirtSpot>();
		retailPrices = new List<RetailPrice>();
		storedRetailPrices = new List<RetailPrice>();
		signAppearanceSettings = new SignAppearanceSettings();
		logoSettings = new LogoSettings();
		interiorDesigns = new List<SerializedInteriorDesign>();
		satisfaction = new Satisfaction();
		promotion = new Promotion();
		marketingCampaigns = new List<MarketingCampaign>();
		cachedAvailableProducts = new List<string>();
		cachedFulfilledCustomerDemands = new List<string>();
		deliveredItems = new List<string>();
		itemsInBuilding = new List<string>();
		businessTypeName = "ba:businesstype_empty";
		businessOwnerRivalId = string.Empty;
		radioVolume = 1f;
		StreetName = address?.streetName ?? string.Empty;
		StreetNumber = address?.streetNumber ?? 0;
		aiEmployees = new List<AiBusinessEmployeeData>();
		dailyIncomes = new List<float>();
		itemInstances = new Dictionary<string, ItemInstance>();
		uniformsBySkill = new Dictionary<string, string>();
		factoryExports = new List<FactoryExport>();
		takenOver = false;
		ResetBuildingSpecific();
	}

	public void ShutDownAIBusiness()
	{
		string text = buildingOwnerRivalId;
		Reset();
		buildingOwnerRivalId = text;
		AvailableForRent = true;
		CityBuildingController cityBuildingController = InstanceBehavior<CityManager>.Instance?.FindCityBuildingController(Address);
		if (cityBuildingController != null)
		{
			cityBuildingController.UpdateSign();
		}
	}

	public Contact GetOrAddBusinessContact(bool hasWelcomeMessages = false)
	{
		if (BuildingCached == null || BuildingCached.SpecialService == null)
		{
			return null;
		}
		if (BuildingCached.SpecialService.businessTypeName == "ba:businesstype_hospital")
		{
			return Contact.AddContact("hospital_health_insurance_manager", ContactCategoryName.Business, "hospital_health_insurance_manager_description", BuildingHelper.GetBuildingRegistration(GameManager.hospitalAddress));
		}
		return Contact.AddContact(this, BuildingCached.SpecialService.contactCategory, hasWelcomeMessages);
	}

	public CallDialogType GetCallDialogType()
	{
		if (BuildingCached == null || BuildingCached.SpecialService == null)
		{
			return CallDialogType.FurnitureStoreManagerDialog;
		}
		return BuildingCached.SpecialService.dialogType;
	}

	public virtual void ResetBuildingSpecific()
	{
	}

	public void ResetScheduleDays()
	{
		scheduleDays = new List<ScheduleDay>();
		foreach (DayOfWeekOrdered value in Enum.GetValues(typeof(DayOfWeekOrdered)))
		{
			scheduleDays.Add(new ScheduleDay
			{
				day = value,
				isOpen = (businessTypeName == "ba:businesstype_headquarters" && value != DayOfWeekOrdered.Saturday && value != DayOfWeekOrdered.Sunday),
				openingHourSlots = new List<OpeningHourSlot>
				{
					new OpeningHourSlot(8, 16)
				}
			});
		}
	}

	public void AddToPlayer()
	{
		businessOwnerRivalId = string.Empty;
		DiscardOpenNegotiations();
		RentedByPlayer = true;
		AvailableForRent = false;
		takenOver = true;
		creationDay = SaveGameManager.Current.Day;
		unprocessedCompletedOrders.Clear();
		orderHistory.Clear();
		stolenItemsCost = 0f;
		RentPerDay = BuildingCached.GetBuildingDailyMarketRent();
		lastDeposit = BuildingHelper.CalculateDeposit(this, RentPerDay);
		logoSettings = logoSettings.Clone();
		BusinessLayoutSetHelper.InsertBusinessLayoutSet(Address, businessTypeName, new BuildingSizeInfo(BuildingCached), Layout, shouldRandomlyFillShelves: true);
		Layout = null;
	}

	public float GetAvgWeeklyIncome()
	{
		return GetAvgDailyIncome(7) * 7f;
	}

	public float GetAvgDailyIncome(int numberOfDays)
	{
		List<FinancialSummary.BusinessIncomeStatement> list = new List<FinancialSummary.BusinessIncomeStatement>();
		foreach (FinancialSummary financialSummary in SaveGameManager.Current.financialSummaries)
		{
			if (financialSummary.dayNumber < SaveGameManager.Current.Day - numberOfDays)
			{
				continue;
			}
			foreach (FinancialSummary.BusinessIncomeStatement businessIncomeStatement in financialSummary.businessIncomeStatements)
			{
				if (!(businessIncomeStatement.Address != Address))
				{
					list.Add(businessIncomeStatement);
				}
			}
		}
		if (list.Count == 0)
		{
			return 0f;
		}
		return list.SumValues((FinancialSummary.BusinessIncomeStatement x) => x.TotalProfit) / (float)list.Count;
	}

	private void DiscardOpenNegotiations()
	{
		foreach (AiBusinessEmployeeData aiEmployee in aiEmployees)
		{
			foreach (CandidateSalaryNegotiation candidateSalaryNegotiation in SaveGameManager.Current.candidateSalaryNegotiations)
			{
				if (!(candidateSalaryNegotiation.employeeInstance.id != aiEmployee.id) && !candidateSalaryNegotiation.completed)
				{
					candidateSalaryNegotiation.completed = true;
					candidateSalaryNegotiation.accepted = false;
				}
			}
		}
	}

	public (LanguageChangeEventDataHolder, Color) GetOpenStatus()
	{
		if (string.IsNullOrEmpty(BusinessName))
		{
			return ("common_value".Localize(new
			{
				value = "-"
			}), Color.white);
		}
		bool num = BusinessHelper.IsBusinessOpen(this);
		string key = (num ? "common_open" : "common_closed");
		Color32 color = (num ? InstanceBehavior<GlobalReferences>.Instance.colors.green : InstanceBehavior<GlobalReferences>.Instance.colors.lightRed);
		return (LanguageChangeEventDataHolder.Create(key), color);
	}

	public (LanguageChangeEventDataHolder, Color) GetOccupancyStatus()
	{
		Color32 color = ((RealEstate.OccupancyPercentage < 30) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : ((RealEstate.OccupancyPercentage > 70) ? InstanceBehavior<GlobalReferences>.Instance.colors.green : InstanceBehavior<GlobalReferences>.Instance.colors.white));
		return (LanguageChangeEventDataHolder.Create("common_value", new
		{
			value = $"{RealEstate.OccupancyPercentage}%"
		}), color);
	}

	public List<string> GetListOfItemsForSale()
	{
		if (ContractItemsForSaleService.TryGetItemsForAddress(Address, out var itemNames))
		{
			return itemNames;
		}
		if (RentedByPlayer)
		{
			return cachedAvailableProducts;
		}
		if (string.IsNullOrEmpty(Layout))
		{
			return new List<string>();
		}
		BusinessType data = BusinessTypeHelper.GetData(businessTypeName);
		if (data.suitableBuildingType == "ba:buildingtype_office")
		{
			return new List<string>(data.GetPrimaryProducts());
		}
		if (data.customerType == CustomerType.None)
		{
			return new List<string>();
		}
		BusinessLayoutSet orLoadBusinessLayoutSet = BusinessLayoutSetHelper.GetOrLoadBusinessLayoutSet(businessTypeName, new BuildingSizeInfo(BuildingCached), Layout, warnIfNotFound: false);
		if (orLoadBusinessLayoutSet == null)
		{
			return new List<string>();
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>();
		foreach (BusinessLayoutSets.Item item in orLoadBusinessLayoutSet.Items)
		{
			BigAmbitions.Items.Item byName = ItemsGetter.GetByName(item.itemName);
			if (!byName.isSeasonalForSale)
			{
				continue;
			}
			string itemNameBySeason = byName.GetItemNameBySeason(PlayerPrefSettings.SeasonalDecorations ? SeasonHelper.CurrentSeasonName : SeasonName.None);
			if (string.IsNullOrEmpty(itemNameBySeason))
			{
				itemNameBySeason = byName.GetItemNameBySeason(SeasonName.None);
				if (string.IsNullOrEmpty(itemNameBySeason))
				{
					continue;
				}
			}
			if (hashSet.Add(itemNameBySeason))
			{
				list.Add(itemNameBySeason);
			}
		}
		foreach (BusinessLayoutSets.Item item2 in orLoadBusinessLayoutSet.Items)
		{
			PlayerItemPurchaserSettings playerItemPurchaserSettings = item2.playerItemPurchaserSettings;
			if (playerItemPurchaserSettings != null && playerItemPurchaserSettings.enabled && !ItemsGetter.GetByName(item2.itemName).isSeasonalForSale && ItemsGetter.GetByName(playerItemPurchaserSettings.itemName).IsAvailableInCurrentSeason() && hashSet.Add(playerItemPurchaserSettings.itemName))
			{
				list.Add(playerItemPurchaserSettings.itemName);
			}
		}
		foreach (string primaryProduct in data.GetPrimaryProducts())
		{
			if ((ItemsGetter.GetByName(primaryProduct).type & ItemType.RetailProduct) == 0)
			{
				list.Add(primaryProduct);
			}
		}
		return list;
	}

	public void GenerateInteriorDesignerLookup()
	{
		if (_interiorLookup == null)
		{
			_interiorLookup = new Dictionary<string, SerializedInteriorDesign>();
		}
		if (interiorDesigns == null || interiorDesigns.Count == 0)
		{
			return;
		}
		_interiorLookup.Clear();
		_interiorLookup.EnsureCapacity(interiorDesigns.Count);
		foreach (SerializedInteriorDesign interiorDesign in interiorDesigns)
		{
			_interiorLookup.TryAdd(interiorDesign.UUID, interiorDesign);
		}
	}

	public Dictionary<string, SerializedInteriorDesign> GetInteriorDesignerLookup()
	{
		if (_interiorLookup == null || _interiorLookup.Count == 0)
		{
			GenerateInteriorDesignerLookup();
		}
		return _interiorLookup;
	}

	public float CalculateInteriorDesignPrice()
	{
		float num = 0f;
		foreach (SerializedInteriorDesign interiorDesign in interiorDesigns)
		{
			SerializedInteriorDesign.SerializableInteriorMaterial[] materials = interiorDesign.materials;
			for (int i = 0; i < materials.Length; i++)
			{
				SerializedInteriorDesign.SerializableInteriorMaterial serializableInteriorMaterial = materials[i];
				if (InteriorElementsHelper.PresetsDictionary.TryGetValue(serializableInteriorMaterial.MaterialID, out var value))
				{
					num += value.price;
				}
			}
		}
		return num;
	}

	public float GetMarketingEfficiency()
	{
		float num = 0f;
		foreach (MarketingCampaign marketingCampaign in marketingCampaigns)
		{
			if (marketingCampaign.enabled)
			{
				num += (float)MarketingTypeSettings.Get(marketingCampaign.marketingTypeName).sqmReach;
			}
		}
		float marketingReachMultiplier = BuildingTypeHelper.GetData(BuildingCached.BuildingType).marketingReachMultiplier;
		return Mathf.Min(num * marketingReachMultiplier / (float)BuildingSizeHelper.GetData(BuildingCached.BuildingSize).squareMeters, 1f) * 100f;
	}

	public float GetDailyMarketingExpenses()
	{
		float num = 0f;
		foreach (MarketingCampaign marketingCampaign in marketingCampaigns)
		{
			if (marketingCampaign.enabled)
			{
				num += MarketingTypeSettings.Get(marketingCampaign.marketingTypeName).pricePerDay;
			}
		}
		return num;
	}

	public void TemporarilyClose(bool closed)
	{
		BusinessSimulatorHelper.Work.ForceCompleteAllWork();
		temporarilyClosed = closed;
		GameEvent.Invoke("ba:gameevent_changedbusinessopenstate");
		GlobalEvents.onBuildingRegistrationChange?.Invoke(Address);
		if (!closed)
		{
			LicensingFeesHelper.PayLicensingFees(this);
		}
		CustomerEntriesHelper.UpdateCustomerEntriesForPlayerBusiness(this, TimeHelper.GetDayOfWeek());
		if (closed)
		{
			InstanceBehavior<UIs>.Instance.tasksUI.CreateTodoTask(TodoTaskType.BusinessTemporarilyClosed, Address);
		}
		else
		{
			TasksUI.UpdateTasksFromBusiness(this);
		}
		if (InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivity is WorkActivity && InstanceBehavior<BuildingManager>.Instance.buildingRegistration == this)
		{
			InstanceBehavior<UIs>.Instance.playerActivityUI.CancelActivity();
		}
	}

	public List<ItemInstance> GetItemsOfType(ItemType type)
	{
		List<ItemInstance> list = new List<ItemInstance>();
		foreach (ItemInstance value in itemInstances.Values)
		{
			if ((value.ItemCached.type & type) != 0)
			{
				list.Add(value);
			}
		}
		return list;
	}

	public List<ItemInstance> GetAssignableItems()
	{
		List<ItemInstance> list = new List<ItemInstance>();
		List<string> list2 = new List<string>(BusinessTypeHelper.GetData(businessTypeName).employeePrimarySkills);
		list2.AddRange(BuildingTypeHelper.GetData(GetBuildingType()).requiredBuildingSkills);
		foreach (ItemInstance value in itemInstances.Values)
		{
			BigAmbitions.Items.Item itemCached = value.ItemCached;
			if (!itemCached.assignable)
			{
				continue;
			}
			string[] suitableSkills = itemCached.suitableSkills;
			foreach (string item in suitableSkills)
			{
				if (list2.Contains(item))
				{
					list.Add(value);
					break;
				}
			}
		}
		return list;
	}

	public float GetEntranceFeeForPlayer()
	{
		if (!RentedByPlayer)
		{
			return GetAIBusinessEntranceFee();
		}
		return 0f;
	}

	public float GetAIBusinessEntranceFee()
	{
		BusinessType data = BusinessTypeHelper.GetData(businessTypeName);
		if (!data.hasEntranceFee)
		{
			return 0f;
		}
		string entranceFeeNameForBusinessType = BusinessTypeHelper.GetEntranceFeeNameForBusinessType(data);
		if (!string.IsNullOrEmpty(entranceFeeNameForBusinessType))
		{
			return ItemHelper.GetPrice(entranceFeeNameForBusinessType, this);
		}
		return 0f;
	}

	public void RemoveUnusedRetailPrices()
	{
		int num = 0;
		while (num < retailPrices.Count)
		{
			RetailPrice retailPrice = retailPrices[num];
			if (cachedAvailableProducts.Contains(retailPrice.itemName))
			{
				num++;
				continue;
			}
			string itemName = retailPrice.itemName;
			retailPrices.RemoveAt(num);
			ProductMarketHelper.UpdateMarketDemand(itemName);
		}
	}

	public Sprite GetPOIIcon()
	{
		BusinessType data = BusinessTypeHelper.GetData(businessTypeName);
		if (businessTypeName == "ba:businesstype_empty")
		{
			return BuildingTypeHelper.GetData(BuildingCached).poiIcon;
		}
		if (BuildingCached.SpecialService != null && BuildingCached.SpecialService.overridePoiIcon != null)
		{
			return BuildingCached.SpecialService.overridePoiIcon;
		}
		return data.icon;
	}

	public Color GetPOIBackgroundColor()
	{
		BusinessType data = BusinessTypeHelper.GetData(businessTypeName);
		if (!(businessTypeName != "ba:businesstype_empty"))
		{
			return BuildingTypeHelper.GetData(BuildingCached).mapFilterColor;
		}
		return data.cityMapFilterColor;
	}

	public RadioStation GetBusinessRadioStation()
	{
		if (!RentedByPlayer)
		{
			return BusinessTypeHelper.GetData(businessTypeName).radioStation;
		}
		return radioStation;
	}

	public float GetBusinessRadioVolume()
	{
		if (!RentedByPlayer)
		{
			return BusinessTypeHelper.GetData(businessTypeName).radioVolume * PlayerPrefSettings.AiStoreMusicVolume;
		}
		return radioVolume;
	}

	public List<BusinessEmployeeGenerator.SkillForce> GetRequiredSkillForceForLayout(BusinessLayoutSet layoutSet = null)
	{
		if (businessTypeName == "ba:businesstype_factory")
		{
			List<BusinessEmployeeGenerator.SkillForce> list = new List<BusinessEmployeeGenerator.SkillForce>();
			int num = UnityEngine.Random.Range(6, 24);
			for (int i = 0; i < num; i++)
			{
				list.Add(new BusinessEmployeeGenerator.SkillForce("ba:skill_factoryworker")
				{
					hours = 40
				});
			}
			return list;
		}
		if (layoutSet == null)
		{
			layoutSet = BusinessLayoutSetHelper.GetOrLoadBusinessLayoutSet(businessTypeName, new BuildingSizeInfo(BuildingCached), Layout, warnIfNotFound: false);
		}
		if (layoutSet == null)
		{
			Debug.LogError($"Couldn't calculate required skill force for {Address}");
			return null;
		}
		List<string> list2 = new List<string>();
		foreach (BusinessLayoutSets.Item item in layoutSet.Items)
		{
			BigAmbitions.Items.Item byName = ItemsGetter.GetByName(item.itemName);
			if (byName.assignable)
			{
				list2.Add(byName.itemName);
			}
		}
		return this.CalculateNeededSkillForce(list2);
	}

	public void GenerateAiBusinessEmployees()
	{
		aiEmployees = new List<AiBusinessEmployeeData>();
		List<BusinessEmployeeGenerator.SkillForce> requiredSkillForceForLayout = GetRequiredSkillForceForLayout();
		if (requiredSkillForceForLayout == null)
		{
			return;
		}
		foreach (BusinessEmployeeGenerator.SkillForce item in requiredSkillForceForLayout)
		{
			int num = item.hours;
			while (num > 0)
			{
				AiBusinessEmployeeData aiBusinessEmployeeData = new AiBusinessEmployeeData(UuidHelper.GenerateBase64Uuid(), item.skillName, Address);
				aiEmployees.Add(aiBusinessEmployeeData);
				num -= aiBusinessEmployeeData.GetExpectedHoursPerWeek();
			}
		}
	}

	public void ReplaceAiBusinessEmployee(string oldEmployeeId)
	{
		AiBusinessEmployeeData aiBusinessEmployeeData = aiEmployees.Find((AiBusinessEmployeeData x) => x.id == oldEmployeeId);
		if (aiBusinessEmployeeData != null)
		{
			aiEmployees.Remove(aiBusinessEmployeeData);
			aiEmployees.Add(new AiBusinessEmployeeData(UuidHelper.GenerateBase64Uuid(), aiBusinessEmployeeData.primarySkillName, Address, replacement: true));
			return;
		}
		EmployeeInstance employeeInstance = poachedEmployees.Find((EmployeeInstance x) => x.id == oldEmployeeId);
		if (employeeInstance != null)
		{
			poachedEmployees.Remove(employeeInstance);
			aiEmployees.Add(new AiBusinessEmployeeData(UuidHelper.GenerateBase64Uuid(), employeeInstance.GetPrimarySkill(), Address, replacement: true));
		}
	}

	public string GetComposedName()
	{
		if (!string.IsNullOrEmpty(BusinessName))
		{
			return BusinessName + " (" + Address.ToFormattedString() + ")";
		}
		return Address.ToFormattedString();
	}

	public bool OpensWithinHours(int hours)
	{
		int num = Mathf.CeilToInt((float)(SaveGameManager.Current.Hour + hours) / 24f);
		if ((SaveGameManager.Current.Hour + hours) % 24 == 0)
		{
			num++;
		}
		DayOfWeekOrdered day = TimeHelper.GetDayOfWeek();
		for (int i = 0; i < num; i++)
		{
			ScheduleDay scheduleDay = scheduleDays.Find((ScheduleDay x) => x.day == day);
			if (scheduleDay == null)
			{
				day = day.Next();
				continue;
			}
			if (day == TimeHelper.GetDayOfWeek())
			{
				if (scheduleDay.openingHourSlots.Exists((OpeningHourSlot x) => x.startingHour > SaveGameManager.Current.Hour && x.startingHour <= Math.Min(SaveGameManager.Current.Hour + hours, 24)))
				{
					return true;
				}
			}
			else
			{
				int endTime = ((num - i == 1) ? (hours % 24) : 24);
				if (scheduleDay.openingHourSlots.Exists((OpeningHourSlot x) => x.startingHour < endTime))
				{
					return true;
				}
			}
			day = day.Next();
		}
		return false;
	}

	public void UpdateEmployeesAssignedWorkStationItems()
	{
		if (RentedByPlayer)
		{
			List<EmployeeInstance> employeeInstances = EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
			{
				withAssignedAddress = Address
			});
			for (int i = 0; i < employeeInstances.Count; i++)
			{
				employeeInstances[i].UpdateAssignedWorkStationItems();
			}
		}
	}

	public void AddItemInstanceToBuilding(ItemInstance itemInstance)
	{
		itemInstance.AddressCached = Address;
		itemInstances.Add(itemInstance.id, itemInstance);
	}

	public void RemoveItemInstanceFromBuilding(ItemInstance itemInstance, bool triggerAction = true)
	{
		if (!itemInstances.Remove(itemInstance.id))
		{
			Debug.LogError("Tried to remove " + itemInstance.itemName + " from " + BusinessName + " but it wasn't part of the building inventory");
		}
		else
		{
			if (string.IsNullOrEmpty(itemInstance.parentId))
			{
				itemInstance.AddressCached = null;
			}
			if (triggerAction)
			{
				itemInstance.onInstanceRemoved?.Invoke();
			}
		}
	}

	public void OnBusinessTypeChanged()
	{
		foreach (LogisticsManagerPlan logisticsManagerPlan in SaveGameManager.Current.logisticsManagerPlans)
		{
			if (logisticsManagerPlan.targetAddress == Address)
			{
				logisticsManagerPlan.UnAssignAddress();
			}
		}
	}
}
