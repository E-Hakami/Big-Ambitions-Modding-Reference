using EnhancedUI.EnhancedScroller;
using Entities;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.MarketInsider;

public class MarketEventCellView : EnhancedScrollerCellView
{
	public class EventModel
	{
		public MarketEventType EventType;

		public int Day;

		public MarketEvent MarketEvent;

		public EventModel(MarketEventType eventType, int day, MarketEvent marketEvent)
		{
			EventType = eventType;
			Day = day;
			MarketEvent = marketEvent;
		}
	}

	public TextLocalizationComponent eventType;

	public TextLocalizationComponent eventTargetName;

	public TextLocalizationComponent day;

	public TextLocalizationComponent eventText;

	public Image boxImage;

	public void SetData(EventModel data)
	{
		string localizeKey = data.EventType.GetLocalizeKey();
		MarketEventInfo marketEventInfo = data.EventType.GetMarketEventInfo();
		SetEventTargetNameText(data, marketEventInfo);
		SetEventText(data, localizeKey);
		eventType.SetData(LanguageChangeEventDataHolder.Create(localizeKey));
		day.SetData(LanguageChangeEventDataHolder.Create("common_day_number", new
		{
			number = data.Day
		}));
		boxImage.sprite = marketEventInfo.boxImage;
	}

	private void SetEventText(EventModel data, string eventTypeLocalizeKey)
	{
		string key = eventTypeLocalizeKey + "_description";
		string neighbourhood = (string.IsNullOrEmpty(data.MarketEvent.neighbourhood) ? "" : data.MarketEvent.neighbourhood.GetLocalization());
		eventText.SetData(key.Localize(new
		{
			rivalName = data.MarketEvent.rivalName,
			businessName = data.MarketEvent.businessName,
			address = data.MarketEvent.address?.ToFormattedString(),
			neighbourhood = neighbourhood,
			itemName = LocalizationHelper.GetItemLabel(data.MarketEvent.itemName),
			source = (data.MarketEvent.GetBuildingRegistration()?.BusinessName ?? ""),
			product = LocalizationHelper.GetItemLabel(data.MarketEvent.itemName),
			durationInDays = data.MarketEvent.durationInDays
		}));
	}

	private void SetEventTargetNameText(EventModel data, MarketEventInfo marketEventInfo)
	{
		if (marketEventInfo.useBusinessTypeAsEventTargetName)
		{
			eventTargetName.Key = data.MarketEvent.businessTypeName;
		}
		else if (marketEventInfo.useItemNameAsEventTargetName)
		{
			eventTargetName.SetData(LocalizationHelper.GetItemLabel(data.MarketEvent.itemName));
		}
	}
}
