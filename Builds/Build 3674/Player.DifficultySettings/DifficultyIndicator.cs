using System;
using UnityEngine;

namespace Player.DifficultySettings;

[Serializable]
public class DifficultyIndicator
{
	public string key;

	[Range(-1f, 2f)]
	public int difficulty;

	public Color color = Color.white;
}
