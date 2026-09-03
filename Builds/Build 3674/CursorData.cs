using System;
using NaughtyAttributes;
using UnityEngine;

[Serializable]
public class CursorData
{
	[ReadOnly]
	[AllowNesting]
	public string name;

	public Texture2D cursorTexture;

	public Vector2 hotspot;

	public CursorType type;
}
