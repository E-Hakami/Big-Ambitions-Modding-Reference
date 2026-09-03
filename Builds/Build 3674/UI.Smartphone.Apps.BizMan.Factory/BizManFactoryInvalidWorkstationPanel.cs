using Extensions;
using Localizor;
using TMPro;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.Factory;

public class BizManFactoryInvalidWorkstationPanel : BizManFactoryWorkstationPanel
{
	[SerializeField]
	private Transform producerMachineTemplate;

	public override void SetUp(FactoryWorkstationInstance instance, string alias, BuildingRegistration reg)
	{
		base.SetUp(instance, alias, reg);
		producerMachineTemplate.ResetTemplate();
		foreach (string requiredProductionMachine in instance.Workstation.requiredProductionMachines)
		{
			Transform obj = producerMachineTemplate.CreateElement();
			GameObject gameObject = obj.Find("Missing").gameObject;
			GameObject gameObject2 = obj.Find("Active").gameObject;
			obj.Find("Text").GetComponent<TMP_Text>().text = requiredProductionMachine.GetLocalization();
			if (instance.HasProductionMachine(requiredProductionMachine))
			{
				gameObject.SetActive(value: false);
				gameObject2.SetActive(value: true);
			}
			else
			{
				gameObject.SetActive(value: true);
				gameObject2.SetActive(value: false);
			}
		}
	}
}
