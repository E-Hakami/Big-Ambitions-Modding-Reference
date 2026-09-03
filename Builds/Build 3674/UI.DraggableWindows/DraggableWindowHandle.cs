using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.DraggableWindows;

public class DraggableWindowHandle : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
{
	[NonSerialized]
	public bool isDragging;

	[NonSerialized]
	public Vector3 offset;

	public event Action OnDragEnded;

	private void OnDisable()
	{
		if (isDragging)
		{
			OnDragEnded?.Invoke();
		}
		isDragging = false;
		offset = Vector2.zero;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		isDragging = true;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (isDragging)
		{
			OnDragEnded?.Invoke();
		}
		isDragging = false;
		offset = Vector2.zero;
	}
}
