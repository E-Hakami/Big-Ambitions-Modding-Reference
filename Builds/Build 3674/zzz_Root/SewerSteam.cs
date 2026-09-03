using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class SewerSteam : MonoBehaviour
{
	private const float MinColorComponent = 0.05f;

	public static readonly List<SewerSteam> Instances = new List<SewerSteam>();

	private static readonly Vector3 CullBoundsSize = Vector3.one * 10f;

	private static readonly int ColorId = Shader.PropertyToID("Color");

	[SerializeField]
	private VisualEffect steamEffect;

	private bool _hiddenByColor;

	private void Awake()
	{
		Instances.Add(this);
	}

	private void OnEnable()
	{
		CheckVisibility();
		InvokeRepeating("CheckVisibility", Random.value, 1f);
	}

	private void OnDisable()
	{
		CancelInvoke("CheckVisibility");
	}

	private void OnDestroy()
	{
		Instances.Remove(this);
	}

	public void UpdateVisuals(Color color)
	{
		_hiddenByColor = color.r + color.g + color.b < 0.15f;
		if (steamEffect.HasVector4(ColorId))
		{
			steamEffect.SetVector4(ColorId, color);
		}
	}

	private void CheckVisibility()
	{
		if (_hiddenByColor)
		{
			steamEffect.enabled = false;
			return;
		}
		Bounds bounds = new Bounds(base.transform.position, CullBoundsSize);
		Plane[] planes = GeometryUtility.CalculateFrustumPlanes(GameManager.GetMainCamera());
		steamEffect.enabled = GeometryUtility.TestPlanesAABB(planes, bounds);
	}
}
