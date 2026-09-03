using System;
using UnityEngine;

[Serializable]
public class GameVariables
{
	public Difficulty difficulty = Difficulty.Normal;

	public int startingAge = 18;

	public bool disableAging;

	public bool disableEnergy;

	public bool disableHappiness;

	public bool allCoursesUnlocked;

	public int startingMoney = 4200;

	public int taxPercentage = 10;

	public int daysPerYear = 60;

	public float marketPriceMultiplier = 1f;

	public float employeeHourlySalaryMultiplier = 1f;

	public float bankInterestMultiplier = 1f;

	public bool tutorialEnabled = true;

	public float rivalsDifficultyMultiplier = 1f;

	public bool disableVehicleDamage;

	public bool disableVehicleFuel;

	public bool allContactsUnlocked;

	public float baseCustomerPromotionMultiplier = 0.5f;

	public float wholesaleUrgentFeeMultiplier = 0.2f;

	public float importerUrgentFeeMultiplier = 0.75f;

	public bool disableWholesaleAndImportLimits;

	public bool allProductsAvailableFromImporters;

	public float exportMultiplier = 0.65f;

	public float sellingMultiplier = 0.75f;

	public bool EqualsValues(object obj)
	{
		if (EqualsFlexibleValues(obj))
		{
			return EqualsRigidValues(obj);
		}
		return false;
	}

	public bool EqualsFlexibleValues(object obj)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}
		GameVariables gameVariables = (GameVariables)obj;
		if (disableAging == gameVariables.disableAging && disableEnergy == gameVariables.disableEnergy && disableHappiness == gameVariables.disableHappiness && allCoursesUnlocked == gameVariables.allCoursesUnlocked && disableVehicleDamage == gameVariables.disableVehicleDamage && disableVehicleFuel == gameVariables.disableVehicleFuel && allContactsUnlocked == gameVariables.allContactsUnlocked && taxPercentage == gameVariables.taxPercentage && Mathf.Approximately(marketPriceMultiplier, gameVariables.marketPriceMultiplier) && Mathf.Approximately(employeeHourlySalaryMultiplier, gameVariables.employeeHourlySalaryMultiplier) && Mathf.Approximately(bankInterestMultiplier, gameVariables.bankInterestMultiplier) && Mathf.Approximately(rivalsDifficultyMultiplier, gameVariables.rivalsDifficultyMultiplier) && Mathf.Approximately(baseCustomerPromotionMultiplier, gameVariables.baseCustomerPromotionMultiplier) && Mathf.Approximately(wholesaleUrgentFeeMultiplier, gameVariables.wholesaleUrgentFeeMultiplier) && Mathf.Approximately(importerUrgentFeeMultiplier, gameVariables.importerUrgentFeeMultiplier) && Mathf.Approximately(sellingMultiplier, gameVariables.sellingMultiplier) && disableWholesaleAndImportLimits == gameVariables.disableWholesaleAndImportLimits)
		{
			return allProductsAvailableFromImporters == gameVariables.allProductsAvailableFromImporters;
		}
		return false;
	}

	public bool EqualsRigidValues(object obj)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}
		GameVariables gameVariables = (GameVariables)obj;
		if (startingAge == gameVariables.startingAge && startingMoney == gameVariables.startingMoney)
		{
			return daysPerYear == gameVariables.daysPerYear;
		}
		return false;
	}
}
