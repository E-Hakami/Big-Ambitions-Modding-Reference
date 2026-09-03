using UI;
using UnityEngine;

namespace Tutorial;

public abstract class TutorialPointerData : ScriptableObject
{
	[SerializeField]
	private TutorialPointerHideCondition[] hideConditions;

	[SerializeField]
	private bool showOnlyOncePerPlaySession;

	private bool AlreadyShown { get; set; }

	public TutorialPointerType Type => GetTutorialPointerType();

	public virtual bool ShouldBeEnabled()
	{
		if (UiFader.isFading || (showOnlyOncePerPlaySession && AlreadyShown))
		{
			return false;
		}
		for (int i = 0; i < hideConditions.Length; i++)
		{
			if (hideConditions[i].ConditionMet())
			{
				return false;
			}
		}
		return true;
	}

	public virtual void Relocate(TutorialPointer tutorialPointer)
	{
	}

	public virtual void OnShow(TutorialPointer tutorialPointer)
	{
	}

	protected virtual TutorialPointerType GetTutorialPointerType()
	{
		return TutorialPointerType.Ui;
	}

	public virtual void Init()
	{
	}

	public virtual void Dispose()
	{
	}

	public void OnHide()
	{
		AlreadyShown = true;
	}
}
