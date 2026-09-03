using System.Collections.Generic;
using BigAmbitions.Characters.Skills;
using BigAmbitions.Tags;
using Buildings;
using Entities;
using Entities.Employee.JobDemands;
using Extensions;
using Items.SpecialItems;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Player.HUD.ItemInfoOverlays;

public class EmployeeOverlay : IOverlay
{
	[Header("Employee")]
	[SerializeField]
	private TMP_Text employeeNameField;

	[SerializeField]
	private TMP_Text demandLabel;

	[SerializeField]
	private TMP_Text satisfactionLabel;

	[SerializeField]
	private Transform demandsTemplate;

	[SerializeField]
	private TMP_Text skillsTemplate;

	[Header("Sprites")]
	[SerializeField]
	private Sprite fulfilled;

	[SerializeField]
	private Sprite notFulfilled;

	[Header("GameObjects")]
	[SerializeField]
	private List<GameObject> employeeInformationObjects;

	public override bool IsValid(EntityController entityController)
	{
		return entityController is EmployeeStationController;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness || InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName == "ba:businesstype_empty")
		{
			return false;
		}
		if (entityController is FactoryAssemblyMachineController factoryAssemblyMachineController)
		{
			return !string.IsNullOrEmpty(factoryAssemblyMachineController.employee?.employeeInstance?.characterData?.name);
		}
		return BuildingTypeHelper.GetData(InstanceBehavior<BuildingManager>.Instance.building).HasTag(TagRef.Buildingtypetag.hasdetailedemployeeoverlay);
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		EmployeeInstance employeeInstance = GetEmployeeInstance(entityController);
		if (string.IsNullOrEmpty(employeeInstance?.characterData?.name))
		{
			foreach (GameObject employeeInformationObject in employeeInformationObjects)
			{
				if (employeeInformationObject.activeSelf)
				{
					employeeInformationObject.SetActive(value: false);
				}
			}
			return;
		}
		foreach (GameObject employeeInformationObject2 in employeeInformationObjects)
		{
			if (!employeeInformationObject2.activeSelf)
			{
				employeeInformationObject2.SetActive(value: true);
			}
		}
		employeeNameField.text = "common_name".Localize().ToString() + ": " + employeeInstance.characterData.name;
		HandleDemand(employeeInstance);
		HandleSkills(employeeInstance.characterData.skills);
		satisfactionLabel.text = ColoredTextHelper.GetSatisfactionText(employeeInstance.satisfaction);
		LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform as RectTransform);
	}

	public void OnManageEmployeeClicked()
	{
		EmployeeInstance employeeInstance = GetEmployeeInstance(linkedController);
		if (employeeInstance != null)
		{
			InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
			InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.MyEmployees);
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.DelayShowEmployee(employeeInstance);
		}
	}

	public void OnManageScheduleClicked()
	{
		InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
		InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.BizMan);
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address, "Schedule");
	}

	private void HandleDemand(EmployeeInstance employee)
	{
		demandsTemplate.ResetTemplate();
		int num = 0;
		foreach (string demand in employee.demands)
		{
			bool flag = JobDemandHelper.GetByName(demand).Fulfilled(employee, null);
			Color32 color = (flag ? InstanceBehavior<GlobalReferences>.Instance.colors.green : InstanceBehavior<GlobalReferences>.Instance.colors.red);
			Transform obj = Object.Instantiate(demandsTemplate, demandsTemplate.parent);
			Transform obj2 = obj.Find("DemandName");
			obj2.GetComponent<TextLocalizationComponent>().Key = demand;
			obj2.GetComponent<TMP_Text>().color = color;
			obj.Find("Image").GetComponent<Image>().sprite = (flag ? fulfilled : notFulfilled);
			if (flag)
			{
				num++;
			}
			obj.gameObject.SetActive(value: true);
		}
		demandLabel.text = ColoredTextHelper.GetDemandText(num, employee.demands.Count);
	}

	private void HandleSkills(List<Skill> skills)
	{
		skillsTemplate.transform.ResetTemplate();
		foreach (Skill skill in skills)
		{
			TMP_Text tMP_Text = Object.Instantiate(skillsTemplate, skillsTemplate.transform.parent);
			string text = $"{skill.name.Localize()}: {skill.GetRoundedValue()}%";
			tMP_Text.text = text;
			tMP_Text.gameObject.SetActive(value: true);
		}
	}

	private static EmployeeInstance GetEmployeeInstance(EntityController entityController)
	{
		if (entityController is EmployeeStationController employeeStationController)
		{
			return employeeStationController.employeeInstance;
		}
		return null;
	}
}
