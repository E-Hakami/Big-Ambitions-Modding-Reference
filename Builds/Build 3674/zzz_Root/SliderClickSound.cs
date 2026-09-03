using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderClickSound : MonoBehaviour
{
	[SerializeField]
	private UiSound soundType;

	private void Start()
	{
		GetComponent<Slider>().onValueChanged.AddListener(delegate
		{
			UiSoundHelper.Play(soundType);
		});
	}
}
