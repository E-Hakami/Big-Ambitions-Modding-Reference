using System;
using Buildings.Office.Headquarters;
using Extensions;
using UnityEngine;
using UnityEngine.UI;

public class HeadhunterPlanUI : MonoBehaviour
{
	public NoManagerAssignedPopUp noManagerAssignedPopUp;

	[Header("Recruiting")]
	[SerializeField]
	private Button recruitingTabButton;

	[SerializeField]
	private GameObject recruitingPanel;

	[Header("Automatic Replacement")]
	[SerializeField]
	private Button automaticReplacementTabButton;

	[SerializeField]
	private GameObject automaticReplacementPanel;

	[HideInInspector]
	public HeadhunterPlan currentPlan;

	public Action<int, int> onHrManagerPlansChanged;

	private void Awake()
	{
		NoManagerAssignedPopUp obj = noManagerAssignedPopUp;
		obj.deletePlan = (Action)Delegate.Combine(obj.deletePlan, new Action(DeletePlan));
	}

	public void LoadPlan(HeadhunterPlan plan)
	{
		currentPlan = plan;
		SelectRecruitingTab();
		if (plan.assignedEmployeeId == null)
		{
			noManagerAssignedPopUp.Show();
		}
		else
		{
			noManagerAssignedPopUp.Hide();
		}
		base.gameObject.SetActive(value: true);
	}

	private void DeletePlan()
	{
		if (currentPlan == null)
		{
			Debug.LogError("No plan selected");
		}
		else
		{
			HeadhunterHelper.DeletePlan(currentPlan.id);
		}
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
		noManagerAssignedPopUp.Hide();
	}

	public void SelectRecruitingTab()
	{
		UnSelectTab(automaticReplacementPanel, automaticReplacementTabButton);
		SelectTab(recruitingPanel, recruitingTabButton);
	}

	public void SelectAutomaticReplacement()
	{
		UnSelectTab(recruitingPanel, recruitingTabButton);
		SelectTab(automaticReplacementPanel, automaticReplacementTabButton);
	}

	private void SelectTab(GameObject tabPanel, Button tabButton)
	{
		tabPanel.SetActive(value: true);
		tabButton.interactable = false;
		tabButton.transform.GetLabelByName("Label").color = InstanceBehavior<GlobalReferences>.Instance.colors.midnight;
	}

	private void UnSelectTab(GameObject tabPanel, Button tabButton)
	{
		tabPanel.SetActive(value: false);
		tabButton.interactable = true;
		tabButton.transform.GetLabelByName("Label").color = InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey;
	}
}
