using System;
using System.Collections.Generic;
using AI.Employees.SalaryNegotiation;
using BigAmbitions.DayNightCycle;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.Items;
using BigAmbitions.Neighborhoods;
using BigAmbitions.Rivals;
using Boats;
using Buildings.Office.Headquarters;
using Entities;
using Helpers;
using Player.FoodDeliveryJob;
using Player.PlayerMissions;
using PlayerActivity;
using Seasons;
using UI.Notification;
using UI.Topbar.Accessories;
using UnityEngine;
using UnityEngine.Serialization;
using Vehicles;

[Serializable]
public class GameInstance
{
	[Serializable]
	public class DisabledLicensingFee
	{
		public Address address;

		public string itemId;

		public DayOfWeekOrdered day;
	}

	public int Day;

	public int Hour;

	public float Minute;

	public string seed;

	public float Money;

	public List<float> midnightBankBalances = new List<float>();

	public float Energy;

	public float EnergyGeneratedFromConsumables;

	public float Hunger;

	public float Happiness;

	public Timestamp timeEnteredTemporalBoost;

	public float currentActivityHappinessPerHour;

	public Timestamp nextRainStartTime = new Timestamp();

	public AccessoriesData accessoriesData = new AccessoriesData();

	public string characterId;

	public int numberOfDoctorOperations;

	public string CurrentStreetName;

	public int CurrentStreetNumber;

	public string ActiveVehicleId;

	public bool CityInitialized;

	public SerializableVector3 LastPlayerPosition = new Vector3(215f, 0f, 0f);

	public SerializableQuaternion LastPlayerRotation;

	public bool LastPlayerPause;

	public List<BuildingRegistration> BuildingRegistrations = new List<BuildingRegistration>();

	public List<JobInstance> JobInstances = new List<JobInstance>();

	public List<Contact> Contacts = new List<Contact>();

	public List<VehicleInstance> VehicleInstances = new List<VehicleInstance>();

	public List<NeighbourhoodStats> NeighbourhoodStats = new List<NeighbourhoodStats>();

	public List<EmployeeInstance> EmployeeInstances = new List<EmployeeInstance>();

	public List<EmployeeInstance> CandidateEmployeeInstances = new List<EmployeeInstance>();

	public List<Loan> Loans = new List<Loan>();

	public string SaveGameName;

	public int currentAutoSaveNumber;

	public Queue<Transaction> Transactions = new Queue<Transaction>(1000);

	[Obsolete("Since 1.0, use currentTaxPeriodDeductibleExpenses instead")]
	public List<(string, float)> CurrentTaxPeriodDeductibleTransactions = new List<(string, float)>();

	public List<TaxDeductibleExpense> currentTaxPeriodDeductibleExpenses = new List<TaxDeductibleExpense>();

	public float CurrentTaxPeriodGamblingWinnings;

	public float CurrentTaxPeriodGamblingLosses;

	public List<string> CompletedQuestEntries = new List<string>();

	public List<string> completedSideQuestEntries = new List<string>();

	public List<string> activeSideQuestEntries = new List<string>();

	public Address customDestination;

	public List<RecruitmentCampaign> RecruitmentCampaigns = new List<RecruitmentCampaign>();

	public List<DeliveryContract> DeliveryContracts = new List<DeliveryContract>();

	public List<FurnitureDeliveryContract> FurnitureDeliveryContracts = new List<FurnitureDeliveryContract>();

	public List<FoodDeliveryContract> FoodDeliveryContracts = new List<FoodDeliveryContract>();

	public List<Diploma> PlayerDiplomas = new List<Diploma>();

	public List<TodoTask> TodoTasks = new List<TodoTask>();

	public List<string> SelectedCitymapFilters = new List<string>();

	public List<string> CollapsedCitymapFilterCategories = new List<string>();

	public PlayerDefaults PlayerDefaults = new PlayerDefaults();

	public int buildNumberAtStart;

	public int buildNumberAtLastSave;

	public List<MarketEvent> marketEvents = new List<MarketEvent>();

	public List<FinancialSummary> financialSummaries = new List<FinancialSummary>();

	public List<string> completedPersonalGoals = new List<string>();

	public bool hasEverUsedMods;

	public List<CharacterData> charactersData = new List<CharacterData>();

