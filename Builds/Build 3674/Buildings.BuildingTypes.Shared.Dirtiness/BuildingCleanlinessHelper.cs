using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Blueprints;
using Buildings.Indoors.InteriorDesign;
using Entities;
using Extensions;
using Helpers;
using IngameDebugConsole;
using UnityEngine;

namespace Buildings.BuildingTypes.Shared.Dirtiness;

public static class BuildingCleanlinessHelper
{
	public const float DirtinessNeededToShowVisualDirt = 5f;

	private const float AmountOfDirtToIncreasePerTime = 1.2f;

	private const float PercentageOfCellsToAffectInItem = 66f;

	private const float CleanersNeededPerSquareMeter = 0.005f;

	public static readonly float[] FloorTileCleanlinessStates = new float[2] { 80f, 60f };

	private static readonly Dictionary<(BuildingRegistration, ItemInstance), int> DirtBulks = new Dictionary<(BuildingRegistration, ItemInstance), int>();

	private static readonly int Highlight = Shader.PropertyToID("_hov_Highlight");

	public static void RunHourly()
	{
		ApplyDirtBulks();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && !buildingRegistration.temporarilyClosed && BuildingTypeHelper.GetData(BuildingHelper.GetBuilding(buildingRegistration.Address)).NeedsCleaning)
			{
				SimulateCleaning(buildingRegistration);
			}
		}
		if (BuildingManager.IsInsideBuilding)
		{
			InstanceBehavior<BuildingManager>.Instance?.UpdateDirtinessInCurrentBuilding();
		}
	}

	public static void AddDirtBulkEntry(ItemInstance instance, BuildingRegistration registration)
	{
		(BuildingRegistration, ItemInstance) key = (registration, instance);
		if (!DirtBulks.ContainsKey(key))
		{
			DirtBulks.Add(key, 1);
		}
		else
		{
			DirtBulks[key]++;
		}
	}

	public static void ApplyDirt(BuildingRegistration buildingRegistration, ItemInstance itemInstance, int times = 1)
	{
		if (!buildingRegistration.RentedByPlayer)
		{
			return;
		}
		if (itemInstance.dirtSpotsThatAffects == null)
		{
			Debug.LogWarning("Dirt spots that affects item " + itemInstance.itemName + " not found in " + buildingRegistration.BusinessName);
			return;
		}
		int count = Mathf.CeilToInt(66f * (float)itemInstance.dirtSpotsThatAffects.Count / 100f);
		foreach (int item in itemInstance.dirtSpotsThatAffects.GetRandom(count))
		{
			if (item < buildingRegistration.dirtSpots.Count)
			{
				float num = 1.2f * (float)times;
				buildingRegistration.dirtSpots[item].dirtiness = Mathf.Min(100f, buildingRegistration.dirtSpots[item].dirtiness + num);
				if (BuildingManager.IsInsideBuilding && InstanceBehavior<BuildingManager>.Instance.buildingRegistration == buildingRegistration)
				{
					InstanceBehavior<BuildingManager>.Instance.UpdateDirtinessInSpecificSpot(item);
				}
			}
		}
	}

	public static float GetCleanliness(this BuildingRegistration buildingRegistration)
	{
		if (buildingRegistration.dirtSpots == null || buildingRegistration.dirtSpots.Count == 0)
		{
			return 100f;
		}
		float num = buildingRegistration.dirtSpots.Where((DirtSpot x) => x.dirtiness > 5f).Sum((DirtSpot x) => x.dirtiness) / (float)buildingRegistration.dirtSpots.Count;
		if (num <= 0f)
		{
			return 100f;
		}
		float num2 = buildingRegistration.dirtSpots.Average((DirtSpot x) => x.dirtiness);
		return Mathf.Max(0, (int)(100f - num2 - num));
	}

	public static List<DirtSpot> GetDirtSpotsForBuilding(Building building)
	{
		BuildingSizeInfo buildingSizeInfo = new BuildingSizeInfo(building);
		List<DirtSpot> list = new List<DirtSpot>();
		Transform transform = ((building.IsHamptonsHouse() && !InteriorDesignerHelper.BlueprintCreatorMode) ? InstanceBehavior<CityManager>.Instance.FindCityBuildingController(building.Address).transform : InstanceBehavior<BuildingManager>.Instance.GetBuildingTransform(buildingSizeInfo));
		MultipleHeightsBuildingController multipleHeightsBuildingController = transform?.GetComponent<MultipleHeightsBuildingController>();
		if (multipleHeightsBuildingController != null)
		{
			GameObject[] floorsParents = multipleHeightsBuildingController.GetFloorsParents();
			if (floorsParents == null)
			{
				Debug.LogWarning("No floors found for " + buildingSizeInfo.ToString());
				return list;
			}
			GameObject[] array = floorsParents;
			for (int i = 0; i < array.Length; i++)
			{
				foreach (Transform item in array[i].transform)
				{
					Vector3 position = item.position;
					list.Add(new DirtSpot
					{
						x = (int)position.x,
						z = (int)position.z
					});
				}
			}
			return list;
		}
		Transform transform2 = transform?.Find("Floors");
		if (transform2 == null)
		{
			Debug.LogWarning("No floors found for " + buildingSizeInfo.ToString());
			return list;
		}
		foreach (Transform item2 in transform2)
		{
			Vector3 position2 = item2.position;
			list.Add(new DirtSpot
			{
				x = (int)position2.x,
				z = (int)position2.z
			});
		}
		return list;
	}

	public static void ShowDirtinessHighlighting()
	{
		Shader.SetGlobalFloat(Highlight, 0.75f);
	}

	public static void HideDirtinessHighlighting()
	{
		Shader.SetGlobalFloat(Highlight, 0f);
	}

	private static void ApplyDirtBulks()
	{
		foreach (KeyValuePair<(BuildingRegistration, ItemInstance), int> dirtBulk in DirtBulks)
		{
			dirtBulk.Deconstruct(out var key, out var value);
			(BuildingRegistration, ItemInstance) tuple = key;
			BuildingRegistration item = tuple.Item1;
			ItemInstance item2 = tuple.Item2;
			int times = value;
			ApplyDirt(item, item2, times);
		}
		DirtBulks.Clear();
	}

	private static void SimulateCleaning(BuildingRegistration registration)
	{
		if (registration.dirtSpots == null)
		{
			return;
		}
		ScheduleDay scheduleDay = registration.scheduleDays.First((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek());
		if (scheduleDay == null || !scheduleDay.isOpen || scheduleDay.workShifts == null)
		{
			return;
		}
		int currentHour = TimeHelper.CurrentHour;
		float num = 0f;
		foreach (WorkShift item in scheduleDay.workShifts.Where((WorkShift x) => x.type == WorkShiftType.Cleaning && x.startingHour <= currentHour && x.endingHour > currentHour))
		{
			if (!string.IsNullOrEmpty(item.employeeId))
			{
				EmployeeInstance employeeById = EmployeeHelper.GetEmployeeById(item.employeeId);
				if (employeeById.IsEmployeeAvailable() && employeeById.HasSkill("ba:skill_cleaning"))
				{
					num += employeeById.GetSkillValue("ba:skill_cleaning") * (employeeById.satisfaction / 100f);
				}
			}
		}
		if (num == 0f)
		{
			return;
		}
		num /= (float)GetRequiredCleaningForceSize(registration.Address);
		foreach (DirtSpot dirtSpot in registration.dirtSpots)
		{
			dirtSpot.dirtiness = Mathf.Max(0f, dirtSpot.dirtiness - num);
		}
	}

	private static int GetRequiredCleaningForceSize(Address address)
	{
		return Mathf.CeilToInt((float)BuildingHelper.GetBuildingSquareMeters(address) * 0.005f);
	}

	[ConsoleMethod("SetDirtiness", "Set an amount of dirtiness in a given number of spots in the current building", new string[] { })]
	public static void SetDirtiness(int spots, float dirtiness)
	{
		if (BuildingManager.IsInsideBuilding)
		{
			IList<DirtSpot> list = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.dirtSpots.ToList().Shuffle();
			for (int i = 0; i < spots && i < list.Count; i++)
			{
				list[i].dirtiness = Mathf.Min(100f, dirtiness);
			}
			InstanceBehavior<BuildingManager>.Instance.UpdateDirtinessInCurrentBuilding();
		}
	}
}
