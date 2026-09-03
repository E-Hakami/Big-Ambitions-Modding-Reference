using System;
using UnityEngine;

[RequireComponent(typeof(LODGroup))]
public class LODCityMapSwitcher : MonoBehaviour
{
	public void Start()
	{
		LODGroup lodGroup = GetComponent<LODGroup>();
		if ((bool)lodGroup)
		{
			lodGroup.ForceLOD(0);
		}
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool open)
		{
			if ((bool)lodGroup)
			{
				if (PlayerPrefSettings.LowDetailCityMap)
				{
					if (open)
					{
						lodGroup.ForceLOD(1);
					}
				}
				else
				{
					lodGroup.ForceLOD(open ? 1 : 0);
				}
			}
		});
		GlobalEvents.onCityMapClosed = (Action)Delegate.Combine(GlobalEvents.onCityMapClosed, (Action)delegate
		{
			if ((bool)lodGroup)
			{
				lodGroup.ForceLOD(0);
			}
		});
	}
}
