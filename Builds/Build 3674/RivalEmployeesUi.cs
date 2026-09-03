using System.Collections.Generic;
using System.Linq;
using Buildings.BuildingTypes.Shared;
using Entities;
using Localizor.LanguageChangeEvent;
using UI.Smartphone.Apps.BizMan;
using UnityEngine;

public class RivalEmployeesUi : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent titleLocalization;

	[SerializeField]
	private RivalEmployeeScrollerController employeesTable;

	private readonly List<AiBusinessEmployeeData> _aiEmployees = new List<AiBusinessEmployeeData>();

	public void Show(BuildingRegistration buildingRegistration)
	{
		titleLocalization.Arguments = new
		{
			businessName = buildingRegistration.BusinessName
		};
		_aiEmployees.Clear();
		_aiEmployees.AddRange(buildingRegistration.aiEmployees);
		List<AiBusinessEmployeeData> list = buildingRegistration.poachedEmployees?.Select((EmployeeInstance x) => new AiBusinessEmployeeData(x)).ToList();
		if (list != null && list.Count > 0)
		{
			_aiEmployees.AddRange(list);
		}
		employeesTable.LoadList(_aiEmployees);
		base.gameObject.SetActive(value: true);
	}

	public void Toggle(bool newState)
	{
		base.gameObject.SetActive(newState);
	}
}
