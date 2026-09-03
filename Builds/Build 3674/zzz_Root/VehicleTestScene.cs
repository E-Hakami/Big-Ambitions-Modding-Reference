using System.Collections.Generic;
using System.Linq;
using BigAmbitions.SaveSystem;
using Cinemachine;
using Extensions;
using GleyTrafficSystem;
using JimmysUnityUtilities;
using NWH.VehiclePhysics2;
using TMPro;
using UnityEngine;
using Vehicles.VehicleTypes;

public class VehicleTestScene : MonoBehaviour
{
	public CinemachineVirtualCamera vehicleCam;

	public TMP_Dropdown dropdown;

	public bool initializeTrafficComponent;

	public TrafficComponent trafficComponent;

	public bool initializeParkingLanes;

	public Transform vehicleSpawnPosition;

	public SfxManager sfxManager;

	private void Awake()
	{
		GlobalEvents.Init();
		AddressableLoader.Register<VehicleType>("VehicleTypes", VehicleTypeHelper.OnVehicleTypesLoaded);
		AddressableLoader.ForceLoad();
	}

	private void Start()
	{
		List<NWH.VehiclePhysics2.VehicleController> playerVehicles = AddressablesHelper.LoadAddressablesFromFolder_EDITOR<NWH.VehiclePhysics2.VehicleController>("Prefabs/Vehicles/PlayerVehicles");
		dropdown.ClearOptions();
		dropdown.options.Add(new TMP_Dropdown.OptionData("-"));
		dropdown.AddOptions(playerVehicles.Select((NWH.VehiclePhysics2.VehicleController x) => new TMP_Dropdown.OptionData(x.name)).ToList());
		dropdown.onValueChanged.AddListener(delegate(int index)
		{
			int index2 = index - 1;
			NWH.VehiclePhysics2.VehicleController[] array2 = Object.FindObjectsByType<NWH.VehiclePhysics2.VehicleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			for (int i = 0; i < array2.Length; i++)
			{
				Object.Destroy(array2[i].gameObject);
			}
			NWH.VehiclePhysics2.VehicleController newVc = Object.Instantiate(playerVehicles[index2], vehicleSpawnPosition.position, Quaternion.Euler(0f, vehicleSpawnPosition.eulerAngles.y, 0f));
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				Transform transform = newVc.transform;
				newVc.effectsManager.lightsManager.lowBeamLights.SetState(state: true);
				newVc.effectsManager.lightsManager.tailLights.SetState(state: true);
				newVc.enabled = true;
				newVc.powertrain.engine.StartEngine();
				newVc.GetComponent<CarController>().controlledByPlayer = true;
				vehicleCam.Follow = transform;
				vehicleCam.LookAt = transform;
			});
			if ((bool)trafficComponent)
			{
				Manager.SetCamera(newVc.transform);
			}
		});
		if (initializeParkingLanes)
		{
			ParkingLaneGenerator[] array = Object.FindObjectsByType<ParkingLaneGenerator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			for (int num = 0; num < array.Length; num++)
			{
				array[num].Init();
			}
		}
		if (initializeTrafficComponent && (bool)trafficComponent)
		{
			int numberOfVehicles = trafficComponent.vehiclePool.GetNumberOfVehicles();
			Manager.Initialize(trafficComponent.player, numberOfVehicles, trafficComponent.vehiclePool, trafficComponent.minDistanceToAdd, trafficComponent.distanceToRemove, trafficComponent.greenLightTime, trafficComponent.yellowLightTime, new int[3] { 34, 33, 33 }, trafficComponent.blinkerTurnLookaheadDistance, trafficComponent.blinkerStopDelay);
		}
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			float value = Mathf.Log10(100f) * 20f;
			sfxManager.audioMixer.SetFloat("MasterVolume", value);
			sfxManager.vehicleAudioMixer.SetFloat("attenuation", value);
		});
	}
}
