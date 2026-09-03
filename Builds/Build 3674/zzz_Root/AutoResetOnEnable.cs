using TMPro;
using UnityEngine;

public class AutoResetOnEnable : MonoBehaviour
{
	private void OnEnable()
	{
		if (TryGetComponent<TMP_InputField>(out var component))
		{
			component.text = "";
		}
	}
}
