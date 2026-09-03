using System;
using System.Collections.Generic;
using System.Linq;
using BA_Packages.Rivals.Scripts;
using BigAmbitions.Characters;
using BigAmbitions.Tags;
using Blueprints;
using Buildings;
using Entities;
using Extensions;
using Helpers;
using IngameDebugConsole;
using JimmysUnityUtilities;
using UI;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;

namespace BigAmbitions.Rivals;

public static class RivalsHelper
{
	public const string SpecialRivalsAddressableLabel = "Rivals";

	public const string RivalsSettingsAddressableLabel = "RivalsSettings";

	public const int NumberOfInitialRivals = 19;

	public const int NumberOfWholesaleRivals = 7;

	public const int NumberOfImportRivals = 8;

	private static readonly Dictionary<string, RivalData> RivalDataCache = new Dictionary<string, RivalData>();

	private static readonly Dictionary<string, SpecialRival> SpecialRivalCache = new Dictionary<string, SpecialRival>();

	private static readonly Dictionary<string, SpecialRival> SpecialRivalByNeighborhoodCache = new Dictionary<string, SpecialRival>();

	private static SpecialRival[] SpecialRivalsCache;

	private static List<string> PlannedMessages;

	private static List<string> SentMessages;

	private static RivalsSettings RivalsSettings;

	public static readonly List<Address> IgnoredAddresses = new List<Address>
	{
		new Address("ba:street_thirdstreet", 23)
	};

	public static bool IsFeatureEnabled => SaveGameManager.Current.gameVariables.rivalsDifficultyMultiplier > 0.001f;

	public static void OnSpecialRivalsLoaded(IList<SpecialRival> specialRivals)
	{
		SpecialRivalsCache = specialRivals.ToArray();
	}

	public static void OnRivalsSettingsLoaded(IList<RivalsSettings> rivalsSettings)
	{
		RivalsSettings = rivalsSettings.FirstOrDefault();
	}

	public static void RunHourly()
	{
		CheckRivalTimelines();
		RivalDefenseHelper.RunHourly();
	}

	public static void RunDaily()
	{
		if (CompetitionHelper.AreRecalculationsPending())
		{
			CoroutineUtility.RunAfterFrameDelay(RunDaily, 1);
			return;
		}
		RefreshRivals();
		int day = SaveGameManager.Current.Day;
		int startingDay = day - 6;
		foreach (RivalState rivalState2 in SaveGameManager.Current.rivalStates)
		{
			RivalData rivalData = GetRivalData(rivalState2.rivalId);
			RivalState rivalState = rivalState2;
			if (rivalState.weeklyIncomeHistory == null)
			{
				rivalState.weeklyIncomeHistory = new List<Tuple<int, float>>();
			}
			rivalState = rivalState2;
			if (rivalState.numberOfBusinessesHistory == null)
			{
				rivalState.numberOfBusinessesHistory = new List<Tuple<int, int>>();
			}
			rivalState2.weeklyIncomeHistory.Add(new Tuple<int, float>(day, rivalData.WeeklyIncome));
			if (rivalState2.weeklyIncomeHistory.Count > 7)
			{
				rivalState2.weeklyIncomeHistory.RemoveAll((Tuple<int, float> x) => x.Item1 < startingDay);
			}
			rivalState2.numberOfBusinessesHistory.Add(new Tuple<int, int>(day, rivalData.ownedBusinesses.Count));
			if (rivalState2.numberOfBusinessesHistory.Count > 7)
			{
				rivalState2.numberOfBusinessesHistory.RemoveAll((Tuple<int, int> x) => x.Item1 < startingDay);
			}
		}
	}

	public static SpecialRival GetSpecialRivalByNeighborhood(string neighborhood)
	{
		if (SpecialRivalByNeighborhoodCache.TryGetValue(neighborhood, out var value))
		{
			return value;
		}
		SpecialRival specialRival = SpecialRivalCache.Values.FirstOrDefault((SpecialRival x) => x.primaryNeighborhood == neighborhood);
		if (specialRival != null)
		{
			SpecialRivalByNeighborhoodCache.Add(neighborhood, specialRival);
		}
		return specialRival;
	}

