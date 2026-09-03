using Extensions;
using Helpers;
using UI.Smartphone.Apps.EconoView;
using UnityEngine;

public class EconoViewLoans : MonoBehaviour
{
	[SerializeField]
	private Transform loanEntry;

	[SerializeField]
	private GameObject noLoansLabel;

	[SerializeField]
	private GameObject headers;

	private EconoViewOverview _econoViewOverview;

	private void Start()
	{
		_econoViewOverview = GetComponentInParent<EconoViewOverview>();
	}

	public void Init()
	{
		loanEntry.ResetTemplate();
		bool flag = LoanHelper.HasLoans();
		noLoansLabel.gameObject.SetActive(!flag);
		headers.gameObject.SetActive(flag);
		foreach (Loan loan in SaveGameManager.Current.Loans)
		{
			LoanEntryUi component = Object.Instantiate(loanEntry, loanEntry.parent).GetComponent<LoanEntryUi>();
			component.Setup(loan, OnLoanChanged);
			component.gameObject.SetActive(value: true);
		}
	}

	private void OnLoanChanged()
	{
		Init();
		_econoViewOverview.RefreshTransactions();
	}
}
