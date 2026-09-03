using BigAmbitions.Items;

namespace Vehicles.DeliveryDriverJob;

public class DeliveryVehicleInstance : VehicleInstance
{
	public DeliveryVehicleInstance(string vehicleTypeName)
		: base(vehicleTypeName)
	{
	}

	public override bool TryToAddToCargo(CargoInstance cargoInstance)
	{
		if (cargoInstance.itemName != "ba:itemname_handtruck" && cargoInstance.itemName != "ba:itemname_flatbed" && !cargoInstance.IsSealed)
		{
			return false;
		}
		return base.TryToAddToCargo(cargoInstance);
	}
}
