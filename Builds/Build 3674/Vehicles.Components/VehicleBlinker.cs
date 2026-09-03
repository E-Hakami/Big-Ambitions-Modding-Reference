using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace Vehicles.Components;

public class VehicleBlinker : MonoBehaviour
{
	private static readonly int IsBlinkerOn = Shader.PropertyToID("_IsBlinkerOn");

	private static readonly int LeftBlinker = Shader.PropertyToID("_IsLeftBlinkerOn");

	private static readonly int RightBlinker = Shader.PropertyToID("_IsRightBlinkerOn");

	private static MaterialPropertyBlock _propertyBlock;

	[HideInInspector]
	public UnityEvent onEnterCar = new UnityEvent();

	[HideInInspector]
	public UnityEvent onExitCar = new UnityEvent();

	[SerializeField]
	private CarFeatures carFeatures;

	[BoxGroup("Blinkers")]
	[SerializeField]
	private float blinkerInterval = 1f;

	[BoxGroup("Blinkers")]
	[SerializeField]
	private AudioSource blinkerAudioSource;

	[BoxGroup("Blinkers")]
	[SerializeField]
	private AudioClip blinkerOnSound;

	[BoxGroup("Blinkers")]
	[SerializeField]
	private AudioClip blinkerOffSound;

	private Coroutine _blinkerRoutine;

	private WaitForSeconds _blinkerWait;

	private bool _isBlinkerOn;

	private bool _isLeftBlinkerOn;

	private bool _isRightBlinkerOn;

	private static MaterialPropertyBlock PropertyBlock => _propertyBlock ?? (_propertyBlock = new MaterialPropertyBlock());

	private void Start()
	{
		onEnterCar.AddListener(delegate
		{
			if (_blinkerRoutine != null)
			{
				StopCoroutine(_blinkerRoutine);
			}
			_blinkerRoutine = StartCoroutine(BlinkerRoutine());
		});
		onExitCar.AddListener(delegate
		{
			_isBlinkerOn = false;
		});
		_blinkerWait = new WaitForSeconds(blinkerInterval);
	}

	private IEnumerator BlinkerRoutine()
	{
		_isBlinkerOn = true;
		Renderer meshRenderer = carFeatures.bodyMeshes[0];
		while (_isBlinkerOn)
		{
			meshRenderer.GetPropertyBlock(PropertyBlock);
			int num = _propertyBlock.GetInt(IsBlinkerOn);
			_propertyBlock.SetInt(IsBlinkerOn, (num != 1) ? 1 : 0);
			meshRenderer.SetPropertyBlock(PropertyBlock);
			if (blinkerAudioSource != null && (_isLeftBlinkerOn || _isRightBlinkerOn))
			{
				blinkerAudioSource.clip = ((num == 1) ? blinkerOnSound : blinkerOffSound);
				blinkerAudioSource.Play();
			}
			yield return _blinkerWait;
		}
		ResetBlinkers();
		_blinkerRoutine = null;
	}

	public void ToggleLeftBlinker()
	{
		_isLeftBlinkerOn = !_isLeftBlinkerOn;
		Renderer[] bodyMeshes = carFeatures.bodyMeshes;
		for (int i = 0; i < bodyMeshes.Length; i++)
		{
			SetBlinker(bodyMeshes[i], LeftBlinker);
		}
		if (_isLeftBlinkerOn && _isRightBlinkerOn)
		{
			ToggleRightBlinker();
		}
	}

	public void ToggleRightBlinker()
	{
		_isRightBlinkerOn = !_isRightBlinkerOn;
		Renderer[] bodyMeshes = carFeatures.bodyMeshes;
		for (int i = 0; i < bodyMeshes.Length; i++)
		{
			SetBlinker(bodyMeshes[i], RightBlinker);
		}
		if (_isLeftBlinkerOn && _isRightBlinkerOn)
		{
			ToggleLeftBlinker();
		}
	}

	private static void SetBlinker(Renderer meshRenderer, int nameId)
	{
		meshRenderer.GetPropertyBlock(PropertyBlock);
		int num = _propertyBlock.GetInt(nameId);
		_propertyBlock.SetInt(nameId, (num != 1) ? 1 : 0);
		meshRenderer.SetPropertyBlock(PropertyBlock);
	}

	private void ResetBlinkers()
	{
		_isLeftBlinkerOn = false;
		_isRightBlinkerOn = false;
		Renderer[] bodyMeshes = carFeatures.bodyMeshes;
		foreach (Renderer obj in bodyMeshes)
		{
			obj.GetPropertyBlock(PropertyBlock);
			_propertyBlock.SetInt(LeftBlinker, 0);
			_propertyBlock.SetInt(RightBlinker, 0);
			_propertyBlock.SetInt(IsBlinkerOn, 0);
			obj.SetPropertyBlock(PropertyBlock);
		}
	}
}
