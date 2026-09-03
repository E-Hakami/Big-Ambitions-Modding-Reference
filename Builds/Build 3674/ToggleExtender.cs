using UnityEngine;
using UnityEngine.UI;

public class ToggleExtender : Toggle
{
	public Image icon;

	public Color iconOnColor;

	public Image background;

	public Sprite backgroundOnSprite;

	public Sprite backgroundOffSprite;

	protected override void Awake()
	{
		base.Awake();
		onValueChanged.AddListener(OnValueChanged);
	}

	private void OnValueChanged(bool isOn)
	{
		if (icon != null)
		{
			icon.color = (isOn ? iconOnColor : Color.white);
		}
		if (background != null)
		{
			background.sprite = (isOn ? backgroundOnSprite : backgroundOffSprite);
		}
	}
}
