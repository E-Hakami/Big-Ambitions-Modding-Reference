using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleClickSound : MonoBehaviour
{
	[SerializeField]
	private UiSound soundType;

	private void Awake()
	{
		GetComponent<Toggle>().onValueChanged.AddListener(delegate
		{
			UiSoundHelper.Play(soundType);
		});
	}
}
