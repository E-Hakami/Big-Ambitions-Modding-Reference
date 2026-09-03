using System.Collections.Generic;
using Helpers;
using UnityEngine;

namespace Streets;

public class BridgeController : ViewBlockingEntity
{
	private const float RoadProbeHeight = 2f;

	private const float RoadProbeDistance = 2.5f;

	public float[] steeringAnglesAssists;

	public float aiTrafficVehiclesSpawnRadius = 60f;

	public float aiTrafficVehiclesDeSpawnRadius = 80f;

	[SerializeField]
	private bool enablePlayerNavigation = true;

	private readonly List<CarFeatures> _carFeaturesInBridge = new List<CarFeatures>();

	private Collider[] _roadColliders;

	public override void Awake()
	{
		_roadColliders = GetComponentsInChildren<Collider>();
		if (enablePlayerNavigation)
		{
			base.gameObject.layer = LayerHelper.GroundLayerIndex;
		}
	}

	public override void OnIoEnter()
	{
	}

	public override void OnIoExit()
	{
	}

	public override bool SetCameraBlockMode(bool isOn)
	{
		if (temporarilyDisableCameraBlock || isInCameraBlockMode == isOn)
		{
			if (temporarilyDisableCameraBlock && isInCameraBlockMode != isOn)
			{
				Debug.Log($"[BridgeFlip] '{base.name}' SKIPPED flip to blockMode={isOn} " + "(temporarilyDisableCameraBlock), layer=" + LayerMask.LayerToName(base.gameObject.layer), this);
			}
			return false;
		}
		isInCameraBlockMode = isOn;
		foreach (Renderer item in renderersToHide)
		{
			if (!(item == null))
			{
				ViewBlockingEntity.SetRendererToHideVisibility(item, isOn);
			}
		}
		foreach (GameObject item2 in objectsToDisable)
		{
			item2.SetActive(!isOn);
		}
		if (enablePlayerNavigation)
		{
			base.gameObject.layer = (isOn ? LayerHelper.RoadsLayerIndex : LayerHelper.GroundLayerIndex);
		}
		for (int i = 0; i < _carFeaturesInBridge.Count; i++)
		{
			_carFeaturesInBridge[i].UpdateBridgeHiddenState();
		}
		return true;
	}

	public bool IsRoadBelow(Vector3 position)
	{
		Ray ray = new Ray(position + Vector3.up * 2f, Vector3.down);
		Collider[] roadColliders = _roadColliders;
		foreach (Collider collider in roadColliders)
		{
			if (!collider.isTrigger && collider.Raycast(ray, out var _, 2.5f))
			{
				return true;
			}
		}
		return false;
	}

	public void AddCarToBridge(CarFeatures carFeatures)
	{
		_carFeaturesInBridge.Add(carFeatures);
		carFeatures.EnterBridge(this);
	}

	public void RemoveCarFromBridge(CarFeatures carFeatures)
	{
		if (_carFeaturesInBridge.Remove(carFeatures))
		{
			carFeatures.ExitBridge(this);
		}
	}
}
