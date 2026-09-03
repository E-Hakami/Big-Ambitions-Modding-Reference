using Buildings.Indoors.InteriorDesign;
using Entities;
using Helpers;
using UnityEngine;

namespace Buildings.BuildingTypes.Shared.BusinessRequirement;

[CreateAssetMenu(menuName = "BigAmbitions/BusinessRequirement/PaidLicensingFees")]
public class PaidLicensingFees : BusinessRequirement
{
	public override TodoTaskType GetTodoTaskType()
	{
		return TodoTaskType.UnpaidLicensingFees;
	}

	protected override bool MustInputLocalizeKey()
	{
		return false;
	}

	public override string GetLocalizeKey()
	{
		return "businessrequirement_paidlicensingfees";
	}

	protected override bool MustInputTodoTaskItemName()
	{
		return false;
	}

	public override bool IsRequirementMet(BuildingRegistration registration)
	{
		if (InteriorDesignerHelper.BlueprintCreatorMode)
		{
			return true;
		}
		if (BusinessHelper.IsBusinessOpen(registration))
		{
			return SaveGameManager.Current.paidLicensingFeesToday.Exists(((Address, string) pair) => pair.Item1 == registration.Address);
		}
		return true;
	}
}
