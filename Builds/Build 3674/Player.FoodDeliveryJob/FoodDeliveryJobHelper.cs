using System;
using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using BigAmbitions.GameAnalytics;
using BigAmbitions.Items;
using BigAmbitions.SoundSystem;
using BigAmbitions.Tags;
using Controllers;
using EmployeeStations;
using Extensions;
using Helpers;
using IngameDebugConsole;
using Localizor;
using Player.HUD.ItemWarningIcons;
using Player.PlayerMissions;
using Streets;
using UI;
using UI.Guiders;
using UI.Notification;
using UnityEngine;
using Vehicles.DeliveryDriverJob;

namespace Player.FoodDeliveryJob;

public static class FoodDeliveryJobHelper
{
	public const string CityMapFilterKey = "food_delivery_job_title";

	private const string AcceptButtonKey = "dialog_accept_button";

	private const string AlreadyOngoingMissionKey = "notification_already_ongoing_mission";

	private const string BackpackPrefabName = "DeliveryBackpack";

	private const string DeclineButtonKey = "dialog_decline_button";

	private const string NoAvailableDestinationsKey = "notification_no_available_destinations";

	private const string OfferDialogKey = "food_delivery_job_dialog";

	private const string DeliveryCompleteKey = "notification_food_delivery_complete";

	private const string DeliveryCompleteWithTipKey = "notification_food_delivery_complete_with_tip";

	private const string DeliveryFailedTimeUpKey = "notification_food_delivery_failed_timeup";

	private const string DeliveryCanceledKey = "notification_food_delivery_canceled";

	private const string PaymentAmountKey = "paymentAmount";

	private const string TipAmountKey = "tipAmount";

	private const string StartedJobState = "started";

	private const string CompletedJobState = "completed";

	private const string CanceledJobState = "canceled";

	private const int MaxAttemptsPerOffer = 10;

	private static readonly List<CityBuildingController> EligibleSources = new List<CityBuildingController>();

	private static readonly List<CityBuildingController> EligibleDestinations = new List<CityBuildingController>();

	private static readonly List<string> ItemPool = new List<string>();

	private static Transform Backpack;

	private static List<FoodDeliveryOffer> ActiveOffers
	{
		get
		{
			GameInstance current = SaveGameManager.Current;
			return current.foodDeliveryOffers ?? (current.foodDeliveryOffers = new List<FoodDeliveryOffer>());
		}
	}

	private static FoodDeliveryJobConfig Config => InstanceBehavior<GlobalReferences>.Instance.foodDeliveryJobConfig;

	public static void Init()
	{
		SetBackpackVisible(visible: false);
		GameInstance current = SaveGameManager.Current;
		if (current.foodDeliveryOffers == null)
		{
			current.foodDeliveryOffers = new List<FoodDeliveryOffer>();
		}
		GlobalEvents.RegisterOnGameLoadedLateCallback(OnGameLoadedLate);
	}

	public static void RunHourly()
	{
		PruneOffers();
		FillOffersToCount(Mathf.Min(ActiveOffers.Count + Config.NewOffersPerHour, Config.MaxActiveOffers));
		RefreshOfferPresentation();
	}

	public static void OnClickAcceptOffer(CashRegisterController cashRegisterController)
	{
		if (HudConfirm.isOpen || !ValidatePlayerCanAcceptOffer())
		{
			return;
		}
		Address address = cashRegisterController.BuildingContext.Building.Address;
		FoodDeliveryOffer offer = GetOffer(address);
		if (offer != null)
		{
			Vector3 firstQueuePosition = ((IWaitingLineHolder)cashRegisterController).GetFirstQueuePosition();
			InstanceBehavior<GameManager>.Instance.playerController.SetGoal(firstQueuePosition, delegate
			{
				PromptOffer(offer);
			});
		}
	}

