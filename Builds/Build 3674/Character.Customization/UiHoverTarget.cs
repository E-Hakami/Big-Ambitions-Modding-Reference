using UnityEngine;
using UnityEngine.EventSystems;

namespace Character.Customization;

public class UiHoverTarget : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public bool IsHovered { get; private set; }

	private void OnDisable()
	{
		IsHovered = false;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		IsHovered = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		IsHovered = false;
	}
}