	public float cityMapZoom;

	public float placementCameraZoom;

	public Taxes currentUnpaidTaxes;

	public float currentBackTaxes;

	public List<ProductMarketEntry> productMarketEntries = new List<ProductMarketEntry>();

	public List<BuildingForSale> buildingsForSale = new List<BuildingForSale>();

	public List<RealEstate> realEstate = new List<RealEstate>();

	public List<HappinessModifierData> happinessModifiers = new List<HappinessModifierData>();

	public List<string> usedHappinessModifiers = new List<string>();

	public GameVariables gameVariables = new GameVariables();

	public UIVariables uiVariables = new UIVariables();

	public Queue<Notification> notifications = new Queue<Notification>();

	public List<InvestmentFund> investmentFunds = new List<InvestmentFund>();

	public List<EmployeePreset> employeePresets = new List<EmployeePreset>();

	public AchievementsData achievementsData = new AchievementsData();

	public List<BoatData> playerBoats = new List<BoatData>();

	public List<HealthInsurancePlanOffer> healthInsurancePlanOffers = new List<HealthInsurancePlanOffer>();

	public List<CandidateSalaryNegotiation> candidateSalaryNegotiations = new List<CandidateSalaryNegotiation>();

	public List<InteriorInstallationFirmContract> interiorInstallationFirmContracts = new List<InteriorInstallationFirmContract>();

	public List<MovingServiceContract> movingServiceContracts = new List<MovingServiceContract>();

	public List<string> acquiredSpecialItems = new List<string>();

	public List<ItemWithSeasonalVisualsController.SpecialAddon> acquiredSpecialItemAddons = new List<ItemWithSeasonalVisualsController.SpecialAddon>();

	public List<SpecialRivalState> specialRivalStates = new List<SpecialRivalState>();

	public List<RivalState> rivalStates = new List<RivalState>();

	public string[] wholesaleRivalIds;

	public string[] importRivalIds;

	public Queue<int> daysWithComplaints = new Queue<int>();

	public List<Tuple<int, float>> playerWeeklyIncomeHistory = new List<Tuple<int, float>>();

	public List<Tuple<int, int>> playerNumberOfBusinessesHistory = new List<Tuple<int, int>>();

	[FormerlySerializedAs("currentWorkOutPlan")]
	public WorkoutPlan currentWorkoutPlan;

	public List<ImportPartnership> importPartnerships = new List<ImportPartnership>();

	public Dictionary<Address, List<ItemAmountTarget>> itemsOrderedThisWeekByImporter = new Dictionary<Address, List<ItemAmountTarget>>();

	public List<LogisticsManagerPlan> logisticsManagerPlans = new List<LogisticsManagerPlan>();

	public List<HeadhunterPlan> headhunterPlans = new List<HeadhunterPlan>();

	public List<HrManagerPlan> hrManagerPlans = new List<HrManagerPlan>();

	public List<PricingManagerPlan> pricingManagerPlans = new List<PricingManagerPlan>();

	public WallsVisibility wallsVisibility = WallsVisibility.PartlyHidden;

	public PlayerMission currentPlayerMission;

	public List<FoodDeliveryOffer> foodDeliveryOffers = new List<FoodDeliveryOffer>();

	public List<Address> deliveryJobLocationsDoneToday = new List<Address>();

	public bool hasCinemaTheaterTicket;

	public List<VehicleDeliveryContract> vehicleDeliveryContracts = new List<VehicleDeliveryContract>();

	public string activePrivateDriverContract;

	public bool activePrivateDriverContractUnpaid;

	public List<VehicleInstance> privateDriverVehicleInstances = new List<VehicleInstance>();

	public List<DisabledLicensingFee> disabledLicensingFees = new List<DisabledLicensingFee>();

	public List<(Address, string)> paidLicensingFeesToday = new List<(Address, string)>();

	public Dictionary<string, string> modData = new Dictionary<string, string>();

	public bool minigameMusicDisabled;

	[Obsolete("Since EA 0.8")]
	public HashSet<ItemInstance> WorldItemsHashSet;

	[Obsolete("Since EA 0.8")]
	public string InventoryItemInstanceId;

	[Obsolete("Since EA 0.9")]
	public List<SerializableColor> playerColors = new List<SerializableColor>();
}
