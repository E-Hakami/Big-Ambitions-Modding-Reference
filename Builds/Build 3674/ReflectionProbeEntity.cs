using UnityEngine;

public class ReflectionProbeEntity : MonoBehaviour
{
	[SerializeField]
	private ReflectionProbe reflectionProbe;

	private void Start()
	{
		if ((bool)InstanceBehavior<ReflectionProbesController>.Instance)
		{
			ReflectionProbesController.RegisterProbe(this);
		}
	}

	public void OnOpenCityMap()
	{
		reflectionProbe.enabled = false;
	}

	public void OnCloseCityMap()
	{
		reflectionProbe.enabled = true;
	}
}
