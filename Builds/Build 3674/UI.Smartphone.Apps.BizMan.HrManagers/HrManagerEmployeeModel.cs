using Entities;
using Entities.Employee.JobDemands.Requirements;
using Helpers;
using UI.Smartphone.Apps.Shared;

namespace UI.Smartphone.Apps.BizMan.HrManagers;

public sealed class HrManagerEmployeeModel : BaseEmployeeModel
{
	public string businessName;

	public string insuranceDemandText;

	public int insuranceDemand;

	public bool assigned;

	public HrManagerEmployeeModel(EmployeeInstance instance, int insuranceDemand, bool assigned)
		: base(instance.id, instance.isBeingReplaced ? EmployeeHelper.GetAwaitingReplacementText() : instance.characterData.name, instance.hourlyWage, instance.characterData.skills[0].name, instance.characterData.skills[0].GetRoundedValue(), instance.satisfaction, instance.demands)
	{
		demands = instance.demands;
		businessName = (instance.IsAssignedToAnyBusiness() ? BuildingHelper.GetBuildingRegistration(instance.assignedAddress).BusinessName : "");
		insuranceDemandText = instance.GetDemandOfTypeLocalized<HasHealthInsurance>();
		this.insuranceDemand = insuranceDemand;
		this.assigned = assigned;
	}
}
