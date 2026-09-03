using System;
using Helpers;
using Localizor;

namespace Entities;

[Serializable]
public class CandidateInfo
{
	public Address sourceAddress;

	public string sourceHeadhunterId;

	public bool fromJobBoard;

	public int hoursUntilExpiring;

	public string GetSource()
	{
		if (fromJobBoard)
		{
			return "ba:itemname_jobboard".GetLocalization();
		}
		if (string.IsNullOrEmpty(sourceHeadhunterId))
		{
			return BuildingHelper.GetBuildingRegistration(sourceAddress)?.BusinessName ?? "-";
		}
		EmployeeInstance employeeById = EmployeeHelper.GetEmployeeById(sourceHeadhunterId, showError: false);
		if (employeeById == null)
		{
			return "myemployees_candidates_source_headhunter_default".GetLocalization();
		}
		return "myemployees_candidates_source_headhunter".Localize(new
		{
			headhunterName = employeeById.characterData.name
		}).ToString();
	}
}
