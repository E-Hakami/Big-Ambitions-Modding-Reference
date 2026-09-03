using System;
using System.Collections.Generic;
using Buildings;
using HGAttributes;
using Parking.UndergroundParking;
using UI.Load;
using UnityEngine;

public class NeighborhoodSfxModifier : MonoBehaviour
{
	[Serializable]
	private class MixerParameterModifier
	{
		[field: SerializeField]
		public string Parameter { get; private set; }

		[field: SerializeField]
		[field: Range(-80f, 20f)]
		public float Value { get; private set; }
	}

	[Serializable]
	private class NeighborhoodModifier
	{
		[field: SerializeField]
		[field: AutocompleteDropdown("Neighborhoods")]
		public string Neighborhood { get; private set; }

		[field: SerializeField]
		public bool ApplyIndoors { get; private set; }

		[field: SerializeField]
		[field: Min(0f)]
		public float TransitionDuration { get; private set; } = 1f;

		[field: SerializeField]
		public MixerParameterModifier[] MixerParameters { get; private set; }
	}

	private class MixerParameterState
	{
		public float OriginalDecibels { get; set; }

		public float StartLinearVolume { get; set; }

		public float TargetLinearVolume { get; set; }

		public float TargetDecibels { get; set; }

		public float Duration { get; set; }

		public float Elapsed { get; set; }

		public bool IsTransitioning { get; set; }

		public bool ClearOnComplete { get; set; }
	}

	private const float MinimumLinearVolume = 1E-05f;

	[SerializeField]
	private SfxManager sfxManager;

	[SerializeField]
	[Min(0.1f)]
	private float neighborhoodCheckInterval = 0.5f;

	[SerializeField]
	[Min(0.1f)]
	private float instantTransitionDistanceThreshold = 10f;

	[SerializeField]
	private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	[SerializeField]
	private AnimationCurve fadeOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private NeighborhoodModifier[] modifiers = Array.Empty<NeighborhoodModifier>();

	private readonly Dictionary<string, MixerParameterState> _parameterStates = new Dictionary<string, MixerParameterState>();

	private readonly List<string> _parameterKeys = new List<string>();

	private NeighborhoodModifier _activeModifier;

	private float _nextNeighborhoodCheckTime;

	private Vector3 _previousPlayerPosition;

	private bool _isInitialized;

	private bool _hasPreviousPlayerPosition;

	private float _sqrTransitionDistanceThreshold;

	private bool IsValid
	{
		get
		{
			if ((bool)sfxManager && !LoadScene.isLoading && SaveGameManager.Current != null && (bool)InstanceBehavior<CityManager>.Instance)
			{
				CityBuildingController[] cityBuildingControllers = InstanceBehavior<CityManager>.Instance.cityBuildingControllers;
				if (cityBuildingControllers != null && cityBuildingControllers.Length > 0 && (bool)InstanceBehavior<GameManager>.Instance && (bool)InstanceBehavior<GameManager>.Instance.playerController)
				{
					return !InstanceBehavior<GameManager>.Instance.playerController.awaitingRepositioning;
				}
			}
			return false;
		}
	}

	private void OnEnable()
	{
		_nextNeighborhoodCheckTime = 0f;
		_hasPreviousPlayerPosition = false;
		_sqrTransitionDistanceThreshold = instantTransitionDistanceThreshold * instantTransitionDistanceThreshold;
	}

	private void LateUpdate()
	{
		if (!IsValid)
		{
			return;
		}
		Vector3 position = InstanceBehavior<GameManager>.Instance.playerController.transform.position;
		bool flag = HasTeleported(position);
		_previousPlayerPosition = position;
		_hasPreviousPlayerPosition = true;
		if (!_isInitialized)
		{
			Initialize();
			return;
		}
		UpdateParameterTransitions();
		NeighborhoodModifier currentModifier;
		if (flag && TryGetCurrentModifierAfterPositionChange(out var modifier))
		{
			ChangeModifier(modifier, 0f);
			_nextNeighborhoodCheckTime = Time.unscaledTime + neighborhoodCheckInterval;
		}
		else if (!(Time.unscaledTime < _nextNeighborhoodCheckTime) && TryGetCurrentModifier(out currentModifier))
		{
			_nextNeighborhoodCheckTime = Time.unscaledTime + neighborhoodCheckInterval;
			if (currentModifier != _activeModifier)
			{
				ChangeModifier(currentModifier);
			}
		}
	}

	private bool HasTeleported(Vector3 playerPosition)
	{
		if (_hasPreviousPlayerPosition)
		{
			return (playerPosition - _previousPlayerPosition).sqrMagnitude >= _sqrTransitionDistanceThreshold;
		}
		return false;
	}

	private void Initialize()
	{
		if (TryGetCurrentModifierAfterPositionChange(out var modifier))
		{
			InitializeModifier(modifier);
			_nextNeighborhoodCheckTime = Time.unscaledTime + neighborhoodCheckInterval;
		}
	}

	private bool TryGetCurrentModifierAfterPositionChange(out NeighborhoodModifier modifier)
	{
		modifier = null;
		if (ClosestBuildingFromPlayer.TryGet(out var _))
		{
			return TryGetCurrentModifier(out modifier);
		}
		return false;
	}

	private void OnDisable()
	{
		ClearOverrides();
		_activeModifier = null;
		_isInitialized = false;
		_hasPreviousPlayerPosition = false;
	}

