using UnityEngine.EventSystems;

namespace UI.InteriorDesigner;

public class PackageCursorChangeEvent : ICursorHoverEvent
{
	public bool ChangedCursor { get; set; }

	public CursorType CursorType => CursorType.Move;

	public void OnPointerExit(PointerEventData eventData)
	{
	}
}
