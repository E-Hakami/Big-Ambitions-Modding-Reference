using UnityEngine;

namespace Streets;

public class LayerSwitcher : MonoBehaviour
{
	[SerializeField]
	private int layerToSwitchTo;

	private void Awake()
	{
		base.gameObject.layer = layerToSwitchTo;
	}
}