	public static string GetRandomRivalForBuilding(string neighborhood = null, bool isHamptonsHouse = false)
	{
		if (isHamptonsHouse)
		{
			return GetRandomRivalSuitableForAHamptonsHouse();
		}
		if (string.IsNullOrEmpty(neighborhood))
		{
			return RivalDataCache.Values.GetRandom().id;
		}
		if (new System.Random().PercentageChance(24))
		{
			SpecialRival specialRivalByNeighborhood = GetSpecialRivalByNeighborhood(neighborhood);
			if (specialRivalByNeighborhood != null && !IsRivalDefeated(specialRivalByNeighborhood.rivalData.id))
			{
				return specialRivalByNeighborhood.rivalData.id;
			}
		}
		return GetNonSpecialRivals().GetRandom().id;
	}

	private static string GetRandomRivalSuitableForAHamptonsHouse()
	{
		List<RivalData> nonSpecialRivals = GetNonSpecialRivals();
		for (int num = nonSpecialRivals.Count - 1; num >= 0; num--)
		{
			foreach (BuildingRegistration ownedBuilding in nonSpecialRivals[num].ownedBuildings)
			{
				if (ownedBuilding.BuildingCached.IsHamptonsHouse())
				{
					nonSpecialRivals.RemoveAt(num);
					break;
				}
			}
		}
		return nonSpecialRivals.GetRandom().id;
	}

	public static bool IsRivalDefeated(string rivalId)
	{
		if (rivalId.IsSpecialRival())
		{
			return GetSpecialRivalState(rivalId)?.isDefeated ?? false;
		}
		RivalData rivalData = GetRivalData(rivalId);
		if (rivalData == null)
		{
			return true;
		}
		return rivalData.ownedBusinesses.Count <= 0;
	}

	public static AiBusinessDefault GetRandomBusinessDefault(this ICollection<AiBusinessDefault> businessDefaults, BuildingRegistration registration)
	{
		SpecialRival specialRivalByNeighborhood = GetSpecialRivalByNeighborhood(registration.Neighborhood);
		bool flag = specialRivalByNeighborhood != null && IsRivalDefeated(specialRivalByNeighborhood.rivalData.id);
		BuildingSizeInfo sizeInfo = new BuildingSizeInfo(registration);
		bool flag2 = specialRivalByNeighborhood != null && !flag && IsBuildingSuitableForSpecialRival(registration.BuildingCached.BuildingType, sizeInfo);
		bool flag3 = IsBuildingSuitableForImporterRival(registration.BuildingCached.BuildingType, sizeInfo);
		bool flag4 = IsBuildingSuitableForWholesalerRival(registration.BuildingCached.BuildingType, sizeInfo);
		if ((flag2 && !flag4 && !flag3) || (flag2 && new System.Random().PercentageChance(40)))
		{
			return businessDefaults.GetRandomBusinessDefault(specialRivalByNeighborhood);
		}
		if (!flag3 && !flag4)
		{
			return null;
		}
		if (flag3 && !flag4)
		{
			return businessDefaults.GetRandomBusinessDefault(null, AiBusinessGoodsSource.Import);
		}
		if (!flag3)
		{
			return businessDefaults.GetRandomBusinessDefault(null, AiBusinessGoodsSource.Wholesale);
		}
		if (!RngHelper.Chance(50))
		{
			return businessDefaults.GetRandomBusinessDefault(null, AiBusinessGoodsSource.Wholesale);
		}
		return businessDefaults.GetRandomBusinessDefault(null, AiBusinessGoodsSource.Import);
	}

	public static bool IsBuildingSuitableForWholesalerRival(string buildingType, BuildingSizeInfo sizeInfo)
	{
		return IsBuildingSuitableForRival(buildingType, sizeInfo, RivalsSettings.wholesalerRivalRetailBuildingSizes, RivalsSettings.wholesalerRivalOfficeBuildingSizes);
	}

	public static bool IsBuildingSuitableForImporterRival(string buildingType, BuildingSizeInfo sizeInfo)
	{
		return IsBuildingSuitableForRival(buildingType, sizeInfo, RivalsSettings.importerRivalRetailBuildingSizes, RivalsSettings.importerRivalOfficeBuildingSizes);
	}

	public static bool IsBuildingSuitableForSpecialRival(string buildingType, BuildingSizeInfo sizeInfo)
	{
		return IsBuildingSuitableForRival(buildingType, sizeInfo, RivalsSettings.specialRivalRetailBuildingSizes, RivalsSettings.specialRivalOfficeBuildingSizes, RivalsSettings.cinemaTheaterBuildingSizes);
	}

