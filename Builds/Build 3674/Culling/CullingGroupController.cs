using System;
using System.Collections.Generic;
using UnityEngine;

namespace Culling;

public class CullingGroupController
{
	private readonly float _firstBand;

	private readonly float _secondBand;

	private readonly List<ICullable> _cullables = new List<ICullable>();

	private CullingGroup _cullingGroup;

	private BoundingSphere[] _spheres = new BoundingSphere[4069];

	private bool _isInitialized;

	private bool _recreateCullingGroup;

	public CullingGroupController(float firstBand, float secondBand)
	{
		_firstBand = firstBand;
		_secondBand = secondBand;
		Init();
	}

	private void Init()
	{
		GlobalEvents.RegisterOnGameLoadedLateCallback(CreateCullingGroup);
	}

	public void Update()
	{
		if (_recreateCullingGroup)
		{
			CreateCullingGroup();
			_recreateCullingGroup = false;
		}
	}

	private void CreateCullingGroup()
	{
		if (_cullingGroup == null)
		{
			_cullingGroup = new CullingGroup();
			CullingGroup cullingGroup = _cullingGroup;
			cullingGroup.onStateChanged = (CullingGroup.StateChanged)Delegate.Combine(cullingGroup.onStateChanged, new CullingGroup.StateChanged(OnCullingGroupStateChanged));
			_cullingGroup.targetCamera = GameManager.GetMainCamera();
			_cullingGroup.SetBoundingDistances(new float[2] { _firstBand, _secondBand });
			_cullingGroup.SetDistanceReferencePoint(InstanceBehavior<GameManager>.Instance.playerController.transform);
			_cullingGroup.SetBoundingSpheres(_spheres);
		}
		if (_cullables.Count > _spheres.Length)
		{
			IncreaseSpheresArraySize();
		}
		for (int i = 0; i < _cullables.Count; i++)
		{
			_spheres[i] = _cullables[i].GetCullingBoundingSphere();
		}
		_cullingGroup.SetBoundingSphereCount(_cullables.Count);
		_isInitialized = true;
	}

	private void IncreaseSpheresArraySize(bool copy = false)
	{
		BoundingSphere[] array = new BoundingSphere[_spheres.Length * 2];
		while (array.Length < _cullables.Count)
		{
			array = new BoundingSphere[array.Length * 2];
		}
		if (copy)
		{
			Array.Copy(_spheres, array, _spheres.Length);
		}
		_spheres = array;
		_cullingGroup.SetBoundingSpheres(_spheres);
	}

	private void OnCullingGroupStateChanged(CullingGroupEvent sphere)
	{
		if (sphere.currentDistance == 0)
		{
			_cullables[sphere.index].OnLod0();
		}
		else if (sphere.currentDistance == 1)
		{
			_cullables[sphere.index].OnLod1();
		}
		else
		{
			_cullables[sphere.index].OnLod2();
		}
	}

	public void Dispose()
	{
		_cullingGroup?.Dispose();
	}

	public void RegisterCullable(ICullable cullable)
	{
		_cullables.Add(cullable);
		if (_isInitialized)
		{
			if (_cullables.Count > _spheres.Length)
			{
				IncreaseSpheresArraySize(copy: true);
			}
			_spheres[_cullables.Count - 1] = cullable.GetCullingBoundingSphere();
			_cullingGroup.SetBoundingSphereCount(_cullables.Count);
		}
	}

	public void UnregisterCullable(ICullable cullable)
	{
		_cullables.Remove(cullable);
		if (_isInitialized)
		{
			_recreateCullingGroup = true;
		}
	}
}
