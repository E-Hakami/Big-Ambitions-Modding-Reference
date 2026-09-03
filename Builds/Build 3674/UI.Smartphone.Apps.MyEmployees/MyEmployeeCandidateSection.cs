using Buildings.Office.Headquarters;
using Entities;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;

namespace UI.Smartphone.Apps.MyEmployees;

public class MyEmployeeCandidateSection : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent timeInfoLabel;

	[SerializeField]
	private TMP_Text candidateSourceType;

	[SerializeField]
	private TMP_Text candidateSourceName;

	public void UpdateUI(EmployeeInstance employeeInstance)
	{
		string candidateExpireDuration = GetCandidateExpireDuration(employeeInstance);
		timeInfoLabel.SetValue("myemployees_expiresin".GetLocalization() + ": " + candidateExpireDuration, clearKey: true);
		SetActive(TrySetCandidateSource(employeeInstance));
	}

	public void SetActive(bool active)
	{
		base.gameObject.SetActive(active);
	}

	private bool TrySetCandidateSource(EmployeeInstance employeeInstance)
	{
		CandidateInfo candidateInfo = employeeInstance.candidateInfo;
		if (candidateInfo == null)
		{
			return false;
		}
		if (candidateInfo.fromJobBoard)
		{
			candidateSourceType.text = "ba:itemname_jobboard".GetLocalization() + ":";
			candidateSourceName.text = BuildingHelper.GetBuildingRegistration(candidateInfo.sourceAddress)?.BusinessName ?? "";
			return true;
		}
		if (!string.IsNullOrEmpty(candidateInfo.sourceHeadhunterId))
		{
			string text = HeadhunterHelper.GetHeadhunterPlanById(candidateInfo.sourceHeadhunterId)?.HeadhunterInstance?.characterData?.name ?? "common_unassigned".GetLocalization();
			candidateSourceType.text = "ba:skill_headhunter".GetLocalization() + ":";
			candidateSourceName.text = text;
			return true;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(candidateInfo.sourceAddress);
		if (buildingRegistration != null)
		{
			candidateSourceType.text = "ba:businesstype_recruitmentagency".GetLocalization() + ":";
			candidateSourceName.text = buildingRegistration.BusinessName;
			return true;
		}
		return false;
	}

	private static string GetCandidateExpireDuration(EmployeeInstance employeeInstance)
	{
		if (employeeInstance.candidateInfo.hoursUntilExpiring < 24)
		{
			return "common_hours".Localize(new
			{
				hours = employeeInstance.candidateInfo.hoursUntilExpiring
			}).ToString();
		}
		int value = Mathf.RoundToInt((float)employeeInstance.candidateInfo.hoursUntilExpiring / 24f);
		return "common_days".Localize(new { value }).ToString();
	}
}
