using BigAmbitions.Neighborhoods;
using Extensions;
using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.MarketInsider;

public class MarketInsiderNeighborhoodData : MonoBehaviour
{
	[BoxGroup("Citizen Data")]
	[SerializeField]
	private TMP_Text workingClassPercentage;

	[BoxGroup("Citizen Data")]
	[SerializeField]
	private TMP_Text middleClassPercentage;

	[BoxGroup("Citizen Data")]
	[SerializeField]
	private TMP_Text upperClassPercentage;

	[BoxGroup("Real Estate Data")]
	[SerializeField]
	private TMP_Text totalBuildingsValue;

	[BoxGroup("Real Estate Data")]
	[SerializeField]
	private TextLocalizationComponent averageRetailPriceLabel;

	[BoxGroup("Real Estate Data")]
	[SerializeField]
	private TMP_Text averageRetailPriceValue;

	[BoxGroup("Real Estate Data")]
	[SerializeField]
	private TextLocalizationComponent averageOfficePriceLabel;

	[BoxGroup("Real Estate Data")]
	[SerializeField]
	private TMP_Text averageOfficePriceValue;

	[BoxGroup("Real Estate Data")]
	[SerializeField]
	private TextLocalizationComponent averageWarehousePriceLabel;

	[BoxGroup("Real Estate Data")]
	[SerializeField]
	private TMP_Text averageWarehousePriceValue;

	[BoxGroup("Real Estate Data")]
	[SerializeField]
	private TextLocalizationComponent averageResidentialPriceLabel;

	[BoxGroup("Real Estate Data")]
	[SerializeField]
	private TMP_Text averageResidentialPriceValue;

	[BoxGroup("Buttons")]
	[SerializeField]
	private Button viewOnMapButton;

	private string _selectedNeighborhood;

	private void Awake()
	{
		viewOnMapButton.onClick.AddListener(ViewNeighborhoodOnMap);
	}

	private void OnEnable()
	{
		string areaUnit = UnitHelper.GetAreaUnit();
		averageRetailPriceLabel.Arguments = new
		{
			buildingType = "ba:buildingtype_retail",
			unit = areaUnit
		};
		averageOfficePriceLabel.Arguments = new
		{
			buildingType = "ba:buildingtype_office",
			unit = areaUnit
		};
		averageWarehousePriceLabel.Arguments = new
		{
			buildingType = "ba:buildingtype_warehouse",
			unit = areaUnit
		};
		averageResidentialPriceLabel.Arguments = new
		{
			buildingType = "ba:buildingtype_residential",
			unit = areaUnit
		};
	}

	public void ShowNeighborhoodData(string neighborhood)
	{
		_selectedNeighborhood = neighborhood;
		NeighborhoodData data = NeighborhoodHelper.GetData(neighborhood);
		workingClassPercentage.text = $"{data.workingClassPercentage}%";
		middleClassPercentage.text = $"{data.middleClassPercentage}%";
		upperClassPercentage.text = $"{data.upperClassClassPercentage}%";
		totalBuildingsValue.text = NeighborhoodHelper.TotalBuildings(neighborhood).ToString();
		float num = NeighborhoodHelper.AverageBuildingTypePrice(neighborhood, "ba:buildingtype_retail");
		averageRetailPriceValue.text = ((num == 0f) ? "-" : num.ToShortCurrencyFormat());
		float num2 = NeighborhoodHelper.AverageBuildingTypePrice(neighborhood, "ba:buildingtype_office");
		averageOfficePriceValue.text = ((num2 == 0f) ? "-" : num2.ToShortCurrencyFormat());
		float num3 = NeighborhoodHelper.AverageBuildingTypePrice(neighborhood, "ba:buildingtype_warehouse");
		averageWarehousePriceValue.text = ((num3 == 0f) ? "-" : num3.ToShortCurrencyFormat());
		float num4 = NeighborhoodHelper.AverageBuildingTypePrice(neighborhood, "ba:buildingtype_residential");
		averageResidentialPriceValue.text = ((num4 == 0f) ? "-" : num4.ToShortCurrencyFormat());
	}

	private void ViewNeighborhoodOnMap()
	{
		if (!string.IsNullOrEmpty(_selectedNeighborhood))
		{
			InstanceBehavior<CityManager>.Instance.cityMap.buildingToFocus = null;
			InstanceBehavior<UIs>.Instance.fullMenu.Toggle(show: false);
			CityMapNeighborhoodZones.CityMapNeighborhoodZone neighbourhoodZone = CityMapNeighborhoodZones.GetNeighbourhoodZone(_selectedNeighborhood);
			InstanceBehavior<UIs>.Instance.mapFilters.ToggleFilter(_selectedNeighborhood, isOn: true);
			Transform parent = InstanceBehavior<GameManager>.Instance.citymapCamera.transform.parent;
			float y = parent.position.y;
			Vector3 position = neighbourhoodZone.zoneRenderer.transform.position;
			position.x += neighbourhoodZone.zoneRenderer.transform.localScale.x;
			position.z -= neighbourhoodZone.zoneRenderer.transform.localScale.z;
			position.y = y;
			if (!CityMap.IsOpen)
			{
				InstanceBehavior<CityManager>.Instance.cityMap.Toggle(position);
				return;
			}
			parent.position = position;
			InstanceBehavior<CityManager>.Instance.cityMap.cityMapCam.ForceUpdateCameraPosition();
		}
	}
}
