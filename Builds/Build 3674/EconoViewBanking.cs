using Extensions;
using Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EconoViewBanking : MonoBehaviour
{
	private const string LoansTab = "loans";

	private const string InvestmentsTab = "investments";

	private const string TaxesTab = "taxes";

	public EconoViewLoans loansContainer;

	public EconoViewInvestments investmentsContainer;

	public EconoViewTaxes taxesContainer;

	public Button loansButton;

	public Button investmentsButton;

	public Button taxesButton;

	public TMP_Text taxesButtonLabel;

	private void OnEnable()
	{
		if (SaveGameManager.Current != null)
		{
			if (InvestmentFundHelper.HasAnyInvestments() && !LoanHelper.HasLoans())
			{
				SetTab("investments");
			}
			else
			{
				SetTab("loans");
			}
		}
	}

	public void SetTab(string newTab)
	{
		loansContainer.gameObject.SetActive(newTab == "loans");
		investmentsContainer.gameObject.SetActive(newTab == "investments");
		if ((bool)taxesContainer)
		{
			taxesContainer.gameObject.SetActive(newTab == "taxes");
		}
		loansButton.interactable = newTab != "loans";
		investmentsButton.interactable = newTab != "investments";
		if ((bool)taxesButton)
		{
			taxesButton.interactable = newTab != "taxes";
		}
		loansButton.transform.GetLabelByName("Label").color = ((newTab == "loans") ? InstanceBehavior<GlobalReferences>.Instance.colors.black : InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey);
		investmentsButton.transform.GetLabelByName("Label").color = ((newTab == "investments") ? InstanceBehavior<GlobalReferences>.Instance.colors.black : InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey);
		taxesButtonLabel.color = ((newTab == "taxes") ? InstanceBehavior<GlobalReferences>.Instance.colors.black : InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey);
		switch (newTab)
		{
		case "loans":
			loansContainer.Init();
			break;
		case "investments":
			investmentsContainer.Init();
			break;
		case "taxes":
			if ((bool)taxesContainer)
			{
				taxesContainer.Init();
			}
			break;
		}
	}

	public void ShowTaxes()
	{
		SetTab("taxes");
	}
}
