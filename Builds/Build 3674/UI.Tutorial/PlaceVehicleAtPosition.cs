using System.Linq;
using Extensions;
using Helpers;
using Tutorial;
using UnityEngine;
using Vehicles.VehicleTypes;

namespace UI.Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Quest Actions/Place Vehicle At Position")]
public class PlaceVehicleAtPosition : QuestAction
{
	public string customId;

	public string vehicleTypeName;

	public Vector3 position;

	public float yRotation;

	[Range(0f, 1f)]
	public float fuelState = 1f;

	[Range(0f, 1f)]
	public float dirtinessState;

	public override void Do()
	{
		if (!SaveGameManager.Current.VehicleInstances.Any((VehicleInstance x) => x.id == customId))
		{
			VehicleHelper.CreateAndSpawnVehicle(new VehicleInstance(vehicleTypeName)
			{
				id = customId,
				vehicleColorName = InstanceBehavior<GlobalReferences>.Instance.vehicleColors.GetRandom().name,
				fuel = VehicleTypeHelper.GetVehicleType(vehicleTypeName).maxFuel * fuelState,
				dirtiness = dirtinessState
			}, position, Quaternion.Euler(0f, yRotation, 0f));
		}
	}
}
