using System;
using System.Linq;
using HGAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/UIElement/UiElementWithVariableItemNamePath")]
public class TutorialPointerDataUiElementWithVariableItemNamePath : TutorialPointerDataUiElement
{
	[SerializeField]
	protected string uiVariablePath;

	[SerializeField]
	[AutocompleteDropdown("Items")]
	protected string[] itemNames;

	[NonSerialized]
	private RectTransform _uiPermanentElementTarget;

	protected override RectTransform GetUiElementTarget()
	{
		if (uiElementTarget != null)
		{
			return uiElementTarget;
		}
		if (_uiPermanentElementTarget == null)
		{
			_uiPermanentElementTarget = GetStaticUiElementTarget();
		}
		if (_uiPermanentElementTarget == null)
		{
			Debug.LogError("No UI element found on static path '" + uiPath + "' (" + base.name + ")", this);
			return null;
		}
		if (!_uiPermanentElementTarget.gameObject.activeInHierarchy)
		{
			return null;
		}
		if (!TargetExistsInHierarchy())
		{
			return null;
		}
		string[] array = itemNames;
		for (int i = 0; i < array.Length; i++)
		{
			string n = array[i] + "/" + uiVariablePath;
			Transform transform = _uiPermanentElementTarget.Find(n);
			if (transform != null)
			{
				uiElementTarget = transform.GetComponent<RectTransform>();
			}
			if (uiElementTarget != null)
			{
				return uiElementTarget;
			}
		}
		return null;
	}

	private bool TargetExistsInHierarchy()
	{
		foreach (Transform item in _uiPermanentElementTarget)
		{
			if (itemNames.Contains(item.name))
			{
				return true;
			}
		}
		return false;
	}
}
