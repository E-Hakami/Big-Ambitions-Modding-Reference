using System.Collections;
using System.Collections.Generic;
using BigAmbitions.SoundSystem;
using DG.Tweening;
using Helpers;
using JimmysUnityUtilities;
using UI;
using UnityEngine;

namespace Vehicles.Components;

[RequireComponent(typeof(BoxCollider))]
public class GarageDoor : MonoBehaviour
{
	private static readonly int CloseOpenDoorId = Shader.PropertyToID("_CloseOpenDoor");

	[SerializeField]
	protected MeshRenderer garageDoorRenderer;

	[SerializeField]
	private int materialIndex;

	protected readonly List<VehicleController> vehiclesInside = new List<VehicleController>();

	private float _currentVisualGarageDoorState;

	private Tweener _visualGarageDoorStateTween;

	private float _targetVisualGarageDoorState;

	private Coroutine _soundCoroutine;

	protected virtual void OnVehicleEnter()
	{
	}

	protected virtual void OnVehicleExit()
	{
	}

	protected virtual void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.layer == LayerHelper.VehiclesLayerIndex && !other.isTrigger && !other.CompareTag("VehicleAntiRocketCollider") && other.TryGetComponentInParent<CarController>(out var component) && !vehiclesInside.Contains(component))
		{
			vehiclesInside.Add(component);
			OnVehicleEnter();
		}
	}

	protected virtual void OnTriggerExit(Collider other)
	{
		if (other.gameObject.layer == LayerHelper.VehiclesLayerIndex && !other.isTrigger && !other.CompareTag("VehicleAntiRocketCollider") && other.TryGetComponentInParent<CarController>(out var component))
		{
			vehiclesInside.Remove(component);
			OnVehicleExit();
		}
	}

	protected void VisuallyChangeGarageDoorState(float finalState, bool instant = false)
	{
		if (!(garageDoorRenderer == null) && !Mathf.Approximately(_targetVisualGarageDoorState, finalState))
		{
			_targetVisualGarageDoorState = finalState;
			VisuallyOpenCloseGarageDoor(finalState, instant);
			if (_soundCoroutine != null)
			{
				StopCoroutine(_soundCoroutine);
				_soundCoroutine = null;
			}
			if (!instant && !InstanceBehavior<UIs>.Instance.timeMachine.isRunning)
			{
				_soundCoroutine = StartCoroutine(DeferPlaySound());
			}
		}
	}

	private void VisuallyOpenCloseGarageDoor(float finalState, bool instant = false)
	{
		_visualGarageDoorStateTween?.Kill();
		Material doorMaterial = garageDoorRenderer.materials[materialIndex];
		if (instant)
		{
			_currentVisualGarageDoorState = finalState;
			doorMaterial.SetFloat(CloseOpenDoorId, finalState);
			return;
		}
		float num = Mathf.Abs(_currentVisualGarageDoorState - finalState) * 2f;
		Ease ease = ((num < 1f) ? Ease.Linear : Ease.InOutQuart);
		_visualGarageDoorStateTween = DOVirtual.Float(_currentVisualGarageDoorState, finalState, num, delegate(float value)
		{
			_currentVisualGarageDoorState = value;
			doorMaterial.SetFloat(CloseOpenDoorId, value);
		}).SetEase(ease);
	}

	private IEnumerator DeferPlaySound()
	{
		yield return new WaitForFixedUpdate();
		if (Mathf.Abs(_targetVisualGarageDoorState - _currentVisualGarageDoorState) > 0.1f)
		{
			InstanceBehavior<SfxManager>.Instance.PlayAudio((_targetVisualGarageDoorState <= _currentVisualGarageDoorState) ? SoundType.GarageDoorClose : SoundType.GarageDoorOpen, base.transform.position);
		}
		_soundCoroutine = null;
	}
}
