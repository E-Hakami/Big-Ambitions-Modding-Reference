using System.Collections.Generic;
using IngameDebugConsole;
using UnityEngine;

public class IndoorLightVisibilityManager : MonoBehaviour
{
	private const float RangeTolerance = 1f;

	private static readonly List<IndoorLightController> Controllers = new List<IndoorLightController>();

	private static bool IsEnabled = true;

	private readonly Plane[] _planes = new Plane[6];

	private float _nextUpdateTime;

	[ConsoleMethod("ToggleLightVisibilityManager", "Toggles Light Visibility Manager", new string[] { })]
	public static void Command_ToggleLightVisibilityManager(bool enable)
	{
		IsEnabled = enable;
		if (IsEnabled)
		{
			return;
		}
		foreach (IndoorLightController controller in Controllers)
		{
			controller.isVisibleByCamera = true;
			if (controller.IsLightOn)
			{
				controller.ToggleLight(lightsOn: true);
			}
		}
	}

	private void Start()
	{
		IsEnabled = true;
		Controllers.Clear();
	}

	private void LateUpdate()
	{
		if (!IsEnabled || !BuildingManager.IsInsideBuilding || Time.frameCount % 2 != 0)
		{
			return;
		}
		GeometryUtility.CalculateFrustumPlanes(GameManager.GetMainCamera(), _planes);
		for (int i = 0; i < Controllers.Count; i++)
		{
			IndoorLightController indoorLightController = Controllers[i];
			float radius = indoorLightController.GetLightsRange() - 1f;
			bool flag = indoorLightController.visible && SphereIntersectsFrustum(_planes, indoorLightController.GetLightsCenter(), radius);
			if (indoorLightController.IsLightOn && flag != indoorLightController.isVisibleByCamera)
			{
				indoorLightController.ToggleLight(flag);
			}
			indoorLightController.isVisibleByCamera = flag;
		}
	}

	private static bool SphereIntersectsFrustum(Plane[] planes, Vector3 center, float radius)
	{
		for (int i = 0; i < planes.Length; i++)
		{
			if (planes[i].GetDistanceToPoint(center) < 0f - radius)
			{
				return false;
			}
		}
		return true;
	}

	public static void Register(IndoorLightController controller)
	{
		if (!(controller == null))
		{
			Controllers.Add(controller);
		}
	}

	public static void Unregister(IndoorLightController controller)
	{
		if (!(controller == null))
		{
			Controllers.Remove(controller);
		}
	}
}
