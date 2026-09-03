using UnityEngine;

public class EmployeeUniformPreview : MonoBehaviour
{
	[SerializeField]
	private GameObject setUp;

	[SerializeField]
	private Camera previewCamera;

	public AppearanceSetter appearanceSetter;

	public CharacterZoom characterZoom;

	private void Awake()
	{
		Hide();
	}

	public void SetCameraPosition(Vector3 cameraPosition)
	{
		previewCamera.transform.position = cameraPosition;
	}

	public void Show()
	{
		setUp.SetActive(value: true);
		appearanceSetter.gameObject.SetActive(value: true);
		characterZoom.ResetZoom();
	}

	public void Hide()
	{
		setUp.SetActive(value: false);
		appearanceSetter.gameObject.SetActive(value: false);
	}
}
