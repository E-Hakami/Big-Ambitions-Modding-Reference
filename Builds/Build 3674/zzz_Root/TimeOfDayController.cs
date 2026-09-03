using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.BlueprintCreator;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Tags;
using Buildings;
using Buildings.Indoors.InteriorDesign;
using Extensions;
using GleyTrafficSystem;
using Helpers;
using HorizonBasedAmbientOcclusion.HighDefinition;
using NaughtyAttributes;
using Parking.UndergroundParking;
using Scenes.MainMenu;
using UI;
using UI.InteriorDesigner;
using UI.Load;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class TimeOfDayController : MonoBehaviour
{
	private static readonly int TimeOfDayCachedName = Shader.PropertyToID("TimeOfDay");

	private static readonly int AmountOfLightsOn = Shader.PropertyToID("_AmountOfLightsOn");

	private static readonly int ExtraFresnelColor = Shader.PropertyToID("_ExtraFresnelColor");

	private static readonly int ExtraFresnelPower = Shader.PropertyToID("_ExtraFresnelPower");

	[SerializeField]
	[Expandable]
	[InfoBox("All changes are saved in play mode!", EInfoBoxType.Warning)]
	private EnvironmentSettings environmentSettings;

	[SerializeField]
	[Expandable]
	[InfoBox("All changes are saved in play mode!", EInfoBoxType.Warning)]
	private EnvironmentSettings environmentSettingsRain;

	[Header("Lighting")]
	public Light sunLight;

	public Light moonLight;

	public float lightFlickerDistance;

	[SerializeField]
	private float degreesDifferenceToUpdateDlRotation = 0.1f;

	[HideInInspector]
	public bool usingSliderDebugTool;

	[HideInInspector]
	public bool enabledShadowDimmer = true;

	[HideInInspector]
	public bool enabledShadowTint = true;

	private bool _isIndoors;

	private HDRISky[] _hdriSkies;

	private Fog[] _fogs;

	private ColorAdjustments _indoorColorAdjustments;

	private Exposure[] _exposures;

	private HBAO[] _hbaos;

	private GradientSky _indoorGradientSky;

	private GradientSky _dayVolumeGradientSky;

	private HDAdditionalLightData _sunLightAdditionalData;

	private HDAdditionalLightData _moonLightAdditionalData;

	private List<HDAdditionalReflectionData> _reflectionData = new List<HDAdditionalReflectionData>();

	private float _lastTimeHours = -1f;

	private bool _previousRaindrops;

	private LightShadows _sunInitialShadows;

	private LightShadows _moonInitialShadows;

	private LightShadows _indoorsInitialShadows;

	private Color _lastSewerSteamColor;

	private string _lastTrafficDensityNeighborhood;

	private int _lastTrafficDensity = -1;

	[Foldout("Debugging")]
	[OnValueChanged("OnValidate")]
	public float forcedHour;

	[SerializeField]
	private VolumeProfile dayVolume;

	[SerializeField]
	private VolumeProfile[] postProcessingVolumeProfiles;

	public static bool isLightOnPeriod;

	public static bool isOutdoorLightOnPeriod;

	public int GetTargetPedestrians()
	{
		if (!RainHelper.isRaining)
		{
			return Mathf.RoundToInt(environmentSettings.pedestrians.Evaluate(GetCurrentHour()));
		}
		if (RainHelper.transitionProgress >= 1f)
		{
			return Mathf.RoundToInt(environmentSettingsRain.pedestrians.Evaluate(GetCurrentHour()));
		}
		return Mathf.RoundToInt(Mathf.Lerp(environmentSettings.pedestrians.Evaluate(GetCurrentHour()), environmentSettingsRain.pedestrians.Evaluate(GetCurrentHour()), RainHelper.transitionProgress));
	}

	public void SetHbaoQuality(Options.Quality quality)
	{
		switch (quality)
		{
		case Options.Quality.Low:
		{
			HBAO[] hbaos = _hbaos;
			for (int i = 0; i < hbaos.Length; i++)
			{
				hbaos[i].active = false;
			}
			if (InstanceBehavior<BuildingManager>.Instance.indoorsHbao != null)
			{
				InstanceBehavior<BuildingManager>.Instance.indoorsHbao.active = false;
			}
			break;
		}
		case Options.Quality.Medium:
		{
			HBAO[] hbaos = _hbaos;
			foreach (HBAO obj2 in hbaos)
			{
				obj2.active = true;
				obj2.SetQuality(HBAO.Quality.Low);
				obj2.SetAoRadius(0.5f);
				obj2.intensity.overrideState = true;
			}
			if (InstanceBehavior<BuildingManager>.Instance.indoorsHbao != null)
			{
				InstanceBehavior<BuildingManager>.Instance.indoorsHbao.active = true;
				InstanceBehavior<BuildingManager>.Instance.indoorsHbao.SetQuality(HBAO.Quality.Low);
				InstanceBehavior<BuildingManager>.Instance.indoorsHbao.SetAoRadius(0.5f);
				InstanceBehavior<BuildingManager>.Instance.indoorsHbao.intensity.overrideState = true;
			}
			break;
		}
		case Options.Quality.High:
		{
			HBAO[] hbaos = _hbaos;
			foreach (HBAO obj in hbaos)
			{
				obj.active = true;
				obj.SetQuality(HBAO.Quality.High);
				obj.SetAoRadius(1f);
				obj.intensity.overrideState = true;
			}
			if (InstanceBehavior<BuildingManager>.Instance.indoorsHbao != null)
			{
				InstanceBehavior<BuildingManager>.Instance.indoorsHbao.active = true;
				InstanceBehavior<BuildingManager>.Instance.indoorsHbao.SetQuality(HBAO.Quality.High);
				InstanceBehavior<BuildingManager>.Instance.indoorsHbao.SetAoRadius(1f);
				InstanceBehavior<BuildingManager>.Instance.indoorsHbao.intensity.overrideState = true;
			}
			break;
		}
		}
	}

	public bool ShouldIndoorLightsBeOn(float hour = -1f, bool skipMapCheck = false)
	{
		if (environmentSettings == null)
		{
			return false;
		}
		if (!skipMapCheck && CityMap.IsOpen)
		{
			return false;
		}
		if (RainHelper.AreRainDropsFalling)
		{
			return true;
		}
		if (hour < 0f)
		{
			hour = GetCurrentHour();
		}
		if (!(hour >= (float)environmentSettings.hourToTurnOnMoon))
		{
			return hour < (float)environmentSettings.hourToTurnOnSun;
		}
		return true;
	}

	public bool ShouldOutdoorLightsBeOn(float hour = -1f, bool skipMapCheck = false)
	{
		if (environmentSettings == null)
		{
			return false;
		}
		if (!skipMapCheck && CityMap.IsOpen)
		{
			return false;
		}
		if (hour < 0f)
		{
			hour = GetCurrentHour();
		}
		if (!(hour >= (float)environmentSettings.outdoorLightsOnStartHour))
		{
			return hour < (float)environmentSettings.outdoorLightsOnEndHour;
		}
		return true;
	}

	private void Awake()
	{
		if (!Application.isEditor)
		{
			forcedHour = -1f;
		}
	}

	private void OnEnable()
	{
		if (!(environmentSettings == null))
		{
			_hdriSkies = new HDRISky[postProcessingVolumeProfiles.Length];
			_fogs = new Fog[postProcessingVolumeProfiles.Length];
			_exposures = new Exposure[postProcessingVolumeProfiles.Length];
			_hbaos = new HBAO[postProcessingVolumeProfiles.Length];
			for (int i = 0; i < postProcessingVolumeProfiles.Length; i++)
			{
				postProcessingVolumeProfiles[i].TryGet<HDRISky>(out _hdriSkies[i]);
				postProcessingVolumeProfiles[i].TryGet<Fog>(out _fogs[i]);
				postProcessingVolumeProfiles[i].TryGet<Exposure>(out _exposures[i]);
				postProcessingVolumeProfiles[i].TryGet<HBAO>(out _hbaos[i]);
			}
			bool penumbraTint = (_isIndoors ? environmentSettings.indoorsSunLightPenumbraTintEnabled : environmentSettings.outdoorsSunLightPenumbraTintEnabled);
			float shadowDimmer = (_isIndoors ? environmentSettings.indoorsSunLightDimmer : environmentSettings.outdoorsSunLightDimmer);
			_sunLightAdditionalData = sunLight.GetComponent<HDAdditionalLightData>();
			_sunLightAdditionalData.penumbraTint = penumbraTint;
			_sunLightAdditionalData.shadowDimmer = shadowDimmer;
			_moonLightAdditionalData = moonLight.GetComponent<HDAdditionalLightData>();
			_moonLightAdditionalData.penumbraTint = environmentSettings.outdoorsSunLightPenumbraTintEnabled;
			_moonLightAdditionalData.shadowDimmer = environmentSettings.outdoorsSunLightDimmer;
		}
	}

	public void Init()
	{
		if (environmentSettings == null)
		{
			return;
		}
		UpdateReflectionProbes();
		InstanceBehavior<BuildingManager>.Instance.indoorVolume.profile.TryGet<ColorAdjustments>(out _indoorColorAdjustments);
		InstanceBehavior<BuildingManager>.Instance.indoorVolume.profile.TryGet<GradientSky>(out _indoorGradientSky);
		dayVolume.TryGet<GradientSky>(out _dayVolumeGradientSky);
		UpdateHourlyValues();
		GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, (Action)delegate
		{
			if (!usingSliderDebugTool)
			{
				UpdateHourlyValues(GetCurrentHour());
			}
		});
		StartCoroutine(StartDelayed());
		GlobalEvents.outdoorLightsStatusChanged = (Action<bool>)Delegate.Combine(GlobalEvents.outdoorLightsStatusChanged, (Action<bool>)delegate
		{
			Manager.UpdateVehicleLights(RainHelper.AreRainDropsFalling || ShouldOutdoorLightsBeOn());
		});
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, (Action<Address>)delegate
		{
			OnEnterBuilding();
		});
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, (Action<Address>)delegate
		{
			OnExitBuilding();
		});
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onEnterVehicle, (Action<VehicleController>)delegate(VehicleController vehicleController)
		{
			if (vehicleController.vehicleType.enclosed)
			{
				RainHelper.OnEnterEnclosedVehicle();
			}
		});
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, (Action<VehicleController>)delegate(VehicleController vehicleController)
		{
			if (vehicleController.vehicleType.enclosed)
			{
				RainHelper.OnExitEnclosedVehicle();
			}
		});
		if ((bool)InstanceBehavior<GameManager>.Instance)
		{
			RainHelper.Init(InstanceBehavior<GameManager>.Instance.playerController.transform.Find("Rain"), InstanceBehavior<GameManager>.Instance.rainAudio, SaveGameManager.Current.seed, SaveGameManager.Current.nextRainStartTime, TimeHelper.NowInMinutes, delegate(Timestamp startTime)
			{
				SaveGameManager.Current.nextRainStartTime = startTime;
			});
		}
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool toggled)
		{
			if (toggled)
			{
				RainHelper.OnCityMapOpen();
			}
			else
			{
				RainHelper.OnCityMapClosed();
			}
		});
		InteriorDesignerUI.onCloseInteriorDesigner = (Action)Delegate.Combine(InteriorDesignerUI.onCloseInteriorDesigner, new Action(UpdateHourlyValues));
		_sunInitialShadows = sunLight.shadows;
		_moonInitialShadows = moonLight.shadows;
		_indoorsInitialShadows = InstanceBehavior<BuildingManager>.Instance.indoorsShadowsDirectionalLight.shadows;
		ApplyShadows(PlayerPrefSettings.shadows);
		GlobalEvents.onShadowsSettingChanged = (Action<int>)Delegate.Combine(GlobalEvents.onShadowsSettingChanged, new Action<int>(ApplyShadows));
	}

	private void OnDestroy()
	{
		InteriorDesignerUI.onCloseInteriorDesigner = (Action)Delegate.Remove(InteriorDesignerUI.onCloseInteriorDesigner, new Action(UpdateHourlyValues));
		GlobalEvents.onShadowsSettingChanged = (Action<int>)Delegate.Remove(GlobalEvents.onShadowsSettingChanged, new Action<int>(ApplyShadows));
		RainHelper.OnDestroy();
	}

	public void OnEnterBlueprintCreator()
	{
		_sunLightAdditionalData.penumbraTint = environmentSettings.indoorsSunLightPenumbraTintEnabled;
		_sunLightAdditionalData.shadowDimmer = environmentSettings.indoorsSunLightDimmer;
		_isIndoors = true;
	}

	public void OnEnterBuilding()
	{
		if (!InstanceBehavior<BuildingManager>.Instance.building.IsHamptonsHouse())
		{
			_sunLightAdditionalData.penumbraTint = environmentSettings.indoorsSunLightPenumbraTintEnabled;
			_sunLightAdditionalData.shadowDimmer = environmentSettings.indoorsSunLightDimmer;
			UpdateLights();
			RainHelper.OnEnterBuilding();
			_isIndoors = true;
		}
	}

	public void OnExitBuilding()
	{
		if (_isIndoors)
		{
			_sunLightAdditionalData.penumbraTint = environmentSettings.outdoorsSunLightPenumbraTintEnabled;
			_sunLightAdditionalData.shadowDimmer = environmentSettings.outdoorsSunLightDimmer;
			UpdateLights();
			RainHelper.OnExitBuilding();
			_isIndoors = false;
		}
	}

	private void ApplyShadows(int shadowsSetting)
	{
		sunLight.shadows = ((shadowsSetting == 1 || shadowsSetting == 2) ? _sunInitialShadows : LightShadows.None);
		moonLight.shadows = ((shadowsSetting == 1 || shadowsSetting == 2) ? _moonInitialShadows : LightShadows.None);
		InstanceBehavior<BuildingManager>.Instance.indoorsShadowsDirectionalLight.shadows = ((shadowsSetting == 1 || shadowsSetting == 2) ? _indoorsInitialShadows : LightShadows.None);
	}

	private float GetCurrentHour()
	{
		if (forcedHour > -1f)
		{
			return forcedHour;
		}
		return SaveGameManager.Current?.Hour ?? 18;
	}

	private IEnumerator StartDelayed()
	{
		yield return null;
		yield return null;
		Manager.UpdateVehicleLights(RainHelper.AreRainDropsFalling || ShouldOutdoorLightsBeOn());
	}

	public void UpdateLights()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			if (_lastTimeHours < 0f)
			{
				_lastTimeHours = TimeHelper.Now().Hour;
			}
			bool flag = ShouldIndoorLightsBeOn(_lastTimeHours) || IsInsideABuildingWithForcedLightsOn();
			if (isLightOnPeriod != flag)
			{
				isLightOnPeriod = flag;
				GlobalEvents.indoorLightsStatusChanged?.Invoke(flag);
				sunLight.gameObject.SetActive(!isLightOnPeriod);
			}
			moonLight.gameObject.SetActive((!BuildingManager.IsInsideBuilding || InstanceBehavior<BuildingManager>.Instance.building.IsHamptonsHouse()) && !BuildingPreview.isPreviewing && isLightOnPeriod);
			if ((bool)InstanceBehavior<BuildingManager>.Instance?.indoorsShadowsDirectionalLight)
			{
				InstanceBehavior<BuildingManager>.Instance.indoorsShadowsDirectionalLight.gameObject.SetActive(((BuildingManager.IsInsideBuilding && !InstanceBehavior<BuildingManager>.Instance.building.IsHamptonsHouse()) || BuildingPreview.isPreviewing) && isLightOnPeriod);
			}
			bool flag2 = ShouldOutdoorLightsBeOn(_lastTimeHours);
			if (isOutdoorLightOnPeriod != flag2)
			{
				isOutdoorLightOnPeriod = flag2;
				GlobalEvents.outdoorLightsStatusChanged?.Invoke(obj: false);
			}
		}
	}

	private static bool IsInsideABuildingWithForcedLightsOn()
	{
		if (InteriorDesignerHelper.BlueprintCreatorMode)
		{
			return HasForcedIndoorLights(BlueprintCreatorSystem.CurrentBuildingType);
		}
		if (BuildingPreview.isPreviewing)
		{
			return HasForcedIndoorLights(InstanceBehavior<UIs>.Instance.buildingPreview.GetCurrentBuildingType);
		}
		if (BuildingManager.IsInsideBuilding)
		{
			return HasForcedIndoorLights(InstanceBehavior<BuildingManager>.Instance.building);
		}
		return false;
	}

	private static bool HasForcedIndoorLights(Building building)
	{
		if (BuildingTypeHelper.TryGetData(building, out var buildingTypeData))
		{
			return buildingTypeData.HasTag(TagRef.Buildingtypetag.forceindoorlights);
		}
		return false;
	}

	private static bool HasForcedIndoorLights(string buildingType)
	{
		if (BuildingTypeHelper.TryGetData(buildingType, out var buildingTypeData))
		{
			return buildingTypeData.HasTag(TagRef.Buildingtypetag.forceindoorlights);
		}
		return false;
	}

	public void OnValidate()
	{
		if (!(environmentSettings == null))
		{
			if (_reflectionData.Count == 0)
			{
				UpdateReflectionProbes();
			}
			if (!usingSliderDebugTool)
			{
				UpdateHourlyValues(GetCurrentHour());
			}
			if ((bool)_sunLightAdditionalData && !Application.isPlaying)
			{
				Update();
			}
		}
	}

	private void Update()
	{
		if (ShouldUpdateEnvironmentValues())
		{
			float timeInHours = (usingSliderDebugTool ? InGameTimeOfDayPanel.selectedTimeInHours : ((float)SaveGameManager.Current.Hour + SaveGameManager.Current.Minute / 60f));
			SetEnvironmentSettings(timeInHours);
			RainHelper.UpdateRain();
			UpdateTrafficDensity(timeInHours);
			if (_previousRaindrops != RainHelper.AreRainDropsFalling)
			{
				UpdateLights();
				_previousRaindrops = RainHelper.AreRainDropsFalling;
			}
		}
	}

	private bool ShouldUpdateEnvironmentValues()
	{
		if (environmentSettings != null && !LoadScene.isLoading && !CityMap.IsOpen && !InteriorDesignerUI.IsOpen && !BuildingPreview.isPreviewing)
		{
			return SaveGameManager.Current != null;
		}
		return false;
	}

	private void UpdateHourlyValues()
	{
		if (SaveGameManager.Current != null)
		{
			UpdateHourlyValues(SaveGameManager.Current.Hour);
		}
	}

	public void UpdateHourlyValues(float timeInHours, bool forceInsideBuilding = false, bool forceUpdateEnvironmentalValues = false)
	{
		_lastTimeHours = timeInHours;
		UpdateLights();
		if (!ShouldUpdateEnvironmentValues() && !forceUpdateEnvironmentalValues)
		{
			return;
		}
		float animationFloatValue = GetAnimationFloatValue(environmentSettings.reflectionProbeMultiplier, environmentSettingsRain.reflectionProbeMultiplier, timeInHours, RainHelper.isRaining);
		float animationFloatValue2 = GetAnimationFloatValue(environmentSettings.reflectionProbeWeight, environmentSettingsRain.reflectionProbeWeight, timeInHours, RainHelper.isRaining);
		foreach (HDAdditionalReflectionData reflectionDatum in _reflectionData)
		{
			reflectionDatum.multiplier = animationFloatValue;
			reflectionDatum.weight = animationFloatValue2;
		}
		Shader.SetGlobalFloat(TimeOfDayCachedName, GetAnimationFloatValue(environmentSettings.fakeInteriorShaderValue, environmentSettingsRain.fakeInteriorShaderValue, timeInHours, RainHelper.isRaining));
		if ((bool)InstanceBehavior<CityManager>.Instance && (bool)InstanceBehavior<CityManager>.Instance.trafficComponent)
		{
			if (InstanceBehavior<GameManager>.Instance.spawnTraffic)
			{
				UpdateTrafficDensity(timeInHours, force: true);
			}
			else
			{
				ResetTrafficDensityCache();
			}
			TrafficManager.Instance.UpdateVehicleGroupTypePercentages(GetVehicleGroupTypeProbabilities(timeInHours));
			if (InstanceBehavior<CityManager>.Instance.pedestrianSpawner.spawningActive)
			{
				InstanceBehavior<CityManager>.Instance.pedestrianSpawner.SetActivePedestrians(Mathf.RoundToInt(GetAnimationFloatValue(environmentSettings.pedestrians, environmentSettingsRain.pedestrians, timeInHours, RainHelper.isRaining)));
			}
		}
	}

	private void UpdateTrafficDensity(float timeInHours, bool force = false)
	{
		if (!InstanceBehavior<CityManager>.Instance || !InstanceBehavior<CityManager>.Instance.trafficComponent)
		{
			return;
		}
		if (!InstanceBehavior<GameManager>.Instance.spawnTraffic)
		{
			ResetTrafficDensityCache();
			return;
		}
		string neighbourhood = ClosestBuildingFromPlayer.Get().Neighbourhood;
		int num = Mathf.RoundToInt(GetAnimationFloatValue(environmentSettings.numberOfAiVehicles, environmentSettingsRain.numberOfAiVehicles, timeInHours, RainHelper.isRaining) * (float)NeighborhoodHelper.GetData(neighbourhood).vehicleTrafficDensityPercentage / 100f);
		if (force || num != _lastTrafficDensity || !(neighbourhood == _lastTrafficDensityNeighborhood))
		{
			_lastTrafficDensity = num;
			_lastTrafficDensityNeighborhood = neighbourhood;
			Manager.SetTrafficDensity(num);
		}
	}

	private void ResetTrafficDensityCache()
	{
		_lastTrafficDensity = -1;
		_lastTrafficDensityNeighborhood = null;
	}

	public int[] GetVehicleGroupTypeProbabilities(float timeInHours)
	{
		return new int[3]
		{
			Mathf.RoundToInt(GetAnimationFloatValue(environmentSettings.percentageOfPersonalVehicles, environmentSettingsRain.percentageOfPersonalVehicles, timeInHours, RainHelper.isRaining)),
			Mathf.RoundToInt(GetAnimationFloatValue(environmentSettings.percentageOfServiceVehicles, environmentSettingsRain.percentageOfServiceVehicles, timeInHours, RainHelper.isRaining)),
			Mathf.RoundToInt(GetAnimationFloatValue(environmentSettings.percentageOfWorkVehicles, environmentSettingsRain.percentageOfWorkVehicles, timeInHours, RainHelper.isRaining))
		};
	}

	public void SetEnvironmentSettings(float timeInHours, bool forceInsideBuilding = false)
	{
		float timeInHoursNormalized = timeInHours / 24f;
		bool flag = forceInsideBuilding || _isIndoors;
		if (InstanceBehavior<BuildingManager>.Instance?.extraIndoorsDirectionalLightData != null)
		{
			InstanceBehavior<BuildingManager>.Instance.extraIndoorsDirectionalLightData.intensity = GetAnimationFloatValue(environmentSettings.extraIndoorsDirectionalLightIntensity, environmentSettingsRain.extraIndoorsDirectionalLightIntensity, timeInHours, RainHelper.isRaining);
		}
		_sunLightAdditionalData.intensity = GetAnimationFloatValue(environmentSettings.sunIntensity, environmentSettingsRain.sunIntensity, timeInHours, RainHelper.isRaining);
		sunLight.colorTemperature = GetAnimationFloatValue(environmentSettings.sunTemperature, environmentSettingsRain.sunTemperature, timeInHours, RainHelper.isRaining);
		Vector3 vector = (flag ? new Vector3(GetFloatTransitionValue(environmentSettings.indoorsSunRotationX, environmentSettingsRain.indoorsSunRotationX, RainHelper.isRaining), GetAnimationFloatValue(environmentSettings.indoorsSunRotationY, environmentSettingsRain.indoorsSunRotationY, timeInHours, RainHelper.isRaining), 0f) : new Vector3(GetAnimationFloatValue(environmentSettings.outdoorsSunRotationX, environmentSettingsRain.outdoorsSunRotationX, timeInHours, RainHelper.isRaining), GetFloatTransitionValue(environmentSettings.outdoorsSunRotationY, environmentSettingsRain.outdoorsSunRotationY, RainHelper.isRaining), 0f));
		if (MathHelper.GetExactAngle(sunLight.transform.rotation, Quaternion.Euler(vector)) >= degreesDifferenceToUpdateDlRotation)
		{
			sunLight.transform.eulerAngles = vector;
		}
		_moonLightAdditionalData.intensity = GetAnimationFloatValue(environmentSettings.moonIntensity, environmentSettingsRain.moonIntensity, timeInHours, RainHelper.isRaining);
		Vector3 vector2 = (flag ? new Vector3(GetFloatTransitionValue(environmentSettings.indoorsMoonRotationX, environmentSettingsRain.indoorsMoonRotationX, RainHelper.isRaining), GetAnimationFloatValue(environmentSettings.indoorsMoonRotationY, environmentSettingsRain.indoorsMoonRotationY, timeInHours, RainHelper.isRaining), 0f) : new Vector3(GetAnimationFloatValue(environmentSettings.outdoorsMoonRotationX, environmentSettingsRain.outdoorsMoonRotationX, timeInHours, RainHelper.isRaining), GetFloatTransitionValue(environmentSettings.outdoorsMoonRotationY, environmentSettingsRain.outdoorsMoonRotationY, RainHelper.isRaining), 0f));
		if (MathHelper.GetExactAngle(moonLight.transform.rotation, Quaternion.Euler(vector2)) >= degreesDifferenceToUpdateDlRotation)
		{
			moonLight.transform.eulerAngles = vector2;
		}
		float animationFloatValue = GetAnimationFloatValue(environmentSettings.fogAttDistance, environmentSettingsRain.fogAttDistance, timeInHours, RainHelper.isRaining);
		float animationFloatValue2 = GetAnimationFloatValue(environmentSettings.exposure, environmentSettingsRain.exposure, timeInHours, RainHelper.isRaining);
		for (int i = 0; i < postProcessingVolumeProfiles.Length; i++)
		{
			if (_fogs[i] != null)
			{
				_fogs[i].meanFreePath.value = animationFloatValue;
			}
			if (_exposures[i] != null)
			{
				_exposures[i].fixedExposure.value = animationFloatValue2;
			}
		}
		float floatTransitionValue = GetFloatTransitionValue(environmentSettings.indoorExposure, environmentSettingsRain.indoorExposure, RainHelper.isRaining);
		_indoorColorAdjustments.postExposure.value = floatTransitionValue;
		if (_indoorGradientSky != null && (flag || UndergroundParkingManager.IsInsideParking))
		{
			_indoorGradientSky.top.value = GetGradientColorValue(environmentSettings.skyTopIndoors.gradient, environmentSettingsRain.skyTopIndoors.gradient, timeInHoursNormalized, RainHelper.isRaining);
			_indoorGradientSky.middle.value = GetGradientColorValue(environmentSettings.skyMiddleIndoors.gradient, environmentSettingsRain.skyMiddleIndoors.gradient, timeInHoursNormalized, RainHelper.isRaining);
			_indoorGradientSky.bottom.value = GetGradientColorValue(environmentSettings.skyBottomIndoors.gradient, environmentSettingsRain.skyBottomIndoors.gradient, timeInHoursNormalized, RainHelper.isRaining);
		}
		else
		{
			_dayVolumeGradientSky.top.value = GetGradientColorValue(environmentSettings.dayVolumeTop.gradient, environmentSettingsRain.dayVolumeTop.gradient, timeInHoursNormalized, RainHelper.isRaining);
			_dayVolumeGradientSky.middle.value = GetGradientColorValue(environmentSettings.dayVolumeMiddle.gradient, environmentSettingsRain.dayVolumeMiddle.gradient, timeInHoursNormalized, RainHelper.isRaining);
			_dayVolumeGradientSky.bottom.value = GetGradientColorValue(environmentSettings.dayVolumeBottom.gradient, environmentSettingsRain.dayVolumeBottom.gradient, timeInHoursNormalized, RainHelper.isRaining);
		}
		Shader.SetGlobalFloat(AmountOfLightsOn, GetAnimationFloatValue(environmentSettings.amountOfLightsOn, environmentSettingsRain.amountOfLightsOn, timeInHours, RainHelper.isRaining));
		Shader.SetGlobalFloat(ExtraFresnelPower, GetAnimationFloatValue(environmentSettings.extraFresnelPower, environmentSettingsRain.extraFresnelPower, timeInHours, RainHelper.isRaining));
		Shader.SetGlobalColor(ExtraFresnelColor, GetGradientColorValue(environmentSettings.extraFresnelColor.gradient, environmentSettingsRain.extraFresnelColor.gradient, timeInHoursNormalized, RainHelper.isRaining));
		SetShadowTint(GetGradientColorValue(environmentSettings.shadowTintColor, environmentSettingsRain.shadowTintColor, timeInHoursNormalized, RainHelper.isRaining));
		UpdateSewers(timeInHoursNormalized);
	}

	private void UpdateSewers(float timeInHoursNormalized)
	{
		Color gradientColorValue = GetGradientColorValue(environmentSettings.sewerSteamColor, environmentSettingsRain.sewerSteamColor, timeInHoursNormalized, RainHelper.isRaining);
		if (gradientColorValue.Approximately(_lastSewerSteamColor))
		{
			return;
		}
		_lastSewerSteamColor = gradientColorValue;
		foreach (SewerSteam instance in SewerSteam.Instances)
		{
			instance.UpdateVisuals(gradientColorValue);
		}
	}

	private float GetFloatTransitionValue(float sunny, float rainy, bool isRaining)
	{
		if (!isRaining)
		{
			return sunny;
		}
		if (RainHelper.environmentTransitionProgress >= 1f)
		{
			return rainy;
		}
		return Mathf.Lerp(sunny, rainy, RainHelper.environmentTransitionProgress);
	}

	private float GetAnimationFloatValue(AnimationCurve sunny, AnimationCurve rainy, float timeInHours, bool isRaining)
	{
		if (!isRaining)
		{
			return sunny.Evaluate(timeInHours);
		}
		if (RainHelper.environmentTransitionProgress >= 1f)
		{
			return rainy.Evaluate(timeInHours);
		}
		return Mathf.Lerp(sunny.Evaluate(timeInHours), rainy.Evaluate(timeInHours), RainHelper.environmentTransitionProgress);
	}

	private Color GetGradientColorValue(Gradient sunny, Gradient rainy, float timeInHoursNormalized, bool isRaining)
	{
		if (!isRaining)
		{
			return sunny.Evaluate(timeInHoursNormalized);
		}
		if (RainHelper.environmentTransitionProgress >= 1f)
		{
			return rainy.Evaluate(timeInHoursNormalized);
		}
		return Color.Lerp(sunny.Evaluate(timeInHoursNormalized), rainy.Evaluate(timeInHoursNormalized), RainHelper.environmentTransitionProgress);
	}

	public void SetShadowTint(Color color)
	{
		if (enabledShadowTint)
		{
			_sunLightAdditionalData.shadowTint = color;
			_moonLightAdditionalData.shadowTint = color;
		}
	}

	public void UpdateReflectionProbes()
	{
		_reflectionData = (from x in UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None)
			select x.GetComponent<HDAdditionalReflectionData>()).ToList();
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		isLightOnPeriod = false;
	}
}
