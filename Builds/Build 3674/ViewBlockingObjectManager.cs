using System;
using System.Collections.Generic;
using UI;
using UI.Load;
using UI.PurchaseVehicle;
using UnityEngine;

public class ViewBlockingObjectManager : MonoBehaviour
{
	public const float DistanceOffset = 4f;

	private static float CurrentDistanceOffset = 4f;

	private static readonly List<ViewBlockingEntity> ActiveEntities = new List<ViewBlockingEntity>();

	[SerializeField]
	private int frameDelayBetweenHides = 2;

	[SerializeField]
	private LayerMask viewBlockingEntitiesLayerMask;

	[SerializeField]
	private float extensionRaycastHideDelay = 0.1f;

	public float pedestrianSphereRadius = 2.5f;

	public float pedestrianRayThickness = 0.5f;

	public float vehicleSphereRadius = 3.75f;

	public float vehicleRayThickness = 2f;

	private readonly Collider[] _colliderHits = new Collider[20];

	private readonly Dictionary<int, ViewBlockingEntity> _extensionHitCache = new Dictionary<int, ViewBlockingEntity>();

	private readonly RaycastHit[] _raycastHits = new RaycastHit[20];

	private int _cameraBlockFrame;

	private float _currentRayThickness;

	private float _currentSphereRadius;

	private bool _extensionRaycastHasHit;

	private float _extensionRaycastHitStartTime;

	private float _extensionRaycastMissStartTime = -1f;

	private int _framesSinceLastHide;

	private Transform _playerTransform;

	private static bool HideEntitiesIsDisabled
	{
		get
		{
			if (!CityMap.IsOpen && !SubwaySystem.IsRiding && ScreenshotController.uiIsVisible && !ScreenshotController.isInFreeLookMode)
			{
				return PurchaseVehicleUI.IsPanelOpen;
			}
			return true;
		}
	}

