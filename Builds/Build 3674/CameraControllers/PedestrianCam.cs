using System;
using System.Collections;
using BigAmbitions.InputSystem;
using Cinemachine;
using DG.Tweening;
using NaughtyAttributes;
using UI;
using UI.Load;
using UI.MiniMenu;
using UI.Smartphone;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CameraControllers;

public class PedestrianCam : MonoBehaviour
{
	public const int IncrementPerTime = 3;

	public static bool invertRotation;

	public static bool blockCameraZoom;

	public float mouseSensitivity = 0.15f;

	public float rotationSpeedDegreesPerSecond = 150f;

	public float gamepadZoomSensitivity = 0.5f;

	public Vector3 offset;

	public bool autoRotate;

	public float autoRotateSpeed = 0.25f;

	public Vector2 minMaxDistance = new Vector2(0.25f, 1f);

	public float distanceSpeed = 2f;

	public float distanceChangeSpeed = 0.15f;

	public float distance = 1f;

	public bool forceUseLateUpdate;

	private float _currentDistance = 1f;

	private bool _isRotating;

	private float _rotationChangeLastMousePos;

	private CinemachineVirtualCamera _vCam;

	[NonSerialized]
	[ShowNonSerializedField]
	public float angle;

	private void Start()
	{
		invertRotation = PlayerPrefSettings.InvertRotation;
		_vCam = GetComponent<CinemachineVirtualCamera>();
		angle = -180f;
		_currentDistance = distance;
	}

	private void Update()
	{
		if (!LoadScene.isLoading)
		{
			UpdateInput(Time.unscaledDeltaTime);
		}
	}

	private void FixedUpdate()
	{
		if (ShouldUseFixedUpdate())
		{
			UpdateCam(Time.fixedUnscaledDeltaTime);
		}
	}

	private void LateUpdate()
	{
		if (!ShouldUseFixedUpdate())
		{
			UpdateCam(Time.unscaledDeltaTime);
		}
	}

	[Button(null, EButtonEnableMode.Always)]
	private void UpdateToValues()
	{
		minMaxDistance.x = (offset * minMaxDistance.x).magnitude;
		minMaxDistance.y = (offset * minMaxDistance.y).magnitude;
		float magnitude = offset.magnitude;
		distance = magnitude * distance;
		if (magnitude != 0f)
		{
			offset /= magnitude;
		}
	}

	private bool ShouldUseFixedUpdate()
	{
		if (!forceUseLateUpdate && InstanceBehavior<GameManager>.Instance != null && (bool)InstanceBehavior<GameManager>.Instance.selectedVehicle)
		{
			return !InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleType.spawnInPlayerObject;
		}
		return false;
	}

	private void UpdateInput(float deltaTime)
	{
		if (FullMenu.IsOpen || CityMap.IsOpen || MiniMenu.IsOpen || BuildingPreview.isPreviewing || GameManager.ShouldBlockKeyboardShortcuts())
		{
			return;
		}
		if (Input.GetMouseButtonDown(1))
		{
			_rotationChangeLastMousePos = Input.mousePosition.x;
		}
		if (Input.GetMouseButton(1) && !Mathf.Approximately(_rotationChangeLastMousePos, Input.mousePosition.x))
		{
			if (_isRotating)
			{
				StopCurrentCameraRotation();
			}
			float num = _rotationChangeLastMousePos - Input.mousePosition.x;
			if (invertRotation)
			{
				num *= -1f;
			}
			angle += num * mouseSensitivity;
			_rotationChangeLastMousePos = Input.mousePosition.x;
		}
		if (autoRotate)
		{
			angle += autoRotateSpeed * deltaTime;
		}
		if (PlayerAction.RotateLeft.Pressed())
		{
			RotateCamera(90, !invertRotation);
		}
		if (PlayerAction.RotateRight.Pressed())
		{
			RotateCamera(90, invertRotation);
		}
		angle %= 360f;
		float num2 = (EventSystem.current.IsPointerOverGameObject() ? 0f : Input.mouseScrollDelta.y);
		num2 += PlayerAction.Zoom.Value() * gamepadZoomSensitivity;
		if (!blockCameraZoom)
		{
			distance = Mathf.Clamp(distance - num2 * distanceChangeSpeed, minMaxDistance.x, minMaxDistance.y);
		}
	}

	private void RotateCamera(int angleChange, bool increment)
	{
		StopCurrentCameraRotation();
		StartCoroutine(RotateCameraCoroutine(angleChange, increment));
	}

	private void StopCurrentCameraRotation()
	{
		base.transform.DOKill();
		StopAllCoroutines();
		_isRotating = false;
	}

	private IEnumerator RotateCameraCoroutine(int angleChange, bool increment)
	{
		_isRotating = true;
		float targetAngle = (float)Mathf.RoundToInt((angle + 360f + (float)(increment ? angleChange : (-angleChange))) / 90f) * 90f - 360f;
		float startAngle = angle;
		float totalDelta = Mathf.DeltaAngle(startAngle, targetAngle);
		float duration = Mathf.Max(0.0001f, Mathf.Abs(totalDelta) / rotationSpeedDegreesPerSecond);
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float num = Mathf.Clamp01(elapsed / duration);
			angle = startAngle + totalDelta * num;
			yield return null;
		}
		angle = targetAngle;
		_isRotating = false;
	}

	private void UpdateCam(float deltaTime)
	{
		if (!(_vCam.Follow == null))
		{
			_currentDistance = Mathf.Lerp(_currentDistance, distance, deltaTime * distanceSpeed);
			base.transform.position = _vCam.Follow.position + offset.normalized * _currentDistance;
			base.transform.RotateAround(_vCam.Follow.position, Vector3.up, angle);
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		invertRotation = false;
		blockCameraZoom = false;
	}
}
