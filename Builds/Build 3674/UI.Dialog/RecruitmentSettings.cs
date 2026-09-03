using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Skills;
using BigAmbitions.Tags;
using Buildings;
using Extensions;
using Helpers;
using TMPro;
using UI.Elements;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dialog;

public class RecruitmentSettings : MonoBehaviour
{
	private const float DefaultPrice = 50f;

	[SerializeField]
	private UI.Elements.Dropdown businessDropdown;

	[SerializeField]
	private UI.Elements.Dropdown skillsDropdown;

	[SerializeField]
	private GameObject scheduleTypeGroup;

	public Slider deadlineSlider;

	public Slider candidatesAmountSlider;

	public Toggle fullTimeToggle;

	public Toggle partTimeToggle;

	public TextMeshProUGUI priceLabel;

	public float totalPrice;

	[HideInInspector]
	public List<string> businessSkills;

	[HideInInspector]
	public BuildingRegistration selectedBusiness;

	[HideInInspector]
	public bool hasSelectedSkill;

	[HideInInspector]
	public string selectedSkill;

	private List<BuildingRegistration> _buildingRegistrations;

	private string[] _availableSkills;

	private void OnEnable()
	{
		selectedBusiness = null;
		hasSelectedSkill = false;
		_buildingRegistrations = BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilter);
		Building building = BuildingHelper.GetBuilding(DialogController.current.contact.Address);
		_availableSkills = ((RecruitmentAgencySettings)building.SpecialService.settings).availableEmployeeSkills;
		businessDropdown.SetPlaceholder("dialog_select_business");
		businessDropdown.SetOptions(_buildingRegistrations.Select((BuildingRegistration x) => x.GetDisplayName()).ToList(), localize: false);
		businessDropdown.onOptionSelected.AddListener(SelectBusiness);
		skillsDropdown.SetPlaceholder("dialog_select_skill");
		skillsDropdown.SetOptions(_availableSkills.ToList());
		skillsDropdown.onOptionSelected.AddListener(SelectSkill);
		skillsDropdown.SetInteractable(interactable: false);
		deadlineSlider.onValueChanged.AddListener(delegate
		{
			SetPrice();
		});
		candidatesAmountSlider.onValueChanged.AddListener(delegate
		{
			SetPrice();
		});
		SetPrice();
	}

	private static bool PlayerBuildingFilter(BuildingRegistration buildingRegistration)
	{
		return buildingRegistration.businessTypeName != "ba:businesstype_empty";
	}

	private void SetPrice()
	{
		float num = deadlineSlider.maxValue - deadlineSlider.value + 1f;
		totalPrice = 50f;
		totalPrice *= num;
		totalPrice *= candidatesAmountSlider.value;
		priceLabel.text = totalPrice.ToShortCurrencyFormat() ?? "";
	}

	private void SelectBusiness(int businessIndex)
	{
		selectedBusiness = _buildingRegistrations[businessIndex];
		BusinessType data = BusinessTypeHelper.GetData(selectedBusiness);
		businessSkills = data.employeePrimarySkills.Intersect(_availableSkills).ToList();
		if (_availableSkills.Contains("ba:skill_cleaning") && BuildingTypeHelper.GetData(selectedBusiness).NeedsCleaning)
		{
			businessSkills.Add("ba:skill_cleaning");
		}
		if (businessSkills.Count == 0)
		{
			Notifications.Show(NotificationType.Warning, "dialog_recruitment_no_skills_available_notification", null, 4f, "dialog_recruitment_no_skills_available_notification");
			skillsDropdown.ResetSelectedOption();
			skillsDropdown.SetInteractable(interactable: false);
			hasSelectedSkill = false;
			return;
		}
		if (hasSelectedSkill)
		{
			hasSelectedSkill = false;
			skillsDropdown.ResetSelectedOption();
		}
		skillsDropdown.SetOptions(businessSkills.ToList());
		skillsDropdown.SetInteractable(interactable: true);
	}

	private void SelectSkill(int skillIndex)
	{
		selectedSkill = businessSkills[skillIndex];
		hasSelectedSkill = true;
		if (SkillHelper.GetData(selectedSkill).HasTag(TagRef.Skilltag.hashoursperweekdemand))
		{
			scheduleTypeGroup.gameObject.SetActive(value: true);
			return;
		}
		scheduleTypeGroup.gameObject.SetActive(value: false);
		fullTimeToggle.isOn = false;
		partTimeToggle.isOn = false;
	}
}
