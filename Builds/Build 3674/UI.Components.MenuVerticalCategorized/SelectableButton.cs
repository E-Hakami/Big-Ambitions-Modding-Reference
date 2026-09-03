using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Components.MenuVerticalCategorized;

public class SelectableButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[SerializeField]
	private GameObject selectedOutline;

	[SerializeField]
	private GameObject hoverOutline;

	private bool _isHovered;

	private bool _isSelected;

	public void OnPointerEnter(PointerEventData eventData)
	{
		_isHovered = true;
		if (!_isSelected)
		{
			hoverOutline.SetActive(value: true);
			selectedOutline.SetActive(value: false);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_isHovered = false;
		if (!_isSelected)
		{
			hoverOutline.SetActive(value: false);
		}
	}

	public void SetSelected(bool isSelected)
	{
		_isSelected = isSelected;
		selectedOutline.SetActive(isSelected);
		if (!isSelected)
		{
			hoverOutline.SetActive(_isHovered);
		}
	}
}
