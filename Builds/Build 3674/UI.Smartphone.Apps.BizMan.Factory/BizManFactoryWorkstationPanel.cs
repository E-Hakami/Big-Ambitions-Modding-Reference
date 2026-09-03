using JimmysUnityUtilities;
using TMPro;
using UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Factory;

public abstract class BizManFactoryWorkstationPanel : MonoBehaviour
{
	[HideInInspector]
	public BizManFactoryMachineList machineList;

	[SerializeField]
	protected IconSwapper isActiveSwapper;

	[SerializeField]
	private TMP_InputField workstationNameInputField;

	[SerializeField]
	private Image workstationNameInputFieldBackground;

	protected FactoryWorkstationInstance workstationInstance;

	protected BuildingRegistration registration;

	protected virtual void Awake()
	{
		workstationNameInputField.onEndEdit.AddListener(OnSaveWorkstationName);
	}

	protected virtual void OnDestroy()
	{
		workstationNameInputField.onEndEdit.RemoveListener(OnSaveWorkstationName);
	}

	public virtual void SetUp(FactoryWorkstationInstance instance, string alias, BuildingRegistration reg)
	{
		workstationInstance = instance;
		registration = reg;
		SetUpHeader(alias);
	}

	public void StartEditingWorkstationName()
	{
		workstationNameInputField.interactable = true;
		workstationNameInputField.Select();
		workstationNameInputField.ActivateInputField();
		workstationNameInputFieldBackground.color = workstationNameInputFieldBackground.color.WithAlpha(1f);
	}

	private void OnSaveWorkstationName(string value)
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			workstationNameInputFieldBackground.color = workstationNameInputFieldBackground.color.WithAlpha(0f);
			workstationNameInputField.interactable = false;
			if (!string.IsNullOrEmpty(value) && !(value == workstationInstance.alias))
			{
				workstationInstance.alias = value;
				machineList.activeWorkstationTemplate.UpdateAlias();
			}
		});
	}

	private void SetUpHeader(string alias)
	{
		isActiveSwapper.IsOn = workstationInstance.IsWorkstationActive(registration);
		workstationNameInputField.text = alias;
		workstationNameInputFieldBackground.color = workstationNameInputFieldBackground.color.WithAlpha(0f);
		workstationNameInputField.interactable = false;
	}
}
