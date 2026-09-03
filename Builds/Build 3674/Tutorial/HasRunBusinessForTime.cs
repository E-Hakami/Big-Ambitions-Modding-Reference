using System.Linq;
using Entities;
using HGAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasRunBusinessForTime")]
public class HasRunBusinessForTime : QuestRequirement
{
	[FormerlySerializedAs("businessType")]
	[AutocompleteDropdown("BusinessTypes")]
	public string businessTypeName;

	[Range(1f, 60f)]
	public int numberOfDays;

	public override bool CheckIfCompleted()
	{
		return SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.RentedByPlayer && (businessTypeName == "ba:businesstype_empty" || x.businessTypeName == businessTypeName)).Any((BuildingRegistration x) => SaveGameManager.Current.financialSummaries.Count((FinancialSummary y) => y.businessIncomeStatements.Any((FinancialSummary.BusinessIncomeStatement z) => z.Address == x.Address && z.Sales.Count > 0)) >= numberOfDays);
	}
}
