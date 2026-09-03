using System;
using System.Collections.Generic;
using System.Linq;
using Entities;
using Localizor;
using UI.Elements;
using UnityEngine;

public class NoManagerAssignedPopUp : MonoBehaviour
{
	[SerializeField]
	private CanvasGroup planCanvasGroup;

	public Dropdown employeeDropdown;

	public Action onDeletePlan;

	public Action deletePlan;

	public Action<int> onChooseEmployee;

	private bool _blockDeleteButton;

	private void Awake()
	{
		employeeDropdown.onOptionSelected.AddListener(delegate(int index)
		{
			onChooseEmployee(index - 1);
		});
	}

	public void Show(bool blockDeleteButton = false)
	{
		planCanvasGroup.alpha = 0.5f;
		planCanvasGroup.interactable = false;
		planCanvasGroup.blocksRaycasts = false;
		base.gameObject.SetActive(value: true);
		_blockDeleteButton = blockDeleteButton;
	}

	public void Hide()
	{
		planCanvasGroup.alpha = 1f;
		planCanvasGroup.interactable = true;
		planCanvasGroup.blocksRaycasts = true;
		base.gameObject.SetActive(value: false);
	}

	public void SetUpEmployeeDropdown(List<EmployeeInstance> employees)
	{
		List<string> list = new List<string> { "common_unassigned".GetLocalization() };
		list.AddRange(employees.Select((EmployeeInstance x) => x.characterData.name).ToList());
		employeeDropdown.SetOptions(list, localize: false, 0);
	}

	public void DeletePlan()
	{
		if (_blockDeleteButton)
		{
			DeliveryHelper.ShowCantModifyContractNotification();
			return;
		}
		HudConfirm.Show(null, "bizman_delete_plan_confirm", delegate
		{
			deletePlan();
			onDeletePlan();
		});
	}
}
