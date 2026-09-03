using BigAmbitions.Factories.Recipes;
using Controllers;
using Factories.Timeline;
using UnityEngine;
using UnityEngine.Playables;

namespace Items.SpecialItems;

public class FactoryProductionMachineController : ItemController
{
	public Transform employeeSpot;

	[SerializeField]
	private PlayableDirector director;

	[SerializeField]
	private SpawnMorphPlayerData playerData;

	[SerializeField]
	private SpawnMorphPlayerData inputMachinePlayerData;

	[Header("Shaders")]
	[SerializeField]
	private Renderer colorRenderer;

	[SerializeField]
	private string colorShaderNameA;

	[SerializeField]
	private string colorShaderNameB;

	[SerializeField]
	private AudioSource[] sfxAudioSources;

	private MaterialPropertyBlock _materialPropertyBlock;

	public override bool Occupied
	{
		get
		{
			if (parentItemController != null)
			{
				return parentItemController.Occupied;
			}
			return false;
		}
	}

	public override void Awake()
	{
		base.Awake();
		_materialPropertyBlock = new MaterialPropertyBlock();
		AudioSource[] array = sfxAudioSources;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].outputAudioMixerGroup = InstanceBehavior<GlobalReferences>.Instance.factoryMachinesMixerGroup;
		}
	}

	public void TryStartVisuals(Recipe recipe, int startTimeMs)
	{
		RecipeMachineVisual machineVisual = recipe.GetMachineVisual(itemName);
		if (string.IsNullOrEmpty(machineVisual.inputItemName) || string.IsNullOrEmpty(machineVisual.outputItemName))
		{
			StopVisuals();
		}
		else
		{
			StartTimeline(machineVisual, startTimeMs);
		}
	}

	public override void OnItemPlacementFinished()
	{
		base.OnItemPlacementFinished();
		if (parentItemController == null)
		{
			StopVisuals();
		}
		else if (parentItemController is FactoryAssemblyMachineController factoryAssemblyMachineController)
		{
			factoryAssemblyMachineController.TryStartVisuals();
		}
	}

	public void StopVisuals()
	{
		if ((bool)director && director.state == PlayState.Playing)
		{
			director.Stop();
			director.time = 0.0;
		}
	}

	private void StartTimeline(RecipeMachineVisual visual, int startTimeMs)
	{
		playerData.SetSpawnMorphTrackItems(visual.inputItemName, visual.outputItemName);
		inputMachinePlayerData.SetSpawnMorphTrackItems(visual.inputItemName);
		SetShaderColors(visual);
		director.Restart(startTimeMs);
	}

	private void SetShaderColors(RecipeMachineVisual visual)
	{
		if (!string.IsNullOrEmpty(colorShaderNameA) || !string.IsNullOrEmpty(colorShaderNameB))
		{
			colorRenderer.GetPropertyBlock(_materialPropertyBlock);
			_materialPropertyBlock.SetColor(colorShaderNameA, visual.shaderColorA);
			_materialPropertyBlock.SetColor(colorShaderNameB, visual.shaderColorB);
			colorRenderer.SetPropertyBlock(_materialPropertyBlock);
		}
	}
}
