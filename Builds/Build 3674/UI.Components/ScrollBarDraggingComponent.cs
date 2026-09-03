using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Components;

public class ScrollBarDraggingComponent : MonoBehaviour, IBeginDragHandler, IEventSystemHandler, IEndDragHandler
{
	public static bool isScrollBarBeingDragged;

	public void OnBeginDrag(PointerEventData eventData)
	{
		isScrollBarBeingDragged = true;
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		isScrollBarBeingDragged = false;
	}
}
