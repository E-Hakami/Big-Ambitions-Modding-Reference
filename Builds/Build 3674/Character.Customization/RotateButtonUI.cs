using UnityEngine;
using UnityEngine.EventSystems;

namespace Character.Customization;

public class RotateButtonUI : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
{
	private Rotate _rotate;

	private void Awake()
	{
		_rotate = InstanceBehavior<GameManager>.Instance.employeeUniformPreview.appearanceSetter.GetComponent<Rotate>();
		if (_rotate == null)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		_rotate.StartDrag();
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		_rotate.StopRotating();
	}
}
