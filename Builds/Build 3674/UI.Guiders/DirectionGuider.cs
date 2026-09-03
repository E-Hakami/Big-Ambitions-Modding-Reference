using System;
using Helpers;
using Localizor;
using Streets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Guiders;

public class DirectionGuider : MonoBehaviour
{
	public Transform target;

	public DirectionGuiderType guiderType;

	public Sprite poiICon;

	public Color poiBackgroundColor;

	public PointOfInterest poi;

	private CityBuildingController _cbcTarget;

	private Transform _dummyTarget;

	private bool _hidden;

	public Address CurrentAddress { get; private set; }

	private void Awake()
	{
		poi = InstanceBehavior<CityManager>.Instance.cityMap.AddPoi(null, poiICon, poiBackgroundColor);
		poi.SetGuider(guiderType);
	}

	public void Reset()
	{
		_hidden = false;
		target = null;
		CurrentAddress = null;
		poi.target = null;
		poi.targetAddress = null;
		PermanentPointsOfInterest.UpdateIgnoredAddresses();
		GuidersManager.UpdateGuidersVisibility();
	}

	public void SetColor(Color color)
	{
		Transform transform = poi.transform.Find("Container")?.Find("Background");
		if (!(transform == null))
		{
			Image component = transform.GetComponent<Image>();
			color.a = component.color.a;
			component.color = color;
		}
	}

	private void Start()
	{
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool isOn)
		{
			if (!(_cbcTarget == null))
			{
				poi.offset = (isOn ? _cbcTarget.poiOffsetOnCityMap : _cbcTarget.poiOffsetOnStreet);
			}
		});
	}

	public void SetTarget(Transform position, string targetName, Address address, CityBuildingController cbc = null)
	{
		target = position;
		CurrentAddress = address;
		_cbcTarget = cbc;
		poi.targetAddress = ((cbc != null) ? cbc.building.Address : null);
		poi.offset = ((cbc != null) ? cbc.poiOffsetOnStreet : Vector3.zero);
		poi.target = position;
		cbc?.UpdatePoi(poi);
		poi.SetPermanent();
		if (string.IsNullOrEmpty(targetName))
		{
			targetName = "poi_custom_location".GetLocalization();
		}
		poi.SetText(targetName);
		GuidersManager.UpdateGuidersVisibility();
		PermanentPointsOfInterest.UpdateIgnoredAddresses();
	}

	public void SetTarget(Vector3 position, string targetName, Address address, Sprite poiIcon, Color poiBackgroundColor)
	{
		if (!_dummyTarget)
		{
			_dummyTarget = new GameObject("DirectionGuiderDummyTarget").transform;
		}
		_dummyTarget.position = position;
		poi.SetIcon(poiIcon, poiBackgroundColor);
		SetTarget(_dummyTarget, targetName, address);
	}

	public void UpdateAddressName(Address updatedAddress)
	{
		if (!CurrentAddress.IsUndefined() && !(CurrentAddress != updatedAddress))
		{
			string text = BuildingHelper.GetBuildingRegistration(updatedAddress).BusinessName;
			if (string.IsNullOrEmpty(text))
			{
				text = updatedAddress.ToFormattedString();
			}
			poi.SetText(text);
		}
	}

	public void UpdatePoiIcon(Address address)
	{
		if (!CurrentAddress.IsUndefined() && !(CurrentAddress != address))
		{
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
			poi.SetIcon(buildingRegistration.GetPOIIcon(), buildingRegistration.GetPOIBackgroundColor());
		}
	}

	public void SetHidden(bool hide)
	{
		if (_hidden != hide)
		{
			_hidden = hide;
			poi.SetHidden(hide);
		}
	}
}
