using EnhancedUI.EnhancedScroller;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BaTable;

public abstract class BaTableCellView<T> : EnhancedScrollerCellView, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public Image backgroundImage;

	public Color selectedBackgroundColor;

	protected Color defaultBackgroundColor;

	public abstract void SetData(T data);

	protected virtual void Awake()
	{
		defaultBackgroundColor = backgroundImage?.color ?? Color.white;
	}

	public virtual void ResetVisuals()
	{
	}

	public virtual void OnPointerEnter(PointerEventData eventData)
	{
	}

	public virtual void OnPointerExit(PointerEventData eventData)
	{
	}

	public virtual void VisualizeSelected(bool selected)
	{
		backgroundImage.color = (selected ? selectedBackgroundColor : defaultBackgroundColor);
		SetTextColors(selected);
	}

	private void SetTextColors(bool selected)
	{
		Color32 color = (selected ? InstanceBehavior<GlobalReferences>.Instance.colors.midnight : InstanceBehavior<GlobalReferences>.Instance.colors.white);
		TextMeshProUGUI[] componentsInChildren = base.transform.GetComponentsInChildren<TextMeshProUGUI>();
		foreach (TextMeshProUGUI textMeshProUGUI in componentsInChildren)
		{
			if (textMeshProUGUI.CompareTag("Dropdown") || textMeshProUGUI.color == InstanceBehavior<GlobalReferences>.Instance.colors.red)
			{
				continue;
			}
			if (selected)
			{
				if (textMeshProUGUI.color == InstanceBehavior<GlobalReferences>.Instance.colors.yellow)
				{
					textMeshProUGUI.color = InstanceBehavior<GlobalReferences>.Instance.colors.orange;
					continue;
				}
			}
			else
			{
				if (textMeshProUGUI.color == InstanceBehavior<GlobalReferences>.Instance.colors.orange)
				{
					textMeshProUGUI.color = InstanceBehavior<GlobalReferences>.Instance.colors.yellow;
					continue;
				}
				if (textMeshProUGUI.color == InstanceBehavior<GlobalReferences>.Instance.colors.yellow)
				{
					continue;
				}
			}
			textMeshProUGUI.color = color;
		}
	}
}
