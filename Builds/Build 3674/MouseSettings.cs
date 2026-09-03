using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MouseSettings", menuName = "BigAmbitions/MouseSettings")]
public class MouseSettings : ScriptableObject
{
	public float vehicleMouseHideTime = 3f;

	public float minPlayerMoveClickDistance = 0.5f;

	public float minPlayerMoveExtrapolateDistance = 2f;

	public List<CursorData> cursors = new List<CursorData>();

	private void OnValidate()
	{
		foreach (CursorData cursor in cursors)
		{
			cursor.name = cursor.type.ToString();
		}
	}
}
