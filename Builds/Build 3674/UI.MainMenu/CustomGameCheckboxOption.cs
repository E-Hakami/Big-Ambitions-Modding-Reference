using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu;

public class CustomGameCheckboxOption : CustomGameOption<bool>
{
	[Header("Checkbox")]
	[SerializeField]
	private Toggle toggle;

	protected override void Awake()
	{
		base.Awake();
		toggle.onValueChanged.AddListener(delegate(bool state)
		{
			onValueChanged?.Invoke(state);
		});
	}

	public override void SetValue(bool value)
	{
		toggle.isOn = value;
	}
}
