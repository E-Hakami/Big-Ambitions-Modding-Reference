using System;
using Buildings.BuildingTypes.Shared;
using Localizor;

namespace UI.Smartphone.Apps.BizMan;

public class RivalEmployeeModel
{
	public readonly string EmployeeName;

	public (string, float) PrimarySkill;

	public readonly AiBusinessEmployeeData EmployeeData;

	public readonly Action<AiBusinessEmployeeData> OnNegotiate;

	public RivalEmployeeModel(AiBusinessEmployeeData employeeData, Action<AiBusinessEmployeeData> onNegotiate)
	{
		EmployeeData = employeeData;
		OnNegotiate = onNegotiate;
		EmployeeName = employeeData.GetFullName();
		PrimarySkill = (employeeData.primarySkillName.GetLocalization(), employeeData.primarySkillValue);
	}
}
