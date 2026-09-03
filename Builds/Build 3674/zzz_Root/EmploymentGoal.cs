using HGAttributes;
using Helpers;
using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/EmploymentGoal")]
public class EmploymentGoal : IntBaseGoal
{
	public bool requireSkill;

	[ShowIf("requireSkill")]
	[AutocompleteDropdown("Skills")]
	public string skill;

	public bool requiresToBeScheduled;

	public override LanguageChangeEventDataHolder GetTitle()
	{
		LanguageChangeEventDataHolder result = base.GetTitle();
		result.Arguments = new
		{
			amount = amount,
			minimumEmployees = amount,
			skill = skill
		};
		return result;
	}

	protected override int GetValue()
	{
		return EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			excludeBeingReplaced = true,
			isAssignedToAnyWorkShift = requiresToBeScheduled,
			withSkills = ((!requireSkill) ? null : new string[1] { skill })
		}).Count;
	}
}
