using Buildings;
using UnityEngine;

namespace SpecialServices.Bank;

[CreateAssetMenu(menuName = "BigAmbitions/SpecialService/BankSettings")]
public class BankSettings : SpecialServiceSettings
{
	public float maxTotalLoanAmount;

	public int annualInterestRate;

	public int yearsToPayLoan;

	public bool allowSideQuestEmergencyLoan;
}
