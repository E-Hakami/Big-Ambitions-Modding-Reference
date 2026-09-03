using BigAmbitions.InputSystem;
using Cinemachine;
using IngameDebugConsole;
using JimmysUnityUtilities;
using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
	private const float MoveSpeed = 5f;

	private const float FastMoveSpeed = 15f;

	private const float MouseSensitivity = 3f;

	private Vector3 _rotation;

	private MaterialPropertyBlock _materialPropertyBlock;

	private void Awake()
	{
		_rotation = base.transform.eulerAngles;
	}

	private void Update()
	{
		Vector3 vector = PlayerAction.Move.Vector().XYtoXZ();
		float num = (Input.GetKey(KeyCode.LeftShift) ? 15f : 5f);
		vector *= num * Time.unscaledDeltaTime;
		if (vector != Vector3.zero)
		{
			base.transform.Translate(vector, Space.Self);
		}
		float num2 = Input.GetAxis("Mouse X") * 3f;
		float num3 = Input.GetAxis("Mouse Y") * 3f;
		if (!Mathf.Approximately(num2, 0f) || !Mathf.Approximately(num3, 0f))
		{
			_rotation.y += num2;
			_rotation.x -= num3;
			_rotation.x = Mathf.Clamp(_rotation.x, -80f, 80f);
			base.transform.rotation = Quaternion.Euler(_rotation);
		}
	}

	private void LateUpdate()
	{
		if (Input.GetMouseButtonDown(0))
		{
			LogClickedObject();
		}
	}

	private void LogClickedObject()
	{
		Camera mainCamera = GameManager.GetMainCamera();
		if (!mainCamera || !Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out var hitInfo, 1000f))
		{
			return;
		}
		Transform parent = hitInfo.collider.transform;
		while ((bool)parent)
		{
			Debug.Log(parent.name + " - LocalPosition: " + parent.position.ToString());
			if ((bool)parent.GetComponentInParent<InteriorElement>())
			{
				LogHeightCutMinMax(parent);
			}
			parent = parent.parent;
		}
	}

	private void LogHeightCutMinMax(Transform child)
	{
		MeshRenderer component = child.GetComponent<MeshRenderer>();
		for (int i = 0; i < component.materials.Length; i++)
		{
			Material material = component.materials[i];
			if (material.HasProperty("_HeightCut_Min_Max"))
			{
				Vector4 vector = material.GetVector("_HeightCut_Min_Max");
				Debug.Log($"   Material[{i}]: {material.name} HeightCut_Min_Max: {vector.x}, {vector.y}");
				if (_materialPropertyBlock == null)
				{
					_materialPropertyBlock = new MaterialPropertyBlock();
				}
				component.GetPropertyBlock(_materialPropertyBlock, i);
				if (_materialPropertyBlock.HasVector("_HeightCut_Min_Max"))
				{
					Vector4 vector2 = _materialPropertyBlock.GetVector("_HeightCut_Min_Max");
					Debug.Log($"   MaterialPropertyBlock[{i}]: HeightCut_Min_Max: {vector2.x}, {vector2.y}");
				}
			}
		}
	}

	[ConsoleMethod("ToggleFreeFlyCamera", "Toggles the free fly camera on and off.", new string[] { })]
	public static void Command_ToggleFreeFlyCamera()
	{
		Camera mainCamera = GameManager.GetMainCamera();
		if (!mainCamera)
		{
			Debug.LogError("No main camera found to toggle FreeFlyCamera on.");
			return;
		}
		CinemachineBrain component = mainCamera.GetComponent<CinemachineBrain>();
		FreeFlyCamera component2 = mainCamera.GetComponent<FreeFlyCamera>();
		if ((bool)component2)
		{
			Object.Destroy(component2);
			if ((bool)component)
			{
				component.enabled = true;
			}
		}
		else
		{
			mainCamera.gameObject.AddComponent<FreeFlyCamera>();
			if ((bool)component)
			{
				component.enabled = false;
			}
		}
	}
}
