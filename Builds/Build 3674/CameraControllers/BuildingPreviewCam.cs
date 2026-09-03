using System;
using System.Collections;
using BigAmbitions.InputSystem;
using Cinemachine;
using DG.Tweening;
using NaughtyAttributes;
using UI;
using UI.Load;
using UnityEngine;

namespace CameraControllers;

public class BuildingPreviewCam : MonoBehaviour
{
	public bool blockCameraZoom;

	public float mouseSensitivity = 0.15f;

	public float rotationSpeedDegreesPerSecond = 200f;

	public float gamepadZoomSensitivity = 0.5f;

	public Vector3 offset;

	public bool autoRotate;

	public float autoRotateSpeed = 0.25f;

	public Vector2 minMaxDistance = new Vector2(0.25f, 1f);

	public float distanceSpeed = 2f;

	public float distanceChangeSpeed = 0.15f;

	public float distance = 1f;

	[NonSerialized]
	[ShowNonSerializedField]
	private float _angle;

	private float _currentDistance = 1f;

	private bool _isRotating;

	private float _rotationChangeLastMousePos;

	private CinemachineVirtualCamera _vCam;

	private void Start()
	{
		_vCam = GetComponent<CinemachineVirtualCamera>();
		_angle = -180f;
		_currentDistance = distance;
	}

	private void Update()
	{
		if (!LoadScene.isLoading)
		{
			UpdateInput(Time.unscaledDeltaTime);
		}
	}

	private void LateUpdate()
	{
		UpdateCam(Time.unscaledDeltaTime);
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

	private void UpdateInput(float deltaTime)
	{
		if (!BuildingPreview.isPreviewing)
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
			if (PedestrianCam.invertRotation)
			{
				num *= -1f;
			}
			_angle += num * mouseSensitivity;
			_rotationChangeLastMousePos = Input.mousePosition.x;
		}
		if (autoRotate)
		{
			_angle += autoRotateSpeed * deltaTime;
		}
		if (PlayerAction.RotateLeft.Pressed())
		{
			RotateCamera(90, !PedestrianCam.invertRotation);
		}
		if (PlayerAction.RotateRight.Pressed())
		{
			RotateCamera(90, PedestrianCam.invertRotation);
		}
		_angle %= 360f;
		float y = Input.mouseScrollDelta.y;
		y += PlayerAction.Zoom.Value() * gamepadZoomSensitivity;
		if (!blockCameraZoom)
		{
			distance = Mathf.Clamp(distance - y * distanceChangeSpeed, minMaxDistance.x, minMaxDistance.y);
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
		float targetAngle = (float)Mathf.RoundToInt((_angle + 360f + (float)(increment ? angleChange : (-angleChange))) / 90f) * 90f - 360f;
		float startAngle = _angle;
		float totalDelta = Mathf.DeltaAngle(startAngle, targetAngle);
		float duration = Mathf.Max(0.0001f, Mathf.Abs(totalDelta) / rotationSpeedDegreesPerSecond);
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float num = Mathf.Clamp01(elapsed / duration);
			_angle = startAngle + totalDelta * num;
			yield return null;
		}
		_angle = targetAngle;
		_isRotating = false;
	}

	private void UpdateCam(float deltaTime)
	{
		if (!(_vCam.Follow == null))
		{
			_currentDistance = Mathf.Lerp(_currentDistance, distance, deltaTime * distanceSpeed);
			base.transform.position = _vCam.Follow.position + offset.normalized * _currentDistance;
			base.transform.RotateAround(_vCam.Follow.position, Vector3.up, _angle);
		}
	}
}
