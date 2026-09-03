using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/UIElement/UiElement")]
public class TutorialPointerDataUiElement : TutorialPointerData
{
	[SerializeField]
	protected string uiPath;

	[SerializeField]
	private bool setPositionEveryFrame;

	[SerializeField]
	private float autoUpdatePositionDuration = 1f;

	protected RectTransform uiElementTarget;

	private float _autoUpdatePositionUntilTime;

	protected override TutorialPointerType GetTutorialPointerType()
	{
		return TutorialPointerType.Ui;
	}

	protected virtual RectTransform GetUiElementTarget()
	{
		if (uiElementTarget == null)
		{
			uiElementTarget = GetStaticUiElementTarget();
		}
		if (uiElementTarget == null)
		{
			Debug.LogError("No UI element found on static path '" + uiPath + "' (" + base.name + ")", this);
		}
		return uiElementTarget;
	}

	protected RectTransform GetStaticUiElementTarget()
	{
		return TutorialPointersManager.FindUiRectByPath(uiPath);
	}

	public override bool ShouldBeEnabled()
	{
		if (base.ShouldBeEnabled() && GetUiElementTarget() != null)
		{
			return GetUiElementTarget().gameObject.activeInHierarchy;
		}
		return false;
	}

	public override void Relocate(TutorialPointer tutorialPointer)
	{
		if (setPositionEveryFrame || Time.unscaledTime <= _autoUpdatePositionUntilTime)
		{
			SetPosition(tutorialPointer);
		}
	}

	public override void OnShow(TutorialPointer tutorialPointer)
	{
		_autoUpdatePositionUntilTime = Time.unscaledTime + autoUpdatePositionDuration;
		SetPosition(tutorialPointer);
	}

	private void SetPosition(TutorialPointer tutorialPointer)
	{
		RectTransform rectTransform = GetUiElementTarget();
		if (!(tutorialPointer == null) && !(tutorialPointer.transform == null) && !(rectTransform == null))
		{
			tutorialPointer.transform.GetChild(0).position = rectTransform.TransformPoint(rectTransform.rect.center);
		}
	}
}
