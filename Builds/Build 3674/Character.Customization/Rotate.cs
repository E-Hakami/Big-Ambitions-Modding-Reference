using BigAmbitions.InputSystem;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Character.Customization;

public class Rotate : MonoBehaviour
{
	[SerializeField]
	private float rotationSpeed;

	[SerializeField]
	private float maxRotationSpeed;

	private bool rotatingLeft;

	private bool rotatingRight;

	private bool dragging;

	private Vector3 currentDragPosition;

	private void Update()
	{
		if (EventSystem.current?.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.name == "InputField")
		{
			StopRotating();
			return;
		}
		if (PlayerAction.RotateLeft.Pressing())
		{
			rotatingLeft = true;
		}
		else if (PlayerAction.RotateLeft.Released())
		{
			rotatingLeft = false;
		}
		if (PlayerAction.RotateRight.Pressing())
		{
			rotatingRight = true;
		}
		else if (PlayerAction.RotateRight.Released())
		{
			rotatingRight = false;
		}
		if (rotatingLeft)
		{
			RotateLeft();
		}
		else if (rotatingRight)
		{
			RotateRight();
		}
		else if (dragging)
		{
			RotateDrag();
		}
	}

	public void StartRotatingLeft()
	{
		rotatingRight = false;
		rotatingLeft = true;
	}

	public void StartRotatingRight()
	{
		rotatingLeft = false;
		rotatingRight = true;
	}

	public void StopRotating()
	{
		rotatingRight = false;
		rotatingLeft = false;
		dragging = false;
	}

	public void StartDrag()
	{
		dragging = true;
		currentDragPosition = Input.mousePosition;
	}

	public void RotateDrag()
	{
		float num = currentDragPosition.x - Input.mousePosition.x;
		if (!(Mathf.Abs(num) <= 0.1f))
		{
			num /= 5f;
			if (num < 0f - maxRotationSpeed)
			{
				num = 0f - maxRotationSpeed;
			}
			else if (num > maxRotationSpeed)
			{
				num = maxRotationSpeed;
			}
			base.transform.Rotate(Vector3.up, num);
			currentDragPosition = Input.mousePosition;
		}
	}

	private void RotateLeft()
	{
		base.transform.Rotate(Vector3.up, (0f - rotationSpeed) * Time.deltaTime);
	}

	private void RotateRight()
	{
		base.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
	}
}
