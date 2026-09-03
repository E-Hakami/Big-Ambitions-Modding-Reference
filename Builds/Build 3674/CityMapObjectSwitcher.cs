using System;
using UnityEngine;

public class CityMapObjectSwitcher : MonoBehaviour
{
	[SerializeField]
	private Transform[] mapTransforms;

	[SerializeField]
	private Transform[] defaultTransforms;

	private void OnEnable()
	{
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, new Action<bool>(Apply));
		Apply(CityMap.IsOpen);
	}

	private void OnDisable()
	{
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Remove(GlobalEvents.onCityMapToggle, new Action<bool>(Apply));
	}

	private void Apply(bool isMapOpen)
	{
		Transform[] array = mapTransforms;
		foreach (Transform transform in array)
		{
			if (transform != null)
			{
				transform.gameObject.SetActive(isMapOpen);
			}
		}
		array = defaultTransforms;
		foreach (Transform transform2 in array)
		{
			if (transform2 != null)
			{
				transform2.gameObject.SetActive(!isMapOpen);
			}
		}
	}
}
