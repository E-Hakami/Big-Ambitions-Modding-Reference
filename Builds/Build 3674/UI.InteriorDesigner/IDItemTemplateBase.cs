using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.InteriorDesigner;

public abstract class IDItemTemplateBase : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[SerializeField]
	protected Button focusButton;

	[HideInInspector]
	public int itemHash = -1;

	protected bool isSelected;

	public abstract void OnPointerEnter(PointerEventData eventData);

	public abstract void OnPointerExit(PointerEventData eventData);

	public abstract void SetUp(IDItemUiTemplateData data);

	public virtual void SetSelected(bool selected)
	{
		isSelected = selected;
	}
}
