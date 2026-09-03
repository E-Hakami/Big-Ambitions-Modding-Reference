using System.Globalization;
using BaTable;
using Buildings.Office.Headquarters;
using Extensions;
using JimmysUnityUtilities;
using Localizor.LanguageChangeEvent;
using UI.Components;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.PricingManagers;

public class PricingManagerProductCellView : BaTableCellView<PricingManagerProductModel>
{
	private const string UnknownPriceText = "-";

	[SerializeField]
	private TextLocalizationComponent productName;

	[SerializeField]
	private GameObject inUseTooltip;

	[SerializeField]
	private TextLocalizationComponent currentPrice;

	[SerializeField]
	private TextLocalizationComponent suggestedPrice;

	[SerializeField]
	private TextLocalizationComponent rivalsPrice;

	[SerializeField]
	private UI.Components.InputField priceInput;

	[SerializeField]
	private Button plusButton;

	[SerializeField]
	private Button minusButton;

	private PricingManagerProductModel _data;

	private void Start()
	{
		priceInput.tmpInputField.onEndEdit.AddListener(delegate
		{
			OnPriceEdited();
		});
		plusButton.onClick.AddListener(delegate
		{
			OnPriceStepped(0.1f);
		});
		minusButton.onClick.AddListener(delegate
		{
			OnPriceStepped(-0.1f);
		});
	}

	public override void SetData(PricingManagerProductModel data)
	{
		_data = data;
		productName.Key = data.Suggestion.itemName;
		inUseTooltip.SetActive(data.Suggestion.isPlayerSelling);
		suggestedPrice.SetValue(GetSuggestedText(data), clearKey: true);
		rivalsPrice.SetValue(GetRivalsText(data.Suggestion), clearKey: true);
		RefreshPriceFields();
	}

	private void RefreshPriceFields()
	{
		bool flag = _data.Plan.TryGetUniformPrice(_data.Suggestion.itemName, out var price);
		currentPrice.SetValue(flag ? price.ToCurrencyFormat() : "-", clearKey: true);
		priceInput.SetText(flag ? FormatPrice(price) : "-", notify: false);
	}

	private static string GetSuggestedText(PricingManagerProductModel data)
	{
		float suggestedMin = data.Suggestion.suggestedMin;
		float suggestedMax = data.Suggestion.suggestedMax;
		if (!Mathf.Approximately(suggestedMin, suggestedMax))
		{
			return suggestedMin.ToCurrencyFormat() + " - " + suggestedMax.ToCurrencyFormat();
		}
		return suggestedMax.ToCurrencyFormat();
	}

	private static string GetRivalsText(PriceSuggestion suggestion)
	{
		if (!(suggestion.rivalReferencePrice > 0f))
		{
			return "-";
		}
		return suggestion.rivalReferencePrice.ToCurrencyFormat();
	}

	private void OnPriceEdited()
	{
		string rawValue = priceInput.GetRawValue();
		float result;
		if (rawValue.IsNullOrEmpty())
		{
			RefreshPriceFields();
		}
		else if (!float.TryParse(rawValue, NumberStyles.Float, CultureHelper.CultureInfo, out result))
		{
			Notifications.ShowError("common_notification_invalid_amount");
			RefreshPriceFields();
		}
		else
		{
			ApplyPrice(result);
		}
	}

	private void OnPriceStepped(float step)
	{
		if (!float.TryParse(priceInput.GetRawValue(), NumberStyles.Float, CultureHelper.CultureInfo, out var result) && !_data.Plan.TryGetUniformPrice(_data.Suggestion.itemName, out result))
		{
			result = ItemHelper.GetDefaultMarketPrice(_data.Suggestion.itemName);
		}
		ApplyPrice(Mathf.Round((result + step) * 100f) / 100f);
	}

	private void ApplyPrice(float price)
	{
		price = Mathf.Clamp(price, 0f, 10000f);
		priceInput.SetText(FormatPrice(price), notify: false);
		currentPrice.SetValue(price.ToCurrencyFormat(), clearKey: true);
		_data.Plan.ApplyManualPrice(_data.Suggestion.itemName, price);
		SaveGameManager.MarkChange();
	}

	private static string FormatPrice(float price)
	{
		return price.ToString("N2", CultureHelper.CultureInfo);
	}
}
