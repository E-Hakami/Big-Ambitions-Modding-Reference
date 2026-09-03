using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.Contacts;

public class ContactIconView : MonoBehaviour
{
	[SerializeField]
	private Image squareImage;

	[SerializeField]
	private Image letterImage;

	public void SetSquareIcon(Sprite sprite)
	{
		letterImage.gameObject.SetActive(value: false);
		squareImage.gameObject.SetActive(value: true);
		squareImage.sprite = sprite;
	}

	public void SetLetterIcon(Sprite sprite, Color tint)
	{
		squareImage.gameObject.SetActive(value: false);
		letterImage.gameObject.SetActive(value: true);
		letterImage.sprite = sprite;
		letterImage.color = tint;
	}
}
