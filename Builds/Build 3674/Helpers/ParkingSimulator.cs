using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using Entities;
using Extensions;
using GleyTrafficSystem;
using Localizor;
using Streets;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using Vehicles.VehicleTypes;

namespace Helpers;

public static class ParkingSimulator
{
	public class ParkingQueueWorker : QueueWorker<ParkingLaneGenerator>
	{
		protected override void Execute(ParkingLaneGenerator work)
		{
			if ((bool)work)
			{
				work.ProcessParkingLane();
			}
		}
	}

	public const float UndergroundParkingHourlyFee = 25f;

	private const int ParkingTicketFee = 125;

	public static readonly UnityEvent ParkingLaneRegeneration = new UnityEvent();

	public static readonly ParkingQueueWorker parkingQueueWorker = new ParkingQueueWorker();

	private static readonly Dictionary<string, ObjectPool<GameObject>> ParkedVehiclePools = new Dictionary<string, ObjectPool<GameObject>>();

	private static Transform ParkedVehiclesStorage;

	public static void ResetPool()
	{
		foreach (ObjectPool<GameObject> value in ParkedVehiclePools.Values)
		{
			value.Clear();
		}
		ParkedVehiclePools.Clear();
	}

	private static void InitVehiclePools()
	{
		ParkedVehiclesStorage = (InstanceBehavior<CityManager>.IsInitialized ? InstanceBehavior<CityManager>.Instance.parkedVehiclesStorage : new GameObject("parkedVehiclesStorage").transform);
		CarType[] trafficCars = InstanceBehavior<GlobalReferences>.Instance.vehiclePool.trafficCars;
		foreach (CarType vehiclePoolTrafficCar in trafficCars)
		{
			if (vehiclePoolTrafficCar.hasParkedVersion)
			{
				ObjectPool<GameObject> value = new ObjectPool<GameObject>(delegate
				{
					GameObject gameObject = PrefabHelper.CreatePrefab("Vehicles/ParkedVehicles/" + vehiclePoolTrafficCar.vehiclePrefab.name, ParkedVehiclesStorage);
					gameObject.name = "ba:vehicletype_" + vehiclePoolTrafficCar.vehiclePrefab.name.ToLowerInvariant();
					return gameObject;
				}, delegate(GameObject obj)
				{
					obj.transform.SetParent(null);
					obj.transform.rotation = Quaternion.identity;
					obj.transform.position = Vector3.zero;
					obj.SetActive(value: true);
				}, delegate(GameObject obj)
				{
					obj.transform.parent = ParkedVehiclesStorage;
					obj.SetActive(value: false);
				}, ObjectPoolHelper.DestroyPoolObject, collectionCheck: false, vehiclePoolTrafficCar.nrOfVehicles, vehiclePoolTrafficCar.nrOfVehicles + 30);
				string key = "ba:vehicletype_" + vehiclePoolTrafficCar.vehiclePrefab.name.ToLowerInvariant();
				ParkedVehiclePools.Add(key, value);
			}
		}
	}

	public static GameObject RequestParkedVehicle(string vehicleName)
	{
		if (ParkedVehiclePools.Count == 0)
		{
			InitVehiclePools();
		}
		return ParkedVehiclePools[vehicleName].Get();
	}

	public static void ReleaseParkedVehicle(GameObject vehicle)
	{
		if (ParkedVehiclePools.Count == 0)
		{
			InitVehiclePools();
		}
		ParkedVehiclePools[vehicle.name].Release(vehicle);
	}

	public static void RunDaily()
	{
		ParkingLaneRegeneration.Invoke();
		Physics.SyncTransforms();
		VehicleHelper.DestroyBlockingParkedVehicles(skipPlayerMountedVehicles: true);
		foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances)
		{
			if (vehicleInstance.unpaidParkingAmount != 0f)
			{
				Dictionary<string, string> data = new Dictionary<string, string> { 
				{
					"vehicleName",
					vehicleInstance.vehicleTypeName.GetLocalization()
				} };
				TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_publicparking", "ba:transactioncategory_parkingfees", data);
				GameManager.ChangeMoneySafe(0f - vehicleInstance.unpaidParkingAmount, transactionInfo, SaveGameManager.Current.Day - 1, null, force: true);
				vehicleInstance.unpaidParkingAmount = 0f;
			}
		}
	}

	public static void RunHourly()
	{
		foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances)
		{
			VehicleType vehicleType = vehicleInstance.VehicleType;
			if (vehicleType == null || vehicleType.spawnInPlayerObject || SaveGameManager.Current.ActiveVehicleId == vehicleInstance.id)
			{
				continue;
			}
			switch (vehicleInstance.parkingState)
			{
			case ParkingState.Illegal:
			{
				if (vehicleInstance.Address.IsUndefined())
				{
					VehicleController vehicleController = VehicleHelper.AllPlayerVehicles.FirstOrDefault((VehicleController x) => x.vehicleInstance.id == vehicleInstance.id);
					if (vehicleController == null || vehicleController.IsVisible)
					{
						break;
					}
				}
				Timestamp latestParkingTicket = vehicleInstance.GetLatestParkingTicket();
				if ((latestParkingTicket == null || Mathf.Ceil(TimeHelper.Now().GetDifferenceInMinutes(latestParkingTicket)) >= 1440f) && RngHelper.Chance(70))
				{
					vehicleInstance.parkingTickets.Add(TimeHelper.Now());
					Dictionary<string, string> data = new Dictionary<string, string> { 
					{
						"vehicleName",
						vehicleInstance.vehicleTypeName.GetLocalization()
					} };
					TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_parkingticket", "ba:transactioncategory_parkingfees", data);
					GameManager.ChangeMoneySafe(-125f, transactionInfo, null, null, force: true);
					string vehicleTypeName = vehicleInstance.vehicleTypeName;
					string value = $"{SaveGameManager.Current.Hour:0#}";
					string value2 = $"{SaveGameManager.Current.Minute:0#}";
					int day = SaveGameManager.Current.Day;
					Contact contact = Contact.GetContact("the_city_of_new_york", ContactCategoryName.General, "government");
					Dictionary<string, string> messageData = new Dictionary<string, string>
					{
						{ "vehicleTypeName", vehicleTypeName },
						{ "hour", value },
						{ "minute", value2 },
						{
							"day",
							day.ToString()
						}
					};
					GameManager.SendTextMessage(contact, "ba:messagetype_phone_government_parking_ticket", messageData, null, null, notify: false);
					ContactsHelper.AddParkingTicketNotification(contact);
					SaveGameManager.Current.achievementsData.parkingTickets++;
					GameEvent.Invoke(string.Empty);
				}
				break;
			}
			case ParkingState.Legal:
			{
				float num = ((vehicleInstance.streetName == "ba:street_parking") ? 25f : NeighborhoodHelper.GetData(vehicleInstance.parkingNeighbourhood).parkingPrice);
				vehicleInstance.unpaidParkingAmount += num;
				break;
			}
			}
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		ParkedVehiclePools.Clear();
		ParkedVehiclesStorage = null;
	}
}
