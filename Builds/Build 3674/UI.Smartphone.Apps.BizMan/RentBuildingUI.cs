using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan;

public class RentBuildingUI : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent rentPerDayLabel;

	[SerializeField]
	private TMP_Text depositLabel;

	[SerializeField]
	private TMP_Text electricalAppliancesLabel;

	[SerializeField]
	private TMP_Text firstDayRentLabel;

	[SerializeField]
	private TextLocalizationComponent totalInitialPaymentLabel;

	[SerializeField]
	private GameObject availableForRentGameObject;

	[SerializeField]
	private GameObject residentialRentedByPlayerGameObject;

	[SerializeField]
	private GameObject nonResidentialRentedByPlayerGameObject;

	[SerializeField]
	private GameObject hamptonsHouseOwnedByPlayerGameObject;

	public void Show(BuildingRegistration buildingRegistration)
	{
		bool flag = buildingRegistration.BuildingCached.BuildingType == "ba:buildingtype_residential";
		bool flag2 = buildingRegistration.BuildingCached.IsHamptonsHouse();
		if (buildingRegistration.RentedByPlayer)
		{
			availableForRentGameObject.SetActive(value: false);
			residentialRentedByPlayerGameObject.SetActive(flag && !flag2);
			hamptonsHouseOwnedByPlayerGameObject.SetActive(flag2);
			nonResidentialRentedByPlayerGameObject.SetActive(!flag);
		}
		else
		{
			availableForRentGameObject.SetActive(value: true);
			residentialRentedByPlayerGameObject.SetActive(value: false);
			nonResidentialRentedByPlayerGameObject.SetActive(value: false);
			hamptonsHouseOwnedByPlayerGameObject.SetActive(value: false);
			int num = ((!buildingRegistration.BuildingOwnedByPlayer) ? buildingRegistration.BuildingCached.GetBuildingDailyMarketRent() : 0);
			int num2 = BuildingHelper.CalculateDeposit(buildingRegistration, num);
			int num3 = BuildingHelper.CalculateDefaultLayoutPrice(buildingRegistration.Address);
			float num4 = ((flag && !TutorialHelper.HasCompletedObjective("tutorial_quest_get_some_sleep_objective_4")) ? 0f : ((float)num));
			rentPerDayLabel.Arguments = new
			{
				dailyRent = num.ToShortCurrencyFormat()
			};
			depositLabel.text = num2.ToShortCurrencyFormat();
			electricalAppliancesLabel.text = num3.ToShortCurrencyFormat();
			firstDayRentLabel.text = num4.ToShortCurrencyFormat();
			totalInitialPaymentLabel.Arguments = new
			{
				amount = ((float)(num2 + num3) + num4).ToShortCurrencyFormat()
			};
		}
		base.gameObject.SetActive(value: true);
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
	}
}
