using UnityEngine;
using UnityEngine.UI;

namespace UI.Components;

public class IconSwapper : MonoBehaviour
{
	[SerializeField]
	private Image image;

	[Header("Sprites")]
	[SerializeField]
	private Sprite activeSprite;

	[SerializeField]
	private Sprite inactiveSprite;

	private bool _isOn;

	public bool IsOn
	{
		set
		{
			_isOn = value;
			image.sprite = (_isOn ? activeSprite : inactiveSprite);
		}
	}
}
