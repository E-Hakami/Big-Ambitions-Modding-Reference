using System;
using System.Collections.Generic;
using System.Linq;
using Entities;
using Helpers;
using IngameDebugConsole;
using UnityEngine;

namespace AI.Employees;

public static class EmployeeCommands
{
	[ConsoleMethod("Employees.ForceSick", "Forces an employee to call in sick on the next daily run. Use employee id or exact name with quotes. Example \"Alice Alisson\".", new string[] { })]
	public static void ForceEmployeeSickNextDay(string employeeIdOrName)
	{
		if (SaveGameManager.Current == null)
		{
			Debug.LogWarning("No save game is currently loaded");
			return;
		}
		EmployeeInstance employeeInstance = FindEmployeeByIdOrName(employeeIdOrName);
		if (employeeInstance != null)
		{
			EmployeeHelper.ForceEmployeeSickNextDay(employeeInstance);
			Debug.Log($"{employeeInstance.characterData.name} will call in sick on the next scheduled workday from day {employeeInstance.nextSickDay}");
		}
	}

	private static EmployeeInstance FindEmployeeByIdOrName(string employeeIdOrName)
	{
		if (string.IsNullOrWhiteSpace(employeeIdOrName))
		{
			Debug.LogWarning("Employee id or name is required");
			return null;
		}
		List<EmployeeInstance> employeeInstances = EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			excludeBeingReplaced = true
		});
		EmployeeInstance employeeInstance = employeeInstances.FirstOrDefault((EmployeeInstance x) => x.id == employeeIdOrName);
		if (employeeInstance != null)
		{
			return employeeInstance;
		}
		List<EmployeeInstance> list = employeeInstances.Where((EmployeeInstance x) => string.Equals(x.characterData.name, employeeIdOrName, StringComparison.OrdinalIgnoreCase)).ToList();
		if (list.Count == 1)
		{
			return list[0];
		}
		if (list.Count > 1)
		{
			Debug.LogWarning("Multiple employees found named '" + employeeIdOrName + "'. Use one of these ids:");
			foreach (EmployeeInstance item in list)
			{
				Debug.LogWarning("\t" + item.characterData.name + ": " + item.id);
			}
		}
		else
		{
			Debug.LogWarning("Employee '" + employeeIdOrName + "' not found");
		}
		return null;
	}
}
