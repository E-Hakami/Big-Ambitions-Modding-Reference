using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI;

public class CursorHoverChangeEvent : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, ICursorHoverEvent
{
	private static bool PrivateFreezeCursorType;

	[SerializeField]
	private CursorType cursorType;

	[SerializeField]
	private CursorHoverChangeEvent parentHover;

	private bool _isHovering;

	public static bool FreezeCursorType
	{
		get
		{
			return PrivateFreezeCursorType;
		}
		set
		{
			if (PrivateFreezeCursorType != value)
			{
				PrivateFreezeCursorType = value;
				OnFreezeCursorTypeChanged?.Invoke(PrivateFreezeCursorType);
			}
		}
	}

	public CursorType CursorType => cursorType;

	public bool ChangedCursor { get; set; }

	public static event Action<bool> OnFreezeCursorTypeChanged;

	private void OnEnable()
	{
		OnFreezeCursorTypeChanged += HandleFreezeCursorTypeChanged;
	}

	private void OnDisable()
	{
		OnFreezeCursorTypeChanged -= HandleFreezeCursorTypeChanged;
		ResetCursor();
	}

	private void OnDestroy()
	{
		ResetCursor();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		_isHovering = true;
		if (!FreezeCursorType && MouseController.SetCursor(this))
		{
			ChangedCursor = true;
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_isHovering = false;
		if (!FreezeCursorType)
		{
			ResetCursor();
			if (parentHover != null && parentHover._isHovering)
			{
				parentHover.OnPointerEnter(eventData);
			}
		}
	}

	private void HandleFreezeCursorTypeChanged(bool isFrozen)
	{
		if (!isFrozen)
		{
			if (_isHovering)
			{
				OnPointerEnter(null);
			}
			else
			{
				OnPointerExit(null);
			}
		}
	}

	private void ResetCursor()
	{
		if (ChangedCursor)
		{
			MouseController.SetCursor(null);
			ChangedCursor = false;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		PrivateFreezeCursorType = false;
		OnFreezeCursorTypeChanged = null;
	}
}
