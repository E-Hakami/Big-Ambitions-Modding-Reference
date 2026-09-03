using System;
using Buildings.Outdoors;
using Culling;
using Enums;
using UnityEngine;

public class BuildingSignController : MonoBehaviour, ICullable
{
	private const string SignBulbMaterialName = "SignBulbMaterial (Instance)";

	private const string SignNeonMaterialName = "SignNeonMaterial (Instance)";

	private static readonly int BaseColorMapId = Shader.PropertyToID("_BaseColorMap");

	private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");

	private static readonly int EmissiveExposureWeightId = Shader.PropertyToID("_EmissiveExposureWeight");

	public MeshRenderer signMeshRenderer;

	public MeshRenderer lightMeshRenderer;

	[SerializeField]
	private Light pointLight;

	[SerializeField]
	private MeshFilter signMeshFilter;

	[SerializeField]
	private MeshFilter lightMeshFilter;

	[SerializeField]
	private WideSignTypes wideSignTypes;

	private bool _isVisible;

	private bool _hasBulbs;

	private Material[] _lightMaterials;

	private void Start()
	{
		GlobalEvents.outdoorLightsStatusChanged = (Action<bool>)Delegate.Combine(GlobalEvents.outdoorLightsStatusChanged, new Action<bool>(ToggleLight));
		InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.RegisterCullable(this);
		GlobalEvents.RegisterOnGameLoadedCallback(delegate
		{
			ToggleLight();
		});
	}

	private void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			GlobalEvents.outdoorLightsStatusChanged = (Action<bool>)Delegate.Remove(GlobalEvents.outdoorLightsStatusChanged, new Action<bool>(ToggleLight));
			InstanceBehavior<CullingManager>.Instance?.generalCullingGroupController.UnregisterCullable(this);
		}
	}

	public void OnLod0()
	{
		_isVisible = true;
		ToggleLight();
	}

	public void OnLod1()
	{
		_isVisible = false;
		pointLight.gameObject.SetActive(value: false);
	}

	public void OnLod2()
	{
		OnLod1();
	}

	public BoundingSphere GetCullingBoundingSphere()
	{
		return new BoundingSphere(base.transform.position + Vector3.up * 2f, 4f);
	}

	private bool ShouldLightBeOn()
	{
		if (!_isVisible)
		{
			return false;
		}
		if (!CityMap.IsOpen)
		{
			return InstanceBehavior<GameManager>.Instance.timeOfDayController.ShouldOutdoorLightsBeOn();
		}
		return false;
	}

	public void ConfigureSign(BuildingRegistration registration)
	{
		SignType signType = registration.signAppearanceSettings.signType;
		WideSignType signType2 = GetSignType(signType);
		if (signType2 == null)
		{
			Debug.LogError("Wide Sign doesn't have sign '" + signType.ToStringFast() + "' implemented");
			return;
		}
		Texture2D texture = (registration.AvailableForRent ? LogoHelper.GetBusinessLogoTexture("AvailableForRent", LogoSize.WideSign) : LogoHelper.GetBusinessLogoTexture(registration.BusinessName, LogoSize.WideSign, registration.RentedByPlayer));
		if (texture != null)
		{
			SetSignTexture(texture);
		}
		else if (!registration.AvailableForRent && registration.RentedByPlayer)
		{
			BusinessLogoGenerator.Create(registration.BusinessName, registration.logoSettings, LogoHelper.GetPlayerBusinessLogoPath(registration.BusinessName), registration.RentedByPlayer, delegate
			{
				texture = LogoHelper.GetBusinessLogoTexture(registration.BusinessName, LogoSize.WideSign, playerBusiness: true);
				if (texture != null)
				{
					SetSignTexture(texture);
				}
			});
		}
		signMeshFilter.mesh = signType2.signMesh;
		if (signType2.lightMesh == null)
		{
			_hasBulbs = false;
			_lightMaterials = null;
			lightMeshRenderer.gameObject.SetActive(value: false);
		}
		else
		{
			lightMeshFilter.mesh = signType2.lightMesh;
			lightMeshRenderer.materials = signType2.lightMaterials;
			_lightMaterials = lightMeshRenderer.materials;
			ApplyLightColors(registration.signAppearanceSettings.signLight, registration.signAppearanceSettings.lamp);
			lightMeshRenderer.gameObject.SetActive(value: true);
		}
		pointLight.color = registration.signAppearanceSettings.lamp;
		ToggleLight();
	}

	private WideSignType GetSignType(SignType signType)
	{
		for (int i = 0; i < wideSignTypes.signTypes.Length; i++)
		{
			WideSignType wideSignType = wideSignTypes.signTypes[i];
			if (wideSignType.type == signType)
			{
				return wideSignType;
			}
		}
		return null;
	}

	private void ApplyLightColors(Color signLightColor, Color bulbColor)
	{
		_hasBulbs = false;
		for (int i = 0; i < _lightMaterials.Length; i++)
		{
			Material material = _lightMaterials[i];
			Color color;
			if (material.name == "SignBulbMaterial (Instance)")
			{
				color = bulbColor;
				_hasBulbs = true;
			}
			else
			{
				if (!(material.name == "SignNeonMaterial (Instance)"))
				{
					continue;
				}
				color = signLightColor;
			}
			float a = material.GetColor(EmissiveColorId).a;
			Color value = color * a;
			value.a = a;
			material.SetColor(EmissiveColorId, value);
		}
	}

	private static bool IsSignLightMaterial(Material material)
	{
		string text = material.name;
		return text == "SignBulbMaterial (Instance)" || text == "SignNeonMaterial (Instance)";
	}

	private void SetSignTexture(Texture2D texture)
	{
		signMeshRenderer.materials[1].SetTexture(BaseColorMapId, texture);
	}

	private void ToggleLight(bool _ = false)
	{
		bool flag = ShouldLightBeOn();
		pointLight.gameObject.SetActive(_hasBulbs & flag);
		if (!lightMeshRenderer.gameObject.activeSelf || _lightMaterials == null)
		{
			return;
		}
		for (int i = 0; i < _lightMaterials.Length; i++)
		{
			Material material = _lightMaterials[i];
			if (IsSignLightMaterial(material))
			{
				material.SetFloat(EmissiveExposureWeightId, (!flag) ? 1 : 0);
			}
		}
	}
}