	public static void SetBackpackVisible(bool visible)
	{
		ThirdPersonCharacter character = InstanceBehavior<GameManager>.Instance.playerController.Character;
		if ((bool)Backpack)
		{
			if (visible)
			{
				return;
			}
			character.RemoveUpperChestObject();
		}
		Backpack = null;
		if (visible)
		{
			Backpack = PrefabHelper.CreatePrefab("DeliveryBackpack").transform;
			AttachedObjectData attachedObjectData = new AttachedObjectData
			{
				objectTransform = Backpack,
				renderer = Backpack.GetComponent<Renderer>(),
				position = Config.BackpackLocalPosition,
				rotation = Quaternion.Euler(Config.BackpackLocalRotation)
			};
			character.AttachObjectToUpperChest(attachedObjectData);
		}
	}

	public static bool HasActiveOffer(Address address)
	{
		return GetOffer(address) != null;
	}

	public static bool TryDeliverToAddress(Address address)
	{
		if (!(SaveGameManager.Current.currentPlayerMission is FoodDeliveryMission foodDeliveryMission) || foodDeliveryMission.destinationAddress != address)
		{
			return false;
		}
		if (!foodDeliveryMission.IsOngoing())
		{
			ExpireMission(foodDeliveryMission);
			return true;
		}
		CompleteMission(foodDeliveryMission);
		return true;
	}

	public static bool TryExpireMission()
	{
		if (!(SaveGameManager.Current?.currentPlayerMission is FoodDeliveryMission foodDeliveryMission) || foodDeliveryMission.IsOngoing())
		{
			return false;
		}
		ExpireMission(foodDeliveryMission);
		return true;
	}

	public static FoodDeliveryMission RestoreMission()
	{
		if (!(SaveGameManager.Current.currentPlayerMission is FoodDeliveryMission foodDeliveryMission))
		{
			return null;
		}
		if (!foodDeliveryMission.IsOngoing())
		{
			ExpireMission(foodDeliveryMission);
			return null;
		}
		SetBackpackVisible(visible: true);
		Transform addressEntranceTransform = BuildingHelper.GetAddressEntranceTransform(foodDeliveryMission.destinationAddress);
		if ((bool)addressEntranceTransform)
		{
			GuidersManager.SetGuiderTarget(addressEntranceTransform.position, foodDeliveryMission.destinationAddress.ToFormattedString(), Config.MapIcon, Config.PoiColor, DirectionGuiderType.JobDestination);
		}
		return foodDeliveryMission;
	}

	public static void CancelMission(FoodDeliveryMission mission)
	{
		if (SaveGameManager.Current.currentPlayerMission == mission)
		{
			if (!mission.IsOngoing())
			{
				ExpireMission(mission);
				return;
			}
			GameAnalytics.TrackFoodDeliveryJob("canceled", 0);
			Notifications.Show(NotificationType.Info, "notification_food_delivery_canceled");
			EndMission(mission);
		}
	}

	private static void OnGameLoadedLate()
	{
		PruneOffers();
		if (ActiveOffers.Count == 0)
		{
			RunHourly();
		}
		else
		{
			RefreshOfferPresentation();
		}
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
	}

	private static void OnBuildingRegistrationChange(Address address)
	{
		if (HasOfferAt(address))
		{
			PruneOffers();
			RefreshOfferPresentation();
		}
	}

	private static bool HasOfferAt(Address address)
	{
		foreach (FoodDeliveryOffer activeOffer in ActiveOffers)
		{
			if (activeOffer != null && (activeOffer.pickupAddress == address || activeOffer.destinationAddress == address))
			{
				return true;
			}
		}
		return false;
	}

	private static void RefreshOfferPresentation()
	{
		if (InstanceBehavior<UIs>.IsInitialized)
		{
			InstanceBehavior<UIs>.Instance.mapFilters.ApplyFilters();
		}
		if (InstanceBehavior<ItemWarningIconManager>.IsInitialized)
		{
			InstanceBehavior<ItemWarningIconManager>.Instance.RefreshCurrentBuildingWarningIcons();
		}
	}

	private static FoodDeliveryOffer GetOffer(Address address)
	{
		foreach (FoodDeliveryOffer activeOffer in ActiveOffers)
		{
			if (activeOffer != null && activeOffer.pickupAddress == address && !IsOfferExpired(activeOffer))
			{
				return activeOffer;
			}
		}
		return null;
	}

