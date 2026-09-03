using System;
using System.Collections.Generic;
using Helpers;
using UI.Load;
using UnityEngine;
using UnityEngine.Rendering;

public class ParkingBuildingWithDifferentHeightsManager : MonoBehaviour
{
	private const float RampsTimeToCameraBlock = 0.1f;

	[SerializeField]
	private ViewBlockingEntity outsideViewBlockingEntity;

	[Space]
	[Header("First Floor")]
	[SerializeField]
	private ViewBlockingEntity[] firstFloorViewBlockingEntities;

	[SerializeField]
	private ViewBlockingEntity firstFloorCarHider;

	[Space]
	[Header("Second Floor")]
	[SerializeField]
	private Transform secondFloorGroundHeight;

	[SerializeField]
	private ViewBlockingEntity secondFloorViewBlockingEntity;

	[SerializeField]
	private ViewBlockingEntity secondFloorRampSingleViewBlockingEntity;

	[SerializeField]
	private ViewBlockingEntity[] secondFloorRampViewBlockingEntities;

	[SerializeField]
	private ParkingLaneGenerator[] secondFloorLaneGenerators;

	[SerializeField]
	private ViewBlockingEntity secondFloorCarHider;

	[Header("Third Floor")]
	[Space]
	[SerializeField]
	private ViewBlockingEntity thirdFloorViewBlockingEntity;

	[SerializeField]
	private ViewBlockingEntity thirdFloorCarHider;

	[SerializeField]
	private ParkingLaneGenerator[] thirdFloorLaneGenerators;

	[Space]
	[SerializeField]
	private Renderer[] floorMeshes;

	[SerializeField]
	private Collider lodCollider;

	private readonly List<GameObject> playerVehiclesToHide = new List<GameObject>();

	private float[] floorHeights = new float[0];

	private int currentHeightIndex = -1;

	private bool _aboveSecondFloorGroundHeight;

	private void CalculateHeights()
	{
		floorHeights = new float[floorMeshes.Length];
		for (int i = 0; i < floorMeshes.Length; i++)
		{
			Bounds bounds = floorMeshes[i].bounds;
			floorHeights[i] = bounds.center.y - bounds.extents.y;
		}
		UpdateHeight();
	}

	private void Awake()
	{
		GlobalEvents.RegisterOnGameLoadedCallback(CalculateHeights);
	}

