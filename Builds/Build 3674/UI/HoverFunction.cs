using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace UI;

public class HoverFunction : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public UnityEvent<bool> onHover = new UnityEvent<bool>();

	public void OnPointerEnter(PointerEventData eventData)
	{
		onHover.Invoke(arg0: true);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		onHover.Invoke(arg0: false);
	}

	public void OnDisable()
	{
		onHover.Invoke(arg0: false);
	}
}
