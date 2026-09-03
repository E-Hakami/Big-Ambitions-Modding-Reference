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

[CreateAssetMenu(fileName = "AssignBusinessMassAction", menuName = "BigAmbitions/Employee Mass Actions/Assign Business")]
public class AssignBusinessMassAction : EmployeeMassAction
{
	public override void Perform()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.massActionsUI.ShowMassActionOptionsPanel("myemployees_mass_action_assign_business_title", GetBusinessOptions(), AssignBusiness);
	}

	private static void AssignBusiness(int index)
	{
		if (index == -1)
		{
			Notifications.ShowError("common_no_option_selected");
			return;
		}
		Address newAddress = null;
		string action;
		if (index == 0)
		{
			action = "myemployees_mass_action_assign_business_unassign_confirm";
		}
		else
		{
			BuildingRegistration buildingRegistration = BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilter).ElementAt(index - 1);
			newAddress = buildingRegistration.Address;
			action = "myemployees_mass_action_assign_business_confirm".Localize(new
			{
				businessName = buildingRegistration.BusinessName
			}).ToString();
		}
		LanguageChangeEventDataHolder bodyData = "myemployees_mass_action_confirm".Localize(new
		{
			employeesAffected = MyEmployeesMassActionsUI.massActionSelectedEmployees.Count,
			action = action
		});
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			BuildingRegistration buildingRegistration2 = BuildingHelper.GetBuildingRegistration(newAddress);
			List<string> list = new List<string>();
			if (buildingRegistration2 != null)
			{
				BusinessType data = BusinessTypeHelper.GetData(buildingRegistration2);
				list = data.employeePrimarySkills.ToList();
				BuildingTypeData data2 = BuildingTypeHelper.GetData(buildingRegistration2);
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
			}
			foreach (EmployeeInstance employee in MyEmployeesMassActionsUI.massActionSelectedEmployees)
			{
				if (!(employee.assignedAddress == newAddress))
				{
					if (employee.IsTraining)
					{
						Dictionary<string, string> notificationData = new Dictionary<string, string> { 
						{
							"employeeName",
							employee.characterData.name
						} };
						Notifications.Show(NotificationType.Error, "myemployees_mass_action_cant_assign_business_to_training_employee", notificationData);
					}
					else if (buildingRegistration2 != null && !list.Intersect(employee.characterData.skills.Select((Skill x) => x.name)).Any())
					{
						Dictionary<string, string> notificationData2 = new Dictionary<string, string>
						{
							{
								"employeeName",
								employee.characterData.name
							},
							{ "businessName", buildingRegistration2.BusinessName }
						};
						Notifications.Show(NotificationType.Error, "myemployees_mass_action_employee_has_no_valid_skills", notificationData2);
					}
					else
					{
						Address assignedAddress = employee.assignedAddress;
						if (!employee.IsCandidate)
						{
							EmployeeHelper.UnassignEmployeeFromAllWorkshifts(employee);
							CustomerDemandHelper.ReloadCachedFulfilled(newAddress);
							CustomerDemandHelper.ReloadCachedFulfilled(assignedAddress);
							employee.AddTodoTask((!employee.IsAssignedToAnyBusiness()) ? TodoTaskType.EmployeeUnassigned : TodoTaskType.EmployeeIdle);
						}
						employee.assignedAddress = newAddress;
						EmployeeModel employeeModel = InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.employeeScrollerController.data.FirstOrDefault((EmployeeModel x) => x.employeeInstance == employee);
						if (employeeModel != null)
						{
							employeeModel.task = employee.GetCurrentTask();
							employeeModel.currentBusinessName = buildingRegistration2?.BusinessName;
						}
					}
				}
			}
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.massActionsUI.OnMassActionPerformed(needsReloadingData: false, needsReorderingData: true);
		});
	}

	private static List<string> GetBusinessOptions()
	{
		List<BuildingRegistration> playerBuildingRegistrations = BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilter);
		List<string> list = new List<string>();
		list.Add("common_unassign".GetLocalization());
		list.AddRange(playerBuildingRegistrations.Select((BuildingRegistration business) => business.BusinessName));
		return list;
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
