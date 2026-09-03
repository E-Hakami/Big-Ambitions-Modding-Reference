using System;
using System.Collections.Generic;
using Extensions;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Components;
using UI.MainMenu;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Factory;

public class BizManFactoryWorkstationGroupTemplate : MonoBehaviour, ISelectable
{
	[SerializeField]
	private CustomGameFoldout foldout;

	[SerializeField]
	private Image workstationGroupIcon;

	[SerializeField]
	private TextLocalizationComponent workstationGroupName;

	[SerializeField]
	private TMP_Text activeWorkstationsText;

	[SerializeField]
	private IconSwapper isActiveSwapper;

	[SerializeField]
	private ReorderableList workstationsList;

	[Header("Templates")]
	[SerializeField]
	private Transform workstationTemplate;

	[Header("Selection")]
	[SerializeField]
	private SelectorGroup group;

	[SerializeField]
	private Image backgroundImage;

	[SerializeField]
	private Color selectedColor;

	public List<FactoryWorkstationInstance> workstations;

	private BuildingRegistration _buildingRegistration;

	private Color _originalSelectionColor;

	private void Start()
	{
		group.Register(this);
		CustomGameFoldout customGameFoldout = foldout;
		customGameFoldout.onToggleFoldout = (Action<bool>)Delegate.Combine(customGameFoldout.onToggleFoldout, new Action<bool>(ForceRebuild));
		workstationsList.OnItemReordered += OnReorderItem;
		_originalSelectionColor = backgroundImage.color;
	}

	private void OnDestroy()
	{
		CustomGameFoldout customGameFoldout = foldout;
		customGameFoldout.onToggleFoldout = (Action<bool>)Delegate.Remove(customGameFoldout.onToggleFoldout, new Action<bool>(ForceRebuild));
		workstationsList.OnItemReordered -= OnReorderItem;
		group.Unregister(this);
	}

	public void Load(string workstationType, List<FactoryWorkstationInstance> workstations, BuildingRegistration registration)
	{
		_buildingRegistration = registration;
		this.workstations = workstations;
		workstationGroupName.Key = workstationType;
		int num = 0;
		foreach (FactoryWorkstationInstance workstation in this.workstations)
		{
			if (workstation.IsWorkstationActive(registration))
			{
				num++;
			}
		}
		activeWorkstationsText.SetText($"{num}/{this.workstations.Count}");
		isActiveSwapper.IsOn = num != 0;
		workstationGroupIcon.sprite = workstations[0].Workstation.icon84;
		InstantiateList(workstationType);
	}

	private void InstantiateList(string workstationType)
	{
		workstationTemplate.ResetTemplate();
		workstations.SortInt((FactoryWorkstationInstance instance) => instance.priority);
		int num = 1;
		for (int num2 = 0; num2 < workstations.Count; num2++)
		{
			FactoryWorkstationInstance factoryWorkstationInstance = workstations[num2];
			factoryWorkstationInstance.priority = num2;
			BizManFactoryWorkstationTemplate component = workstationTemplate.CreateElement().GetComponent<BizManFactoryWorkstationTemplate>();
			string alias;
			if (string.IsNullOrEmpty(factoryWorkstationInstance.alias))
			{
				alias = $"{workstationType.GetLocalization()} {num}";
				num++;
			}
			else
			{
				alias = factoryWorkstationInstance.alias;
			}
			component.SetUp(this, factoryWorkstationInstance, alias, _buildingRegistration);
		}
	}

	private void ForceRebuild(bool _)
	{
		LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform.parent as RectTransform);
	}

	private void OnReorderItem(int oldIndex, int newIndex)
	{
		workstations.Move(oldIndex, newIndex);
		for (int i = 0; i < workstations.Count; i++)
		{
			workstations[i].priority = i;
		}
	}

	public void ShowWorkstationGroupPanel()
	{
		BizManFactory.requestWorkstationGroupPanel(this);
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
