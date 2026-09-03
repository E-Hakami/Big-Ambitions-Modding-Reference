using UnityEngine;

public class FullMenuAppButton : AppButton
{
	[SerializeField]
	private GameObject selectedIcon;

	public void ShowSelectedIcon()
	{
		selectedIcon.SetActive(value: true);
	}

	public void HideSelectedIcon()
	{
		selectedIcon.SetActive(value: false);
	}
}
