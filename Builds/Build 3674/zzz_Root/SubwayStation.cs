using System;
using Enums;
using Helpers;
using Player.HUD.ItemInfoOverlays;
using UI;
using UI.Notification;

public class SubwayStation : EntityController
{
	public const float PricePerRide = 3f;

	public static bool mapFilterIsOn;

	public SubwayStationName stationName;

	public string neighbourhood;

	private PointOfInterest _cityMapPoi;

	public PointOfInterest CityMapPoi
	{
		get
		{
			if (_cityMapPoi == null)
			{
				_cityMapPoi = InstanceBehavior<CityManager>.Instance.cityMap.AddPoi(base.transform, InstanceBehavior<CityManager>.Instance.subwaySystem.poiIcon, InstanceBehavior<CityManager>.Instance.subwaySystem.poiBackgroundColor);
			}
			return _cityMapPoi;
		}
	}

	public override void Start()
	{
		base.Start();
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool isOn)
		{
			if (!isOn)
			{
				SetOutlineColor(InstanceBehavior<GlobalReferences>.Instance.colors.white);
				OnIoExit();
			}
		});
	}

	public override bool ShouldReactToIoEnter()
	{
		return true;
	}

	public override bool OnIoLeftClick()
	{
		if (InstanceBehavior<CityManager>.Instance.cityMap.isSubwayMode)
		{
			InstanceBehavior<CityManager>.Instance.subwaySystem.TravelTo(this);
			return true;
		}
		return base.OnIoLeftClick();
	}

	public override void OnIoExit()
	{
		if (InstanceBehavior<CityManager>.Instance.cityMap.isSubwayMode)
		{
			return;
		}
		if (mapFilterIsOn && CityMap.IsOpen)
		{
			if (InstanceBehavior<OverlayManager>.Instance.IsShowingOverlayOverItem(this))
			{
				InstanceBehavior<OverlayManager>.Instance.HideSimpleOverlayAndClearCta();
			}
		}
		else
		{
			base.OnIoExit();
		}
	}

	public override bool Interact()
	{
		if (base.Interact())
		{
			return true;
		}
		if (VehicleHelper.IsInsideVehicle())
		{
			return false;
		}
		if (InstanceBehavior<UIs>.Instance.gameSpeed.Paused)
		{
			return false;
		}
		if (SaveGameManager.Current.Money >= 3f)
		{
			InstanceBehavior<CityManager>.Instance.subwaySystem.lastSubwayStation = this;
			InstanceBehavior<CityManager>.Instance.cityMap.Toggle();
			InstanceBehavior<CityManager>.Instance.cityMap.ToggleSubwayMode(isOn: true);
		}
		else
		{
			Notifications.ShowInsufficientMoney();
		}
		return true;
	}
}
