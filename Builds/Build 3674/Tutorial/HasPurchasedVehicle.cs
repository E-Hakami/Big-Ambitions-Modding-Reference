using System.Linq;
using HGAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Vehicles/HasPurchasedVehicle")]
public class HasPurchasedVehicle : QuestRequirement
{
	[AutocompleteDropdown("VehicleTypes")]
	public string[] vehicleTypes;

	public override bool CheckIfCompleted()
	{
		return SaveGameManager.Current.VehicleInstances.Any((VehicleInstance x) => vehicleTypes.Contains(x.vehicleTypeName));
	}
}
