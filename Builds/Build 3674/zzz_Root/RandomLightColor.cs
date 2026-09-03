using Extensions;
using UnityEngine;

public class RandomLightColor : MonoBehaviour
{
	public Color[] colors;

	private void OnEnable()
	{
		Color random = colors.GetRandom();
		GetComponent<Light>().color = random;
	}
}
