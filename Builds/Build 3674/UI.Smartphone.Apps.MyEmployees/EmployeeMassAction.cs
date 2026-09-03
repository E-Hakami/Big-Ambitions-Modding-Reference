using System.Collections.Generic;
using UnityEngine;

namespace UI.Smartphone.Apps.MyEmployees;

public abstract class EmployeeMassAction : ScriptableObject
{
	public string type;

	public List<string> supportedTabs;

	public abstract void Perform();
}