	private void Start()
	{
		ViewBlockingEntity[] array = secondFloorRampViewBlockingEntities;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].timeToCameraBlock = 0.1f;
		}
		secondFloorRampSingleViewBlockingEntity.timeToCameraBlock = 0.1f;
		SubscribeToLaneGeneratorEvents();
		SubscribeToVehicleEvents();
		base.enabled = false;
	}

	private void OnDestroy()
	{
		UnSubscribeToLaneGeneratorEvents();
		UnSubscribeToVehicleEvents();
	}

	private void SubscribeToLaneGeneratorEvents()
	{
		ParkingLaneGenerator[] array = secondFloorLaneGenerators;
		foreach (ParkingLaneGenerator obj in array)
		{
			obj.onGenerateVehicle = (Action<GameObject>)Delegate.Combine(obj.onGenerateVehicle, new Action<GameObject>(AddVehicleToHideSecondFloor));
		}
		array = thirdFloorLaneGenerators;
		foreach (ParkingLaneGenerator obj2 in array)
		{
			obj2.onGenerateVehicle = (Action<GameObject>)Delegate.Combine(obj2.onGenerateVehicle, new Action<GameObject>(AddVehicleToHideThirdFloor));
		}
		array = secondFloorLaneGenerators;
		foreach (ParkingLaneGenerator obj3 in array)
		{
			obj3.onReleaseVehicle = (Action<GameObject>)Delegate.Combine(obj3.onReleaseVehicle, new Action<GameObject>(RemoveVehicleToHideSecondFloor));
		}
		array = thirdFloorLaneGenerators;
		foreach (ParkingLaneGenerator obj4 in array)
		{
			obj4.onReleaseVehicle = (Action<GameObject>)Delegate.Combine(obj4.onReleaseVehicle, new Action<GameObject>(RemoveVehicleToHideThirdFloor));
		}
	}

	private void SubscribeToVehicleEvents()
	{
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onEnterVehicle, new Action<VehicleController>(OnEnterVehicle));
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, new Action<VehicleController>(OnExitVehicle));
		VehicleHelper.onVehicleDestroyed.AddListener(delegate(VehicleController vehicle)
		{
			CancelPlayerVehicleHiding(vehicle.gameObject);
		});
	}

	private void UnSubscribeToLaneGeneratorEvents()
	{
		ParkingLaneGenerator[] array = secondFloorLaneGenerators;
		foreach (ParkingLaneGenerator obj in array)
		{
			obj.onGenerateVehicle = (Action<GameObject>)Delegate.Remove(obj.onGenerateVehicle, new Action<GameObject>(AddVehicleToHideSecondFloor));
		}
		array = thirdFloorLaneGenerators;
		foreach (ParkingLaneGenerator obj2 in array)
		{
			obj2.onGenerateVehicle = (Action<GameObject>)Delegate.Remove(obj2.onGenerateVehicle, new Action<GameObject>(AddVehicleToHideThirdFloor));
		}
		array = secondFloorLaneGenerators;
		foreach (ParkingLaneGenerator obj3 in array)
		{
			obj3.onReleaseVehicle = (Action<GameObject>)Delegate.Remove(obj3.onReleaseVehicle, new Action<GameObject>(RemoveVehicleToHideSecondFloor));
		}
		array = thirdFloorLaneGenerators;
		foreach (ParkingLaneGenerator obj4 in array)
		{
			obj4.onReleaseVehicle = (Action<GameObject>)Delegate.Remove(obj4.onReleaseVehicle, new Action<GameObject>(RemoveVehicleToHideThirdFloor));
		}
	}

	private void UnSubscribeToVehicleEvents()
	{
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Remove(GlobalEvents.onEnterVehicle, new Action<VehicleController>(OnEnterVehicle));
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Remove(GlobalEvents.onExitVehicle, new Action<VehicleController>(OnExitVehicle));
		VehicleHelper.onVehicleDestroyed.RemoveListener(delegate(VehicleController vehicle)
		{
			CancelPlayerVehicleHiding(vehicle.gameObject);
		});
	}

	private void AddVehicleToHideSecondFloor(GameObject vehicle)
	{
		AddCarToHide(vehicle, secondFloorCarHider);
	}

	private void RemoveVehicleToHideSecondFloor(GameObject vehicle)
	{
		RemoveCarToHide(vehicle, secondFloorCarHider);
	}

	private void AddVehicleToHideThirdFloor(GameObject vehicle)
	{
		AddCarToHide(vehicle, thirdFloorCarHider);
	}

	private void RemoveVehicleToHideThirdFloor(GameObject vehicle)
	{
		RemoveCarToHide(vehicle, thirdFloorCarHider);
	}

	private void OnExitVehicle(VehicleController vehicle)
	{
		if (base.enabled)
		{
			SetPlayerVehicleToHide(vehicle.gameObject);
		}
	}

	private void OnEnterVehicle(VehicleController vehicle)
	{
		if (base.enabled)
		{
			CancelPlayerVehicleHiding(vehicle.gameObject);
		}
	}

	private void SetPlayerVehicleToHide(GameObject vehicle)
	{
		if (!playerVehiclesToHide.Contains(vehicle))
		{
			float y = vehicle.transform.position.y;
			int floorIndex = GetFloorIndex(y);
			ViewBlockingEntity floorCarHider = GetFloorCarHider(floorIndex);
			if (floorCarHider != null)
			{
				AddCarToHide(vehicle, floorCarHider);
				playerVehiclesToHide.Add(vehicle);
			}
		}
	}

	private void CancelPlayerVehicleHiding(GameObject vehicle)
	{
		if (playerVehiclesToHide.Contains(vehicle))
		{
			playerVehiclesToHide.Remove(vehicle);
			int floorIndex = GetFloorIndex(vehicle.transform.position.y);
			ViewBlockingEntity floorCarHider = GetFloorCarHider(floorIndex);
			if (floorCarHider != null)
			{
				RemoveCarToHide(vehicle, floorCarHider);
			}
		}
	}

	private int GetFloorIndex(float vehicleY)
	{
		for (int i = 0; i < floorHeights.Length; i++)
		{
			float num = floorHeights[i];
			if (vehicleY >= num)
			{
				if (i >= floorHeights.Length - 1)
				{
					return i;
				}
				if (vehicleY < floorHeights[i + 1])
				{
					return i;
				}
			}
		}
		return 0;
	}

	private ViewBlockingEntity GetFloorCarHider(int floorIndex)
	{
		return floorIndex switch
		{
			0 => firstFloorCarHider, 
			1 => secondFloorCarHider, 
			_ => thirdFloorCarHider, 
		};
	}

	private void AddCarToHide(GameObject vehicle, ViewBlockingEntity hider)
	{
		MeshRenderer[] componentsInChildren = vehicle.GetComponentsInChildren<MeshRenderer>();
		foreach (MeshRenderer meshRenderer in componentsInChildren)
		{
			if (meshRenderer.shadowCastingMode != ShadowCastingMode.ShadowsOnly)
			{
				hider.renderersToHide.Add(meshRenderer);
			}
		}
	}

	private void RemoveCarToHide(GameObject vehicle, ViewBlockingEntity hider)
	{
		MeshRenderer[] componentsInChildren = vehicle.GetComponentsInChildren<MeshRenderer>();
		foreach (MeshRenderer meshRenderer in componentsInChildren)
		{
			if (hider.renderersToHide.Remove(meshRenderer) && hider.IsInCameraBlockMode)
			{
				ViewBlockingEntity.SetRendererToHideVisibility(meshRenderer, shouldHide: false);
			}
		}
	}

	private void Update()
	{
		if (!LoadScene.isLoading)
		{
			UpdateHeight();
		}
	}

	private void UpdateHeight()
	{
		float y = InstanceBehavior<GameManager>.Instance.playerController.transform.position.y;
		int floorIndex = GetFloorIndex(y);
		if (currentHeightIndex != floorIndex)
		{
			currentHeightIndex = floorIndex;
			switch (currentHeightIndex)
			{
			case 0:
				OnFirstFloorEnter();
				break;
			case 1:
				OnSecondFloorEnter();
				break;
			default:
				OnThirdFloorEnter();
				break;
			}
		}
		bool flag = y > secondFloorGroundHeight.position.y;
		if (_aboveSecondFloorGroundHeight != flag)
		{
			_aboveSecondFloorGroundHeight = flag;
			OnPassSecondFloorGround();
		}
	}

	private void OnFirstFloorEnter()
	{
		SetCameraBlockMode(firstFloorCarHider, isActive: false);
		ViewBlockingEntity[] array = secondFloorRampViewBlockingEntities;
		foreach (ViewBlockingEntity viewBlockingEntity in array)
		{
			viewBlockingEntity.enabled = false;
			SetCameraBlockMode(viewBlockingEntity, isActive: false);
		}
		secondFloorRampSingleViewBlockingEntity.enabled = false;
		SetCameraBlockMode(secondFloorRampSingleViewBlockingEntity, isActive: false);
		secondFloorViewBlockingEntity.enabled = true;
		SetCameraBlockMode(secondFloorViewBlockingEntity, isActive: true);
		secondFloorCarHider.enabled = true;
		SetCameraBlockMode(secondFloorCarHider, isActive: true);
		thirdFloorViewBlockingEntity.enabled = true;
		SetCameraBlockMode(thirdFloorViewBlockingEntity, isActive: true);
		thirdFloorCarHider.enabled = true;
		SetCameraBlockMode(thirdFloorCarHider, isActive: true);
	}

	private void OnSecondFloorEnter()
	{
		SetCameraBlockMode(firstFloorCarHider, isActive: true);
		secondFloorViewBlockingEntity.enabled = false;
		SetCameraBlockMode(secondFloorViewBlockingEntity, isActive: false);
		secondFloorCarHider.enabled = false;
		SetCameraBlockMode(secondFloorCarHider, isActive: false);
		thirdFloorViewBlockingEntity.enabled = true;
		thirdFloorCarHider.enabled = true;
		ViewBlockingEntity[] array = secondFloorRampViewBlockingEntities;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = _aboveSecondFloorGroundHeight;
		}
		secondFloorRampSingleViewBlockingEntity.enabled = !_aboveSecondFloorGroundHeight;
	}

	private void OnThirdFloorEnter()
	{
		SetCameraBlockMode(firstFloorCarHider, isActive: true);
		thirdFloorViewBlockingEntity.enabled = false;
		SetCameraBlockMode(thirdFloorViewBlockingEntity, isActive: false);
		thirdFloorCarHider.enabled = false;
		SetCameraBlockMode(thirdFloorCarHider, isActive: false);
	}

	private void OnTriggerEnter(Collider other)
	{
		bool num = other.CompareTag("Vehicle");
		bool flag = IsActiveVehicle(other);
		if (num && !flag)
		{
			VehicleController componentInParent = other.GetComponentInParent<VehicleController>();
			if (componentInParent != null)
			{
				SetPlayerVehicleToHide(componentInParent.gameObject);
			}
		}
		else if (IsPlayer(other) | flag)
		{
			base.enabled = true;
			lodCollider.enabled = false;
			outsideViewBlockingEntity.enabled = false;
			SetCameraBlockMode(outsideViewBlockingEntity, isActive: false);
			outsideViewBlockingEntity.SetFadeSate(1f);
			ViewBlockingEntity[] array = firstFloorViewBlockingEntities;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
		}
		ViewBlockingObjectManager.SetDistanceOffset(3f);
	}

	private void OnPassSecondFloorGround()
	{
		ViewBlockingEntity[] array;
		if (_aboveSecondFloorGroundHeight)
		{
			array = secondFloorRampViewBlockingEntities;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
			secondFloorRampSingleViewBlockingEntity.enabled = false;
			SetCameraBlockMode(secondFloorRampSingleViewBlockingEntity, isActive: false);
			return;
		}
		array = secondFloorRampViewBlockingEntities;
		foreach (ViewBlockingEntity viewBlockingEntity in array)
		{
			viewBlockingEntity.enabled = false;
			SetCameraBlockMode(viewBlockingEntity, isActive: false);
		}
		secondFloorRampSingleViewBlockingEntity.enabled = true;
	}

	private void OnTriggerExit(Collider other)
	{
		if (IsPlayerOrActiveVehicle(other))
		{
			ViewBlockingEntity[] array = firstFloorViewBlockingEntities;
			foreach (ViewBlockingEntity viewBlockingEntity in array)
			{
				viewBlockingEntity.enabled = false;
				SetCameraBlockMode(viewBlockingEntity, isActive: false);
				viewBlockingEntity.SetFadeSate(1f);
			}
			lodCollider.enabled = true;
			outsideViewBlockingEntity.enabled = true;
			ViewBlockingObjectManager.SetDistanceOffset(4f);
		}
	}

	private void SetCameraBlockMode(ViewBlockingEntity viewBlockingEntity, bool isActive)
	{
		viewBlockingEntity.SetCameraBlockMode(isActive);
		if (isActive)
		{
			ViewBlockingObjectManager.MarkEntityActive(viewBlockingEntity);
		}
	}

	private static bool IsPlayerOrActiveVehicle(Collider other)
	{
		if (!IsPlayer(other))
		{
			return IsActiveVehicle(other);
		}
		return true;
	}

	private static bool IsActiveVehicle(Collider other)
	{
		if (other.CompareTag("Vehicle"))
		{
			return other.GetComponentInParent<VehicleController>().vehicleInstance.id == SaveGameManager.Current.ActiveVehicleId;
		}
		return false;
	}

	private static bool IsPlayer(Collider other)
	{
		return other.gameObject.CompareTag("Player");
	}
}
