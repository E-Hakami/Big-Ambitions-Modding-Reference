using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings;
using Entities;
using Extensions;
using UnityEngine;

namespace Helpers;

public static class BusinessSecurityHelper
{
	private const float MaxThievesPerCustomerPerShelf = 0.04f;

	private const float SecuritySignProbabilityToSkipUnsecuredShelf = 5f;

	private const float BuildingSizePower = 1f;

	private const float AvgPricesPower = 3f;

	private const float SecurityGuardsPower = 0.55f;

	private const float SecurityCamerasPower = 0.35f;

	private const float SecurityPanelsPower = 0.1f;

	private const float SqmForMaxSecurityLevel = 500f;

	private const float AvgPricesForMaxSecurityLevel = 100f;

	private const float IdealAmountOfSecurityGuards = 2f;

	private const float IdealAmountOfSecurityPanelsPerEntrance = 2f;

	private const float MaxSecurityPanelDistanceFromEntrance = 1.75f;

	private const float SecurityCameraCoverageRadiusInMeters = 5f;

	public static Action onCamerasCoverageUpdated;

	private static readonly List<EmployeeInstance> TempGuardsList = new List<EmployeeInstance>();

	private static readonly string[] SecurityGuardSkills = new string[1] { "ba:skill_securityguard" };

