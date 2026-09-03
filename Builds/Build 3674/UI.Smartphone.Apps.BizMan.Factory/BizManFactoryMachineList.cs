using System.Collections.Generic;
using BigAmbitions.Factories.Workstations;
using BigAmbitions.Items;
using Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Factory;

public class BizManFactoryMachineList : MonoBehaviour
{
	[HideInInspector]
	public BizManFactoryWorkstationGroupTemplate activeWorkstationGroupTemplate;

	[HideInInspector]
	public BizManFactoryWorkstationTemplate activeWorkstationTemplate;

	[SerializeField]
	private RectTransform content;

	[SerializeField]
	private Transform workstationGroupTemplate;

	[SerializeField]
	private BizManFactoryValidWorkstationPanel validWorkstationPanel;

	private readonly Dictionary<string, List<FactoryWorkstationInstance>> _workstationsByType = new Dictionary<string, List<FactoryWorkstationInstance>>();

	private BuildingRegistration _registration;

	public void SetUp(BuildingRegistration registration)
	{
		_registration = registration;
		FetchData();
		Load();
		LayoutRebuilder.ForceRebuildLayoutImmediate(content);
	}

	private void FetchData()
	{
		_workstationsByType.Clear();
		foreach (ItemInstance value2 in _registration.itemInstances.Values)
		{
			if (value2 is FactoryWorkstationInstance factoryWorkstationInstance && !string.IsNullOrEmpty(factoryWorkstationInstance.workstationType))
			{
				if (_workstationsByType.TryGetValue(factoryWorkstationInstance.workstationType, out var value))
				{
					value.Add(factoryWorkstationInstance);
				}
				else if (!string.IsNullOrEmpty(factoryWorkstationInstance.workstationType))
				{
					_workstationsByType.Add(factoryWorkstationInstance.workstationType, new List<FactoryWorkstationInstance> { factoryWorkstationInstance });
				}
			}
		}
	}

	private void Load()
	{
		workstationGroupTemplate.ResetTemplate();
		foreach (string workstationType in FactoryWorkstationHelper.GetWorkstationTypes())
		{
			if (_workstationsByType.TryGetValue(workstationType, out var value))
			{
				workstationGroupTemplate.CreateElement().GetComponent<BizManFactoryWorkstationGroupTemplate>().Load(workstationType, value, _registration);
			}
		}
	}
}
