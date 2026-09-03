using System.Linq;
using Localizor;
using UI.Guiders;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Targets/VehicleTarget")]
public class VehicleTarget : QuestEntryTarget
{
	public string vehicleInstanceId;

	public override void SetTarget()
	{
		GuidersManager.SetGuiderTarget(GetTargetPosition(), localizeKey.GetLocalization(), InstanceBehavior<GlobalReferences>.Instance.vehiclePOIIcon, InstanceBehavior<GlobalReferences>.Instance.vehiclePOIBackgroundColor, guiderType);
	}

	private Vector3 GetTargetPosition()
	{
		if (string.IsNullOrEmpty(vehicleInstanceId))
		{
			return Vector3.zero;
		}
		VehicleInstance vehicleInstance = SaveGameManager.Current.VehicleInstances.FirstOrDefault((VehicleInstance x) => x.id == vehicleInstanceId);
		if (vehicleInstance == null)
		{
			return Vector3.zero;
		}
		return vehicleInstance.position;
	}
}
