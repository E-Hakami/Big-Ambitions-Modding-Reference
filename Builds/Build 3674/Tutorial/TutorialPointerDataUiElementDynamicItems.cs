using System;
using System.Collections.Generic;
using Tutorial.ItemOrderingConditions;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/UIElement/UiElementDynamicItems")]
public class TutorialPointerDataUiElementDynamicItems : TutorialPointerDataUiElement
{
	[SerializeField]
	private string uiVariablePath;

	[SerializeField]
	private HasDynamicItemsInTarget questRequirement;

	[SerializeField]
	private CustomBuildingTarget customBuildingTarget;

	[SerializeField]
	private ItemOrderingComparison itemOrderingComparison;

	[NonSerialized]
	private TutorialDynamicItems _dynamicItemsToPlace;

	[NonSerialized]
	private readonly Dictionary<string, RectTransform> _itemTargets = new Dictionary<string, RectTransform>();

	[NonSerialized]
	private readonly List<string> _itemTargetsOrdered = new List<string>();

	[NonSerialized]
	private RectTransform _uiPermanentElementTarget;

	[NonSerialized]
	private bool _reloadVisuals;

	protected override TutorialPointerType GetTutorialPointerType()
	{
		return TutorialPointerType.Ui;
	}

	protected override RectTransform GetUiElementTarget()
	{
		return uiElementTarget;
	}

	private void SetUiElementTarget()
	{
		if (!IsInsideTargetBuilding())
		{
			SetUiElementTarget(null);
			return;
		}
		_dynamicItemsToPlace = questRequirement.GetRemainingDynamicItemsForTutorialPointers();
		if (_dynamicItemsToPlace == null)
		{
			SetUiElementTarget(null);
			return;
		}
		SetItemTargets();
		if (_itemTargetsOrdered.Count != 0)
		{
			SetUiElementTarget(_itemTargets[_itemTargetsOrdered[0]]);
		}
		else
		{
			SetUiElementTarget(null);
		}
	}

	private void SetUiElementTarget(RectTransform elementTarget)
	{
		RectTransform rectTransform = uiElementTarget;
		uiElementTarget = elementTarget;
		if (rectTransform != uiElementTarget)
		{
			_reloadVisuals = true;
		}
	}

	private bool IsInsideTargetBuilding()
	{
		if (BuildingManager.IsInsideBuilding)
		{
			return InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address == customBuildingTarget.GetAddress();
		}
		return false;
	}

	private void SetItemTargets()
	{
		if (_uiPermanentElementTarget == null)
		{
			_uiPermanentElementTarget = TutorialPointersManager.FindUiRectByPath(uiPath);
		}
		if (_uiPermanentElementTarget == null || !_uiPermanentElementTarget.gameObject.activeInHierarchy)
		{
			return;
		}
		_itemTargets.Clear();
		for (int i = 0; i < _dynamicItemsToPlace.dynamicItems.Count; i++)
		{
			if (!_dynamicItemsToPlace.dynamicItemsFulfilled[i])
			{
				string text = _dynamicItemsToPlace.dynamicItems[i][0];
				if ((!_itemTargets.ContainsKey(text) || !(_itemTargets[text] != null)) && TargetExistsInHierarchy(text))
				{
					string n = text + "/" + uiVariablePath;
					_itemTargets[text] = _uiPermanentElementTarget.Find(n)?.GetComponent<RectTransform>();
				}
			}
		}
		_itemTargetsOrdered.Clear();
		foreach (KeyValuePair<string, RectTransform> itemTarget in _itemTargets)
		{
			if (itemTarget.Value != null)
			{
				_itemTargetsOrdered.Add(itemTarget.Key);
			}
		}
		if (itemOrderingComparison != null)
		{
			_itemTargetsOrdered.Sort(itemOrderingComparison.Comparison);
		}
	}

	private bool TargetExistsInHierarchy(string itemName)
	{
		foreach (Transform item in _uiPermanentElementTarget)
		{
			if (item.name == itemName.ToString())
			{
				return true;
			}
		}
		return false;
	}

	public override bool ShouldBeEnabled()
	{
		SetUiElementTarget();
		if (base.ShouldBeEnabled() && IsInsideTargetBuilding() && uiElementTarget != null)
		{
			return uiElementTarget.gameObject.activeInHierarchy;
		}
		return false;
	}

	public override void Relocate(TutorialPointer tutorialPointer)
	{
		if (_reloadVisuals)
		{
			OnShow(tutorialPointer);
			_reloadVisuals = false;
		}
		base.Relocate(tutorialPointer);
	}
}
