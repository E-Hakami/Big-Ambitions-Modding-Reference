using JimmysUnityUtilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Character.Customization;

public class RotateButton : MonoBehaviour
{
	public Rotate[] rotates;

	[SerializeField]
	private bool isLeft;

	[SerializeField]
	private bool isRight;

	private void OnMouseDown()
	{
		if (EventSystem.current == null || EventSystem.current.IsPointerOverGameObject())
		{
			return;
		}
		if (isLeft)
		{
			rotates.ForEach(delegate(Rotate x)
			{
				x.StartRotatingLeft();
			});
		}
		else if (isRight)
		{
			rotates.ForEach(delegate(Rotate x)
			{
				x.StartRotatingRight();
			});
		}
		else
		{
			rotates.ForEach(delegate(Rotate x)
			{
				x.StartDrag();
			});
		}
	}

	private void OnMouseUp()
	{
		rotates.ForEach(delegate(Rotate x)
		{
			x.StopRotating();
		});
	}
}
