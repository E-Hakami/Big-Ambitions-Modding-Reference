using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

public class SmartphoneAppButton : AppButton
{
	[SerializeField]
	private Badge badge;

	[SerializeField]
	private Image outline;

	[SerializeField]
	private float outlineOpacity;

	public void UpdateBadgeCount(int badgeCount)
	{
		badge.UpdateBadge(badgeCount);
	}

	public void ShowOutline()
	{
		outline.color = new Color(1f, 1f, 1f, outlineOpacity);
	}

	public void HideOutline()
	{
		outline.color = new Color(1f, 1f, 1f, 0f);
	}
}
