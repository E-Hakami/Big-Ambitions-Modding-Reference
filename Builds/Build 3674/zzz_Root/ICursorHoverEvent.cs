using UnityEngine.EventSystems;

public interface ICursorHoverEvent
{
	bool ChangedCursor { get; set; }

	CursorType CursorType { get; }

	void OnPointerExit(PointerEventData eventData);
}
