using UnityEngine;
using UnityEngine.EventSystems;

namespace UI;

public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[SerializeField]
	private GameObject effect;

	private void Start()
	{
		effect.SetActive(value: false);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		effect.SetActive(value: true);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		effect.SetActive(value: false);
	}

	public void OnDisable()
	{
		effect.SetActive(value: false);
	}
}
