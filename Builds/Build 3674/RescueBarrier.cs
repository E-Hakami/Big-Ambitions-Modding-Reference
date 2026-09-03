using System.Collections.Generic;
using System.Linq;
using System.Text;
using BigAmbitions.ModsInternal;
using Helpers;
using UI.Notification;
using UI.Smartphone.Apps.Feedback;
using UnityEngine;

public class RescueBarrier : MonoBehaviour
{
	private string _hitColliderPath;

	private Vector3 _hitPosition;

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player") || other.CompareTag("Vehicle"))
		{
			_hitPosition = other.transform.position;
			_hitColliderPath = GetInScenePath(other.transform);
			Debug.LogWarning($"{_hitColliderPath} hit rescue barrier at {_hitPosition}");
			if (other.CompareTag("Player"))
			{
				InstanceBehavior<GameManager>.Instance.TransportToHospital();
			}
			else if (VehicleHelper.GetCurrentVehicleBase()?.vehicleCollider == other)
			{
				RescueVehicleBeingDriven();
			}
			else
			{
				RescueVehicleNotBeingDriven(other);
			}
		}
	}

	private void RescueVehicleBeingDriven()
	{
		if (VehicleHelper.IsInsideMotorVehicle())
		{
			VehicleHelper.TowVehicle(VehicleHelper.GetCurrentVehicle(), "ba:towdestination_autorepairshop");
		}
		else
		{
			VehicleHelper.GetCurrentVehicleBase()?.ExitVehicle();
		}
		if (!ModLifecycleLoader.AnyGameplayModsLoaded)
		{
			Notifications.Show(NotificationType.Error, "notification_fell_through_map", null, 10f, null, delegate
			{
				InstanceBehavior<Feedback>.Instance.Toggle(show: true);
			}, notificationSound: true, trackOnSaveGame: false);
		}
	}

	private void RescueVehicleNotBeingDriven(Collider bodyCollider)
	{
		VehicleController vehicleController = VehicleHelper.AllPlayerVehicles.FirstOrDefault((VehicleController x) => x.vehicleCollider == bodyCollider);
		if (!(vehicleController == null))
		{
			Vector3 vehicleRespawnSafetySpot = InstanceBehavior<GlobalReferences>.Instance.vehicleRespawnSafetySpot;
			vehicleController.transform.position = vehicleRespawnSafetySpot;
			vehicleController.transform.rotation = Quaternion.identity;
		}
	}

	private static string GetInScenePath(Transform transform)
	{
		Transform transform2 = transform;
		List<string> list = new List<string> { transform2.name };
		while (transform2 != transform.root)
		{
			transform2 = transform2.parent;
			list.Add(transform2.name);
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string item in Enumerable.Reverse(list))
		{
			stringBuilder.Append("\\" + item);
		}
		return stringBuilder.ToString().TrimStart('\\');
	}
}
