using System;
using System.Collections;
using BigAmbitions.InputSystem;
using CameraControllers;
using Cinemachine;
using DG.Tweening;
using NaughtyAttributes;
using Scenes.MainMenu;
using UI;
using UI.MiniMenu;
using UI.Smartphone;
using UI.Smartphone.Apps.Feedback;
using UnityEngine;
using UnityEngine.EventSystems;

public class CityMapCam : MonoBehaviour
{
	private const int MoveToTargetDurationSampleCount = 16;

	private const float MoveToTargetMinCurveSpeed = 0.0001f;

	public Vector2 minMaxDistance = new Vector2(100f, 350f);

	public float mouseSensitivity = 5f;

	public float zoomGamepadMultiplier = 2f;

	public float movementKeyMultiplier = 0.5f;

	public float movementKeyFastMultiplier = 3f;

	public float scrollSpeedMultiplier = 2f;

	public float mouseSpeedMultiplier = 40f;

	public float rotationSpeedDegreesPerSecond = 200f;

	public float moveToTargetMaxDuration = 1.5f;

	public float moveToTargetStopDistance = 0.2f;

	public Bounds bounds;

	public Vector3 moveToPosition;

	public AnimationCurve moveCurve;

	private float _currentAngle;

	private Vector2 _inputAxis;

	private bool _isRotating;

	private float _moveToObjectInitialDistance;

	private Vector3 _rotationChangeLastMousePos;

	private Vector3 _startPosition;

	private Transform _vCam;

	private bool _wasClickingUI;

	private float _moveToTargetSpeedMultiplier = 1f;

	private bool _isMovingToTarget;

	[NonSerialized]
	[ShowNonSerializedField]
	public float distance;

