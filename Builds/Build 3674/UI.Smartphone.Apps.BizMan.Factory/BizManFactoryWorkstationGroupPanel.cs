using System.Collections.Generic;
using Localizor.LanguageChangeEvent;
using UI.Smartphone.Apps.BizMan.Factory.Table;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Factory;

public class BizManFactoryWorkstationGroupPanel : MonoBehaviour
{
	public BizManFactoryWorkstationGroupScrollerController scroller;

	[SerializeField]
	private Image workstationImage;

	[SerializeField]
	private TextLocalizationComponent workstationName;

	public void SetUp(List<FactoryWorkstationInstance> workstations, BuildingRegistration registration)
	{
		workstationName.Key = workstations[0].workstationType;
		scroller.Load(workstations, registration);
		workstationImage.sprite = workstations[0].Workstation.icon84;
	}
}
