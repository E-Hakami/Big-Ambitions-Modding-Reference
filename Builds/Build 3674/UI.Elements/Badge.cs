using TMPro;
using UnityEngine;

namespace UI.Elements;

public class Badge : MonoBehaviour
{
	[SerializeField]
	private int maxBadgeCount = 99;

	[SerializeField]
	private TMP_Text label;

	public void UpdateBadge(int value)
	{
		base.gameObject.SetActive(value > 0);
		label.SetText((value > maxBadgeCount) ? $"{maxBadgeCount}+" : value.ToString());
	}
}