	private void Start()
	{
		_vCam = GetComponentInChildren<CinemachineVirtualCamera>().transform;
		if (SaveGameManager.Current.cityMapZoom == 0f)
		{
			SaveGameManager.Current.cityMapZoom = minMaxDistance.y;
		}
		distance = SaveGameManager.Current.cityMapZoom;
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool isOpen)
		{
			if (isOpen)
			{
				_wasClickingUI = false;
			}
		});
	}

	private void Update()
	{
		if (!CityMap.IsOpen || BuildingPreview.isPreviewing)
		{
			return;
		}
		if (_isMovingToTarget && HasManualCameraInput())
		{
			CancelMoveToTarget();
		}
		if (!_isMovingToTarget && (PlayerAction.Click.Pressed() || PlayerAction.RightClick.Pressed()))
		{
			_wasClickingUI = EventSystem.current.IsPointerOverGameObject();
		}
		float num = 0f;
		if (!_isMovingToTarget && !_wasClickingUI)
		{
			if (Input.GetMouseButtonDown(1))
			{
				_rotationChangeLastMousePos = Input.mousePosition;
			}
			if (Input.GetMouseButton(1) && _rotationChangeLastMousePos != Input.mousePosition)
			{
				if (_isRotating)
				{
					StopCurrentCameraRotation();
				}
				float num2 = _rotationChangeLastMousePos.x - Input.mousePosition.x;
				if (PedestrianCam.invertRotation)
				{
					num2 *= -1f;
				}
				num += num2 * mouseSensitivity;
				_rotationChangeLastMousePos = Input.mousePosition;
			}
		}
		Vector3 vector = Vector3.zero;
		if (!_isMovingToTarget && !_wasClickingUI)
		{
			if (Input.GetMouseButtonDown(0))
			{
				_startPosition = GetMousePos();
			}
			if (Input.GetMouseButton(0))
			{
				vector = GetMousePos() - _startPosition;
				_startPosition = GetMousePos();
			}
			if (Input.GetMouseButtonUp(0))
			{
				_startPosition = Vector3.zero;
			}
		}
		if (!_isMovingToTarget)
		{
			float num3 = movementKeyMultiplier;
			if (PlayerAction.FastMapPan.Pressing())
			{
				num3 *= movementKeyFastMultiplier;
			}
			vector -= (Vector3)PlayerAction.Move.Vector() * (num3 * Time.unscaledDeltaTime);
		}
		if (FullMenu.IsOpen || MiniMenu.IsOpen || Feedback.IsOpen || Options.IsVisible || GameManager.HasInputSelected() || (InstanceBehavior<UIs>.Instance != null && (InstanceBehavior<UIs>.Instance.draggableWindows.isCurrentlyDragging || InstanceBehavior<UIs>.Instance.timeMachine.canvas.gameObject.activeSelf)))
		{
			return;
		}
		if (!_isMovingToTarget)
		{
			if (PlayerAction.RotateLeft.Pressed())
			{
				RotateCamera(90, !PedestrianCam.invertRotation);
			}
			if (PlayerAction.RotateRight.Pressed())
			{
				RotateCamera(90, PedestrianCam.invertRotation);
			}
		}
		Vector3 zero = Vector3.zero;
		zero -= GetFlatForward() * vector.y;
		zero += GetFlatRight() * vector.x;
		zero *= mouseSpeedMultiplier * distance;
		if (!_isMovingToTarget)
		{
			MoveCenter(zero);
			float num4 = PlayerAction.Zoom.Value() * zoomGamepadMultiplier;
			if (!EventSystem.current.IsPointerOverGameObject())
			{
				num4 += Input.mouseScrollDelta.y;
			}
			MoveCamera(num4);
			RotateCamera(num);
		}
		else
		{
			MoveToTarget();
			MoveCamera(0f);
		}
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

	private Vector3 GetForward()
	{
		return base.transform.position - _vCam.transform.position;
	}

	private Vector3 GetForwardNormalized()
	{
		return GetForward().normalized;
	}

	private Vector3 GetFlatForward()
	{
		Vector3 forward = GetForward();
		forward.y = 0f;
		return forward.normalized;
	}

	private Vector3 GetFlatRight()
	{
		return Vector3.Cross(GetFlatForward(), base.transform.up);
	}

	private Vector3 GetMousePos()
	{
		return GameManager.GetMainCamera().ScreenToViewportPoint(Input.mousePosition);
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
		SaveGameManager.Current.cityMapZoom = distance;
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

	private void RotateCamera(float angle)
	{
		_currentAngle += angle;
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
		float targetAngle = (float)Mathf.RoundToInt((_currentAngle + 360f + (float)(increment ? angleChange : (-angleChange))) / 90f) * 90f - 360f;
		float startAngle = _currentAngle;
		float f = Mathf.DeltaAngle(startAngle, targetAngle);
		float duration = Mathf.Max(0.0001f, Mathf.Abs(f) / rotationSpeedDegreesPerSecond);
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			float target = Mathf.LerpAngle(startAngle, targetAngle, t);
			float angle = Mathf.DeltaAngle(_currentAngle, target);
			RotateCamera(angle);
			yield return null;
		}
		RotateCamera(Mathf.DeltaAngle(_currentAngle, targetAngle));
		_isRotating = false;
	}

	public void MoveCameraToTarget(Vector3 target)
	{
		if (target == Vector3.zero)
		{
			CancelMoveToTarget();
			return;
		}
		moveToPosition = new Vector3(target.x, base.transform.position.y, target.z);
		_moveToObjectInitialDistance = Mathf.Max(Vector3.Distance(base.transform.position, moveToPosition), moveToTargetStopDistance);
		float moveToTargetDuration = GetMoveToTargetDuration(_moveToObjectInitialDistance);
		_moveToTargetSpeedMultiplier = ((moveToTargetMaxDuration > 0f && moveToTargetDuration > moveToTargetMaxDuration) ? (moveToTargetDuration / moveToTargetMaxDuration) : 1f);
		_isMovingToTarget = true;
	}

	private bool HasManualCameraInput()
	{
		if (PlayerAction.Move.Vector() != Vector2.zero)
		{
			return true;
		}
		if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
		{
			return true;
		}
		if (PlayerAction.RotateLeft.Pressed() || PlayerAction.RotateRight.Pressed())
		{
			return true;
		}
		if (Mathf.Abs(PlayerAction.Zoom.Value()) > Mathf.Epsilon || Mathf.Abs(Input.mouseScrollDelta.y) > Mathf.Epsilon)
		{
			return true;
		}
		return false;
	}

	private void CancelMoveToTarget()
	{
		_isMovingToTarget = false;
		moveToPosition = Vector3.zero;
		_moveToObjectInitialDistance = 0f;
		_moveToTargetSpeedMultiplier = 1f;
		_wasClickingUI = false;
	}

	private float GetMoveToTargetDuration(float targetDistance)
	{
		float num = 0f;
		float num2 = GetMoveToTargetCurveSpeed(0f);
		float num3 = 0.0625f;
		for (int i = 1; i <= 16; i++)
		{
			float moveToTargetCurveSpeed = GetMoveToTargetCurveSpeed((float)i * num3);
			num += targetDistance * num3 * 0.5f * (1f / num2 + 1f / moveToTargetCurveSpeed);
			num2 = moveToTargetCurveSpeed;
		}
		return num;
	}

	private float GetMoveToTargetCurveSpeed(float t)
	{
		return Mathf.Max((moveCurve != null) ? moveCurve.Evaluate(t) : 0f, 0.0001f);
	}

	private void MoveToTarget()
	{
		float num = Vector3.SqrMagnitude(base.transform.position - moveToPosition);
		if (num <= moveToTargetStopDistance * moveToTargetStopDistance)
		{
			base.transform.position = moveToPosition;
			CancelMoveToTarget();
			return;
		}
		float value = Mathf.Sqrt(num);
		float t = Mathf.InverseLerp(_moveToObjectInitialDistance, 0f, value);
		float maxDistanceDelta = GetMoveToTargetCurveSpeed(t) * _moveToTargetSpeedMultiplier * Time.unscaledDeltaTime;
		base.transform.position = Vector3.MoveTowards(base.transform.position, moveToPosition, maxDistanceDelta);
	}
}
