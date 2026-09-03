using System.Collections.Generic;
using System.Linq;
using AI.Citizens;
using AI.Customers.CustomerEntries;
using BigAmbitions.Characters.Skills;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Entities;
using Helpers;
using Helpers.BusinessSimulation;
using UnityEngine;

namespace Buildings.Office;

[CreateAssetMenu(menuName = "BusinessSimulator/Office")]
public class OfficeBusinessSimulator : BusinessSimulator
{
	private readonly List<CustomerEntry> _customerEntries = new List<CustomerEntry>();

	private readonly List<(ItemInstance stationInstance, EmployeeInstance employeeInstance)> _employeeStationsAndEmployees = new List<(ItemInstance, EmployeeInstance)>();

	public override void SimulateCurrentHour()
	{
		BusinessType businessType = BusinessTypeHelper.GetData(buildingRegistration);
		List<(ItemInstance, EmployeeInstance)> employeeStationsAndEmployees = GetEmployeeStationsAndEmployees(businessType);
		if (!businessType.HasTag(TagRef.Businesstag.generatesrevenue))
		{
			foreach (var item2 in employeeStationsAndEmployees)
			{
				BuildingCleanlinessHelper.AddDirtBulkEntry(item2.Item1, buildingRegistration);
			}
			return;
		}
		_customerEntries.Clear();
		_customerEntries.AddRange(from x in CustomerEntriesHelper.GetEntriesByAddress(buildingRegistration.Address)
			where x.spawnTime.Hour == currentHour
			select x);
		float cleanliness = buildingRegistration.GetCleanliness();
		for (int num = 0; num < Mathf.Min(_customerEntries.Count, employeeStationsAndEmployees.Count); num++)
		{
			CustomerEntry customerEntry = _customerEntries[num];
			EmployeeInstance item = employeeStationsAndEmployees[num].Item2;
			CitizenData citizenData = CitizenHelper.CreateRandomCitizenDataNonAlloc(buildingRegistration.Neighborhood);
			Skill skill = item.characterData.skills.First((Skill x) => businessType.employeePrimarySkills.Contains(x.name));
			foreach (OrderEntry entry in customerEntry.order.entries)
			{
				entry.price = ItemHelper.GetPrice(entry.itemName, buildingRegistration);
				entry.available = true;
				entry.priceAccceptable = citizenData.IsPriceAcceptable(entry.itemName, entry.price);
				entry.paid = true;
			}
			BuildingCleanlinessHelper.AddDirtBulkEntry(employeeStationsAndEmployees[num].Item1, buildingRegistration);
			customerEntry.order.customerServiceSkill = skill.value * (item.satisfaction / 100f);
			Order order = customerEntry.order;
			if (order.timestamp == null)
			{
				order.timestamp = new Timestamp(TimeHelper.CurrentDay, currentHour, 0f);
			}
			customerEntry.order.completed = true;
			customerEntry.order.cleanliness = cleanliness;
			customerEntry.order.customerDemandScore = OrderHelper.CalculateOrderDemandScore(customerEntry.order, buildingRegistration);
			buildingRegistration.unprocessedCompletedOrders.Add(customerEntry.order);
			customerEntry.completed = true;
		}
	}

	public override void OnTimeMachineEnd(BuildingRegistration registration)
	{
	}

	private List<(ItemInstance stationInstance, EmployeeInstance employeeInstance)> GetEmployeeStationsAndEmployees(BusinessType businessType)
	{
		_employeeStationsAndEmployees.Clear();
		if (buildingRegistration.scheduleDays == null)
		{
			return _employeeStationsAndEmployees;
		}
		DayOfWeekOrdered dayOfWeek = TimeHelper.GetDayOfWeek();
		foreach (ScheduleDay scheduleDay in buildingRegistration.scheduleDays)
		{
			if (scheduleDay.day != dayOfWeek)
			{
				continue;
			}
			foreach (WorkShift workShift in scheduleDay.workShifts)
			{
				if (string.IsNullOrEmpty(workShift.employeeId) || workShift.type == WorkShiftType.Cleaning || workShift.startingHour > SaveGameManager.Current.Hour || workShift.endingHour <= SaveGameManager.Current.Hour || !buildingRegistration.itemInstances.TryGetValue(workShift.itemInstanceId, out var value) || ItemHelper.HasAnyMissingRequirements(value))
				{
					continue;
				}
				EmployeeInstance employeeAtStationAndHour = EmployeeHelper.GetEmployeeAtStationAndHour(buildingRegistration, value.id, SaveGameManager.Current.Hour);
				if (employeeAtStationAndHour == null)
				{
					continue;
				}
				foreach (Skill skill in employeeAtStationAndHour.characterData.skills)
				{
					if (businessType.employeePrimarySkills.Contains(skill.name))
					{
						_employeeStationsAndEmployees.Add((value, employeeAtStationAndHour));
						break;
					}
				}
			}
		}
		return _employeeStationsAndEmployees;
	}
}