	private static bool IsOfferExpired(FoodDeliveryOffer offer)
	{
		if (offer.IsExpired())
		{
			if (BuildingManager.IsInsideBuilding)
			{
				return InstanceBehavior<BuildingManager>.Instance.building.Address != offer.pickupAddress;
			}
			return true;
		}
		return false;
	}

	private static void RemoveOffer(FoodDeliveryOffer offer)
	{
		if (ActiveOffers.Remove(offer))
		{
			RefreshOfferPresentation();
		}
	}

	private static void PruneOffers()
	{
		for (int num = ActiveOffers.Count - 1; num >= 0; num--)
		{
			FoodDeliveryOffer foodDeliveryOffer = ActiveOffers[num];
			if (foodDeliveryOffer == null || IsOfferExpired(foodDeliveryOffer) || !IsOfferValid(foodDeliveryOffer))
			{
				ActiveOffers.RemoveAt(num);
			}
		}
		while (ActiveOffers.Count > Config.MaxActiveOffers)
		{
			ActiveOffers.RemoveAt(ActiveOffers.Count - 1);
		}
	}

	private static bool IsOfferValid(FoodDeliveryOffer offer)
	{
		if (!IsEligibleSource(BuildingHelper.GetBuildingRegistration(offer.pickupAddress)))
		{
			return false;
		}
		Transform addressEntranceTransform = BuildingHelper.GetAddressEntranceTransform(offer.pickupAddress);
		if (!addressEntranceTransform)
		{
			return false;
		}
		return IsEligibleDestination(InstanceBehavior<CityManager>.Instance.FindCityBuildingController(offer.destinationAddress), offer.pickupAddress, addressEntranceTransform.position);
	}