	private bool TryGetCurrentModifier(out NeighborhoodModifier currentModifier)
	{
		currentModifier = null;
		if (!ClosestBuildingFromPlayer.TryGet(out var building))
		{
			return false;
		}
		bool flag = BuildingManager.IsInsideBuilding || UndergroundParkingManager.IsInsideParking;
		for (int i = 0; i < modifiers.Length; i++)
		{
			NeighborhoodModifier neighborhoodModifier = modifiers[i];
			if (!(neighborhoodModifier.Neighborhood != building.Neighbourhood))
			{
				bool flag2 = !flag || neighborhoodModifier.ApplyIndoors;
				currentModifier = (flag2 ? neighborhoodModifier : null);
				return true;
			}
		}
		return true;
	}

	private void InitializeModifier(NeighborhoodModifier modifier)
	{
		_isInitialized = true;
		_activeModifier = modifier;
		if (_activeModifier != null)
		{
			MixerParameterModifier[] mixerParameters = _activeModifier.MixerParameters;
			for (int i = 0; i < mixerParameters.Length; i++)
			{
				ApplyOverride(mixerParameters[i], 0f);
			}
		}
	}

	private void ChangeModifier(NeighborhoodModifier modifier)
	{
		float transitionDuration = modifier?.TransitionDuration ?? _activeModifier.TransitionDuration;
		ChangeModifier(modifier, transitionDuration);
	}

	private void ChangeModifier(NeighborhoodModifier modifier, float transitionDuration)
	{
		RestoreOverrides(transitionDuration);
		_activeModifier = modifier;
		if (_activeModifier != null)
		{
			MixerParameterModifier[] mixerParameters = _activeModifier.MixerParameters;
			for (int i = 0; i < mixerParameters.Length; i++)
			{
				ApplyOverride(mixerParameters[i], transitionDuration);
			}
		}
	}

	private void ApplyOverride(MixerParameterModifier modifier, float transitionDuration)
	{
		string parameter = modifier.Parameter;
		if (string.IsNullOrEmpty(parameter) || !sfxManager.audioMixer.GetFloat(parameter, out var value))
		{
			Debug.LogWarning("SFX mixer parameter '" + parameter + "' is not exposed or does not exist", this);
			return;
		}
		if (!_parameterStates.TryGetValue(parameter, out var value2))
		{
			value2 = new MixerParameterState
			{
				OriginalDecibels = value
			};
			_parameterStates.Add(parameter, value2);
		}
		StartParameterTransition(parameter, value2, value, modifier.Value, transitionDuration, clearOnComplete: false);
	}

	private void RestoreOverrides(float transitionDuration)
	{
		_parameterKeys.Clear();
		foreach (string key in _parameterStates.Keys)
		{
			_parameterKeys.Add(key);
		}
		for (int i = 0; i < _parameterKeys.Count; i++)
		{
			RestoreOverride(_parameterKeys[i], transitionDuration);
		}
	}

	private void RestoreOverride(string parameter, float transitionDuration)
	{
		if (_parameterStates.TryGetValue(parameter, out var value))
		{
			if (!sfxManager.audioMixer.GetFloat(parameter, out var value2))
			{
				_parameterStates.Remove(parameter);
			}
			else
			{
				StartParameterTransition(parameter, value, value2, value.OriginalDecibels, transitionDuration, clearOnComplete: true);
			}
		}
	}

	private void ClearOverrides()
	{
		if ((bool)sfxManager)
		{
			foreach (string key in _parameterStates.Keys)
			{
				sfxManager.audioMixer.ClearFloat(key);
			}
		}
		_parameterStates.Clear();
	}

	private void StartParameterTransition(string parameter, MixerParameterState state, float currentDecibels, float targetDecibels, float transitionDuration, bool clearOnComplete)
	{
		if (transitionDuration <= 0f)
		{
			sfxManager.audioMixer.SetFloat(parameter, targetDecibels);
			if (clearOnComplete)
			{
				_parameterStates.Remove(parameter);
			}
			else
			{
				state.IsTransitioning = false;
			}
		}
		else
		{
			state.StartLinearVolume = DecibelsToLinearVolume(currentDecibels);
			state.TargetLinearVolume = DecibelsToLinearVolume(targetDecibels);
			state.TargetDecibels = targetDecibels;
			state.Duration = transitionDuration;
			state.Elapsed = 0f;
			state.IsTransitioning = true;
			state.ClearOnComplete = clearOnComplete;
		}
	}

	private void UpdateParameterTransitions()
	{
		_parameterKeys.Clear();
		foreach (var (item, mixerParameterState2) in _parameterStates)
		{
			if (!mixerParameterState2.IsTransitioning)
			{
				continue;
			}
			mixerParameterState2.Elapsed = Mathf.Min(mixerParameterState2.Elapsed + Time.unscaledDeltaTime, mixerParameterState2.Duration);
			float num = mixerParameterState2.Elapsed / mixerParameterState2.Duration;
			float t = ((mixerParameterState2.TargetLinearVolume > mixerParameterState2.StartLinearVolume) ? fadeInCurve : fadeOutCurve).Evaluate(num);
			float value = ((num >= 1f) ? mixerParameterState2.TargetDecibels : LinearVolumeToDecibels(Mathf.Lerp(mixerParameterState2.StartLinearVolume, mixerParameterState2.TargetLinearVolume, t)));
			sfxManager.audioMixer.SetFloat(item, value);
			if (!(num < 1f))
			{
				if (mixerParameterState2.ClearOnComplete)
				{
					_parameterKeys.Add(item);
				}
				else
				{
					mixerParameterState2.IsTransitioning = false;
				}
			}
		}
		for (int i = 0; i < _parameterKeys.Count; i++)
		{
			_parameterStates.Remove(_parameterKeys[i]);
		}
	}

	private static float DecibelsToLinearVolume(float decibels)
	{
		return Mathf.Pow(10f, decibels / 20f);
	}

	private static float LinearVolumeToDecibels(float linearVolume)
	{
		return 20f * Mathf.Log10(Mathf.Max(linearVolume, 1E-05f));
	}
}
