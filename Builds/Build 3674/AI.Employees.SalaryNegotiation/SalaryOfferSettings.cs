using UI.Dialog;
using UnityEngine;

namespace AI.Employees.SalaryNegotiation;

public class SalaryOfferSettings : MonoBehaviour
{
	[SerializeField]
	private PlayerOfferSettings hourlyWageOfferSettings;

	[SerializeField]
	private PlayerOfferSettings signingBonusOfferSettings;

	private void OnEnable()
	{
		DialogController current = DialogController.current;
		hourlyWageOfferSettings.ConfigureKeyboardInput(current.ConfirmCurrentEntry, autoFocus: false);
		signingBonusOfferSettings.ConfigureKeyboardInput(current.ConfirmCurrentEntry, autoFocus: false);
	}

	public void SetInitialAmount(CandidateSalaryOffer offer, int maxSigningBonus)
	{
		hourlyWageOfferSettings.SetInitialAmount(offer.hourlyWage);
		signingBonusOfferSettings.SetInitialAmount(offer.signingBonus);
		signingBonusOfferSettings.SetMaxNumeralAmount(maxSigningBonus);
		hourlyWageOfferSettings.FocusInput();
	}

	public bool GetAmounts(ref CandidateSalaryOffer candidateSalaryOffer)
	{
		if (hourlyWageOfferSettings.GetAmount(ref candidateSalaryOffer.hourlyWage))
		{
			return signingBonusOfferSettings.GetAmount(ref candidateSalaryOffer.signingBonus);
		}
		return false;
	}
}
