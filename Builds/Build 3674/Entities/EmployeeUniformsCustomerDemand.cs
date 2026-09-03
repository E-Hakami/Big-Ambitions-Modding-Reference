using System.Collections.Generic;
using BigAmbitions.Items;
using Helpers;
using UnityEngine;

namespace Entities;

[CreateAssetMenu(fileName = "EmployeeUniformsCustomerDemand", menuName = "BigAmbitions/CustomerDemands/EmployeeUniforms")]
public class EmployeeUniformsCustomerDemand : CustomerDemand
{
	private static readonly HashSet<string> SkillNamesToCheckUniforms = new HashSet<string>();

	public override bool Fulfilled(BuildingRegistration registration, HashSet<Item> items)
	{
		List<EmployeeInstance> employeeInstances = EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			withAssignedAddress = registration.Address
		});
		if (employeeInstances.Count == 0)
		{
			return true;
		}
		SkillNamesToCheckUniforms.Clear();
		foreach (EmployeeInstance item in employeeInstances)
		{
			WorkShift workShiftAssignedInThisMoment = item.GetWorkShiftAssignedInThisMoment(specificShiftType: true);
			if (workShiftAssignedInThisMoment == null)
			{
				continue;
			}
			string itemInstanceId = workShiftAssignedInThisMoment.itemInstanceId;
			if (!registration.itemInstances.TryGetValue(itemInstanceId, out var value))
			{
				continue;
			}
			Item byName = ItemsGetter.GetByName(value.itemName);
			if (byName == null)
			{
				continue;
			}
			string[] suitableSkills = byName.suitableSkills;
			foreach (string text in suitableSkills)
			{
				if (item.HasSkill(text))
				{
					SkillNamesToCheckUniforms.Add(text);
					break;
				}
			}
		}
		foreach (string skillNamesToCheckUniform in SkillNamesToCheckUniforms)
		{
			if (!registration.uniformsBySkill.ContainsKey(skillNamesToCheckUniform))
			{
				return false;
			}
		}
		return true;
	}
}
