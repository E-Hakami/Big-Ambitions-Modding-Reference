using System.Collections.Generic;
using Data.VehicleColors;
using Streets;
using UnityEngine;

public class CarFeatures : MonoBehaviour
{
	private static MaterialPropertyBlock _propertyBlock;

	private static readonly int TintId = Shader.PropertyToID("Color_3d0f0cdbe6b74be28a1a5be5bab71dea");

	private static readonly int Color = Shader.PropertyToID("Color_f78fac473bac467092fb27521e9f71ea");

	private static readonly int Power = Shader.PropertyToID("Vector1_481fa2a8a5e94165a039319bfd512b76");

	private static readonly int Dirtiness = Shader.PropertyToID("_Dirtiness");

	public Renderer[] bodyMeshes;

	public LODGroup bodyLOD;

	public Renderer driverRenderer;

	public GameObject lights;

	private readonly List<BridgeController> _bridgesContaining = new List<BridgeController>();

	private bool _isHiddenByBridge;

	public VehicleColor VehicleColor { get; private set; }

	private static MaterialPropertyBlock PropertyBlock => _propertyBlock ?? (_propertyBlock = new MaterialPropertyBlock());

	private void OnDisable()
	{
		for (int num = _bridgesContaining.Count - 1; num >= 0; num--)
		{
			_bridgesContaining[num].RemoveCarFromBridge(this);
		}
	}

	public void EnterBridge(BridgeController bridge)
	{
		_bridgesContaining.Add(bridge);
		UpdateBridgeHiddenState();
	}

	public void ExitBridge(BridgeController bridge)
	{
		if (_bridgesContaining.Remove(bridge))
		{
			UpdateBridgeHiddenState();
		}
	}

	public BridgeController GetBridgeBelow()
	{
		Vector3 position = base.transform.position;
		for (int i = 0; i < _bridgesContaining.Count; i++)
		{
			if (_bridgesContaining[i].IsRoadBelow(position))
			{
				return _bridgesContaining[i];
			}
		}
		return null;
	}

	public void UpdateBridgeHiddenState()
	{
		bool flag = IsOnHiddenBridge();
		if (flag != _isHiddenByBridge)
		{
			_isHiddenByBridge = flag;
			if ((bool)lights)
			{
				lights.SetActive(!flag);
			}
			Renderer[] array = bodyMeshes;
			for (int i = 0; i < array.Length; i++)
			{
				ViewBlockingEntity.SetRendererToHideVisibility(array[i], flag);
			}
		}
	}

	private bool IsOnHiddenBridge()
	{
		Vector3 position = base.transform.position;
		for (int i = 0; i < _bridgesContaining.Count; i++)
		{
			BridgeController bridgeController = _bridgesContaining[i];
			if (bridgeController.IsInCameraBlockMode && bridgeController.IsRoadBelow(position))
			{
				return true;
			}
		}
		return false;
	}

	public void SetColor(VehicleColor vehicleColor)
	{
		VehicleColor = vehicleColor;
		Renderer[] array = bodyMeshes;
		for (int i = 0; i < array.Length; i++)
		{
			SetColor(array[i], vehicleColor);
		}
	}

	private static void SetColor(Renderer meshRenderer, VehicleColor vehicleColor)
	{
		meshRenderer.GetPropertyBlock(PropertyBlock);
		_propertyBlock.SetColor(TintId, vehicleColor.tint);
		_propertyBlock.SetColor(Color, vehicleColor.fresnelColor);
		_propertyBlock.SetFloat(Power, vehicleColor.fresnelPower);
		meshRenderer.SetPropertyBlock(PropertyBlock);
	}

	public void SetDirtiness(float dirtiness)
	{
		Renderer[] array = bodyMeshes;
		for (int i = 0; i < array.Length; i++)
		{
			SetDirtiness(array[i], dirtiness);
		}
	}

	private static void SetDirtiness(Renderer meshRenderer, float dirtiness)
	{
		meshRenderer.GetPropertyBlock(PropertyBlock);
		_propertyBlock.SetFloat(Dirtiness, dirtiness);
		meshRenderer.SetPropertyBlock(PropertyBlock);
	}
}
