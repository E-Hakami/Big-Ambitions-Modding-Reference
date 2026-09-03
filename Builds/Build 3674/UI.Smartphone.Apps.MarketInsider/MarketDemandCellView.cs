using BaTable;
using BigAmbitions.Items;
using Extensions;
using Helpers;
using JetBrains.Annotations;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;

namespace UI.Smartphone.Apps.MarketInsider;

public class MarketDemandCellView : BaTableCellView<MarketDemandCellView.DemandModel>
{
	public class DemandModel
	{
		public string ItemName;

		[UsedImplicitly]
		public string ProductName;

		public int Demand;

		public float LowestMarketPrice;

		public float ImportPriceIndex;

		public int Providers;

		public int demandChangeByEvents;

		public DemandModel(string itemName, int demand, float lowestMarketPrice, float importPriceIndex, int providers, int demandChangeByEvents)
		{
			ItemName = itemName;
			ProductName = itemName.GetLocalization();
			Demand = demand;
			LowestMarketPrice = lowestMarketPrice;
			ImportPriceIndex = importPriceIndex;
			Providers = providers;
			this.demandChangeByEvents = demandChangeByEvents;
		}
	}

	[SerializeField]
	private GameObject demandIncreaseIcon;

	[SerializeField]
	private GameObject demandDecreaseIcon;

	public TextLocalizationComponent productName;

	public TextMeshProUGUI demand;

	public TextMeshProUGUI lowestMarketPrice;

	public TextMeshProUGUI importPriceIndex;

	public TextLocalizationComponent providers;

	public override void SetData(DemandModel data)
	{
		Item byName = ItemsGetter.GetByName(data.ItemName);
		productName.SetData(LocalizationHelper.GetItemLabel(data.ItemName));
		demand.text = data.Demand + "%";
		lowestMarketPrice.text = ((data.LowestMarketPrice < 0f) ? "-" : data.LowestMarketPrice.ToCurrencyFormat());
		importPriceIndex.text = (((byName.type & ItemType.ServiceProduct) != 0) ? "-" : data.ImportPriceIndex.ToString("F1"));
		providers.SetData(LanguageChangeEventDataHolder.Create("common_number_of_businesses", new
		{
			amount = data.Providers
		}));
		demandIncreaseIcon.SetActive(data.demandChangeByEvents > 0);
		demandDecreaseIcon.SetActive(data.demandChangeByEvents < 0);
	}
}