	private static void PromptOffer(FoodDeliveryOffer offer)
	{
		if (!HudConfirm.isOpen && ValidatePlayerCanAcceptOffer() && ValidateOfferStillAvailable(offer))
		{
			string destinationAddress = offer.destinationAddress.ToFormattedString();
			int timeLimitMinutes = offer.timeLimitMinutes;
			string deliveryReward = offer.deliveryReward.ToShortCurrencyFormat();
			InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.FoodDeliveryJob);
			HudConfirm.Show("food_delivery_job_title".Localize(), "food_delivery_job_dialog".Localize(new { destinationAddress, timeLimitMinutes, deliveryReward }), delegate
			{
				AcceptOffer(offer);
			}, DeclineOffer, "dialog_accept_button", "dialog_decline_button");
		}
	}

	private static void AcceptOffer(FoodDeliveryOffer offer)
	{
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.FoodDeliveryJob);
		if (ValidatePlayerCanAcceptOffer() && ValidateOfferStillAvailable(offer))
		{
			Timestamp startTime = TimeHelper.Now();
			Timestamp timestamp = TimeHelper.Now();
			timestamp.AddMinutes(offer.timeLimitMinutes);
			FoodDeliveryMission currentPlayerMission = new FoodDeliveryMission
			{
				startTime = startTime,
				endTime = timestamp,
				timeLimitMinutes = offer.timeLimitMinutes,
				destinationAddress = offer.destinationAddress,
				items = offer.items,
				deliveryReward = offer.deliveryReward
			};
			SaveGameManager.Current.currentPlayerMission = currentPlayerMission;
			RemoveOffer(offer);
			GameEvent.Invoke("ba:gameevent_newjob");
			GameAnalytics.TrackFoodDeliveryJob("started", 0);
			InstanceBehavior<UIs>.Instance.tasksUI.foodDeliveryJobUI.Init();
			InstanceBehavior<SfxManager>.Instance?.PlayAudio(SoundType.AddProductToBasket, PlayerHelper.GetPosition());
		}
	}

	private static void DeclineOffer()
	{
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.FoodDeliveryJob);
	}

	private static bool ValidatePlayerCanAcceptOffer()
	{
		if (SaveGameManager.Current.currentPlayerMission != null)
		{
			Notifications.ShowError("notification_already_ongoing_mission");
			return false;
		}
		return true;
	}

	private static bool ValidateOfferStillAvailable(FoodDeliveryOffer offer)
	{
		if (IsOfferValidForAcceptance(offer))
		{
			return true;
		}
		if (offer != null && ActiveOffers.Contains(offer))
		{
			RemoveOffer(offer);
		}
		Notifications.ShowError("notification_no_available_destinations");
		return false;
	}

	private static bool IsOfferValidForAcceptance(FoodDeliveryOffer offer)
	{
		if (offer == null || !ActiveOffers.Contains(offer) || (object)offer.pickupAddress == null || (object)offer.destinationAddress == null || IsOfferExpired(offer))
		{
			return false;
		}
		return IsOfferValid(offer);
	}

	private static void CompleteMission(FoodDeliveryMission mission)
	{
		(float tipAmount, bool wasFastDelivery) tuple = RollTip(mission);
		float item = tuple.tipAmount;
		bool item2 = tuple.wasFastDelivery;
		float amount = mission.deliveryReward + item;
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_fooddeliveryjobwage", "ba:transactioncategory_salaryincome");
		GameManager.ChangeMoneySafe(amount, transactionInfo);
		Dictionary<string, string> dictionary = new Dictionary<string, string> { 
		{
			"paymentAmount",
			mission.deliveryReward.ToShortCurrencyFormat()
		} };
		if (item > 0f)
		{
			dictionary.Add("tipAmount", item.ToShortCurrencyFormat());
			Notifications.Show(NotificationType.Success, "notification_food_delivery_complete_with_tip", dictionary);
		}
		else
		{
			Notifications.Show(NotificationType.Success, "notification_food_delivery_complete", dictionary);
		}
		GameAnalytics.TrackFoodDeliveryJob("completed", 1);
		EndMission(mission);
		if (InstanceBehavior<UIs>.IsInitialized)
		{
			InstanceBehavior<UIs>.Instance.dailySummary.RunFoodDeliverySummary(mission.deliveryReward, item, item2);
		}
	}

	private static (float tipAmount, bool wasFastDelivery) RollTip(FoodDeliveryMission mission)
	{
		DeliveryJobTipsConfig tipsConfig = Config.TipsConfig;
		if (!tipsConfig)
		{
			return (tipAmount: 0f, wasFastDelivery: false);
		}
		bool flag = tipsConfig.IsFastDelivery(mission.startTime.GetDifferenceInMinutes(TimeHelper.Now()), mission.timeLimitMinutes);
		return (tipAmount: tipsConfig.RollTip(flag), wasFastDelivery: flag);
	}

	private static void ExpireMission(FoodDeliveryMission mission)
	{
		GameAnalytics.TrackFoodDeliveryJob("completed", 0);
		Notifications.Show(NotificationType.Warning, "notification_food_delivery_failed_timeup");
		EndMission(mission);
	}

	private static void EndMission(FoodDeliveryMission mission)
	{
		if (SaveGameManager.Current.currentPlayerMission == mission)
		{
			SaveGameManager.Current.currentPlayerMission = null;
			SetBackpackVisible(visible: false);
			GuidersManager.ResetGuider(DirectionGuiderType.JobDestination);
			if (InstanceBehavior<UIs>.IsInitialized)
			{
				InstanceBehavior<UIs>.Instance.tasksUI.foodDeliveryJobUI.Hide();
			}
		}
	}

	private static void FillOffersToCount(int targetCount)
	{
		if (ActiveOffers.Count >= targetCount)
		{
			return;
		}
		FillEligibleSources();
		int num = (targetCount - ActiveOffers.Count) * 10;
		for (int i = 0; i < num; i++)
		{
			if (ActiveOffers.Count >= targetCount)
			{
				break;
			}
			if (EligibleSources.Count <= 0)
			{
				break;
			}
			int index = UnityEngine.Random.Range(0, EligibleSources.Count);
			CityBuildingController pickup = EligibleSources[index];
			EligibleSources[index] = EligibleSources[EligibleSources.Count - 1];
			EligibleSources.RemoveAt(EligibleSources.Count - 1);
			TryCreateOfferAt(pickup);
		}
	}

	private static bool TryCreateOfferAt(CityBuildingController pickup)
	{
		List<ItemAmountTarget> list = GenerateOrderItems(pickup.buildingRegistration);
		if (list == null)
		{
			return false;
		}
		Transform addressEntranceTransform = BuildingHelper.GetAddressEntranceTransform(pickup.building.Address);
		if (!addressEntranceTransform)
		{
			return false;
		}
		Address address = PickDestinationAddress(pickup.building.Address, addressEntranceTransform.position);
		if (address == null)
		{
			return false;
		}
		Transform addressEntranceTransform2 = BuildingHelper.GetAddressEntranceTransform(address);
		float num = Vector3.Distance(addressEntranceTransform.position, addressEntranceTransform2.position);
		FoodDeliveryJobConfig config = Config;
		Vector2Int timeLimitMinutes = config.TimeLimitMinutes;
		Timestamp timestamp = TimeHelper.Now();
		timestamp.AddMinutes(config.OfferActiveMinutes.RandomValue());
		ActiveOffers.Add(new FoodDeliveryOffer
		{
			pickupAddress = pickup.building.Address,
			destinationAddress = address,
			items = list,
			expireTime = timestamp,
			deliveryReward = Mathf.Round(config.BaseReward + num * config.RewardPerMeter),
			timeLimitMinutes = Mathf.Clamp(config.BaseTimeMinutes + Mathf.CeilToInt(num * config.MinutesPerMeter), timeLimitMinutes.x, timeLimitMinutes.y)
		});
		return true;
	}

	private static void FillEligibleSources()
	{
		EligibleSources.Clear();
		CityBuildingController[] cityBuildingControllers = InstanceBehavior<CityManager>.Instance.cityBuildingControllers;
		foreach (CityBuildingController cityBuildingController in cityBuildingControllers)
		{
			if ((bool)cityBuildingController.building && IsEligibleSource(cityBuildingController.buildingRegistration) && !HasActiveOffer(cityBuildingController.building.Address))
			{
				EligibleSources.Add(cityBuildingController);
			}
		}
	}

	private static bool IsEligibleSource(BuildingRegistration registration)
	{
		if (registration != null && !registration.RentedByPlayer && !registration.BuildingOwnedByPlayer && Config.SourceBusinessTypes != null && Array.IndexOf(Config.SourceBusinessTypes, registration.businessTypeName) != -1 && BusinessHelper.IsBusinessOpen(registration))
		{
			return Array.IndexOf(Config.ExcludedPickupAddresses, registration.Address) == -1;
		}
		return false;
	}

	private static List<ItemAmountTarget> GenerateOrderItems(BuildingRegistration registration)
	{
		ItemPool.Clear();
		FillItemPool(registration.GetListOfItemsForSale());
		if (ItemPool.Count == 0)
		{
			FillItemPool(BusinessTypeHelper.GetPrimaryRetailProducts(registration.businessTypeName));
		}
		if (ItemPool.Count == 0)
		{
			return null;
		}
		int num = Mathf.Min(Config.DistinctItemsPerOrder.RandomValue(), ItemPool.Count);
		List<ItemAmountTarget> list = new List<ItemAmountTarget>(num);
		for (int i = 0; i < num; i++)
		{
			string random = ItemPool.GetRandom();
			ItemPool.Remove(random);
			list.Add(new ItemAmountTarget(random, UnityEngine.Random.Range(1, Config.MaxAmountPerItem + 1)));
		}
		return list;
	}

	private static void FillItemPool(List<string> itemNames)
	{
		foreach (string itemName in itemNames)
		{
			if (ItemsGetter.GetByName(itemName).HasTag(TagRef.Itemtag.fooddelivery))
			{
				ItemPool.Add(itemName);
			}
		}
	}

	private static Address PickDestinationAddress(Address pickupAddress, Vector3 pickupEntrancePosition)
	{
		EligibleDestinations.Clear();
		CityBuildingController[] cityBuildingControllers = InstanceBehavior<CityManager>.Instance.cityBuildingControllers;
		foreach (CityBuildingController cityBuildingController in cityBuildingControllers)
		{
			if (IsEligibleDestination(cityBuildingController, pickupAddress, pickupEntrancePosition))
			{
				EligibleDestinations.Add(cityBuildingController);
			}
		}
		if (EligibleDestinations.Count != 0)
		{
			return EligibleDestinations.GetRandom().building.Address;
		}
		return null;
	}

	private static bool IsEligibleDestination(CityBuildingController controller, Address pickupAddress, Vector3 pickupEntrancePosition)
	{
		if (!controller || !controller.building || controller.building.Address == pickupAddress)
		{
			return false;
		}
		BuildingRegistration buildingRegistration = controller.buildingRegistration;
		if (buildingRegistration == null || buildingRegistration.RentedByPlayer || buildingRegistration.BuildingOwnedByPlayer || buildingRegistration.AvailableForRent || controller.building.IsHamptonsHouse() || Config.DestinationBuildingTypes == null || Array.IndexOf(Config.DestinationBuildingTypes, controller.building.BuildingType) == -1 || Array.IndexOf(Config.ExcludedDeliveryAddresses, controller.building.Address) != -1)
		{
			return false;
		}
		Transform addressEntranceTransform = BuildingHelper.GetAddressEntranceTransform(controller.building.Address);
		if (!addressEntranceTransform)
		{
			return false;
		}
		float num = Config.DestinationRadius * Config.DestinationRadius;
		return (addressEntranceTransform.position - pickupEntrancePosition).sqrMagnitude <= num;
	}

	[ConsoleMethod("GenerateFoodDeliveryOffers", "Fills food delivery offers up to the configured maximum.", new string[] { })]
	public static void Command_GenerateOffers()
	{
		PruneOffers();
		FillOffersToCount(Config.MaxActiveOffers);
		RefreshOfferPresentation();
	}

	[ConsoleMethod("ClearFoodDeliveryOffers", "Removes all active food delivery offers.", new string[] { })]
	public static void Command_ClearOffers()
	{
		ActiveOffers.Clear();
		RefreshOfferPresentation();
	}

	[ConsoleMethod("GenerateFoodDeliveryOfferHere", "Creates a food delivery offer for the business you are standing in.", new string[] { })]
	public static void Command_GenerateOfferHere()
	{
		if (!BuildingManager.IsInsideBuilding)
		{
			Debug.Log("Food delivery: you are not inside a building.");
			return;
		}
		Address address = InstanceBehavior<BuildingManager>.Instance.building.Address;
		string text = address.ToFormattedString();
		if (HasActiveOffer(address))
		{
			Debug.Log("Food delivery: " + text + " already has an active offer.");
			return;
		}
		if (!IsEligibleSource(InstanceBehavior<BuildingManager>.Instance.buildingRegistration))
		{
			Debug.Log("Food delivery: " + text + " is not an eligible pickup. It has to be an open, non-player business whose type is listed in the config's source business types, and it must not be one of the config's excluded pickup addresses.");
			return;
		}
		CityBuildingController cityBuildingController = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(address);
		if (!cityBuildingController || !TryCreateOfferAt(cityBuildingController))
		{
			Debug.Log("Food delivery: no offer could be built at " + text + ". It sells nothing tagged for food delivery, or has no eligible destination within the configured radius.");
			return;
		}
		RefreshOfferPresentation();
		Debug.Log("Food delivery: created an offer at " + text + ".");
		if (ActiveOffers.Count > Config.MaxActiveOffers)
		{
			Debug.Log($"Food delivery: this is over the cap of {Config.MaxActiveOffers}, so the next prune " + "drops it again. Accept or check it before the hour rolls over.");
		}
	}

	[ConsoleMethod("LogFoodDeliveryOffers", "Logs all active food delivery offers.", new string[] { })]
	public static void Command_LogOffers()
	{
		Debug.Log($"Active food delivery offers: {ActiveOffers.Count}");
		foreach (FoodDeliveryOffer activeOffer in ActiveOffers)
		{
			string text = string.Empty;
			foreach (ItemAmountTarget item in activeOffer.items)
			{
				text += $"{item.targetAmount}x {item.itemName}, ";
			}
			Debug.Log(activeOffer.pickupAddress.ToFormattedString() + " -> " + activeOffer.destinationAddress.ToFormattedString() + " | " + text + $"reward {activeOffer.deliveryReward}, limit {activeOffer.timeLimitMinutes}m");
		}
	}
}
