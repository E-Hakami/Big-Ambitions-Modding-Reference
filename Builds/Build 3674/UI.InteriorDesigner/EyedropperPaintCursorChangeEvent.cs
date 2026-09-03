using UnityEngine.EventSystems;

namespace UI.InteriorDesigner;

public class EyedropperPaintCursorChangeEvent : ICursorHoverEvent
{
	public bool ChangedCursor { get; set; }

	public CursorType CursorType => CursorType.Brush;

	public void OnPointerExit(PointerEventData eventData)
	{
		ResetCursor();
	}

	private void ResetCursor()
	{
		if (ChangedCursor)
		{
			MouseController.SetCursor(null);
			ChangedCursor = false;
		}
	}
}
