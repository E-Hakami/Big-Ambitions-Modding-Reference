using System.Collections.Generic;
using System.Linq;
using Entities;
using Helpers;
using UI.Elements;
using UnityEngine;

namespace UI.Dialog;

public class ImportPartnershipSettings : MonoBehaviour
{
	[SerializeField]
	public Dropdown purchasingAgentDropdown;

	[HideInInspector]
	public EmployeeInstance selectedEmployeeInstance;

	private void Start()
	{
		selectedEmployeeInstance = null;
		purchasingAgentDropdown.SetPlaceholder("dialog_import_select_purchasing_agent");
		List<EmployeeInstance> employeeInstances = EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			withSkills = new string[1] { "ba:skill_purchasingagent" },
			excludeBeingReplaced = true
		});
		List<EmployeeInstance> availableAgents = employeeInstances.FindAll((EmployeeInstance x) => x.IsAssignedToAnyWorkShift() && !SaveGameManager.Current.importPartnerships.Exists((ImportPartnership y) => y.employeeInstanceId == x.id));
		purchasingAgentDropdown.SetOptions(availableAgents.Select((EmployeeInstance x) => x.characterData.name).ToList(), localize: false);
		purchasingAgentDropdown.onOptionSelected.AddListener(delegate(int index)
		{
			selectedEmployeeInstance = availableAgents[index];
		});
	}
}
