using System;
using System.Collections;
using Helpers;
using Streets;
using UI.MiniMenu;
using UnityEngine;
using Vehicles;

namespace Player;

public class SkipBridgeUi : MonoBehaviour
{
	private const float MinDistanceToShowButton = 100f;

	[SerializeField]
	private GameObject skipBridgeButton;

	private Coroutine _buttonCheckCoroutine;

	private void Start()
	{
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onEnterVehicle, new Action<VehicleController>(OnEnterVehicle));
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, new Action<VehicleController>(OnExitVehicle));
		SkipBridgeHelper.onSkipBridgeEnabled = (Action)Delegate.Combine(SkipBridgeHelper.onSkipBridgeEnabled, new Action(OnSkipBridgeEnabled));
		SkipBridgeHelper.onSkipBridgeDisabled = (Action)Delegate.Combine(SkipBridgeHelper.onSkipBridgeDisabled, new Action(OnSkipBridgeDisabled));
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, new Action<bool>(OnCityMapToggle));
		MiniMenu.OnToggled = (Action<bool>)Delegate.Combine(MiniMenu.OnToggled, new Action<bool>(OnMiniMenuToggle));
		if (SkipBridgeHelper.skipBridgeEnabled)
		{
			OnSkipBridgeEnabled();
		}
	}

	private void OnEnable()
	{
		if (SkipBridgeHelper.skipBridgeEnabled)
		{
			OnSkipBridgeEnabled();
		}
	}

	private void OnDisable()
	{
		if (_buttonCheckCoroutine != null)
		{
			StopCoroutine(_buttonCheckCoroutine);
			_buttonCheckCoroutine = null;
		}
	}

	private void OnCityMapToggle(bool toggled)
	{
		if (SkipBridgeHelper.skipBridgeEnabled)
		{
			if (toggled)
			{
				OnSkipBridgeDisabled();
			}
			else
			{
				OnSkipBridgeEnabled();
			}
		}
	}

	private void OnMiniMenuToggle(bool toggled)
	{
		if (SkipBridgeHelper.skipBridgeEnabled && !CityMap.IsOpen)
		{
			if (toggled)
			{
				OnSkipBridgeDisabled();
			}
			else
			{
				OnSkipBridgeEnabled();
			}
		}
	}

	public void SkipBridge()
	{
		SkipBridgeHelper.SkipBridge();
	}

	private static void OnEnterVehicle(VehicleController _)
	{
		BridgeTriggerController.UpdateStatus();
	}

	private static void OnExitVehicle(VehicleController _)
	{
		BridgeTriggerController.UpdateStatus();
	}

	private void OnSkipBridgeEnabled()
	{
		if (_buttonCheckCoroutine != null)
		{
			StopCoroutine(_buttonCheckCoroutine);
		}
		_buttonCheckCoroutine = StartCoroutine(ButtonCheckCoroutine());
	}

	private void OnSkipBridgeDisabled()
	{
		skipBridgeButton.SetActive(value: false);
		if (_buttonCheckCoroutine != null)
		{
			StopCoroutine(_buttonCheckCoroutine);
			_buttonCheckCoroutine = null;
		}
	}

	private IEnumerator ButtonCheckCoroutine()
	{
		while (SkipBridgeHelper.skipBridgeEnabled)
		{
			VehicleController currentVehicleBase = VehicleHelper.GetCurrentVehicleBase();
			if ((bool)currentVehicleBase)
			{
				Transform transform = SkipBridgeHelper.FindTarget(currentVehicleBase);
				bool flag = Vector3.Distance(currentVehicleBase.transform.position, transform.position) >= 100f;
				if (skipBridgeButton.activeSelf != flag)
				{
					skipBridgeButton.SetActive(flag);
				}
			}
			yield return new WaitForSeconds(0.5f);
		}
		_buttonCheckCoroutine = null;
	}

	private void OnDestroy()
	{
		SkipBridgeHelper.onSkipBridgeEnabled = (Action)Delegate.Remove(SkipBridgeHelper.onSkipBridgeEnabled, new Action(OnSkipBridgeEnabled));
		SkipBridgeHelper.onSkipBridgeDisabled = (Action)Delegate.Remove(SkipBridgeHelper.onSkipBridgeDisabled, new Action(OnSkipBridgeDisabled));
		MiniMenu.OnToggled = (Action<bool>)Delegate.Remove(MiniMenu.OnToggled, new Action<bool>(OnMiniMenuToggle));
	}
}
