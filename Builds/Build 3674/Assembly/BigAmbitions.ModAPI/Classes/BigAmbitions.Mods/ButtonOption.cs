// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.Mods.ButtonOption
using System;
using BigAmbitions.Mods;

public sealed class ButtonOption : ModOption
{
	public Action OnClick { get; }

	public ButtonOption(string label, Action onClick = null)
		: base(null, label)
	{
		OnClick = onClick;
	}
}
