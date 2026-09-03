using System.Collections.Generic;
using System.Linq;
using Entities;
using Extensions;
using Helpers;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.Warehouse;

public class Drivers : MonoBehaviour
{
	private static readonly string[] DriverSkills = new string[1] { "ba:skill_deliverydriver" };

	public DriverStation driverStationTemplate;

	private BizManBusiness _bizManBusiness;

	private List<EmployeeInstance> _employees;

	private int _slotCounter;

	private Entities.Warehouse _warehouse;

	private void Awake()
	{
		_bizManBusiness = GetComponentInParent<BizManBusiness>();
	}

	private void OnEnable()
	{
		RefreshData();
	}

	private void RefreshData()
	{
		_warehouse = (Entities.Warehouse)_bizManBusiness.buildingRegistration;
		SetUpEmployees();
		SetUpDriverStations();
	}

	private void SetUpDriverStations()
	{
		driverStationTemplate.transform.ResetTemplate();
		_slotCounter = 0;
		foreach (VehicleSlot vehicleSlot in _warehouse.vehicleSlots)
		{
			_slotCounter++;
			DriverStation component = Object.Instantiate(driverStationTemplate.gameObject, driverStationTemplate.transform.parent).GetComponent<DriverStation>();
			component.gameObject.SetActive(value: true);
			component.SetUp(_employees, vehicleSlot, _slotCounter, IsDriverAlreadyAssigned, HasDriverEnoughSkill);
		}
	}

	private void SetUpEmployees()
	{
		_employees = EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			withAssignedAddress = _bizManBusiness.buildingRegistration.Address,
			withSkills = DriverSkills
		});
	}

	private bool IsDriverAlreadyAssigned(EmployeeInstance driver)
	{
		return _warehouse.vehicleSlots.Any((VehicleSlot x) => x.employeeDriverId == driver.id);
	}

	private static bool HasDriverEnoughSkill(EmployeeInstance driver, string vehicleInstanceId)
	{
		if (!driver.HasSkill("ba:skill_deliverydriver"))
		{
			return false;
		}
		VehicleInstance vehicleInstance = SaveGameManager.Current.VehicleInstances.First((VehicleInstance x) => x.id == vehicleInstanceId);
		return Entities.Warehouse.CanDriverOperateVehicle(driver, vehicleInstance);
	}
}
