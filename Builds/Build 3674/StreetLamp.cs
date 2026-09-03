using System;
using System.Collections;
using Culling;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class StreetLamp : MonoBehaviour, ICullable
{
	private const float MinSwitchOnDelayTime = 0f;

	private const float MaxSwitchOnDelayTime = 0.5f;

	private const float MinSwitchTime = 0.8f;

	private const float MaxSwitchTime = 1.2f;

	private const float MinTimeBetweenFlicker = 0.05f;

	private const float MaxTimeBetweenFlicker = 0.2f;

	[SerializeField]
	private Light[] lights;

	[SerializeField]
	private HDAdditionalLightData[] additionalLightDatas;

	[SerializeField]
	private GameObject volumetricLightObject;

	[SerializeField]
	private MeshRenderer parentMeshRenderer;

	[SerializeField]
	private MeshRenderer[] childMeshRenderers;

	[SerializeField]
	private float maxValue = 314000f;

	[SerializeField]
	private int emissionMaterialIndex = -1;

	private Coroutine _coroutine;

	private static readonly int EmissiveExposureWeight = Shader.PropertyToID("_EmissiveExposureWeight");

	private static MaterialPropertyBlock _propertyBlock;

	private bool _isOn;

	private bool _isVisible;

	private WaitForSeconds _waitForSecondsBetweenFlickeringInstruction;

	private LightShadows[] _initialLightShadows;

	private static MaterialPropertyBlock PropertyBlock => _propertyBlock ?? (_propertyBlock = new MaterialPropertyBlock());

	public void Start()
	{
		if (parentMeshRenderer == null)
		{
			TryGetComponent<MeshRenderer>(out parentMeshRenderer);
		}
		if ((bool)InstanceBehavior<CullingManager>.Instance)
		{
			InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.RegisterCullable(this);
		}
		GlobalEvents.outdoorLightsStatusChanged = (Action<bool>)Delegate.Combine(GlobalEvents.outdoorLightsStatusChanged, new Action<bool>(ChangeLights));
		_initialLightShadows = new LightShadows[lights.Length];
		for (int i = 0; i < lights.Length; i++)
		{
			_initialLightShadows[i] = lights[i].shadows;
		}
		ApplyShadows(PlayerPrefSettings.shadows);
		GlobalEvents.onShadowsSettingChanged = (Action<int>)Delegate.Combine(GlobalEvents.onShadowsSettingChanged, new Action<int>(ApplyShadows));
	}

	private void OnDisable()
	{
		if (!GameManager.isCitySceneBeingUnloaded && _coroutine != null)
		{
			InstantlyTurnOn();
		}
	}

	private void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			GlobalEvents.outdoorLightsStatusChanged = (Action<bool>)Delegate.Remove(GlobalEvents.outdoorLightsStatusChanged, new Action<bool>(ChangeLights));
			GlobalEvents.onShadowsSettingChanged = (Action<int>)Delegate.Remove(GlobalEvents.onShadowsSettingChanged, new Action<int>(ApplyShadows));
			if ((bool)InstanceBehavior<CullingManager>.Instance)
			{
				InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.UnregisterCullable(this);
			}
		}
	}

	public void ChangeLights(bool forced)
	{
		if (CityMap.IsOpen || !_isVisible)
		{
			TurnOff();
		}
		else if (!InstanceBehavior<GameManager>.Instance.timeOfDayController.ShouldOutdoorLightsBeOn())
		{
			TurnOff();
		}
		else if (forced)
		{
			InstantlyTurnOn();
		}
		else if (!_isOn)
		{
			_coroutine = StartCoroutine(ChangeLightSetting());
		}
	}

	private void ApplyShadows(int shadowsSetting)
	{
		for (int i = 0; i < lights.Length; i++)
		{
			lights[i].shadows = ((shadowsSetting == 2) ? _initialLightShadows[i] : LightShadows.None);
		}
	}

	public void OnLod0()
	{
		_isVisible = true;
		ApplyShadows(PlayerPrefSettings.shadows);
		if (!CityMap.IsOpen && !_isOn && InstanceBehavior<GameManager>.Instance.timeOfDayController.ShouldOutdoorLightsBeOn())
		{
			InstantlyTurnOn();
		}
	}

	public void OnLod1()
	{
		Light[] array = lights;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].shadows = LightShadows.None;
		}
		if (!CityMap.IsOpen && !_isOn && InstanceBehavior<GameManager>.Instance.timeOfDayController.ShouldOutdoorLightsBeOn())
		{
			InstantlyTurnOn();
		}
	}

	public void OnLod2()
	{
		_isVisible = false;
		if (_isOn)
		{
			if (_coroutine != null)
			{
				StopCoroutine(_coroutine);
				_coroutine = null;
			}
			TurnOff();
		}
	}

	public BoundingSphere GetCullingBoundingSphere()
	{
		return new BoundingSphere(base.transform.position + Vector3.up * 2f, 4f);
	}

	public void BecameInvisible()
	{
		_isVisible = false;
		TurnOff();
	}

	private void InstantlyTurnOn()
	{
		if (!_isOn)
		{
			_isOn = true;
			if (_coroutine != null)
			{
				StopCoroutine(_coroutine);
				_coroutine = null;
			}
			SetLightVisibility(on: true);
			HDAdditionalLightData[] array = additionalLightDatas;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].intensity = maxValue;
			}
			ToggleEmission(on: true);
		}
	}

	private void TurnOff()
	{
		if (_isOn)
		{
			_isOn = false;
			SetLightVisibility(on: false);
			HDAdditionalLightData[] array = additionalLightDatas;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].intensity = 0f;
			}
			ToggleEmission(on: false);
			if (_coroutine != null)
			{
				StopCoroutine(_coroutine);
				_coroutine = null;
			}
		}
	}

	private IEnumerator ChangeLightSetting()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 0.5f));
		float num = InstanceBehavior<GameManager>.Instance.timeOfDayController.lightFlickerDistance * InstanceBehavior<GameManager>.Instance.timeOfDayController.lightFlickerDistance;
		HDAdditionalLightData[] array;
		if (parentMeshRenderer != null && !parentMeshRenderer.isVisible && Vector3.SqrMagnitude(base.transform.position - InstanceBehavior<GameManager>.Instance.playerController.transform.position) > num)
		{
			SetLightVisibility(on: true);
			array = additionalLightDatas;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].intensity = maxValue;
			}
			_isOn = true;
			_coroutine = null;
			yield break;
		}
		array = additionalLightDatas;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].intensity = 0f;
		}
		ToggleEmission(on: true);
		SetLightVisibility(on: true);
		_isOn = true;
		float time = 0f;
		float idleTime;
		for (float switchTime = UnityEngine.Random.Range(0.8f, 1.2f); time <= switchTime; time += idleTime)
		{
			array = additionalLightDatas;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].intensity = UnityEngine.Random.Range(0f, 1f) * maxValue;
			}
			idleTime = UnityEngine.Random.Range(0.05f, 0.2f);
			yield return new WaitForSeconds(idleTime);
		}
		array = additionalLightDatas;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].intensity = maxValue;
		}
		_coroutine = null;
	}

	private void ToggleEmission(bool on)
	{
		if (emissionMaterialIndex >= 0)
		{
			parentMeshRenderer.GetPropertyBlock(PropertyBlock);
			PropertyBlock.SetFloat(EmissiveExposureWeight, (!on) ? 1 : 0);
			parentMeshRenderer.SetPropertyBlock(PropertyBlock);
			MeshRenderer[] array = childMeshRenderers;
			foreach (MeshRenderer obj in array)
			{
				obj.GetPropertyBlock(PropertyBlock);
				PropertyBlock.SetFloat(EmissiveExposureWeight, (!on) ? 1 : 0);
				obj.SetPropertyBlock(PropertyBlock);
			}
		}
	}

	private void SetLightVisibility(bool on)
	{
		HDAdditionalLightData[] array = additionalLightDatas;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = on;
		}
		Light[] array2 = lights;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = on;
		}
		if (volumetricLightObject != null)
		{
			volumetricLightObject.SetActive(on);
		}
	}
}
