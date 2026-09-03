using System;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.Factory;

public class BizManFactory : MonoBehaviour
{
	public static Action<BizManFactoryWorkstationTemplate, FactoryWorkstationInstance, string> requestWorkstationPanel;

	public static Action<BizManFactoryWorkstationGroupTemplate> requestWorkstationGroupPanel;

	[SerializeField]
	private BizManBusiness bizManBusiness;

	public BizManFactoryMachineList machineList;

	public BizManFactoryValidWorkstationPanel validWorkstationPanel;

	public BizManFactoryInvalidWorkstationPanel invalidWorkstationPanel;

	public BizManFactoryWorkstationGroupPanel workstationGroupPanel;

	private void Awake()
	{
		validWorkstationPanel.machineList = machineList;
		invalidWorkstationPanel.machineList = machineList;
		requestWorkstationPanel = (Action<BizManFactoryWorkstationTemplate, FactoryWorkstationInstance, string>)Delegate.Combine(requestWorkstationPanel, new Action<BizManFactoryWorkstationTemplate, FactoryWorkstationInstance, string>(OnRequestWorkstationPanel));
		requestWorkstationGroupPanel = (Action<BizManFactoryWorkstationGroupTemplate>)Delegate.Combine(requestWorkstationGroupPanel, new Action<BizManFactoryWorkstationGroupTemplate>(OnRequestWorkstationGroupPanel));
		BizManFactoryValidWorkstationPanel bizManFactoryValidWorkstationPanel = validWorkstationPanel;
		bizManFactoryValidWorkstationPanel.onProduceUpToChanged = (Action<bool, int>)Delegate.Combine(bizManFactoryValidWorkstationPanel.onProduceUpToChanged, new Action<bool, int>(OnValidPanelUpToAmountChanged));
	}

	private void OnDestroy()
	{
		requestWorkstationPanel = (Action<BizManFactoryWorkstationTemplate, FactoryWorkstationInstance, string>)Delegate.Remove(requestWorkstationPanel, new Action<BizManFactoryWorkstationTemplate, FactoryWorkstationInstance, string>(OnRequestWorkstationPanel));
		requestWorkstationGroupPanel = (Action<BizManFactoryWorkstationGroupTemplate>)Delegate.Remove(requestWorkstationGroupPanel, new Action<BizManFactoryWorkstationGroupTemplate>(OnRequestWorkstationGroupPanel));
		BizManFactoryValidWorkstationPanel bizManFactoryValidWorkstationPanel = validWorkstationPanel;
		bizManFactoryValidWorkstationPanel.onProduceUpToChanged = (Action<bool, int>)Delegate.Remove(bizManFactoryValidWorkstationPanel.onProduceUpToChanged, new Action<bool, int>(OnValidPanelUpToAmountChanged));
	}

	private void OnEnable()
	{
		validWorkstationPanel.gameObject.SetActive(value: false);
		invalidWorkstationPanel.gameObject.SetActive(value: false);
		workstationGroupPanel.gameObject.SetActive(value: false);
		machineList.SetUp(bizManBusiness.buildingRegistration);
	}

	private void OnValidPanelUpToAmountChanged(bool produceUpTo, int amount)
	{
		foreach (FactoryWorkstationInstance workstation in machineList.activeWorkstationGroupTemplate.workstations)
		{
			if (!(workstation.CreatedItemName != machineList.activeWorkstationTemplate.CreatedItemName))
			{
				workstation.produceUpToValue = amount;
				workstation.produceUpTo = produceUpTo;
			}
		}
	}

	private void OnRequestWorkstationPanel(BizManFactoryWorkstationTemplate workstationTemplate, FactoryWorkstationInstance workstationInstance, string alias)
	{
		machineList.activeWorkstationTemplate = workstationTemplate;
		machineList.activeWorkstationGroupTemplate = workstationTemplate.groupTemplate;
		if (workstationInstance.IsWorkstationValid())
		{
			validWorkstationPanel.gameObject.SetActive(value: true);
			invalidWorkstationPanel.gameObject.SetActive(value: false);
			workstationGroupPanel.gameObject.SetActive(value: false);
			validWorkstationPanel.SetUp(workstationInstance, alias, bizManBusiness.buildingRegistration);
		}
		else
		{
			invalidWorkstationPanel.gameObject.SetActive(value: true);
			validWorkstationPanel.gameObject.SetActive(value: false);
			workstationGroupPanel.gameObject.SetActive(value: false);
			invalidWorkstationPanel.SetUp(workstationInstance, alias, bizManBusiness.buildingRegistration);
		}
	}

	private void OnRequestWorkstationGroupPanel(BizManFactoryWorkstationGroupTemplate groupTemplate)
	{
		machineList.activeWorkstationTemplate = null;
		machineList.activeWorkstationGroupTemplate = groupTemplate;
		workstationGroupPanel.gameObject.SetActive(value: true);
		invalidWorkstationPanel.gameObject.SetActive(value: false);
		validWorkstationPanel.gameObject.SetActive(value: false);
		workstationGroupPanel.SetUp(groupTemplate.workstations, bizManBusiness.buildingRegistration);
	}
}
