using System;
using System.Collections.Generic;
using System.Linq;
using Entities;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Elements;
using UI.Notification;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.Warehouse;

public class DriverStation : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent slotNumberField;

	[Header("Vehicle")]
	[SerializeField]
	private TMP_Text vehicleNameField;

	[SerializeField]
	private GameObject noVehicleAssignedObj;

	[Header("Driver")]
	[SerializeField]
	private Dropdown driverDropdown;

	private int _currentIndex;

	public RectTransform DriverDropdownTarget => (RectTransform)driverDropdown.transform;

	public void SetUp(List<EmployeeInstance> employees, VehicleSlot vehicleSlot, int slotNumber, Func<EmployeeInstance, bool> isDriverAlreadyAssigned, Func<EmployeeInstance, string, bool> hasDriverEnoughSkill)
	{
		slotNumberField.Arguments = new
		{
			number = slotNumber
		};
		VehicleInstance vehicleInstance = SaveGameManager.Current.VehicleInstances.FirstOrDefault((VehicleInstance x) => x.id == vehicleSlot.vehicleInstanceId);
		noVehicleAssignedObj.SetActive(vehicleInstance == null);
		vehicleNameField.gameObject.SetActive(vehicleInstance != null);
		if (vehicleInstance != null)
		{
			vehicleNameField.SetText(vehicleInstance.vehicleTypeName.GetLocalization());
		}
		List<string> list = new List<string> { "common_unassigned".GetLocalization() };
		list.AddRange(employees.Select((EmployeeInstance x) => x.GetEmployeeNameWithInfo()));
		int selectedOption = (_currentIndex = ((vehicleSlot.employeeDriverId != null) ? (employees.FindIndex((EmployeeInstance x) => x.id == vehicleSlot.employeeDriverId) + 1) : 0));
		driverDropdown.SetOptions(list, localize: false, selectedOption);
		driverDropdown.onOptionSelected.RemoveAllListeners();
		driverDropdown.onOptionSelected.AddListener(delegate(int index)
		{
			EmployeeInstance employeeInstance = (string.IsNullOrEmpty(vehicleSlot.employeeDriverId) ? null : employees.FirstOrDefault((EmployeeInstance x) => x.id == vehicleSlot.employeeDriverId));
			if (index == 0)
			{
				vehicleSlot.employeeDriverId = string.Empty;
				employeeInstance?.UpdateWeeklyHoursAndDays();
				_currentIndex = 0;
			}
			else
			{
				EmployeeInstance employeeInstance2 = employees[index - 1];
				if (!(employeeInstance2.id == vehicleSlot.employeeDriverId))
				{
					if (string.IsNullOrEmpty(vehicleSlot.vehicleInstanceId))
					{
						Notifications.ShowError("bizman_warehouse_driverstation_no_vehicle_assigned");
						driverDropdown.SelectOption(_currentIndex);
					}
					else if (isDriverAlreadyAssigned(employeeInstance2))
					{
						Notifications.ShowError("bizman_warehouse_driverstation_notification_driver_already_assigned");
						driverDropdown.SelectOption(_currentIndex);
					}
					else if (!hasDriverEnoughSkill(employeeInstance2, vehicleSlot.vehicleInstanceId))
					{
						Notifications.ShowError("bizman_warehouse_driverstation_not_enough_skill");
						driverDropdown.SelectOption(_currentIndex);
					}
					else
					{
						vehicleSlot.employeeDriverId = employeeInstance2.id;
						employeeInstance?.UpdateWeeklyHoursAndDays();
						_currentIndex = index;
						employeeInstance2.UpdateWeeklyHoursAndDays();
						GameEvent.Invoke("ba:gameevent_employeeassigned");
					}
				}
			}
		});
	}
}
