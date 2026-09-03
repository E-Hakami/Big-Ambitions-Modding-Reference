using System;
using UnityEngine;

namespace Scenes.MainMenu;

[Serializable]
public class NumberFormatSetup
{
	[field: SerializeField]
	public string VisualFormat { get; private set; }

	[field: SerializeField]
	public string GroupSeparator { get; private set; }

	[field: SerializeField]
	public string DecimalSeparator { get; private set; }

	public string Id => GroupSeparator + "|" + DecimalSeparator;
}
