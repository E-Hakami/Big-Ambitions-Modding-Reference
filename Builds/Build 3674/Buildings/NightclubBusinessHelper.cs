using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Entities;
using Extensions;
using Helpers;
using PlayerActivity;
using UnityEngine;

namespace Buildings;

public static class NightclubBusinessHelper
{
	private const float danceTileSize = 0.5f;

	private static readonly List<DanceSpot> DanceSpots = new List<DanceSpot>();

	private static readonly List<EmployeeStationController> AvailableDjBooths = new List<EmployeeStationController>();

	private static PlayerActivityBalanceConfig NightclubBalanceConfig;

	private static PlayerActivityBalanceConfig DancingBalanceConfig;

	public static float cachedAverageDJSkill;

	public static string[] DanceFloorsNames => ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.isdancefloor);

	public static string[] DJBoothItemName => ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.isdjbooth);

	public static void Init()
	{
		UnloadDanceSpots();
		GlobalEvents.onEnterBuildingDelayed = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuildingDelayed, new Action<Address>(LoadDanceSpots));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		GlobalEvents.onItemDropped = (Action<ItemController>)Delegate.Combine(GlobalEvents.onItemDropped, new Action<ItemController>(OnItemDropped));
		GlobalEvents.onItemGrabbed = (Action<ItemInstance>)Delegate.Combine(GlobalEvents.onItemGrabbed, new Action<ItemInstance>(OnItemGrabbed));
		GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(CalculateAverageDJSkill));
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, (Action<Address>)delegate
		{
			CalculateAverageDJSkill();
		});
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, (Action<Address>)delegate(Address a)
		{
			if (BuildingManager.IsInsideBuilding && a == InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address)
			{
				CalculateAverageDJSkill();
			}
		});
	}

	private static void OnExitBuilding(Address address)
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		if (buildingRegistration != null && buildingRegistration.businessTypeName == "ba:businesstype_nightclub")
		{
			UnloadDanceSpots();
			HandleHappinessOnExitBuilding(buildingRegistration);
		}
	}

	public static bool CanSpawnCustomers()
	{
		if (!InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RentedByPlayer)
		{
			return true;
		}
		AvailableDjBooths.Clear();
		InstanceBehavior<BuildingManager>.Instance.GetEmployeeStationControllersWithAssignedEmployee(DJBoothItemName, AvailableDjBooths);
		return AvailableDjBooths.Count > 0;
	}

	private static void LoadDanceSpots(Address address)
	{
		if (BuildingHelper.GetBuildingRegistration(address).businessTypeName != "ba:businesstype_nightclub")
		{
			return;
		}
		foreach (ItemController item in InstanceBehavior<BuildingManager>.Instance.GetItemControllersByName(DanceFloorsNames))
		{
			LoadDanceSpotsForDanceFloor(item);
		}
	}

	private static void LoadDanceSpotsForDanceFloor(ItemController danceFloor)
	{
		Bounds bounds = danceFloor.Colliders[0].bounds;
		int num = Mathf.RoundToInt(bounds.size.x / 0.5f);
		Vector3 vector = new Vector3(bounds.center.x - bounds.extents.x, 0f, bounds.center.z + bounds.extents.z);
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				float x = UnityEngine.Random.Range(((float)j + 0.1f) * 0.5f, ((float)j + 0.9f) * 0.5f);
				float num2 = UnityEngine.Random.Range(((float)i + 0.1f) * 0.5f, ((float)i + 0.9f) * 0.5f);
				Vector3 position = vector + new Vector3(x, 0f, 0f - num2);
				DanceSpots.Add(new DanceSpot(position, isOccupied: false, danceFloor));
			}
		}
	}

	public static DanceSpot GetRandomDanceFloorSpot()
	{
		return DanceSpots.Where((DanceSpot x) => !x.isOccupied).GetRandom();
	}

	private static void UnloadDanceSpots()
	{
		DanceSpots.Clear();
	}

	public static bool IsThereAnAvailableDanceFloorSpot()
	{
		return DanceSpots.Exists((DanceSpot x) => !x.isOccupied);
	}

	private static void OnItemDropped(ItemController itemController)
	{
		if (DanceFloorsNames.Contains(itemController.itemName))
		{
			DanceSpots.RemoveAll((DanceSpot x) => x.danceFloorController == itemController);
			LoadDanceSpotsForDanceFloor(itemController);
		}
	}

	private static void OnItemGrabbed(ItemInstance itemInstance)
	{
		if (DanceFloorsNames.Contains(itemInstance.itemName))
		{
			DanceSpots.RemoveAll((DanceSpot x) => x.danceFloorController.ItemInstance == itemInstance);
		}
	}

	private static void CalculateAverageDJSkill()
	{
		if (BuildingManager.IsInsideBuilding)
		{
			BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
			if (buildingRegistration.businessTypeName == "ba:businesstype_nightclub" && BusinessHelper.IsBusinessOpen(buildingRegistration))
			{
				cachedAverageDJSkill = GetDjsAverageSkill(buildingRegistration, SaveGameManager.Current.Hour);
			}
		}
	}

	public static float GetDjsAverageSkill(BuildingRegistration registration, int hour, IEnumerable<ItemInstance> cachedDjBooths = null)
	{
		int djTag = TagRef.Itemtag.isdjbooth;
		if (cachedDjBooths == null)
		{
			cachedDjBooths = registration.itemInstances.Values.Where((ItemInstance x) => x.ItemCached.HasTag(djTag));
		}
		return GetBuildingEmployeeAverageSkill(registration, hour, "ba:skill_dj", cachedDjBooths);
	}

	public static float GetBuildingEmployeeAverageSkill(BuildingRegistration registration, int hour, string skill, IEnumerable<ItemInstance> workstations)
	{
		float num = 0f;
		int num2 = 0;
		foreach (ItemInstance workstation in workstations)
		{
			EmployeeInstance employeeAtStationAndHour = EmployeeHelper.GetEmployeeAtStationAndHour(registration, workstation.id, hour);
			if (employeeAtStationAndHour != null)
			{
				num += employeeAtStationAndHour.GetSkillValue(skill) * (employeeAtStationAndHour.satisfaction / 100f);
				num2++;
			}
		}
		if (num2 != 0)
		{
			return num / (float)num2;
		}
		return 0f;
	}

	public static void OnEnterBuilding()
	{
		BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
		if (buildingRegistration == null || !buildingRegistration.RentedByPlayer)
		{
			PlayerActivityBalanceConfig nightclubBalanceConfig = GetNightclubBalanceConfig();
			if (!(nightclubBalanceConfig == null))
			{
				HappinessHelper.EnableTemporalHappinessBoost(nightclubBalanceConfig.TemporalType, nightclubBalanceConfig.FinalType);
				SaveGameManager.Current.timeEnteredTemporalBoost = TimeHelper.Now();
				SaveGameManager.Current.currentActivityHappinessPerHour = nightclubBalanceConfig.BoostHoursPerHour;
			}
		}
	}

	private static void HandleHappinessOnExitBuilding(BuildingRegistration registration)
	{
		if (!registration.RentedByPlayer)
		{
			PlayerActivityBalanceConfig nightclubBalanceConfig = GetNightclubBalanceConfig();
			if (!(nightclubBalanceConfig == null))
			{
				HappinessHelper.DisableTemporalHappinessBoost(nightclubBalanceConfig.TemporalType, nightclubBalanceConfig.FinalType);
			}
		}
	}

	public static PlayerActivityBalanceConfig GetNightclubBalanceConfig()
	{
		if (NightclubBalanceConfig != null)
		{
			return NightclubBalanceConfig;
		}
		NightclubBusinessSimulator nightclubBusinessSimulator = GetNightclubBusinessSimulator();
		if (nightclubBusinessSimulator != null)
		{
			NightclubBalanceConfig = nightclubBusinessSimulator.BalanceConfig;
		}
		if (NightclubBalanceConfig == null)
		{
			Debug.LogError("No player activity balance config assigned to the nightclub simulator.");
		}
		return NightclubBalanceConfig;
	}

	public static PlayerActivityBalanceConfig GetDancingBalanceConfig()
	{
		if (DancingBalanceConfig != null)
		{
			return DancingBalanceConfig;
		}
		NightclubBusinessSimulator nightclubBusinessSimulator = GetNightclubBusinessSimulator();
		if (nightclubBusinessSimulator != null)
		{
			DancingBalanceConfig = nightclubBusinessSimulator.DanceBalanceConfig;
		}
		if (DancingBalanceConfig == null)
		{
			Debug.LogError("No dancing balance config assigned to the nightclub simulator.");
		}
		return DancingBalanceConfig;
	}

	private static NightclubBusinessSimulator GetNightclubBusinessSimulator()
	{
		return BusinessTypeHelper.GetData("ba:businesstype_nightclub")?.simulator as NightclubBusinessSimulator;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		DanceSpots.Clear();
		cachedAverageDJSkill = 0f;
		NightclubBalanceConfig = null;
		DancingBalanceConfig = null;
		GlobalEvents.onEnterBuildingDelayed = (Action<Address>)Delegate.Remove(GlobalEvents.onEnterBuildingDelayed, new Action<Address>(LoadDanceSpots));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		GlobalEvents.onItemDropped = (Action<ItemController>)Delegate.Remove(GlobalEvents.onItemDropped, new Action<ItemController>(OnItemDropped));
		GlobalEvents.onItemGrabbed = (Action<ItemInstance>)Delegate.Remove(GlobalEvents.onItemGrabbed, new Action<ItemInstance>(OnItemGrabbed));
		GlobalEvents.onNewHour = (Action)Delegate.Remove(GlobalEvents.onNewHour, new Action(CalculateAverageDJSkill));
	}
}
