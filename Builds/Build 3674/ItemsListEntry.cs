using Extensions;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemsListEntry : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent nameLabel;

	[SerializeField]
	private TMP_Text priceLabel;

	[SerializeField]
	private Image iconImage;

	public string NameLocalized => nameLabel.TextContainer.text;

	public void Init(LanguageChangeEventDataHolder nameLocalization, Sprite icon)
	{
		nameLabel.SetData(nameLocalization);
		iconImage.sprite = icon;
		priceLabel.gameObject.SetActive(value: false);
	}

	public void Init(LanguageChangeEventDataHolder nameLocalization, Sprite icon, float price)
	{
		nameLabel.SetData(nameLocalization);
		iconImage.sprite = icon;
		priceLabel.text = price.ToCurrencyFormat();
		priceLabel.gameObject.SetActive(value: true);
	}

	public void Show()
	{
		base.gameObject.SetActive(value: true);
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
	}

	public void Remove()
	{
		Object.Destroy(base.gameObject);
	}
}
