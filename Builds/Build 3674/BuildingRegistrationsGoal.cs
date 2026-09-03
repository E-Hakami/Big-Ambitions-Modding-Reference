using System.Collections.Generic;
using Entities;
using Extensions;
using HGAttributes;
using Helpers;
using Localizor.LanguageChangeEvent;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/BuildingRegistrationsGoal")]
public class BuildingRegistrationsGoal : IntBaseGoal
{
	[Tooltip("If BuildingType is Special it counts all buildings the player owns")]
	[AutocompleteDropdown("BuildingTypes")]
	public string buildingType;

	public int minimumProfit;

	public int minimumSquareMeters;

	protected override int GetValue()
	{
		return SaveGameManager.Current.BuildingRegistrations.CountWhere(RegistrationMeetsRequirements);
	}

	private bool RegistrationMeetsRequirements(BuildingRegistration registration)
	{
		if (!registration.RentedByPlayer)
		{
			return false;
		}
		Address address = registration.Address;
		if (buildingType != "ba:buildingtype_special" && BuildingHelper.GetBuilding(address).BuildingType != buildingType)
		{
			return false;
		}
		if (!MeetsMinimumProfit(address))
		{
			return false;
		}
		if (minimumSquareMeters != 0)
		{
			return BuildingHelper.GetBuildingSquareMeters(address) >= minimumSquareMeters;
		}
		return true;
	}

	private bool MeetsMinimumProfit(Address address)
	{
		if (minimumProfit == 0)
		{
			return true;
		}
		List<FinancialSummary> financialSummaries = SaveGameManager.Current.financialSummaries;
		if (financialSummaries.Count == 0)
		{
			return false;
		}
		List<FinancialSummary.BusinessIncomeStatement> businessIncomeStatements = financialSummaries[financialSummaries.Count - 1].businessIncomeStatements;
		for (int i = 0; i < businessIncomeStatements.Count; i++)
		{
			FinancialSummary.BusinessIncomeStatement businessIncomeStatement = businessIncomeStatements[i];
			if (businessIncomeStatement.Address == address)
			{
				return businessIncomeStatement.TotalProfit >= (float)minimumProfit;
			}
		}
		return false;
	}

	public override LanguageChangeEventDataHolder GetTitle()
	{
		LanguageChangeEventDataHolder result = base.GetTitle();
		result.Arguments = new
		{
			amount = amount,
			size = minimumSquareMeters.ToFormattedArea(),
			profit = minimumProfit,
			type = buildingType
		};
		return result;
	}
}
