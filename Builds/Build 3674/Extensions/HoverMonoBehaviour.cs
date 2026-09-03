using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Extensions;

public abstract class HoverMonoBehaviour : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public bool IsHovered { get; private set; }

	public Action OnHoverEnter { get; set; }

	public Action OnHoverExit { get; set; }

	public virtual void OnPointerEnter(PointerEventData eventData)
	{
		IsHovered = true;
		OnHoverEnter?.Invoke();
	}

	public virtual void OnPointerExit(PointerEventData eventData)
	{
		IsHovered = false;
		OnHoverExit?.Invoke();
	}
}
