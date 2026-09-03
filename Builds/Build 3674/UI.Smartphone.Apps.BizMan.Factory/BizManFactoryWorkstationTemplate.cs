using System.Collections.Generic;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using Tooltip;
using UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Factory;

public class BizManFactoryWorkstationTemplate : MonoBehaviour, ISelectable
{
	[HideInInspector]
	public BizManFactoryWorkstationGroupTemplate groupTemplate;

	[SerializeField]
	private Image workstationIcon;

	[SerializeField]
	private TMP_Text workstationName;

	[SerializeField]
	private TextLocalizationComponent createdItemName;

	[SerializeField]
	private IconSwapper isActiveSwapper;

	[SerializeField]
	private ListTooltip tooltip;

	[Header("Selection")]
	[SerializeField]
	private SelectorGroup group;

	[SerializeField]
	private Image backgroundImage;

	[SerializeField]
	private Color selectedColor;

	private string _alias;

	private BuildingRegistration _registration;

	private FactoryWorkstationInstance _workstationInstance;

	private readonly List<string> _inactiveReasonKeys = new List<string>();

	private Color _originalSelectionColor;

	public string CreatedItemName => _workstationInstance.CreatedItemName;

	public void SetUp(BizManFactoryWorkstationGroupTemplate workstationGroupTemplate, FactoryWorkstationInstance workstationInstance, string alias, BuildingRegistration registration)
	{
		groupTemplate = workstationGroupTemplate;
		_workstationInstance = workstationInstance;
		_alias = alias;
		_registration = registration;
		_originalSelectionColor = backgroundImage.color;
		workstationName.SetText(_alias);
		base.name = _alias;
		UpdateRecipe();
	}

	private void Awake()
	{
		group.Register(this);
	}

	private void OnDestroy()
	{
		group.Unregister(this);
	}

	public void UpdateAlias()
	{
		_alias = _workstationInstance.alias;
		workstationName.SetText(_alias);
		base.name = _alias;
	}

	public void UpdateRecipe()
	{
		bool flag = _workstationInstance.IsWorkstationActive(_registration);
		isActiveSwapper.IsOn = flag;
		createdItemName.Key = _workstationInstance.CreatedItemName;
		workstationIcon.sprite = ItemHelper.GetIconWithFallback(_workstationInstance.CreatedItemName);
		tooltip.transform.parent.gameObject.SetActive(!flag);
		if (flag)
		{
			return;
		}
		_inactiveReasonKeys.Clear();
		foreach (string inactiveReasonKey in _workstationInstance.GetInactiveReasonKeys(_registration))
		{
			_inactiveReasonKeys.Add(inactiveReasonKey.GetLocalization());
		}
		tooltip.list.Clear();
		tooltip.list.AddRange(_inactiveReasonKeys);
	}

	public void ShowWorkstationPanel()
	{
		BizManFactory.requestWorkstationPanel(this, _workstationInstance, _alias);
		group.Select(this);
	}

	public void OnSelected()
	{
		backgroundImage.color = selectedColor;
	}

	public void OnDeselected()
	{
		backgroundImage.color = _originalSelectionColor;
	}
}
