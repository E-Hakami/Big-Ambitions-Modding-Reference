using System;
using System.Collections.Generic;
using Extensions;
using Localizor;
using TMPro;
using UI.Components;
using UnityEngine;

public class CityMapSubwayStations : MonoBehaviour
{
	[SerializeField]
	private TMP_InputField searchField;

	public CityMapSubwayStationEntry entryTemplate;

	public float cameraPreviewDelay = 0.2f;

	private readonly List<CityMapSubwayStationEntry> _generatedEntries = new List<CityMapSubwayStationEntry>();

	private const string StationPrefix = "subwaystation_";

	private void Awake()
	{
		KeyboardInputHelper.Configure(searchField, null, autoFocus: false);
		searchField.onValueChanged.AddListener(FilterStations);
	}

	private void OnDestroy()
	{
		searchField.onValueChanged.RemoveListener(FilterStations);
	}

	private void FilterStations(string query)
	{
		string value = query.Trim();
		bool flag = string.IsNullOrWhiteSpace(value);
		for (int i = 0; i < _generatedEntries.Count; i++)
		{
			CityMapSubwayStationEntry cityMapSubwayStationEntry = _generatedEntries[i];
			string localization = ("subwaystation_" + cityMapSubwayStationEntry.name).GetLocalization();
			bool active = flag || localization.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) >= 0;
			cityMapSubwayStationEntry.gameObject.SetActive(active);
		}
	}

	public void Toggle(bool show)
	{
		base.gameObject.SetActive(show);
		if (!show)
		{
			return;
		}
		CityMap.ToggleBuildingHighlights(GetBuildingsToHighlight(), isOn: false, Color.black, null);
		foreach (CityMapSubwayStationEntry generatedEntry in _generatedEntries)
		{
			generatedEntry.Init();
		}
	}

	private static IEnumerable<CityBuildingController> GetBuildingsToHighlight()
	{
		CityBuildingController[] cityBuildingControllers = InstanceBehavior<CityManager>.Instance.cityBuildingControllers;
		foreach (CityBuildingController cityBuildingController in cityBuildingControllers)
		{
			BuildingRegistration buildingRegistration = cityBuildingController.buildingRegistration;
			if (!buildingRegistration.RentedByPlayer && !buildingRegistration.BuildingOwnedByPlayer)
			{
				yield return cityBuildingController;
			}
		}
	}

	public void LoadStations()
	{
		entryTemplate.transform.ResetTemplate();
		_generatedEntries.Clear();
		foreach (SubwayStation item in GetSubwayStationsSortedByLocalizedName())
		{
			CityMapSubwayStationEntry cityMapSubwayStationEntry = UnityEngine.Object.Instantiate(entryTemplate, entryTemplate.transform.parent);
			cityMapSubwayStationEntry.name = item.stationName.ToStringFast();
			cityMapSubwayStationEntry.Setup(item);
			_generatedEntries.Add(cityMapSubwayStationEntry);
		}
	}

	private static List<SubwayStation> GetSubwayStationsSortedByLocalizedName()
	{
		List<SubwayStation> list = new List<SubwayStation>(InstanceBehavior<CityManager>.Instance.subwayStations);
		list.Sort(delegate(SubwayStation a, SubwayStation b)
		{
			string localization = ("subwaystation_" + a.stationName.ToStringFast()).GetLocalization();
			string localization2 = ("subwaystation_" + b.stationName.ToStringFast()).GetLocalization();
			return string.Compare(localization, localization2, StringComparison.CurrentCulture);
		});
		return list;
	}

	public void Cancel()
	{
		InstanceBehavior<CityManager>.Instance.cityMap.ToggleSubwayMode(isOn: false);
		InstanceBehavior<CityManager>.Instance.cityMap.Toggle();
	}
}
