using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class HelpPageLink : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public Color hoverColor;

	[SerializeField]
	private Color defaultColor;

	public TextMeshProUGUI tmPro;

	public TextLocalizationComponent languageChangeEvent;

	public string linkSlug;

	private bool _isSelected;

	public string rawTranslatedKey;

	public void OnPointerEnter(PointerEventData eventData)
	{
		tmPro.color = hoverColor;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (!_isSelected)
		{
			tmPro.color = defaultColor;
		}
	}

	public void SetSelectedState(bool selected)
	{
		_isSelected = selected;
		tmPro.color = (selected ? hoverColor : defaultColor);
	}
}