	private static bool IsBuildingSuitableForRival(string buildingType, BuildingSizeInfo sizeInfo, List<BuildingSizeInfo> rivalRetailBuildingSizes, List<BuildingSizeInfo> rivalOfficeBuildingSizes, List<BuildingSizeInfo> otherBuildingSizes = null)
	{
		if ((!(buildingType == "ba:buildingtype_retail") || !rivalRetailBuildingSizes.Contains(sizeInfo)) && (!(buildingType == "ba:buildingtype_office") || !rivalOfficeBuildingSizes.Contains(sizeInfo)))
		{
			return otherBuildingSizes?.Contains(sizeInfo) ?? false;
		}
		return true;
	}

	public static AiBusinessDefault GetRandomBusinessDefault(this ICollection<AiBusinessDefault> businessDefaults, SpecialRival specialRival, AiBusinessGoodsSource goodsSource = AiBusinessGoodsSource.Wholesale)
	{
		if (!(specialRival != null))
		{
			return businessDefaults.Where((AiBusinessDefault x) => string.IsNullOrEmpty(x.corporationRivalId) && x.goodsSource == goodsSource).GetRandom();
		}
		return businessDefaults.Where((AiBusinessDefault x) => x.corporationRivalId == specialRival.rivalData.id || x.corporationRivalId == "*").GetRandom();
	}

	public static void DefeatRival(RivalData rival)
	{
		if (rival.id.IsSpecialRival())
		{
			SpecialRivalState specialRivalState = GetSpecialRivalState(rival.id);
			if (specialRivalState == null)
			{
				OnRivalDefeat(rival);
				return;
			}
			if (specialRivalState != null && specialRivalState.isDefeated)
			{
				return;
			}
			specialRivalState.isActive = false;
			specialRivalState.isDefeated = true;
		}
		OnRivalDefeat(rival);
	}

	public static void OnRivalDefeat(RivalData rival)
	{
		if (rival.ownedBusinesses.Count > 0)
		{
			ShutdownAllRivalBusinesses(rival);
		}
		if (rival.ownedBuildings.Count > 0)
		{
			SellAllRealEstate(rival.id);
		}
		if (rival.id.IsSpecialRival())
		{
			StopSpecialRivalAttacks(rival);
		}
		rival.ownedBuildings.Clear();
		rival.ownedBusinesses.Clear();
	}

