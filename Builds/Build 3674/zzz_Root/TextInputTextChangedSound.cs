using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class TextInputTextChangedSound : MonoBehaviour
{
	[SerializeField]
	private UiSound soundType = UiSound.Typing;

	private void Awake()
	{
		GetComponent<TMP_InputField>().onValueChanged.AddListener(delegate
		{
			UiSoundHelper.Play(soundType, randomPitch: true);
		});
	}
}
