using UnityEngine;

[RequireComponent(typeof(Collider))]
[ExecuteAlways]
public class CrosswalkToTrafficLightLink : MonoBehaviour
{
	public GameObject redTrafficLight;

	private void OnValidate()
	{
		if (base.gameObject.scene.name != null && !(base.gameObject.scene.name == base.gameObject.name))
		{
			if (!base.gameObject.CompareTag("Crosswalk"))
			{
				Debug.LogError("Wrong tag for crosswalk", base.gameObject);
			}
			if (redTrafficLight == null)
			{
				Debug.LogError("Missing red light for crosswalk", base.gameObject);
			}
			if (TryGetComponent<Collider>(out var component) && !component.isTrigger)
			{
				Debug.LogError("Collider should be trigger", base.gameObject);
			}
		}
	}
}