	private static void ShutdownAllRivalBusinesses(RivalData rival)
	{
		CompetitionHelper.ShutdownBusinessesImmediate(SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.businessOwnerRivalId == rival.id));
		RefreshRivals();
		CheckRivalTimelines();
	}

	private static void SellAllRealEstate(string rivalId)
	{
		foreach (BuildingRegistration item in SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.buildingOwnerRivalId == rivalId))
		{
			RealEstateHelper.SetBuildingForSale(item);
		}
	}

	private static void StopSpecialRivalAttacks(RivalData rival)
	{
		SpecialRivalState specialRivalState = GetSpecialRivalState(rival.id);
		specialRivalState.isActive = false;
		if (specialRivalState?.defenseStates == null)
		{
			return;
		}
		foreach (DefenseState defenseState in specialRivalState.defenseStates)
		{
			if (defenseState.defensiveMechanic != DefensiveMechanic.HireBestEmployees)
			{
				continue;
			}
			foreach (EmployeeInstance item in defenseState.affectedEmployeeIds.Select((string x) => EmployeeHelper.GetEmployeeById(x)))
			{
				item?.StopPoachingRivalSurrender(rival.rivalName);
			}
		}
		specialRivalState.defenseStates.Clear();
	}

	public static void FillData(List<string> rivalsIds)
	{
		RivalDataCache.Clear();
		SpecialRivalCache.Clear();
		RivalData[] array = new RivalData[rivalsIds.Count];
		int i;
		for (i = 0; i < rivalsIds.Count; i++)
		{
			if (RivalDataCache.ContainsKey(rivalsIds[i]))
			{
				continue;
			}
			System.Random random = new System.Random(rivalsIds[i].GetHashCode());
			SpecialRival specialRival = SpecialRivalsCache.FirstOrDefault((SpecialRival x) => x.rivalData.id == rivalsIds[i]);
			List<BuildingRegistration> list = SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.businessOwnerRivalId == rivalsIds[i]).ToList();
			List<BuildingRegistration> ownedBuildings = SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.buildingOwnerRivalId == rivalsIds[i]).ToList();
			List<BuildingRegistration> list2 = new List<BuildingRegistration>();
			foreach (BuildingRegistration item in list)
			{
				if (BuildingTypeHelper.GetData(item).HasTag(TagRef.Buildingtypetag.canbeownedbyrival))
				{
					list2.Add(item);
				}
			}
			Gender gender = random.GetGender();
			array[i] = new RivalData
			{
				id = rivalsIds[i],
				rivalName = ((specialRival != null) ? specialRival.rivalData.rivalName : random.GetFullName(gender)),
				gender = ((specialRival != null) ? specialRival.rivalData.gender : gender),
				ownedBusinesses = list,
				ownedBuildings = ownedBuildings,
				ownedRetailOfficeBusinesses = list2,
				startingAgeInYears = ((specialRival != null) ? specialRival.rivalData.startingAgeInYears : random.Next(21, 55))
			};
			if (specialRival != null)
			{
				specialRival.rivalData = array[i];
				SpecialRivalCache.Add(array[i].id, specialRival);
			}
			RivalDataCache.Add(array[i].id, array[i]);
		}
		SpecialRival[] specialRivalsCache = SpecialRivalsCache;
		foreach (SpecialRival specialRival2 in specialRivalsCache)
		{
			GlobalEvents.onGameUnloaded = (Action)Delegate.Combine(GlobalEvents.onGameUnloaded, new Action(specialRival2.timeline.StopPlannedEntriesCoroutine));
		}
	}

	public static void FillRivalState(string rivalId)
	{
		RivalData rivalData = GetRivalData(rivalId);
		RivalState rivalState = GetRivalState(rivalData.id);
		RivalState rivalState2 = rivalState;
		if (rivalState2.weeklyIncomeHistory == null)
		{
			rivalState2.weeklyIncomeHistory = new List<Tuple<int, float>>();
		}
		rivalState2 = rivalState;
		if (rivalState2.numberOfBusinessesHistory == null)
		{
			rivalState2.numberOfBusinessesHistory = new List<Tuple<int, int>>();
		}
		int num = SaveGameManager.Current.Day - 6;
		for (int i = 0; i < 7; i++)
		{
			int currentDay = num + i;
			if (rivalState.weeklyIncomeHistory.All((Tuple<int, float> x) => x.Item1 != currentDay))
			{
				if (currentDay == SaveGameManager.Current.Day)
				{
					rivalState.weeklyIncomeHistory.Add(new Tuple<int, float>(SaveGameManager.Current.Day, rivalData.WeeklyIncome));
				}
				else
				{
					rivalState.weeklyIncomeHistory.Add(new Tuple<int, float>(currentDay, UnityEngine.Random.Range(rivalData.WeeklyIncome * UnityEngine.Random.Range(0.8f, 0.98f), rivalData.WeeklyIncome * UnityEngine.Random.Range(1.02f, 1.2f))));
				}
			}
			if (rivalState.numberOfBusinessesHistory.All((Tuple<int, int> x) => x.Item1 != currentDay))
			{
				rivalState.numberOfBusinessesHistory.Add(new Tuple<int, int>(currentDay, rivalData.ownedBusinesses.Count));
			}
		}
		rivalState.weeklyIncomeHistory = rivalState.weeklyIncomeHistory.OrderByDescending((Tuple<int, float> x) => x.Item1).Take(7).ToList();
		rivalState.numberOfBusinessesHistory = rivalState.numberOfBusinessesHistory.OrderByDescending((Tuple<int, int> x) => x.Item1).Take(7).ToList();
	}

	public static void RefreshRivals(bool onlySpecialRivals = false)
	{
		IEnumerable<RivalData> enumerable;
		if (!onlySpecialRivals)
		{
			IEnumerable<RivalData> values = RivalDataCache.Values;
			enumerable = values;
		}
		else
		{
			enumerable = SpecialRivalCache.Values.Select((SpecialRival x) => x.rivalData);
		}
		IEnumerable<RivalData> enumerable2 = enumerable;
		Dictionary<string, List<BuildingRegistration>> dictionary = new Dictionary<string, List<BuildingRegistration>>();
		Dictionary<string, List<BuildingRegistration>> dictionary2 = new Dictionary<string, List<BuildingRegistration>>();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!string.IsNullOrEmpty(buildingRegistration.businessOwnerRivalId))
			{
				if (buildingRegistration.RentedByPlayer || IgnoredAddresses.Contains(buildingRegistration.Address))
				{
					buildingRegistration.businessOwnerRivalId = string.Empty;
				}
				else
				{
					if (!dictionary.ContainsKey(buildingRegistration.businessOwnerRivalId))
					{
						dictionary[buildingRegistration.businessOwnerRivalId] = new List<BuildingRegistration>();
					}
					dictionary[buildingRegistration.businessOwnerRivalId].Add(buildingRegistration);
				}
			}
			if (string.IsNullOrEmpty(buildingRegistration.buildingOwnerRivalId))
			{
				continue;
			}
			if (IgnoredAddresses.Contains(buildingRegistration.Address))
			{
				buildingRegistration.buildingOwnerRivalId = string.Empty;
				continue;
			}
			if (!dictionary2.ContainsKey(buildingRegistration.buildingOwnerRivalId))
			{
				dictionary2[buildingRegistration.buildingOwnerRivalId] = new List<BuildingRegistration>();
			}
			dictionary2[buildingRegistration.buildingOwnerRivalId].Add(buildingRegistration);
		}
		foreach (RivalData item in enumerable2)
		{
			item.ownedBusinesses = (dictionary.TryGetValue(item.id, out var value) ? value : new List<BuildingRegistration>());
			item.ownedBuildings = (dictionary2.TryGetValue(item.id, out var value2) ? value2 : new List<BuildingRegistration>());
			List<BuildingRegistration> list = new List<BuildingRegistration>();
			foreach (BuildingRegistration ownedBusiness in item.ownedBusinesses)
			{
				if (BuildingTypeHelper.GetData(ownedBusiness).HasTag(TagRef.Buildingtypetag.canbeownedbyrival))
				{
					list.Add(ownedBusiness);
				}
			}
			item.ownedRetailOfficeBusinesses = list;
		}
	}

	public static void GenerateRivals(GameInstance current)
	{
		RivalDataCache?.Clear();
		SpecialRivalCache?.Clear();
		SpecialRivalByNeighborhoodCache?.Clear();
		string[] array = new string[19];
		string[] array2 = new string[7];
		string[] array3 = new string[8];
		int i;
		for (i = 0; i < SpecialRivalsCache.Length; i++)
		{
			array[i] = SpecialRivalsCache[i].rivalData.id;
		}
		for (int j = 0; j < 7; j++)
		{
			array2[j] = (array[i] = UuidHelper.GenerateBase64Uuid());
			i++;
		}
		current.wholesaleRivalIds = array2;
		for (int k = 0; k < 8; k++)
		{
			array3[k] = (array[i] = UuidHelper.GenerateBase64Uuid());
			i++;
		}
		current.importRivalIds = array3;
		SpecialRival[] specialRivalsCache = SpecialRivalsCache;
		foreach (SpecialRival specialRival in specialRivalsCache)
		{
			current.specialRivalStates.Add(new SpecialRivalState
			{
				rivalId = specialRival.rivalData.id,
				completedTimelineEntryIds = new List<string>()
			});
		}
		current.rivalStates = new List<RivalState>();
		string[] array4 = array;
		foreach (string rivalId in array4)
		{
			current.rivalStates.Add(new RivalState
			{
				rivalId = rivalId,
				weeklyIncomeHistory = new List<Tuple<int, float>>(),
				numberOfBusinessesHistory = new List<Tuple<int, int>>()
			});
		}
	}

	public static AiBusinessDefault GetRivalBusinessDefault(BusinessLayoutSet layout, SpecialRival rival = null, AiBusinessGoodsSource goodsSource = AiBusinessGoodsSource.Wholesale, bool isRetail = true)
	{
		if (rival == null)
		{
			return (from x in CompetitionHelper.GetBusinessDefaultsByType(layout.BusinessType)
				where (x.buildingLayout == layout.LayoutName && string.IsNullOrEmpty(x.corporationRivalId) && x.goodsSource == goodsSource) || !isRetail
				select x).GetRandom();
		}
		return (from x in CompetitionHelper.GetBusinessDefaultsByType(layout.BusinessType)
			where x.buildingLayout == layout.LayoutName && (x.corporationRivalId == rival.rivalData.id || x.corporationRivalId == "*")
			select x).GetRandom();
	}

	public static void CheckRivalTimelines()
	{
		RefreshRivals(onlySpecialRivals: true);
		foreach (SpecialRival value in SpecialRivalCache.Values)
		{
			value.CheckTimeline();
		}
	}

	public static bool HasMessageBeenSent(string rivalId, string messageLocalizationKey)
	{
		return GetSpecialRivalState(rivalId)?.sentMessageKeys?.Contains(messageLocalizationKey) == true;
	}

	public static void SendEntranceMessage(SpecialRival rival)
	{
		SendMessageToPlayerDelayed(rival, rival.entranceMessageKey, rival.entranceAudioClip);
	}

	public static void SendRentBuildingMessage(SpecialRival rival)
	{
		SendMessageToPlayer(rival, rival.rentBuildingMessageKey, rival.rentBuildingAudioClip);
	}

	public static void SendMessageToPlayerDelayed(string rivalId, string messageLocalizationKey, AudioClip clip, Action onMessageSent = null)
	{
		SendMessageToPlayerDelayed(GetSpecialRival(rivalId), messageLocalizationKey, clip, onMessageSent);
	}

	public static void SendMessageToPlayerDelayed(SpecialRival rival, string messageLocalizationKey, AudioClip clip, Action onMessageSent = null)
	{
		InstanceBehavior<GameManager>.Instance.coroutineManager.ExecuteAfterDelay(UnityEngine.Random.Range(10, 30), delegate
		{
			SendMessageToPlayer(rival, messageLocalizationKey, clip, onMessageSent);
		});
	}

	public static void SendMessageToPlayer(string rivalId, string messageLocalizationKey, AudioClip clip, Action onMessageSent = null)
	{
		SendMessageToPlayer(GetSpecialRival(rivalId), messageLocalizationKey, clip, onMessageSent);
	}

	private static void SendMessageToPlayer(SpecialRival rival, string messageLocalizationKey, AudioClip clip, Action onMessageSent = null)
	{
		SpecialRivalState rivalState = GetSpecialRivalState(rival.rivalData.id);
		if (rivalState == null)
		{
			onMessageSent?.Invoke();
			return;
		}
		if (SentMessages == null)
		{
			SentMessages = rivalState.sentMessageKeys ?? new List<string>();
		}
		if (SentMessages.Contains(messageLocalizationKey))
		{
			onMessageSent?.Invoke();
			return;
		}
		if (PlannedMessages == null)
		{
			PlannedMessages = new List<string>();
		}
		if (PlannedMessages.Contains(messageLocalizationKey))
		{
			return;
		}
		if (clip != null)
		{
			PlannedMessages.Add(messageLocalizationKey);
			InstanceBehavior<UIs>.Instance.monologueUI.EnqueueMonologue(messageLocalizationKey, clip, rival.monologueSprite, delegate(string key)
			{
				rival.GetRivalContact().SendMessage(new TextMessage(key, null, read: true));
				SpecialRivalState specialRivalState = rivalState;
				if (specialRivalState.sentMessageKeys == null)
				{
					specialRivalState.sentMessageKeys = new List<string>();
				}
				rivalState.sentMessageKeys.Add(key);
				PlannedMessages.Remove(key);
				SentMessages.Add(key);
				onMessageSent?.Invoke();
				GameEvent.Invoke("ba:gameevent_rivalsentmessage");
			});
		}
		else
		{
			SendMessageWithoutNotification(rival.rivalData.id, messageLocalizationKey, onMessageSent);
		}
	}

	public static void SendMessageWithoutNotification(string rivalId, string messageLocalizationKey, Action onMessageSent = null)
	{
		SpecialRival specialRival = GetSpecialRival(rivalId);
		SpecialRivalState specialRivalState = GetSpecialRivalState(rivalId);
		specialRival.GetRivalContact().SendMessage(new TextMessage(messageLocalizationKey));
		SpecialRivalState specialRivalState2 = specialRivalState;
		if (specialRivalState2.sentMessageKeys == null)
		{
			specialRivalState2.sentMessageKeys = new List<string>();
		}
		specialRivalState.sentMessageKeys.Add(messageLocalizationKey);
		if (SentMessages == null)
		{
			SentMessages = specialRivalState.sentMessageKeys ?? new List<string>();
		}
		SentMessages.Add(messageLocalizationKey);
		onMessageSent?.Invoke();
		GameEvent.Invoke("ba:gameevent_rivalsentmessage");
	}

	public static SpecialRival GetSpecialRival(string rivalId)
	{
		if (!string.IsNullOrEmpty(rivalId))
		{
			return SpecialRivalCache.GetValueOrDefault(rivalId);
		}
		return null;
	}

	public static RivalData GetRivalData(string rivalId)
	{
		if (string.IsNullOrEmpty(rivalId))
		{
			return null;
		}
		return RivalDataCache.GetValueOrDefault(rivalId);
	}

	public static float GetBuyBuildingAcceptRate(string rivalId)
	{
		return 1f;
	}

	public static float GetOvertakeBusinessAcceptRate(string rivalId, Address address)
	{
		SpecialRival specialRival = GetSpecialRival(rivalId);
		float num;
		if (specialRival != null)
		{
			SpecialRivalState specialRivalState = GetSpecialRivalState(rivalId);
			if (specialRivalState != null && specialRivalState.isActive)
			{
				num = specialRival.businessOvertakeAcceptRate;
				goto IL_004d;
			}
		}
		num = (float)new System.Random(rivalId.GetHashCode()).Next(-30, 20) / 100f + 1f;
		goto IL_004d;
		IL_004d:
		return (float)new System.Random(address.GetHashCode()).Next(-5, 3) / 100f + num;
	}

	public static int GetRivalAgeInYears(this RivalData rival)
	{
		return rival.startingAgeInYears + TimeHelper.GetYearsByDays(SaveGameManager.Current.Day);
	}

	public static AiBusinessDefault GetBusinessDefault(string rivalId, string businessTypeName)
	{
		return (from x in CompetitionHelper.GetBusinessDefaultsByType(businessTypeName)
			where x.corporationRivalId == rivalId || x.corporationRivalId == "*"
			select x).GetRandom();
	}

	public static Contact GetRivalContact(this SpecialRival rival)
	{
		return Contact.GetContact(rival.rivalData.rivalName, ContactCategoryName.Rivals, "rival");
	}

	public static bool HasContactedPlayer(this SpecialRival rival)
	{
		return SaveGameManager.Current.Contacts.Any((Contact x) => x.id == rival.rivalData.rivalName);
	}

	public static bool IsSpecialRival(this string rivalId)
	{
		if (!string.IsNullOrEmpty(rivalId))
		{
			return SpecialRivalCache.ContainsKey(rivalId);
		}
		return false;
	}

	public static List<RivalData> GetNonSpecialRivals()
	{
		List<RivalData> list = new List<RivalData>(RivalDataCache.Values);
		foreach (SpecialRival value in SpecialRivalCache.Values)
		{
			list.Remove(value.rivalData);
		}
		return list;
	}

	public static string[] GetWholesaleRivalIds()
	{
		return SaveGameManager.Current.wholesaleRivalIds;
	}

	public static string[] GetImportRivalIds()
	{
		return SaveGameManager.Current.importRivalIds;
	}

	public static IReadOnlyCollection<SpecialRival> GetSpecialRivals()
	{
		return SpecialRivalsCache;
	}

	public static RivalState GetRivalState(string rivalId)
	{
		foreach (RivalState rivalState in SaveGameManager.Current.rivalStates)
		{
			if (rivalState.rivalId == rivalId)
			{
				return rivalState;
			}
		}
		return null;
	}

	public static SpecialRivalState GetSpecialRivalState(string rivalId)
	{
		foreach (SpecialRivalState specialRivalState in SaveGameManager.Current.specialRivalStates)
		{
			if (specialRivalState.rivalId == rivalId)
			{
				return specialRivalState;
			}
		}
		return null;
	}

	public static List<SpecialRival> GetActiveSpecialRivals()
	{
		IReadOnlyCollection<SpecialRival> specialRivals = GetSpecialRivals();
		List<SpecialRival> list = new List<SpecialRival>();
		foreach (SpecialRival item in specialRivals)
		{
			SpecialRivalState specialRivalState = GetSpecialRivalState(item.rivalData.id);
			if (specialRivalState != null && specialRivalState.isActive)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static string GetRandomSpecialRivalId(bool canFallbackToImport, bool canFallbackToWholesale)
	{
		List<SpecialRival> list = SpecialRivalCache.Values.Where((SpecialRival x) => !IsRivalDefeated(x.rivalData.id)).ToList();
		if (list.Count > 0)
		{
			return list.GetRandom().rivalData.id;
		}
		if (canFallbackToImport && GetImportRivalIds().Length != 0)
		{
			return GetImportRivalIds().GetRandom();
		}
		if (canFallbackToWholesale && GetWholesaleRivalIds().Length != 0)
		{
			return GetWholesaleRivalIds().GetRandom();
		}
		return null;
	}

	public static List<RivalData> GetAllRivalData()
	{
		return new List<RivalData>(RivalDataCache.Values);
	}

	public static string GetRivalName(this string rivalId)
	{
		RivalData rivalData = GetRivalData(rivalId);
		if (rivalData != null)
		{
			return rivalData.rivalName;
		}
		Debug.LogError("Could not find name for ID: '" + rivalId + "'");
		return "Undefined";
	}

	public static int GetPlayerRanking()
	{
		List<RivalData> allRivalData = GetAllRivalData();
		float playerIncome = FinancialSummaryHelper.GetLastFinancialSummaries(7).Sum((FinancialSummary x) => x.totalProfit);
		return allRivalData.Count((RivalData x) => x.WeeklyIncome > playerIncome) + 1;
	}

	public static SpecialRival GetFirstMessageRival()
	{
		foreach (SpecialRival specialRival in GetSpecialRivals())
		{
			SpecialRivalState specialRivalState = GetSpecialRivalState(specialRival.rivalData.id);
			if (specialRivalState != null && specialRivalState.sentMessageKeys != null && specialRivalState.sentMessageKeys.Contains(specialRival.entranceMessageKey))
			{
				return specialRival;
			}
		}
		return null;
	}

	public static SpecialRival GetFirstActiveRival()
	{
		List<SpecialRival> activeSpecialRivals = GetActiveSpecialRivals();
		if (activeSpecialRivals.Count <= 0)
		{
			return null;
		}
		return activeSpecialRivals[0];
	}

	public static SpecialRival GetFirstAttackRival()
	{
		foreach (SpecialRival specialRival in GetSpecialRivals())
		{
			SpecialRivalState specialRivalState = GetSpecialRivalState(specialRival.rivalData.id);
			if ((specialRivalState == null || specialRivalState.defenseStates != null) && specialRivalState.defenseStates.Count >= 1)
			{
				return specialRival;
			}
		}
		return null;
	}

	[ConsoleMethod("PrintBuildingOwnership", "Prints the distribution of building ownership in the neighborhood", new string[] { }, AutoCompleteMap = new string[] { "neighborhood=Neighborhoods" })]
	public static void PrintBuildingOwnershipDistribution(string neighborhood)
	{
		List<string> source = ((!string.IsNullOrEmpty(neighborhood)) ? (from x in SaveGameManager.Current.BuildingRegistrations
			where x.Neighborhood == neighborhood
			select x.buildingOwnerRivalId).ToList() : SaveGameManager.Current.BuildingRegistrations.Select((BuildingRegistration x) => x.buildingOwnerRivalId).ToList());
		foreach (string rivalId in source.Distinct())
		{
			string rivalName = rivalId.GetRivalName();
			Debug.Log($"{rivalName} owns {source.Count((string x) => x == rivalId)} buildings in {neighborhood}");
		}
	}

	[ConsoleMethod("CheckRivalTimeline", "Force checks the rival timeline", new string[] { }, AutoCompleteMap = new string[] { "neighborhood=Neighborhoods" })]
	public static void CheckRivalTimeline(string neighborhood)
	{
		if (string.IsNullOrEmpty(neighborhood))
		{
			CheckRivalTimelines();
			return;
		}
		RefreshRivals(onlySpecialRivals: true);
		GetSpecialRivalByNeighborhood(neighborhood)?.CheckTimeline();
	}

	[ConsoleMethod("PrintTimelineValues", "Prints the timeline values which are used in checking if an entry is completed", new string[] { }, AutoCompleteMap = new string[] { "neighborhood=Neighborhoods" })]
	public static void PrintTimelineValues(string neighborhood)
	{
		SpecialRival specialRivalByNeighborhood = GetSpecialRivalByNeighborhood(neighborhood);
		specialRivalByNeighborhood.timeline.PrintDebugValues(specialRivalByNeighborhood);
	}

	[ConsoleMethod("PrintAddressOwner", "Prints all ownership info about the address", new string[] { }, AutoCompleteMap = new string[] { "streetName=StreetNames" })]
	public static void PrintAddressOwner(int streetNumber, string streetName)
	{
		Address address = new Address(streetName, streetNumber);
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		Building building = BuildingHelper.GetBuilding(address);
		Debug.Log("Building owner: " + buildingRegistration.buildingOwnerRivalId.GetRivalName() + " (" + buildingRegistration.buildingOwnerRivalId + ") | Business owner: " + buildingRegistration.businessOwnerRivalId.GetRivalName() + " (" + buildingRegistration.businessOwnerRivalId + ") | Building type: " + building.BuildingType);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		RivalDataCache.Clear();
		SpecialRivalCache.Clear();
		SpecialRivalByNeighborhoodCache.Clear();
		SpecialRivalsCache = null;
		SentMessages = null;
		PlannedMessages = null;
	}
}
