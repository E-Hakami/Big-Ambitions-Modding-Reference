using System;
using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class IndoorLightController : ItemController
{
	[Header("IndoorLightController")]
	[SerializeField]
	protected bool ignoreTimeOfDay;

	[SerializeField]
	protected int lightMaterialIndex;

	[SerializeField]
	protected MeshRenderer[] volumetricLightRenderers;

	[HideInInspector]
	public Color originalLightColor = Color.white;

	[NonSerialized]
	public bool isVisibleByCamera = true;

	protected MeshRenderer _meshRenderer;

	protected Material lightMaterial;

	protected Light[] lights;

	private LightShadows[] _initialLightShadows;

	protected readonly List<HDAdditionalLightData> lightsData = new List<HDAdditionalLightData>();

	protected static readonly int EmissiveExposureWeight = Shader.PropertyToID("_EmissiveExposureWeight");

	private static readonly int VolumetricColorId = Shader.PropertyToID("_Color");

	public bool IsLightOn { get; protected set; }

	public override void Awake()
	{
		base.Awake();
		if (!playerItemPurchaserSettings.enabled)
		{
			GetComponentReferences();
		}
	}

	private void GetComponentReferences()
	{
		_meshRenderer = GetComponentInChildren<MeshRenderer>();
		lights = GetComponentsInChildren<Light>();
		Light[] array = lights;
		foreach (Light light in array)
		{
			lightsData.Add(light.GetComponent<HDAdditionalLightData>());
		}
		if (lightMaterialIndex >= 0)
		{
			lightMaterial = _meshRenderer.materials[lightMaterialIndex];
		}
	}

	public override void Start()
	{
		base.Start();
		if (!playerItemPurchaserSettings.enabled)
		{
			if (lights.Length != 0)
			{
				originalLightColor = lights[0].color;
			}
			GlobalEvents.indoorLightsStatusChanged = (Action<bool>)Delegate.Combine(GlobalEvents.indoorLightsStatusChanged, new Action<bool>(OnLightsStatusChanged));
			OnLightsStatusChanged(RainHelper.AreRainDropsFalling || TimeOfDayController.isLightOnPeriod);
			_initialLightShadows = new LightShadows[lights.Length];
			for (int i = 0; i < lights.Length; i++)
			{
				_initialLightShadows[i] = lights[i].shadows;
			}
			ApplyShadows(PlayerPrefSettings.shadows);
			GlobalEvents.onShadowsSettingChanged = (Action<int>)Delegate.Combine(GlobalEvents.onShadowsSettingChanged, new Action<int>(ApplyShadows));
			IndoorLightVisibilityManager.Register(this);
		}
	}

	public override void Hide()
	{
		base.Hide();
		ToggleLight(lightsOn: false);
	}

	public override void Show()
	{
		base.Show();
		if (IsLightOn)
		{
			ToggleLight(lightsOn: true);
		}
	}

	protected virtual void OnLightsStatusChanged(bool lightsOn)
	{
		if (ignoreTimeOfDay)
		{
			if (!IsLightOn)
			{
				IsLightOn = true;
			}
			return;
		}
		IsLightOn = lightsOn;
		if (!lightsOn || (visible && isVisibleByCamera))
		{
			ToggleLight(lightsOn);
		}
	}

	public void ToggleLight(bool lightsOn)
	{
		Light[] array = lights;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = lightsOn;
		}
		foreach (HDAdditionalLightData lightsDatum in lightsData)
		{
			lightsDatum.enabled = lightsOn;
		}
		MeshRenderer[] array2 = volumetricLightRenderers;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].gameObject.SetActive(lightsOn);
		}
		if (!(lightMaterial == null))
		{
			lightMaterial.SetFloat(EmissiveExposureWeight, lightsOn ? 0f : 1f);
		}
	}

	public float GetLightsRange()
	{
		if (lights.Length != 1)
		{
			return GetMinEnclosingRadiusForAllLightRanges();
		}
		return lights[0].range;
	}

	public Vector3 GetLightsCenter()
	{
		if (lights.Length != 1)
		{
			return base.transform.position;
		}
		return lights[0].transform.position;
	}

	private float GetMinEnclosingRadiusForAllLightRanges()
	{
		Vector3 position = base.transform.position;
		float num = 0f;
		for (int i = 0; i < lights.Length; i++)
		{
			Light light = lights[i];
			float num2 = Vector3.Distance(position, light.transform.position);
			num = Mathf.Max(num, num2 + light.range);
		}
		return num;
	}

	public override void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded && !playerItemPurchaserSettings.enabled)
		{
			base.OnDestroy();
			GlobalEvents.indoorLightsStatusChanged = (Action<bool>)Delegate.Remove(GlobalEvents.indoorLightsStatusChanged, new Action<bool>(OnLightsStatusChanged));
			GlobalEvents.onShadowsSettingChanged = (Action<int>)Delegate.Remove(GlobalEvents.onShadowsSettingChanged, new Action<int>(ApplyShadows));
			IndoorLightVisibilityManager.Unregister(this);
		}
	}

	private void ApplyShadows(int shadowsSetting)
	{
		for (int i = 0; i < lights.Length; i++)
		{
			lights[i].shadows = ((shadowsSetting == 2) ? _initialLightShadows[i] : LightShadows.None);
		}
	}

	public void SetColor(Color color)
	{
		Light[] array = lights;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].color = color;
		}
		foreach (HDAdditionalLightData lightsDatum in lightsData)
		{
			lightsDatum.color = color;
		}
		MeshRenderer[] array2 = volumetricLightRenderers;
		if (array2 != null && array2.Length > 0)
		{
			array2 = volumetricLightRenderers;
			foreach (MeshRenderer obj in array2)
			{
				obj.GetPropertyBlock(EntityController.PropertyBlockGetter);
				EntityController.PropertyBlockGetter.SetColor(VolumetricColorId, color);
				obj.SetPropertyBlock(EntityController.PropertyBlockGetter);
			}
		}
	}
}
