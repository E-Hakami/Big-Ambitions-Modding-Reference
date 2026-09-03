using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BigAmbitions.InputSystem;
using BigAmbitions.InteriorDesigner.InteriorElements;
using BigAmbitions.PlacementSystem;
using Buildings.Indoors.InteriorDesign;
using Cinemachine;
using DG.Tweening;
using NaughtyAttributes;
using UI;
using UI.InteriorDesigner;
using UI.Load;
using UI.Smartphone;
using UI.Smartphone.Apps.Feedback;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CameraControllers;

public class PlacementCam : MonoBehaviour
{
	public Vector2 minMaxDistance = new Vector2(100f, 350f);

	public float mouseSensitivity = 5f;

	public float zoomGamepadMultiplier = 2f;

	public float movementKeyMultiplier = 4f;

	public float scrollSpeedMultiplier = 2f;

	public float mouseSpeedMultiplier = 40f;

	public float rotationSpeedDegreesPerSecond = 200f;

	private const float CamTweenMoveDuration = 0.25f;

	private const Ease CamTweenMoveEase = Ease.OutCubic;

	public Bounds bounds;

	private Vector2 _inputAxis;

	private bool _isRotating;

	private bool _mouseDownOverUI;

	private float _rotationChangeLastMousePos;

	private Vector3 _startPosition;

	private Transform _vCam;

	private Tween _moveCamTween;

	[NonSerialized]
	[ShowNonSerializedField]
	public float currentAngle;

	[NonSerialized]
	[ShowNonSerializedField]
	public float distance;

	private void Start()
	{
		_vCam = GetComponentInChildren<CinemachineVirtualCamera>().transform;
		if (SaveGameManager.Current != null)
		{
			if (SaveGameManager.Current.placementCameraZoom == 0f)
			{
				SaveGameManager.Current.placementCameraZoom = minMaxDistance.y;
			}
			distance = SaveGameManager.Current.placementCameraZoom;
		}
	}

	private void Update()
	{
		EventSystem current = EventSystem.current;
		if ((object)current != null && current.IsPointerOverGameObject())
		{
			if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
			{
				_mouseDownOverUI = true;
			}
			else if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
			{
				_mouseDownOverUI = false;
			}
		}
		else if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
		{
			_mouseDownOverUI = false;
		}
		if (LoadScene.isLoading || CustomColorPicker.isOpen || Feedback.IsOpen || GameManager.ShouldBlockKeyboardShortcuts() || _mouseDownOverUI || PlayerAction.SnapFreePlacement.Pressing() || (!PlacementSystem.IsInPlacementMode && !InteriorDesignerUI.IsOpen))
		{
			return;
		}
		if (PlayerAction.RightClick.Pressed())
		{
			_rotationChangeLastMousePos = Input.mousePosition.x;
		}
		float num = 0f;
		if (PlayerAction.RightClick.Pressing() && _rotationChangeLastMousePos != Input.mousePosition.x)
		{
			if (_isRotating)
			{
				StopCurrentCameraRotation();
			}
			float num2 = _rotationChangeLastMousePos - Input.mousePosition.x;
			if (PedestrianCam.invertRotation)
			{
				num2 *= -1f;
			}
			num += num2 * mouseSensitivity;
			_rotationChangeLastMousePos = Input.mousePosition.x;
		}
		bool flag = InteriorDesignerController.CurrentTool != null && InteriorDesignerController.CurrentTool.BlockMouseMoveInput();
		if ((Input.GetMouseButtonDown(0) && !flag) || Input.GetMouseButtonDown(2))
		{
			_startPosition = GetMousePos();
		}
		Vector3 inputVelocity = Vector3.zero;
		if ((Input.GetMouseButton(0) && !flag) || Input.GetMouseButton(2))
		{
			inputVelocity = GetMousePos() - _startPosition;
			_startPosition = GetMousePos();
		}
		if ((Input.GetMouseButtonUp(0) && !flag) || Input.GetMouseButtonUp(2))
		{
			_startPosition = Vector3.zero;
		}
		if (PlayerAction.RotateLeft.Pressed())
		{
			RotateCamera(90, !PedestrianCam.invertRotation);
		}
		if (PlayerAction.RotateRight.Pressed())
		{
			RotateCamera(90, PedestrianCam.invertRotation);
		}
		num %= 360f;
		bool flag2 = IsMovingInput(inputVelocity);
		inputVelocity -= (Vector3)PlayerAction.Move.Vector() * (movementKeyMultiplier * Time.unscaledDeltaTime);
		if (FullMenu.IsOpen || (InstanceBehavior<UIs>.Instance != null && InstanceBehavior<UIs>.Instance.draggableWindows.isCurrentlyDragging))
		{
			return;
		}
		if (flag2)
		{
			PlayerAction.Click.Reset();
		}
		Vector3 zero = Vector3.zero;
		zero -= GetFlatForward() * inputVelocity.y;
		zero += GetFlatRight() * inputVelocity.x;
		zero *= mouseSpeedMultiplier * distance;
		MoveCenter(zero);
		float num3 = 0f;
		if (!Keyboard.current.leftShiftKey.isPressed)
		{
			num3 = PlayerAction.Zoom.Value() * zoomGamepadMultiplier;
			if (!EventSystem.current.IsPointerOverGameObject())
			{
				num3 += Input.mouseScrollDelta.y;
			}
		}
		MoveCamera(num3);
		RotateCamera(num);
	}

