using BigAmbitions.InputSystem;
using Cinemachine;
using Helpers;
using UnityEngine;

public class ScreenshotController : MonoBehaviour
{
	[SerializeField]
	private float freeLookCamSpeed;

	[SerializeField]
	private float freeLookCamOffsetOnStart;

	[SerializeField]
	private CanvasGroup uiCanvas;

	public static bool uiIsVisible = true;

	public static bool isInFreeLookMode;

	private Transform _freeLookCamera;

	private CanvasGroup _poiCanvas;

	private float _verticalSpeed;

	private float _dampVelocity;

	private CinemachineVirtualCameraBase _currentCamera;

	private void Start()
	{
		if (!InstanceBehavior<GameManager>.Instance.IsUIDevScene)
		{
			_freeLookCamera = InstanceBehavior<GameManager>.Instance.freeLookCamera.transform;
			_poiCanvas = InstanceBehavior<CityManager>.Instance.cityMap.transform.Find("Pois").GetComponent<CanvasGroup>();
		}
		base.enabled = false;
	}

	public void ToggleUIVisibility()
	{
		uiIsVisible = !uiIsVisible;
		int num = (uiIsVisible ? 1 : 0);
		uiCanvas.alpha = num;
		_poiCanvas.alpha = num;
		GlobalEvents.onScreenshotModeToggled?.Invoke(uiIsVisible);
	}

	public void ToggleFreeLookCamera()
	{
		isInFreeLookMode = !base.enabled;
		base.enabled = isInFreeLookMode;
		Cursor.visible = isInFreeLookMode;
		Cursor.lockState = (isInFreeLookMode ? CursorLockMode.Locked : CursorLockMode.None);
		if (isInFreeLookMode)
		{
			EnableFreeLookCamera();
		}
		else
		{
			DisableFreeLookCamera();
		}
	}

	private void EnableFreeLookCamera()
	{
		InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.FreeLookCamera);
		if (InstanceBehavior<GameManager>.Instance.selectedVehicle is CarController carController)
		{
			carController.vehicleController.enabled = false;
		}
		_currentCamera = CameraHelper.GetCurrentCamera();
		RepositionFreeLookCamera();
		CameraHelper.SetCamera(InstanceBehavior<GameManager>.Instance.freeLookCamera);
	}

	private void DisableFreeLookCamera()
	{
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.FreeLookCamera);
		if (InstanceBehavior<GameManager>.Instance.selectedVehicle is CarController carController)
		{
			carController.vehicleController.enabled = true;
		}
		CameraHelper.SetCamera(_currentCamera);
	}

	private void Update()
	{
		Vector3 position = _freeLookCamera.position;
		Vector2 vector = PlayerAction.Move.Vector();
		Vector3 vector2 = _freeLookCamera.right * vector.x;
		Vector3 vector3 = _freeLookCamera.forward * vector.y;
		_verticalSpeed = Mathf.SmoothDamp(_verticalSpeed, Input.GetKey(KeyCode.Q) ? 1 : (Input.GetKey(KeyCode.E) ? (-1) : 0), ref _dampVelocity, 0.1f);
		Vector3 vector4 = _freeLookCamera.up * _verticalSpeed;
		float num = freeLookCamSpeed;
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
		{
			num *= 5f;
		}
		position = Vector3.MoveTowards(position, position + vector2 + vector3 + vector4, Time.unscaledDeltaTime * num);
		_freeLookCamera.position = position;
		Vector2 vector5 = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
		Vector3 eulerAngles = _freeLookCamera.eulerAngles;
		float num2 = (0f - vector5.y) / 5f;
		float num3 = vector5.x / 5f;
		float num4 = eulerAngles.x + num2;
		float num5 = num4 % 180f;
		if (num5 > 80f && num5 < 100f)
		{
			num4 = eulerAngles.x;
		}
		_freeLookCamera.rotation = Quaternion.Euler(num4, eulerAngles.y + num3, 0f);
	}

	private void RepositionFreeLookCamera()
	{
		_freeLookCamera.position = _currentCamera.transform.position + freeLookCamOffsetOnStart * Vector3.up;
		_freeLookCamera.LookAt(InstanceBehavior<GameManager>.Instance.playerController.transform);
	}

	public void HidePoiCanvas(bool hide)
	{
		_poiCanvas.alpha = ((!hide) ? 1 : 0);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		uiIsVisible = true;
		isInFreeLookMode = false;
	}
}
