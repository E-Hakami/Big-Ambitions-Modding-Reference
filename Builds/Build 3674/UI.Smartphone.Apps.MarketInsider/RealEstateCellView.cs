using BaTable;
using Extensions;
using Localizor.LanguageChangeEvent;
using Streets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.MarketInsider;

public class RealEstateCellView : BaTableCellView<RealEstateCellView.RealEstateModel>
{
	public class RealEstateModel
	{
		public Address Address;

		public string BuildingType;

		public int TotalSize;

		public float EstimatedValue;

		public float Price;

		public string Neighborhood;

		public string FormattedAddress;

		public bool IsForSale;

		public RealEstateModel(Address address, string buildingType, int totalSize, float estimatedValue, float price, string neighborhood)
		{
			Address = address;
			BuildingType = buildingType;
			TotalSize = totalSize;
			EstimatedValue = estimatedValue;
			Price = price;
			Neighborhood = neighborhood;
			FormattedAddress = address.ToFormattedString();
		}
	}

	public TextLocalizationComponent address;

	public TextLocalizationComponent neighborhood;

	public TextLocalizationComponent buildingType;

	public TextMeshProUGUI totalSize;

	public TextMeshProUGUI estimatedValue;

	public TextMeshProUGUI price;

	[SerializeField]
	private Button button;

	public override void SetData(RealEstateModel data)
	{
		address.SetValue(data.FormattedAddress, clearKey: true);
		if ((bool)neighborhood)
		{
			neighborhood.Key = data.Neighborhood;
		}
		buildingType.Key = data.BuildingType;
		totalSize.text = data.TotalSize.ToFormattedArea();
		estimatedValue.text = data.EstimatedValue.ToShortCurrencyFormat();
		price.text = ((data.Price == 0f) ? "-" : data.Price.ToShortCurrencyFormat());
		button.onClick.RemoveAllListeners();
		button.onClick.AddListener(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(data.Address, "Presentation");
		});
	}
}
