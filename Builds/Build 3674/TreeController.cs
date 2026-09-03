using System;
using System.Collections.Generic;
using Culling;
using Helpers;
using NaughtyAttributes;
using UnityEngine;

public class TreeController : ViewBlockingEntity, ICullable
{
	public static readonly List<TreeController> AllTrees = new List<TreeController>();

	private static MaterialPropertyBlock GpuInstancingPropertyBlock;

	private static readonly int TreeGpuInstancingMarker = Shader.PropertyToID("_TreeGpuInstancingMarker");

	[SerializeField]
	private LODGroup lodGroup;

	[SerializeField]
	private bool hasParticles = true;

	[ShowIf("hasParticles")]
	[SerializeField]
	private ParticleSystem leavesFallingParticleSystem;

	private int _layerIndex;

	public override void Start()
	{
		base.Start();
		ConfigureGpuInstancing();
		AllTrees.Add(this);
		_layerIndex = base.gameObject.layer;
		if (hasParticles)
		{
			leavesFallingParticleSystem.gameObject.SetActive(value: false);
		}
		lodGroup.ForceLOD(0);
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool open)
		{
			if (!PlayerPrefSettings.LowDetailCityMap && base.gameObject.activeInHierarchy)
			{
				lodGroup.ForceLOD(open ? 1 : 0);
			}
		});
		if ((bool)InstanceBehavior<CullingManager>.Instance)
		{
			InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.RegisterCullable(this);
		}
	}

	private void ConfigureGpuInstancing()
	{
		EnsurePropertyBlock();
		LOD[] lODs = lodGroup.GetLODs();
		for (int i = 0; i < lODs.Length; i++)
		{
			Renderer[] array = lODs[i].renderers;
			foreach (Renderer renderer in array)
			{
				if ((bool)renderer)
				{
					renderer.SetPropertyBlock(GpuInstancingPropertyBlock);
				}
			}
		}
	}

	private static void EnsurePropertyBlock()
	{
		if (GpuInstancingPropertyBlock == null)
		{
			GpuInstancingPropertyBlock = new MaterialPropertyBlock();
			GpuInstancingPropertyBlock.SetFloat(TreeGpuInstancingMarker, 0f);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!GameManager.isCitySceneBeingUnloaded && (bool)InstanceBehavior<CullingManager>.Instance)
		{
			InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.UnregisterCullable(this);
		}
	}

	public void HideForScreenshot()
	{
		foreach (Transform item in base.transform)
		{
			item.gameObject.layer = LayerHelper.VehiclesLayerIndex;
		}
	}

	public void ShowForScreenshot()
	{
		foreach (Transform item in base.transform)
		{
			item.gameObject.layer = _layerIndex;
		}
	}

	public void OnLod0()
	{
		if (hasParticles)
		{
			leavesFallingParticleSystem.gameObject.SetActive(value: true);
		}
	}

	public void OnLod1()
	{
		if (hasParticles)
		{
			leavesFallingParticleSystem.gameObject.SetActive(value: false);
		}
	}

	public void OnLod2()
	{
		OnLod1();
	}

	public BoundingSphere GetCullingBoundingSphere()
	{
		return new BoundingSphere(base.transform.position + Vector3.up * 2f, 4f);
	}
}
