using UnityEngine;
using UnityEngine.UI;

namespace UI.Elements;

public class Rating : MonoBehaviour
{
	public Image selection;

	public void SetPercentage(int percentage)
	{
		selection.fillAmount = percentage / 100;
	}

	private void Start()
	{
		SetPercentage(0);
	}
}
