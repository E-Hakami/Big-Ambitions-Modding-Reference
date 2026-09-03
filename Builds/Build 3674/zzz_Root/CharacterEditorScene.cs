using UnityEngine;

public class CharacterEditorScene : MonoBehaviour
{
	[SerializeField]
	private Transform setUp;

	[SerializeField]
	private Camera mainCamera;

	[SerializeField]
	private float rotationSpeed = 100f;

	[SerializeField]
	private float zoomSpeed = 2f;

	[SerializeField]
	private float maxZoom = 100f;

	[SerializeField]
	private float minZoom = 2f;

	[SerializeField]
	private float moveSpeed = 1f;

	[SerializeField]
	private float maxY = 1f;

	[SerializeField]
	private float minY = -1f;

	private static bool RotatingLeft
	{
		get
		{
			if (!Input.GetKey(KeyCode.Q))
			{
				return Input.GetKey(KeyCode.LeftArrow);
			}
			return true;
		}
	}

	private static bool RotatingRight
	{
		get
		{
			if (!Input.GetKey(KeyCode.E))
			{
				return Input.GetKey(KeyCode.RightArrow);
			}
			return true;
		}
	}

	private static bool MovingUp
	{
		get
		{
			if (!Input.GetKey(KeyCode.W))
			{
				return Input.GetKey(KeyCode.UpArrow);
			}
			return true;
		}
	}

	private static bool MovingDown
	{
		get
		{
			if (!Input.GetKey(KeyCode.S))
			{
				return Input.GetKey(KeyCode.DownArrow);
			}
			return true;
		}
	}

	private void Update()
	{
		if (RotatingLeft)
		{
			setUp.Rotate(Vector3.up, (0f - rotationSpeed) * Time.deltaTime);
		}
		if (RotatingRight)
		{
			setUp.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
		}
		if (MovingUp && setUp.position.y > minY)
		{
			setUp.position += Vector3.down * (moveSpeed * Time.deltaTime);
		}
		if (MovingDown && setUp.position.y < maxY)
		{
			setUp.position += Vector3.up * (moveSpeed * Time.deltaTime);
		}
		float fieldOfView = mainCamera.fieldOfView;
		fieldOfView -= zoomSpeed * Input.mouseScrollDelta.y;
		fieldOfView = Mathf.Clamp(fieldOfView, minZoom, maxZoom);
		mainCamera.fieldOfView = fieldOfView;
	}
}
