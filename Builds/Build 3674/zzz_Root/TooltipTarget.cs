using System.Collections;
using Tooltip;
using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTarget : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	private static TooltipTarget CurrentTooltipTarget;

	public object localizationArguments;

	public void OnPointerEnter(PointerEventData eventData)
	{
		StartCoroutine(DelayTooltip());
	}

	public void Hide()
	{
		StopAllCoroutines();
		if (CurrentTooltipTarget == this)
		{
			TooltipSystem.Hide();
			CurrentTooltipTarget = null;
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		Hide();
	}

	private IEnumerator DelayTooltip()
	{
		yield return new WaitForSecondsRealtime(0.1f);
		if (!CurrentTooltipTarget || !(CurrentTooltipTarget != this))
		{
			TooltipSystem.Show();
			CurrentTooltipTarget = this;
			ShowTooltip();
		}
	}

	private void OnDisable()
	{
		Hide();
	}

	protected virtual void ShowTooltip()
	{
	}
}