	public static void UpdateSecurityLevel(this BuildingRegistration buildingRegistration)
	{
		if (buildingRegistration != null && !(buildingRegistration.businessTypeName == "ba:businesstype_empty") && BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.allowtheft))
		{
			float idealSecurityProtection = buildingRegistration.GetIdealSecurityProtection();
			float securityGuardsProtection = buildingRegistration.GetSecurityGuardsProtection();
			buildingRegistration.GetSecurityCamerasAndPanelsProtection(out var securityCamerasProtection, out var securityPanelsProtection);
			float a = (securityGuardsProtection * 0.55f + securityCamerasProtection * 0.35f + securityPanelsProtection * 0.1f) * 100f / idealSecurityProtection;
			buildingRegistration.securityLevelPercentage = Mathf.Min(a, 100f);
		}
	}

	private static float GetIdealSecurityProtection(this BuildingRegistration buildingRegistration)
	{
		float num = Mathf.Min((float)BuildingSizeHelper.GetData(buildingRegistration).squareMeters / 500f, 1f);
		float num2 = 0f;
		int num3 = 0;
		foreach (string cachedAvailableProduct in buildingRegistration.cachedAvailableProducts)
		{
			Item byName = ItemsGetter.GetByName(cachedAvailableProduct);
			if ((object)byName == null || (byName.type & ItemType.RetailProduct) != 0)
			{
				num2 += ItemHelper.GetPrice(cachedAvailableProduct, buildingRegistration);
				num3++;
			}
		}
		if (num3 > 0)
		{
			num2 /= (float)num3;
		}
		float num4 = Mathf.Min(num2 / 100f, 2f);
		return Mathf.Min((num * 1f + num4 * 3f) / 4f, 1f) * 100f;
	}

	private static float GetSecurityGuardsProtection(this BuildingRegistration buildingRegistration)
	{
		float num = 0f;
		int num2 = 0;
		List<EmployeeInstance> employeeInstances = EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			excludeAbsentsNotReplaced = true,
			withAssignedAddress = buildingRegistration.Address,
			withSkills = SecurityGuardSkills
		}, TempGuardsList);
		for (int i = 0; i < employeeInstances.Count; i++)
		{
			EmployeeInstance employeeInstance = employeeInstances[i];
			if (employeeInstance.IsWorking())
			{
				num += employeeInstance.GetSkillValue("ba:skill_securityguard");
				num2++;
			}
		}
		if (num2 != 0)
		{
			return Mathf.Min(num / 2f, 100f);
		}
		return 0f;
	}

	private static void GetSecurityCamerasAndPanelsProtection(this BuildingRegistration buildingRegistration, out float securityCamerasProtection, out float securityPanelsProtection)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (ItemInstance value in ItemHelper.GetItemsByAddress(buildingRegistration.Address).Values)
		{
			if ((value.ItemCached.type & ItemType.ShowcaseShelf) != 0)
			{
				num++;
				if (value.isSecured)
				{
					num2++;
				}
			}
			else if (value.isSecured && value.ItemCached.HasTag(TagRef.Itemtag.issecuritypanel))
			{
				num3++;
			}
		}
		securityCamerasProtection = (float)num2 * 100f / (float)num;
		float num4 = 2f * (float)BuildingSizeHelper.GetData(buildingRegistration).numberOfEntrances;
		float a = (float)num3 * 100f / num4;
		securityPanelsProtection = Mathf.Min(a, 100f);
	}

	public static void SimulateTheft(this BuildingRegistration buildingRegistration)
	{
		float num = (float)buildingRegistration.GetAmountOfCustomersThisHour() * 0.04f * (1f - buildingRegistration.securityLevelPercentage / 100f);
		if (num == 0f)
		{
			return;
		}
		List<ItemInstance> list = buildingRegistration.itemInstances.Values.Where((ItemInstance x) => (x.ItemCached.type & (ItemType.PointOfSale | ItemType.ShowcaseShelf)) != 0 && x.GetStockInstance().amount > 0).ToList();
		if (list.Count == 0)
		{
			return;
		}
		bool flag = buildingRegistration.itemInstances.Values.Any((ItemInstance x) => x.ItemCached.HasTag(TagRef.Itemtag.issecuritysign));
		foreach (ItemInstance item in list)
		{
			if ((item.isSecured && (float)UnityEngine.Random.Range(0, 100) <= buildingRegistration.securityLevelPercentage) || ((!item.isSecured & flag) && (float)UnityEngine.Random.Range(0, 100) <= 5f))
			{
				continue;
			}
			int num2 = 0;
			if (num <= 0f)
			{
				continue;
			}
			if (num < 1f)
			{
				float num3 = (1f - num) * 100f;
				if (UnityEngine.Random.Range(0f, 100f) <= num3)
				{
					continue;
				}
				num2 = UnityEngine.Random.Range(1, Mathf.CeilToInt(num) + 1);
			}
			CargoInstance stockInstance = item.GetStockInstance();
			int num4 = ((stockInstance.amount < num2) ? stockInstance.amount : num2);
			stockInstance.amount -= num4;
			if (stockInstance.amount <= 0)
			{
				item.FillUpShowcaseShelfOrPointOfSale();
			}
			buildingRegistration.stolenItemsCost += (float)num4 * stockInstance.pricePerUnit;
			item.OnItemsInCargoUpdated()?.Invoke();
		}
	}

	private static int GetAmountOfCustomersThisHour(this BuildingRegistration buildingRegistration)
	{
		if (SaveGameManager.Current.Hour == 0)
		{
			return (buildingRegistration.orderHistory.FirstOrDefault((OrderHistoryEntry x) => x.dayNumber == SaveGameManager.Current.Day)?.hourReports.FirstOrDefault((OrderHistoryEntry.HourReport x) => x.hour == SaveGameManager.Current.Hour)?.customers).GetValueOrDefault();
		}
		return buildingRegistration.unprocessedCompletedOrders.Count((Order x) => x.timestamp?.Hour == SaveGameManager.Current.Hour - 1);
	}

	public static void UpdateSecurityPanelCoverage(this ItemInstance itemInstance, List<ExitZone> exitZones = null)
	{
		if (exitZones != null || (!(InstanceBehavior<BuildingManager>.Instance == null) && BuildingManager.IsInsideBuilding))
		{
			if (exitZones == null)
			{
				exitZones = InstanceBehavior<BuildingManager>.Instance.exitZones;
			}
			itemInstance.isSecured = exitZones.Any((ExitZone x) => MathHelper.DistanceSqr(x.transform.position, itemInstance.position) <= 1.75f);
		}
	}

	public static void ForceUpdateSecurityPanelCoverage()
	{
		List<ExitZone> exitZones = InstanceBehavior<BuildingManager>.Instance.exitZones;
		foreach (ItemController itemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			if (itemController.Item.HasTag(TagRef.Itemtag.issecuritypanel))
			{
				itemController.ItemInstance.isSecured = exitZones.Any((ExitZone x) => MathHelper.DistanceSqr(x.transform.position, itemController.ItemInstance.position) <= 1.75f);
			}
		}
	}

	public static void UpdateCamerasCoverage()
	{
		UpdateCamerasCoverage(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.itemInstances.Values.ToList());
	}

	public static void UpdateCamerasCoverage(Address address)
	{
		UpdateCamerasCoverage(ItemHelper.GetItemsByAddress(address).Values.ToList());
	}

	private static void UpdateCamerasCoverage(List<ItemInstance> items)
	{
		List<ItemInstance> list = new List<ItemInstance>();
		List<ItemInstance> list2 = new List<ItemInstance>();
		foreach (ItemInstance item in items)
		{
			string itemName = item.itemName;
			if (ItemsGetter.GetByName(itemName).HasTag(TagRef.Itemtag.issecuritycamera))
			{
				list.Add(item);
			}
			else if (!string.IsNullOrEmpty(itemName) && (ItemsGetter.GetByName(itemName).type & ItemType.ShowcaseShelf) != 0)
			{
				list2.Add(item);
			}
		}
		foreach (ItemInstance item2 in list2)
		{
			float x = item2.position.x;
			float z = item2.position.z;
			bool isSecured = false;
			int i = 0;
			for (int count = list.Count; i < count; i++)
			{
				ItemInstance itemInstance = list[i];
				float num = itemInstance.position.x - x;
				float num2 = itemInstance.position.z - z;
				if (num * num + num2 * num2 <= 25f)
				{
					isSecured = true;
					break;
				}
			}
			item2.isSecured = isSecured;
		}
		onCamerasCoverageUpdated?.Invoke();
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		onCamerasCoverageUpdated = null;
	}
}
