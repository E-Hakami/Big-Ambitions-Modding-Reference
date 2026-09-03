using TMPro;
using UnityEngine;

namespace Player.HUD.ControlHints;

public class ControlsHintBindingUI : MonoBehaviour
{
	[SerializeField]
	private TMP_Text label;

	public void SetText(string value)
	{
		label.text = value;
	}
}
