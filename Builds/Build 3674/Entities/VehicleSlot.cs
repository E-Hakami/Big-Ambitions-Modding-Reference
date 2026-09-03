using System;
using Helpers;

namespace Entities;

[Serializable]
public class VehicleSlot
{
	public string vehicleInstanceId;

	public string employeeDriverId;

	public int DestinationsThatCanDeliver
	{
		get
		{
			if (string.IsNullOrEmpty(vehicleInstanceId) || string.IsNullOrEmpty(employeeDriverId))
			{
				return 0;
			}
			return SaveGameManager.Current.VehicleInstances.Find((VehicleInstance x) => x.id == vehicleInstanceId)?.VehicleType.destinationsThatCanDeliver ?? 0;
		}
	}

	public EmployeeInstance AssignVehicle(VehicleInstance vehicleInstance)
	{
		vehicleInstanceId = vehicleInstance.id;
		return ClearDriverIfCannotOperate(vehicleInstance);
	}

	private EmployeeInstance ClearDriverIfCannotOperate(VehicleInstance vehicleInstance)
	{
		if (string.IsNullOrEmpty(employeeDriverId))
		{
			return null;
		}
		EmployeeInstance employeeById = EmployeeHelper.GetEmployeeById(employeeDriverId, showError: false);
		if (employeeById != null && Warehouse.CanDriverOperateVehicle(employeeById, vehicleInstance))
		{
			return null;
		}
		employeeDriverId = null;
		employeeById?.UpdateWeeklyHoursAndDays();
		return employeeById;
	}
}
