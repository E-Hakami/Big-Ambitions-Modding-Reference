using System;
using Buildings.Indoors.InteriorDesign;
using Extensions;
using TMPro;
using UnityEngine;

namespace UI.InteriorDesigner;

public class CostInfoPanelUI : InfoPanelUI
{
	[SerializeField]
	private TMP_Text playerBalanceText;

	[SerializeField]
	private TMP_Text costDifferenceText;

	[SerializeField]
	private TMP_Text blueprintCostText;

	[Space]
	[SerializeField]
	private GameObject playerBalanceObj;

	[SerializeField]
	private GameObject costDifferenceObj;

	[SerializeField]
	private GameObject blueprintCostObj;

	public override bool ShouldShow()
	{
		return true;
	}

	public override void OnEnterInteriorDesignerMode()
	{
		InteriorDesignerInfoPanelEvents.onPlayerBalanceChanged = (Action<double>)Delegate.Combine(InteriorDesignerInfoPanelEvents.onPlayerBalanceChanged, new Action<double>(UpdatePlayerBalance));
		InteriorDesignerInfoPanelEvents.onCostBalanceChanged = (Action<double>)Delegate.Combine(InteriorDesignerInfoPanelEvents.onCostBalanceChanged, new Action<double>(UpdateTotalCostBalance));
		InteriorDesignerInfoPanelEvents.onBlueprintCostChanged = (Action<double>)Delegate.Combine(InteriorDesignerInfoPanelEvents.onBlueprintCostChanged, new Action<double>(UpdateBlueprintCost));
		if (InteriorDesignerHelper.BlueprintCreatorMode)
		{
			playerBalanceObj.SetActive(value: false);
			costDifferenceObj.SetActive(value: false);
			blueprintCostObj.SetActive(value: true);
			UpdateBlueprintCost(InteriorDesignerController.BlueprintTotalCost);
		}
		else
		{
			playerBalanceObj.SetActive(value: true);
			costDifferenceObj.SetActive(value: true);
			blueprintCostObj.SetActive(value: false);
			UpdatePlayerBalance(SaveGameManager.Current.Money);
			UpdateTotalCostBalance(0.0);
		}
	}

	public override void OnExitInteriorDesignerMode()
	{
		InteriorDesignerInfoPanelEvents.onPlayerBalanceChanged = (Action<double>)Delegate.Remove(InteriorDesignerInfoPanelEvents.onPlayerBalanceChanged, new Action<double>(UpdatePlayerBalance));
		InteriorDesignerInfoPanelEvents.onCostBalanceChanged = (Action<double>)Delegate.Remove(InteriorDesignerInfoPanelEvents.onCostBalanceChanged, new Action<double>(UpdateTotalCostBalance));
		InteriorDesignerInfoPanelEvents.onBlueprintCostChanged = (Action<double>)Delegate.Remove(InteriorDesignerInfoPanelEvents.onBlueprintCostChanged, new Action<double>(UpdateBlueprintCost));
	}

	private void UpdatePlayerBalance(double balance)
	{
		playerBalanceText.text = balance.ToShortCurrencyFormat(abbreviated: true);
	}

	private void UpdateTotalCostBalance(double costBalance)
	{
		costDifferenceText.text = costBalance.ToShortCurrencyFormat();
		costDifferenceText.color = ColoredTextHelper.GetBalanceColor(costBalance);
	}

	private void UpdateBlueprintCost(double blueprintCost)
	{
		blueprintCostText.text = blueprintCost.ToShortCurrencyFormat();
	}
}
