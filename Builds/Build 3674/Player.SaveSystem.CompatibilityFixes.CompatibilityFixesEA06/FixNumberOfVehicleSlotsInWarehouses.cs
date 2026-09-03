using System.Linq;
using Buildings;
using Entities;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class FixNumberOfVehicleSlotsInWarehouses : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (Warehouse warehouse in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => (bool)x.BuildingCached && x.RentedByPlayer && x.GetBuildingType() == "ba:buildingtype_warehouse").OfType<Warehouse>())
		{
			int? num = BuildingSizeHelper.GetData(BuildingHelper.GetBuilding(warehouse.Address)?.BuildingSize)?.numberOfVehicleSlots;
			if (num > warehouse.vehicleSlots.Count)
			{
				for (int num2 = warehouse.vehicleSlots.Count; num2 < num; num2++)
				{
					warehouse.vehicleSlots.Add(new VehicleSlot());
				}
			}
			else
			{
				if (!(num < warehouse.vehicleSlots.Count))
				{
					continue;
				}
				int i;
				for (i = warehouse.vehicleSlots.Count - 1; i >= num; i--)
				{
					EmployeeInstance employeeInstance = gameInstance.EmployeeInstances.FirstOrDefault((EmployeeInstance x) => x.id == warehouse.vehicleSlots[i].employeeDriverId);
					if (employeeInstance != null)
					{
						EmployeeHelper.UnassignEmployeeFromAllWorkshifts(employeeInstance);
					}
					warehouse.vehicleSlots.RemoveAt(i);
				}
			}
		}
	}
}
