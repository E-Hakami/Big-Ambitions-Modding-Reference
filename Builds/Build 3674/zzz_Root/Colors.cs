using System;
using UnityEngine;

[Serializable]
public class Colors
{
	public Color32 red;

	public Color32 green;

	public Color32 yellow;

	public Color32 darkGrey;

	public Color32 black;

	public Color32 blue;

	public Color32 darkBlue;

	public Color32 white;

	public Color32 lime;

	public Color32 lightGrey;

	public Color32 darkGreen;

	public Color32 orange;

	public Color32 lightRed;

	public Color32 midnight;

	public Color32 gold;

	public static Color32 Red => InstanceBehavior<GlobalReferences>.Instance.colors.red;

	public static Color32 Green => InstanceBehavior<GlobalReferences>.Instance.colors.green;

	public static Color32 Yellow => InstanceBehavior<GlobalReferences>.Instance.colors.yellow;

	public static Color32 DarkGrey => InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey;

	public static Color32 Black => InstanceBehavior<GlobalReferences>.Instance.colors.black;

	public static Color32 Blue => InstanceBehavior<GlobalReferences>.Instance.colors.blue;

	public static Color32 White => InstanceBehavior<GlobalReferences>.Instance.colors.white;

	public static Color32 Lime => InstanceBehavior<GlobalReferences>.Instance.colors.lime;

	public static Color32 LightGrey => InstanceBehavior<GlobalReferences>.Instance.colors.lightGrey;

	public static Color32 DarkGreen => InstanceBehavior<GlobalReferences>.Instance.colors.darkGreen;

	public static Color32 Orange => InstanceBehavior<GlobalReferences>.Instance.colors.orange;

	public static Color32 LightRed => InstanceBehavior<GlobalReferences>.Instance.colors.lightRed;

	public static Color32 Midnight => InstanceBehavior<GlobalReferences>.Instance.colors.midnight;

	public static Color32 Gold => InstanceBehavior<GlobalReferences>.Instance.colors.gold;
}
