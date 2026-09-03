using System.Linq;
using BigAmbitions.Characters.Skills;
using Entities;
using Helpers;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/EmployeeMaxLevelGoal")]
public class EmployeeMaxLevelGoal : FloatBaseGoal
{
	protected override float GetValue()
	{
		return (from x in EmployeeHelper.GetEmployeeInstances().SelectMany((EmployeeInstance x) => x.characterData.skills)
			select x.value).DefaultIfEmpty(0f).Max();
	}
}
