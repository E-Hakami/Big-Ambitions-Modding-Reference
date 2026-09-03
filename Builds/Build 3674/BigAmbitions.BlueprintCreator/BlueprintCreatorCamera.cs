using System.Collections;
using CameraControllers;
using Cinemachine;
using Helpers;
using UnityEngine;

namespace BigAmbitions.BlueprintCreator;

public class BlueprintCreatorCamera : MonoBehaviour
{
	public PlacementCam placementCam;

	public PedestrianCam indoorCam;

	[SerializeField]
	private CinemachineVirtualCameraBase buildingPreviewCamera;

	[SerializeField]
	private CinemachineVirtualCameraBase indoorPlacementCamera;

	[HideInInspector]
	public TimeOfDayController timeOfDayController;

	private Transform _focusTarget;

	private void Awake()
	{
		timeOfDayController = Object.FindAnyObjectByType<TimeOfDayController>();
		timeOfDayController.Init();
	}

	public void PreviewVersion(Transform buildingTransform)
	{
		BuildingPreviewHandle component = (buildingTransform.Find("PreviewCameraData") ?? buildingTransform.Find("Structure/PreviewCameraData")).GetComponent<BuildingPreviewHandle>();
		_focusTarget = component.transform;
		buildingPreviewCamera.LookAt = _focusTarget;
		buildingPreviewCamera.Follow = _focusTarget;
		BuildingPreviewCam component2 = buildingPreviewCamera.GetComponent<BuildingPreviewCam>();
		component2.offset = component.direction;
		component2.minMaxDistance = component.minMaxDistance;
		CameraHelper.SetCamera(buildingPreviewCamera);
		ForceDayLighting();
	}

	private void ForceDayLighting()
	{
		timeOfDayController.SetEnvironmentSettings(12f, forceInsideBuilding: true);
		timeOfDayController.UpdateHourlyValues(12f, forceInsideBuilding: true, forceUpdateEnvironmentalValues: true);
	}

	internal IEnumerator AlternativeCameraToggle(bool show)
	{
		if (show)
		{
			placementCam.UpdateBounds();
			placementCam.distance = indoorCam.distance;
			placementCam.transform.position = _focusTarget.position;
			placementCam.ForceUpdateCameraPosition();
			placementCam.RotateCamera(indoorCam.angle - placementCam.currentAngle);
			yield return CameraHelper.SetCameraRoutine(indoorPlacementCamera);
		}
		else
		{
			ForceDayLighting();
			yield return CameraHelper.SetCameraRoutine(buildingPreviewCamera);
		}
	}
}
