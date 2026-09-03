using BaTable;
using Extensions;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;

namespace UI.Smartphone.Apps.EconoView;

public class EconoViewLastTransactionCellView : BaTableCellView<EconoViewLastTransactionModel>
{
	[SerializeField]
	private TextLocalizationComponent label;

	[SerializeField]
	private TMP_Text price;

	[SerializeField]
	private TextLocalizationComponent day;

	public override void SetData(EconoViewLastTransactionModel data)
	{
		label.SetData(data.labelData);
		price.text = data.amount.ToShortCurrencyFormat();
		day.SetData(LanguageChangeEventDataHolder.Create("common_day_format", new
		{
			day = "common_day",
			dayNumber = data.day
		}));
		price.color = ((data.amount > 0f) ? InstanceBehavior<GlobalReferences>.Instance.colors.green : InstanceBehavior<GlobalReferences>.Instance.colors.red);
	}
}
