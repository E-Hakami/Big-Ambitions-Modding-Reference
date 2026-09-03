using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.MainMenu;

public class NewGameModeButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public string mode;

	[SerializeField]
	private GameObject selectedOverlay;

	[SerializeField]
	private GameObject hoverOverlay;

	[SerializeField]
	private GameObject panel;

	private bool _isSelected;

	public void ShowPanel(bool show)
	{
		_isSelected = show;
		panel.SetActive(show);
		selectedOverlay.SetActive(show);
		if (show)
		{
			hoverOverlay.SetActive(value: false);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (!_isSelected)
		{
			hoverOverlay.SetActive(value: true);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (!_isSelected)
		{
			hoverOverlay.SetActive(value: false);
		}
	}
}
