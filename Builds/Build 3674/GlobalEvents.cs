using System;
using BigAmbitions.Items;
using Extensions;
using UnityEngine;

public static class GlobalEvents
{
	private static Action _onGameLoaded;

	private static Action _onGameLoadedLate;

	private static bool _onGameLoadedCalled;

	public static Action onGameUnloaded;

	public static Action onNewDay;

	public static Action onNewHour;

	public static Action<VehicleController> onEnterVehicle;

	public static Action<VehicleController> onExitVehicle;

	public static Action onVehicleVariablesChanged;

	public static Action<Address> onBuildingRegistrationChange;

	public static Action onSaveGame;

	public static Action onJobChange;

	public static Action<Address> onExitBuilding;

	public static Action<Address> onEnterBuilding;

	public static Action<Address> onEnterBuildingDelayed;

	public static Action<bool> outdoorLightsStatusChanged;

	public static Action<bool> indoorLightsStatusChanged;

	public static Action<bool> onCityMapToggle;

	public static Action<bool> onFullMenuToggle;

	public static Action onCityMapClosed;

	public static Action onBindingsChanged;

	public static Action<bool> onPause;

	public static Action<ItemController> onItemDropped;

	public static Action<ItemInstance> onItemSelected;

	public static Action<ItemInstance> onItemGrabbed;

	public static Action<ItemInstance> onItemDiscarded;

	public static Action onTimeMachineStarted;

	public static Action onTimeMachineEnded;

	public static Action<bool> onScreenshotModeToggled;

	public static Action onHospitalRespawnStarts;

	public static Action onCultureInfoChanged;

	public static Action<CargoInstance> onAccessoryEquipped;

	public static Action<CargoInstance> onAccessoryUnEquipped;

	public static Action<int> onShadowsSettingChanged;

	public static Action onCurrentHeightChanged;

	public static void Init()
	{
		_onGameLoaded = null;
		_onGameLoadedLate = null;
		_onGameLoadedCalled = false;
		onGameUnloaded = null;
		onNewDay = null;
		onNewHour = null;
		onEnterVehicle = null;
		onExitVehicle = null;
		onVehicleVariablesChanged = null;
		onBuildingRegistrationChange = null;
		onSaveGame = null;
		onJobChange = null;
		onExitBuilding = null;
		onEnterBuilding = null;
		onEnterBuildingDelayed = null;
		outdoorLightsStatusChanged = null;
		indoorLightsStatusChanged = null;
		onCityMapToggle = null;
		onFullMenuToggle = null;
		onCityMapClosed = null;
		onBindingsChanged = null;
		onPause = null;
		onItemDropped = null;
		onItemSelected = null;
		onItemGrabbed = null;
		onItemDiscarded = null;
		onTimeMachineStarted = null;
		onTimeMachineEnded = null;
		onScreenshotModeToggled = null;
		onHospitalRespawnStarts = null;
		onCultureInfoChanged = null;
		onAccessoryEquipped = null;
		onAccessoryUnEquipped = null;
		onShadowsSettingChanged = null;
		onCurrentHeightChanged = null;
	}

	public static void RegisterOnGameLoadedCallback(Action call)
	{
		if (_onGameLoadedCalled)
		{
			call();
		}
		else
		{
			_onGameLoaded = (Action)Delegate.Combine(_onGameLoaded, call);
		}
	}

	public static void RegisterOnGameLoadedLateCallback(Action call)
	{
		if (_onGameLoadedCalled)
		{
			call();
		}
		else
		{
			_onGameLoadedLate = (Action)Delegate.Combine(_onGameLoadedLate, call);
		}
	}

	public static void InvokeOnGameLoaded()
	{
		_onGameLoadedCalled = true;
		_onGameLoaded?.InvokeSafely();
		_onGameLoaded = null;
		_onGameLoadedLate?.InvokeSafely();
		_onGameLoadedLate = null;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		Init();
	}
}
