using UnityEngine;

namespace Controllers;

public class TreadmillController : WorkoutMachineController
{
	private static readonly int Running = Shader.PropertyToID("_Running");

	[SerializeField]
	private MeshRenderer meshRenderer;

	[SerializeField]
	private int beltMaterialIndex;

	[SerializeField]
	private float beltSpeedMultiplier;

	private float _currentShaderValue;

	private Material _beltMaterial;

	public override void Awake()
	{
		base.Awake();
		_beltMaterial = meshRenderer.materials[beltMaterialIndex];
	}

	private void Update()
	{
		if (Occupied)
		{
			_currentShaderValue += beltSpeedMultiplier * Time.deltaTime;
			while (_currentShaderValue > 1f)
			{
				_currentShaderValue--;
			}
			_beltMaterial.SetFloat(Running, _currentShaderValue);
		}
	}
}
