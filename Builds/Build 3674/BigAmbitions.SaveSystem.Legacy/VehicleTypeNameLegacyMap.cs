using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class VehicleTypeNameLegacyMap : LegacyMapperBase
{
	public override List<string> Keys => new List<string> { "VehicleType.vehicleTypeName", "VehicleInstance.vehicleTypeName", "VehicleDeliveryContract.vehicleTypeName", "DataHolder.vehicleName", "VehicleSpawnerController.vehicleType", "HasAssignedVehicleToWarehouse.vehicleTypes", "HasPurchasedVehicle.vehicleTypes" };

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 0, "ba:vehicletype_umcdesert" },
		{ 1, "ba:vehicletype_handtruck" },
		{ 2, "ba:vehicletype_honzamimic" },
		{ 4, "ba:vehicletype_missamvillian" },
		{ 5, "ba:vehicletype_mersaidis500" },
		{ 6, "ba:vehicletype_freighttruckt1" },
		{ 7, "ba:vehicletype_ferdinand112" },
		{ 8, "ba:vehicletype_bima320" },
		{ 9, "ba:vehicletype_vordv150" },
		{ 10, "ba:vehicletype_vordpony" },
		{ 11, "ba:vehicletype_umcnunavut" },
		{ 12, "ba:vehicletype_flatbed" },
		{ 13, "ba:vehicletype_petrollsfanton" },
		{ 14, "ba:vehicletype_mersaidimgagt" },
		{ 15, "ba:vehicletype_anselmoaf90" },
		{ 16, "ba:vehicletype_vordtiaravic" },
		{ 17, "ba:vehicletype_electricscooter" },
		{ 18, "ba:vehicletype_deliverytruck" },
		{ 19, "ba:vehicletype_umcdesertdeliveryjob" },
		{ 20, "ba:vehicletype_mersaididash" },
		{ 21, "ba:vehicletype_limo" }
	};
}