	private static bool IsMovingInput(Vector3 inputVelocity)
	{
		return inputVelocity.sqrMagnitude > 1E-05f;
	}

	private void OnDisable()
	{
		StopCurrentCameraRotation();
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(bounds.center, bounds.size);
		Gizmos.color = Color.white;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Vector3 GetForward()
	{
		return base.transform.position - _vCam.transform.position;
	}

	private Vector3 GetForwardNormalized()
	{
		return GetForward().normalized;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Vector3 GetFlatForward()
	{
		Vector3 forward = GetForward();
		forward.y = 0f;
		return forward.normalized;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Vector3 GetFlatRight()
	{
		return Vector3.Cross(GetFlatForward(), base.transform.up);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Vector3 GetMousePos()
	{
		return GameManager.GetMainCamera().ScreenToViewportPoint(Input.mousePosition);
	}

	public void UpdateBounds()
	{
		if (!BuildingManager.IsInsideBuilding)
		{
			return;
		}
		Transform transform = InstanceBehavior<BuildingManager>.Instance.currentBuildingVersion.Find("PlacementCameraBounds");
		if (transform == null)
		{
			bounds = new Bounds(InteriorElementsHelper.InteriorElementsCache.First().Value.transform.position, Vector3.zero);
			{
				foreach (KeyValuePair<string, InteriorElement> item in InteriorElementsHelper.InteriorElementsCache.Where((KeyValuePair<string, InteriorElement> x) => x.Value.IsFloor))
				{
					bounds.Encapsulate(item.Value.transform.position);
				}
				return;
			}
		}
		bounds = new Bounds(transform.GetChild(0).position, Vector3.zero);
		bounds.Encapsulate(transform.GetChild(1).position);
	}

	public void ForceUpdateCameraPosition()
	{
		MoveCenter(Vector3.zero);
		MoveCamera(0f);
		RotateCamera(0f);
	}

	private void MoveCamera(float change)
	{
		if (change != 0f)
		{
			distance = Mathf.Max(Mathf.Min(distance - change * Time.unscaledDeltaTime * scrollSpeedMultiplier * distance, minMaxDistance.y), minMaxDistance.x);
		}
		_vCam.position = base.transform.position + -GetForwardNormalized() * distance;
		if (SaveGameManager.Current != null)
		{
			SaveGameManager.Current.placementCameraZoom = distance;
		}
	}

	private void MoveCenter(Vector3 velocity)
	{
		Vector3 position = base.transform.position;
		Vector3 vector = position + velocity;
		vector.x = Mathf.Min(Mathf.Max(vector.x, bounds.min.x), bounds.max.x);
		vector.y = Mathf.Min(Mathf.Max(vector.y, bounds.min.y), bounds.max.y);
		vector.z = Mathf.Min(Mathf.Max(vector.z, bounds.min.z), bounds.max.z);
		if (vector != position)
		{
			base.transform.position = vector;
		}
	}

	public void RotateCamera(float angle)
	{
		currentAngle += angle;
		currentAngle %= 360f;
		_vCam.RotateAround(base.transform.position, Vector3.up, angle);
		_vCam.rotation = Quaternion.LookRotation(GetForward());
	}

	private void RotateCamera(int angleChange, bool increment)
	{
		base.transform.DOKill();
		StopAllCoroutines();
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
		float targetAngle = (float)Mathf.RoundToInt((currentAngle + 360f + (float)(increment ? angleChange : (-angleChange))) / 90f) * 90f - 360f;
		float startAngle = currentAngle;
		float f = Mathf.DeltaAngle(startAngle, targetAngle);
		float duration = Mathf.Max(0.0001f, Mathf.Abs(f) / rotationSpeedDegreesPerSecond);
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			float target = Mathf.LerpAngle(startAngle, targetAngle, t);
			float angle = Mathf.DeltaAngle(currentAngle, target);
			RotateCamera(angle);
			yield return null;
		}
		RotateCamera(Mathf.DeltaAngle(currentAngle, targetAngle));
		_isRotating = false;
	}

	public void FocusOnPosition(Vector3 focusPosition)
	{
		MoveWithTween(focusPosition);
	}

	private void MoveWithTween(Vector3 targetPosition)
	{
		_moveCamTween?.Kill();
		_moveCamTween = base.transform.DOMove(targetPosition, 0.25f).SetEase(Ease.OutCubic).SetUpdate(isIndependentUpdate: true);
	}
}
