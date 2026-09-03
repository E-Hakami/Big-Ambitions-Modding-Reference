using System;
using System.Collections.Generic;
using System.Linq;
using Streets;
using UI.Guiders;
using UI.Smartphone;

namespace UI;

public static class PermanentPointsOfInterest
{
	private static Address[] _addressesToExclude = Array.Empty<Address>();

	private static PointOfInterest[] _permanentPointsOfInterest = Array.Empty<PointOfInterest>();

	private static bool _rebuildPOIsOnNextFrame = true;

	public static void UpdatePermanentPointsOfInterest()
	{
		_rebuildPOIsOnNextFrame = true;
	}

	public static void UpdateIgnoredAddresses()
	{
		List<Address> list = new List<Address>();
		foreach (DirectionGuider allGuider in GuidersManager.AllGuiders)
		{
			if (allGuider.poi != null && !allGuider.poi.targetAddress.IsUndefined() && allGuider.poi.IsVisibleToPlayersStreetView())
			{
				list.Add(allGuider.poi.targetAddress);
			}
		}
		_addressesToExclude = list.ToArray();
	}

	public static void HandlePermanentPOIs()
	{
		if (_rebuildPOIsOnNextFrame)
		{
			_permanentPointsOfInterest = InstanceBehavior<CityManager>.Instance.cityMap.pois.Where((PointOfInterest x) => x.Permanent).ToArray();
			_rebuildPOIsOnNextFrame = false;
		}
		PointOfInterest[] permanentPointsOfInterest = _permanentPointsOfInterest;
		foreach (PointOfInterest pointOfInterest in permanentPointsOfInterest)
		{
			if (pointOfInterest.hidden || (!pointOfInterest.isGuider && _addressesToExclude.Contains(pointOfInterest.targetAddress)))
			{
				pointOfInterest.gameObject.SetActive(value: false);
				continue;
			}
			bool flag = pointOfInterest.IsVisibleToPlayersStreetView() || CityMap.IsOpen;
			if (flag)
			{
				pointOfInterest.UpdatePosition(GameManager.GetMainCamera());
			}
			pointOfInterest.gameObject.SetActive(!FullMenu.IsOpen & flag);
		}
	}
}
