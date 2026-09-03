using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Skills;
using BigAmbitions.Tags;
using Buildings;
using Entities;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI.Notification;
using UnityEngine;

namespace UI.Smartphone.Apps.MyEmployees.Types;

[CreateAssetMenu(fileName = "AssignToBusinessAndHireMassAction", menuName = "BigAmbitions/Employee Mass Actions/Assign To Business And Hire")]
public class AssignToBusinessAndHireMassAction : EmployeeMassAction
{
	public override void Perform()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.massActionsUI.ShowMassActionOptionsPanel("myemployees_mass_action_assign_business_and_hire_title", GetBusinessOptions(), HireAndAssignBusiness);
	}

	private static void HireAndAssignBusiness(int index)
	{
		if (index == -1)
		{
			Notifications.ShowError("common_no_option_selected");
			return;
		}
		BuildingRegistration business = BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilter).ElementAt(index);
		string action = "myemployees_mass_action_assign_business_and_hire_confirm".Localize(new
		{
			businessName = business.BusinessName
		}).ToString();
		LanguageChangeEventDataHolder bodyData = "myemployees_mass_action_confirm".Localize(new
		{
			employeesAffected = MyEmployeesMassActionsUI.massActionSelectedEmployees.Count,
			action = action
		});
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			BusinessType data = BusinessTypeHelper.GetData(business);
			List<string> list = data.employeePrimarySkills.ToList();
			BuildingTypeData data2 = BuildingTypeHelper.GetData(business);
			if (data2.NeedsCleaning)
			{
				list.Add("ba:skill_cleaning");
			}
			if (data.HasTag(TagRef.Businesstag.allowtheft))
			{
				list.Add("ba:skill_securityguard");
			}
			if (data2.requiredBuildingSkills.Contains("ba:skill_deliverydriver"))
			{
				list.Add("ba:skill_deliverydriver");
			}
			foreach (EmployeeInstance massActionSelectedEmployee in MyEmployeesMassActionsUI.massActionSelectedEmployees)
			{
				if (!list.Intersect(massActionSelectedEmployee.characterData.skills.Select((Skill x) => x.name)).Any())
				{
					Dictionary<string, string> notificationData = new Dictionary<string, string>
					{
						{
							"employeeName",
							massActionSelectedEmployee.characterData.name
						},
						{ "businessName", business.BusinessName }
					};
					Notifications.Show(NotificationType.Error, "myemployees_mass_action_employee_has_no_valid_skills", notificationData);
				}
				else
				{
					massActionSelectedEmployee.assignedAddress = business.Address;
					EmployeeHelper.HireCandidate(massActionSelectedEmployee);
					InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.candidateScrollerController.RemoveCandidate(massActionSelectedEmployee);
				}
			}
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.massActionsUI.OnMassActionPerformed(needsReloadingData: true);
		});
	}

	private static List<string> GetBusinessOptions()
	{
		return (from business in BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilter)
			select business.BusinessName).ToList();
	}

	private static bool PlayerBuildingFilter(BuildingRegistration buildingRegistration)
	{
		if (buildingRegistration.RentedByPlayer && !string.IsNullOrEmpty(buildingRegistration.BusinessName))
		{
			return buildingRegistration.businessTypeName != "ba:businesstype_empty";
		}
		return false;
	}
}
