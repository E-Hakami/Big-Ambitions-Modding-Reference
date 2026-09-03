using System.Collections.Generic;
using Helpers;

namespace Buildings.BuildingTypes.Special.GasStation;

public class GasStationPartController : ViewBlockingEntity
{
	public static readonly List<GasStationPartController> AllGasStationPartControllers = new List<GasStationPartController>();

	public CityBuildingController gasStationCbc;

	protected override int DefaultLayer => LayerHelper.InteractiveItemsLayerIndex;

	public override void Awake()
	{
	}

	public override void Start()
	{
		base.Start();
		AllGasStationPartControllers.Add(this);
	}

	public override bool OnIoLeftClick()
	{
		if (CityMap.IsOpen)
		{
			return gasStationCbc.OnIoLeftClick();
		}
		return false;
	}

	public override void OnIoRightClick()
	{
	}

	public override void OnIoEnter()
	{
		if (CityMap.IsOpen)
		{
			gasStationCbc.OnIoEnter();
		}
	}

	public override void OnIoExit()
	{
		if (CityMap.IsOpen)
		{
			gasStationCbc.OnIoExit();
		}
	}

	public override bool SetCameraBlockMode(bool isOn)
	{
		if (!base.SetCameraBlockMode(isOn))
		{
			return false;
		}
		base.gameObject.layer = ((!isOn) ? DefaultLayer : 0);
		return true;
	}
}
