using UI.Smartphone.Apps.BizMan.Warehouse;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/UIElement/BizManDriverDropdown")]
public class TutorialPointerDataBizManDriverDropdown : TutorialPointerDataUiElement
{
	private RectTransform _driversContainerTarget;

	protected override RectTransform GetUiElementTarget()
	{
		if (uiElementTarget != null && uiElementTarget.gameObject.activeInHierarchy)
		{
			return uiElementTarget;
		}
		uiElementTarget = null;
		if (_driversContainerTarget == null)
		{
			_driversContainerTarget = GetStaticUiElementTarget();
		}
		if (_driversContainerTarget == null)
		{
			Debug.LogError("No UI element found on static path '" + uiPath + "' (" + base.name + ")", this);
			return null;
		}
		if (!_driversContainerTarget.gameObject.activeInHierarchy)
		{
			return null;
		}
		DriverStation[] componentsInChildren = _driversContainerTarget.GetComponentsInChildren<DriverStation>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].gameObject.activeInHierarchy)
			{
				uiElementTarget = componentsInChildren[i].DriverDropdownTarget;
				return uiElementTarget;
			}
		}
		return null;
	}
}