	private void Start()
	{
		ResetStaticData();
		_currentRayThickness = pedestrianRayThickness;
		_currentSphereRadius = pedestrianSphereRadius;
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onEnterVehicle, (Action<VehicleController>)delegate
		{
			_currentRayThickness = vehicleRayThickness;
			_currentSphereRadius = vehicleSphereRadius;
		});
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, (Action<VehicleController>)delegate
		{
			_currentRayThickness = pedestrianRayThickness;
			_currentSphereRadius = pedestrianSphereRadius;
		});
	}

	private void LateUpdate()
	{
		if (LoadScene.isLoading || ParkingLaneGenerator.pendingDestroyBlockingParkedVehicles || BuildingManager.IsInsideBuilding)
		{
			return;
		}
		_framesSinceLastHide++;
		if (_framesSinceLastHide >= frameDelayBetweenHides)
		{
			_framesSinceLastHide = 0;
			if (!HideEntitiesIsDisabled)
			{
				HideEntities();
			}
			else
			{
				ResetExtensionRaycastTimer();
			}
		}
		if (!base.enabled)
		{
			return;
		}
		for (int num = ActiveEntities.Count - 1; num >= 0; num--)
		{
			ViewBlockingEntity viewBlockingEntity = ActiveEntities[num];
			if (!viewBlockingEntity.DoUpdate())
			{
				viewBlockingEntity.IsActiveInViewBlockingObjectManager = false;
				ActiveEntities.RemoveAt(num);
			}
		}
	}

	private void OnDestroy()
	{
		ClearActiveEntities();
		_extensionHitCache.Clear();
	}

	private void HideEntities()
	{
		if (UiFader.isFading)
		{
			ResetExtensionRaycastTimer();
			return;
		}
		if (_playerTransform == null)
		{
			if (InstanceBehavior<GameManager>.Instance == null || InstanceBehavior<GameManager>.Instance.playerController == null)
			{
				return;
			}
			_playerTransform = InstanceBehavior<GameManager>.Instance.playerController.transform;
		}
		_cameraBlockFrame++;
		Vector3 position = base.transform.position;
		Vector3 vector = _playerTransform.position + Vector3.up * 0.25f;
		Vector3 vector2 = vector - position;
		float magnitude = vector2.magnitude;
		Vector3 vector3 = ((magnitude > 0f) ? (vector2 / magnitude) : Vector3.zero);
		float num = magnitude - _currentRayThickness * CurrentDistanceOffset;
		int num2 = 0;
		if (num > 0f)
		{
			num2 = Physics.SphereCastNonAlloc(position, _currentRayThickness, vector3, _raycastHits, num, viewBlockingEntitiesLayerMask);
		}
		for (int i = 0; i < num2; i++)
		{
			if (TryGetViewBlockingEntity(_raycastHits[i].transform, out var viewBlockingEntity))
			{
				CameraBlock(viewBlockingEntity);
			}
		}
		UpdateViewBlockingEntityExtensionRaycast(vector, -vector3, magnitude);
		num2 = Physics.OverlapSphereNonAlloc(position, _currentSphereRadius, _colliderHits, viewBlockingEntitiesLayerMask);
		for (int j = 0; j < num2; j++)
		{
			if (TryGetViewBlockingEntity(_colliderHits[j].transform, out var viewBlockingEntity2))
			{
				CameraBlock(viewBlockingEntity2);
			}
		}
	}

	private void CameraBlock(ViewBlockingEntity viewBlockingEntity)
	{
		if (viewBlockingEntity.LastViewBlockingObjectManagerFrame != _cameraBlockFrame)
		{
			viewBlockingEntity.LastViewBlockingObjectManagerFrame = _cameraBlockFrame;
			viewBlockingEntity.CameraBlock();
		}
	}

	private void UpdateViewBlockingEntityExtensionRaycast(Vector3 origin, Vector3 direction, float distance)
	{
		if (distance <= 0f)
		{
			ResetExtensionRaycastTimer();
			return;
		}
		float unscaledTime = Time.unscaledTime;
		int num = Physics.RaycastNonAlloc(origin, direction, _raycastHits, distance, viewBlockingEntitiesLayerMask);
		bool flag = false;
		for (int i = 0; i < num; i++)
		{
			if (TryGetCachedViewBlockingEntityExtension(_raycastHits[i].transform, out var viewBlockingEntity))
			{
				flag = true;
				if (RegisterExtensionRaycastHit(unscaledTime) && unscaledTime - _extensionRaycastHitStartTime >= extensionRaycastHideDelay)
				{
					CameraBlock(viewBlockingEntity);
				}
			}
		}
		if (!flag)
		{
			RegisterExtensionRaycastMiss(unscaledTime);
		}
	}

	private bool RegisterExtensionRaycastHit(float time)
	{
		_extensionRaycastMissStartTime = -1f;
		if (_extensionRaycastHasHit)
		{
			return true;
		}
		_extensionRaycastHasHit = true;
		_extensionRaycastHitStartTime = time;
		return false;
	}

	private void RegisterExtensionRaycastMiss(float time)
	{
		if (_extensionRaycastHasHit)
		{
			if (_extensionRaycastMissStartTime < 0f)
			{
				_extensionRaycastMissStartTime = time;
			}
			else if (time - _extensionRaycastMissStartTime >= extensionRaycastHideDelay)
			{
				ResetExtensionRaycastTimer();
			}
		}
	}

	private void ResetExtensionRaycastTimer()
	{
		_extensionRaycastHasHit = false;
		_extensionRaycastHitStartTime = 0f;
		_extensionRaycastMissStartTime = -1f;
	}

	private static bool TryGetViewBlockingEntity(Transform hitTransform, out ViewBlockingEntity viewBlockingEntity)
	{
		if (hitTransform.TryGetComponent<ViewBlockingEntity>(out viewBlockingEntity))
		{
			return true;
		}
		if (hitTransform.TryGetComponent<ViewBlockingEntityExtension>(out var component) && component.TryGetViewBlockingEntity(out viewBlockingEntity))
		{
			return true;
		}
		Transform parent = hitTransform.parent;
		if (parent == null)
		{
			viewBlockingEntity = null;
			return false;
		}
		if (parent.TryGetComponent<ViewBlockingEntity>(out viewBlockingEntity))
		{
			return true;
		}
		if (parent.TryGetComponent<ViewBlockingEntityExtension>(out component) && component.TryGetViewBlockingEntity(out viewBlockingEntity))
		{
			return true;
		}
		viewBlockingEntity = null;
		return false;
	}

	private bool TryGetCachedViewBlockingEntityExtension(Transform hitTransform, out ViewBlockingEntity viewBlockingEntity)
	{
		int instanceID = hitTransform.GetInstanceID();
		if (_extensionHitCache.TryGetValue(instanceID, out viewBlockingEntity))
		{
			return viewBlockingEntity != null;
		}
		if (TryGetViewBlockingEntityExtension(hitTransform, out viewBlockingEntity))
		{
			_extensionHitCache[instanceID] = viewBlockingEntity;
			return true;
		}
		_extensionHitCache[instanceID] = null;
		return false;
	}

	private static bool TryGetViewBlockingEntityExtension(Transform hitTransform, out ViewBlockingEntity viewBlockingEntity)
	{
		if (hitTransform.TryGetComponent<ViewBlockingEntityExtension>(out var component) && component.TryGetViewBlockingEntity(out viewBlockingEntity))
		{
			return true;
		}
		Transform parent = hitTransform.parent;
		if (parent != null && parent.TryGetComponent<ViewBlockingEntityExtension>(out component) && component.TryGetViewBlockingEntity(out viewBlockingEntity))
		{
			return true;
		}
		viewBlockingEntity = null;
		return false;
	}

	public static void UnregisterEntity(ViewBlockingEntity viewBlockingEntity)
	{
		viewBlockingEntity.IsActiveInViewBlockingObjectManager = false;
		ActiveEntities.Remove(viewBlockingEntity);
	}

	public static void MarkEntityActive(ViewBlockingEntity viewBlockingEntity)
	{
		if (!viewBlockingEntity.IsActiveInViewBlockingObjectManager)
		{
			viewBlockingEntity.IsActiveInViewBlockingObjectManager = true;
			ActiveEntities.Add(viewBlockingEntity);
		}
	}

	public static void SetDistanceOffset(float distanceOffset)
	{
		CurrentDistanceOffset = distanceOffset;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		ClearActiveEntities();
		CurrentDistanceOffset = 4f;
	}

	private static void ClearActiveEntities()
	{
		for (int i = 0; i < ActiveEntities.Count; i++)
		{
			ViewBlockingEntity viewBlockingEntity = ActiveEntities[i];
			if (viewBlockingEntity != null)
			{
				viewBlockingEntity.IsActiveInViewBlockingObjectManager = false;
			}
		}
		ActiveEntities.Clear();
	}
}
