using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class PlayerColor : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	public UnityEvent onRemove = new UnityEvent();

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			onRemove.Invoke();
		}
	}
}
