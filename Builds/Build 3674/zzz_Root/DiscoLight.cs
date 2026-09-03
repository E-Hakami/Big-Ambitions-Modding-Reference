using System;
using Helpers;
using UnityEngine;

public class DiscoLight : MonoBehaviour
{
	[SerializeField]
	private float secondsBetweenColors;

	[SerializeField]
	private SkinnedMeshRenderer rendererWithEmission;

	[SerializeField]
	private Light spotLight;

	[SerializeField]
	private Transform stageLightsRotationHandler;

	[SerializeField]
	private float xRotationVariation;

	[SerializeField]
	private float secondsBetweenIterations;

	private Color _nextColor;

	private Color _previousColor;

	private Color32 _currentColor;

	private float _currentColorInterpolation;

	private LightShadows _initialLightShadows;

	private float _initialXModelRotation;

	private float _initialXLightRotation;

	private float _xRotationDecrement;

	private void Awake()
	{
		_previousColor = ColorHelper.GetRandomBaseColor();
		_currentColor = _previousColor;
		_initialXModelRotation = rendererWithEmission.transform.localEulerAngles.x;
		_initialXLightRotation = spotLight.transform.localEulerAngles.x;
		UpdateColorsAndRotation();
		GetNextColor();
		_initialLightShadows = spotLight.shadows;
		ApplyShadows(PlayerPrefSettings.shadows);
		GlobalEvents.onShadowsSettingChanged = (Action<int>)Delegate.Combine(GlobalEvents.onShadowsSettingChanged, new Action<int>(ApplyShadows));
	}

	private void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			GlobalEvents.onShadowsSettingChanged = (Action<int>)Delegate.Remove(GlobalEvents.onShadowsSettingChanged, new Action<int>(ApplyShadows));
		}
	}

	private void Update()
	{
		if (_currentColorInterpolation >= secondsBetweenColors)
		{
			_currentColorInterpolation = 0f;
			GetNextColor();
		}
		else
		{
			_currentColorInterpolation += Time.deltaTime / secondsBetweenColors;
		}
		_currentColor = Color32.Lerp(_currentColor, _nextColor, _currentColorInterpolation);
		_xRotationDecrement = Mathf.PingPong(Time.time / (secondsBetweenIterations / 2f), 1f) * xRotationVariation;
		UpdateColorsAndRotation();
	}

	private void ApplyShadows(int shadowsSetting)
	{
		spotLight.shadows = ((shadowsSetting == 2) ? _initialLightShadows : LightShadows.None);
	}

	private void GetNextColor()
	{
		_nextColor = ColorHelper.GetRandomBaseColor();
		while (_nextColor == _currentColor)
		{
			_nextColor = ColorHelper.GetRandomBaseColor();
		}
	}

	private void UpdateColorsAndRotation()
	{
		stageLightsRotationHandler.transform.localEulerAngles = new Vector3(_initialXModelRotation - _xRotationDecrement, 0f, 0f);
		spotLight.transform.localEulerAngles = new Vector3(_initialXLightRotation - _xRotationDecrement, 0f, 0f);
		rendererWithEmission.material.SetColor("_EmissiveColorLDR", _currentColor);
		spotLight.color = _currentColor;
	}
}
