using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class ReflectionProbesController : InstanceBehavior<ReflectionProbesController>
{
	private static readonly Collider[] ObstructingColliders = new Collider[10];

	[SerializeField]
	private float obstructionCheckRadius = 0.5f;

	private readonly List<ReflectionProbeEntity> _reflectionProbes = new List<ReflectionProbeEntity>();

	public static void RegisterProbe(ReflectionProbeEntity probe)
	{
		InstanceBehavior<ReflectionProbesController>.Instance._reflectionProbes.Add(probe);
	}

	private void Start()
	{
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool open)
		{
			if (!open)
			{
				return;
			}
			foreach (ReflectionProbeEntity reflectionProbe in _reflectionProbes)
			{
				reflectionProbe.OnOpenCityMap();
			}
		});
		GlobalEvents.onCityMapClosed = (Action)Delegate.Combine(GlobalEvents.onCityMapClosed, (Action)delegate
		{
			foreach (ReflectionProbeEntity reflectionProbe2 in _reflectionProbes)
			{
				reflectionProbe2.OnCloseCityMap();
			}
		});
	}

	[Button("Enable all probes", EButtonEnableMode.Always)]
	public void EnableProbes()
	{
		ToggleProbes(show: true);
	}

	[Button("Disable all probes", EButtonEnableMode.Always)]
	public void DisableProbes()
	{
		ToggleProbes(show: false);
	}

	private void ToggleProbes(bool show)
	{
		ReflectionProbe[] array = UnityEngine.Object.FindObjectsOfType<ReflectionProbe>(includeInactive: true);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = show;
		}
	}

	[Button("Find obstructed probes", EButtonEnableMode.Always)]
	private void FindObstructedProbes()
	{
		ReflectionProbe[] array = UnityEngine.Object.FindObjectsOfType<ReflectionProbe>(includeInactive: true);
		int num = 0;
		ReflectionProbe[] array2 = array;
		foreach (ReflectionProbe probe in array2)
		{
			if (CheckProbeViaPhysics(probe))
			{
				num++;
			}
		}
		Debug.Log((num == 0) ? "No collider-obstructed Reflection Probes found." : $"Total collider-obstructed Reflection Probes: {num}. Click the logs to show them in the scene hierarchy.");
	}

	private bool CheckProbeViaPhysics(ReflectionProbe probe)
	{
		int num = Physics.OverlapSphereNonAlloc(probe.transform.position, obstructionCheckRadius, ObstructingColliders);
		bool flag = false;
		for (int i = 0; i < num; i++)
		{
			Collider collider = ObstructingColliders[i];
			if (!(collider == null) && !collider.isTrigger)
			{
				if (!flag)
				{
					flag = true;
					Debug.Log("Obstructed Reflection Probe found: " + probe.name, probe);
				}
				Debug.Log(" - collides with: " + collider.name, collider);
			}
		}
		return flag;
	}
}
