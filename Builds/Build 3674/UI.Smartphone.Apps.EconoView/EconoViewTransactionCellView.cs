using BaTable;
using Extensions;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;

namespace UI.Smartphone.Apps.EconoView;

public class EconoViewTransactionCellView : BaTableCellView<TransactionModel>
{
	[SerializeField]
	private TextLocalizationComponent label;

	[SerializeField]
	private TextLocalizationComponent day;

	[SerializeField]
	private TextLocalizationComponent type;

	[SerializeField]
	private TMP_Text amount;

	[SerializeField]
	private TMP_Text balance;

	public override void SetData(TransactionModel data)
	{
		label.SetData(data.LabelData);
		day.Arguments = new
		{
			number = data.Day
		};
		type.Key = data.Type + "_label";
		amount.text = data.Amount.ToCurrencyFormat();
		if (data.Amount < 0f)
		{
			amount.color = InstanceBehavior<GlobalReferences>.Instance.colors.red;
		}
		else
		{
			amount.color = InstanceBehavior<GlobalReferences>.Instance.colors.green;
			amount.text = "+" + amount.text;
		}
		balance.text = data.Balance.ToShortCurrencyFormat();
	}
}
