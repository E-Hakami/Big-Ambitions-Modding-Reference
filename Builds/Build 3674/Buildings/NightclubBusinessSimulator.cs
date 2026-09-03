using System.Collections.Generic;
using System.Linq;
using AI.Citizens;
using AI.Customers.CustomerEntries;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Buildings.Retail.Simulation;
using Entities;
using Extensions;
using Helpers;
using PlayerActivity;
using UnityEngine;

namespace Buildings;

[CreateAssetMenu(menuName = "BusinessSimulator/Retail/FullServices/Nightclub")]
public class NightclubBusinessSimulator : FullServiceBusinessSimulator
{
	[SerializeField]
	private PlayerActivityBalanceConfig balanceConfig;

	[SerializeField]
	private PlayerActivityBalanceConfig danceBalanceConfig;

	private readonly List<ItemInstance> _djBoothsWithEmployees = new List<ItemInstance>();

	private readonly List<ItemInstance> _coatCheckBoothsWithEmployees = new List<ItemInstance>();

	private int _probabilityOfRemovingEntriesDueToDJSkill;

	private readonly List<ItemInstance> _stationedItemInstances = new List<ItemInstance>();

	public PlayerActivityBalanceConfig BalanceConfig => balanceConfig;

	public PlayerActivityBalanceConfig DanceBalanceConfig => danceBalanceConfig;

	public override void SetUp(BuildingRegistration registration, int hour)
	{
		base.SetUp(registration, hour);
		SetUpBoothsWithEmployees();
		SetUpProbabilityOfRemovingEntriesDueToDJSkill();
		AddDirtToDJBoothsInUse();
	}

	private void SetUpBoothsWithEmployees()
	{
		_djBoothsWithEmployees.Clear();
		_coatCheckBoothsWithEmployees.Clear();
		foreach (ItemInstance item in availableFurniture)
		{
			if (IsBoothWithEmployeeAtCurrentHour(TagRef.Itemtag.isdjbooth, item))
			{
				_djBoothsWithEmployees.Add(item);
			}
			else if (IsBoothWithEmployeeAtCurrentHour(TagRef.Itemtag.iscoatcheck, item))
			{
				_coatCheckBoothsWithEmployees.Add(item);
			}
		}
	}

	private bool IsBoothWithEmployeeAtCurrentHour(int itemTag, ItemInstance furniture)
	{
		if (furniture.ItemCached.HasTag(itemTag))
		{
			return EmployeeHelper.IsEmployeeStationEmployedAtHour(buildingRegistration, furniture.id, currentHour);
		}
		return false;
	}

	protected override PointOfSale GetIdealPointOfSale()
	{
		return pointOfSales.GetRandom();
	}

	protected override float GetCustomerService(PointOfSale pointOfSale)
	{
		float num = 0f;
		int num2 = 0;
		if (pointOfSale != null)
		{
			num += pointOfSale.customerService;
			num2++;
		}
		if (_djBoothsWithEmployees.Count > 0)
		{
			num += NightclubBusinessHelper.GetDjsAverageSkill(buildingRegistration, currentHour, _djBoothsWithEmployees);
			num2++;
		}
		if (_coatCheckBoothsWithEmployees.Count > 0)
		{
			num += NightclubBusinessHelper.GetBuildingEmployeeAverageSkill(buildingRegistration, currentHour, "ba:skill_customerservice", _coatCheckBoothsWithEmployees);
			num2++;
		}
		if (num2 <= 0)
		{
			return 0f;
		}
		return num / (float)num2;
	}

	protected override void ProcessCustomer(CustomerEntry customerEntry)
	{
		if (_djBoothsWithEmployees.Count == 0)
		{
			customerEntry.completed = true;
			return;
		}
		customerEntry.order.entries.RemoveAll((OrderEntry x) => (ItemsGetter.GetByName(x.itemName).type & ItemType.RetailProduct) != 0 && RngHelper.Chance(_probabilityOfRemovingEntriesDueToDJSkill));
		base.ProcessCustomer(customerEntry);
	}

	protected override OrderEntry ProcessItemInOrder(OrderEntry orderEntry, CitizenData citizenData, PointOfSale pointOfSale)
	{
		if ((ItemsGetter.GetByName(orderEntry.itemName).type & ItemType.RetailProduct) != 0)
		{
			if (pointOfSales.Count == 0)
			{
				orderEntry.processed = true;
				return null;
			}
			base.ProcessItemInOrder(orderEntry, citizenData, pointOfSale);
			return null;
		}
		if (!availableProducers.TryGetValue(orderEntry.itemName, out var value))
		{
			return null;
		}
		_stationedItemInstances.Clear();
		_stationedItemInstances.AddRange(value.Where((ItemInstance x) => EmployeeHelper.IsEmployeeStationEmployedAtHour(buildingRegistration, x.id, currentHour)));
		ItemInstance random = _stationedItemInstances.GetRandom();
		if (random == null)
		{
			return null;
		}
		CompleteOrderEntry(orderEntry, citizenData, random);
		return null;
	}

	protected override void AddDirtToUsedFurniture(CustomerEntry spawnEntry)
	{
		if (spawnEntry.order.entries.Exists((OrderEntry x) => x.paid && (ItemsGetter.GetByName(x.itemName).type & ItemType.RetailProduct) != 0))
		{
			base.AddDirtToUsedFurniture(spawnEntry);
		}
	}

	private void AddDirtToDJBoothsInUse()
	{
		foreach (ItemInstance djBoothsWithEmployee in _djBoothsWithEmployees)
		{
			BuildingCleanlinessHelper.AddDirtBulkEntry(djBoothsWithEmployee, buildingRegistration);
		}
	}

	private void SetUpProbabilityOfRemovingEntriesDueToDJSkill()
	{
		if (pointOfSales.Count == 0)
		{
			_probabilityOfRemovingEntriesDueToDJSkill = 100;
			return;
		}
		float djsAverageSkill = NightclubBusinessHelper.GetDjsAverageSkill(buildingRegistration, currentHour, _djBoothsWithEmployees);
		_probabilityOfRemovingEntriesDueToDJSkill = Mathf.RoundToInt(100f - djsAverageSkill);
	}
}
