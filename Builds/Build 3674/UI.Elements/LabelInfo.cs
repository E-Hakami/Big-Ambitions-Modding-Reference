using UnityEngine;

namespace UI.Elements;

public class LabelInfo
{
	public readonly string key;

	public readonly object arguments;

	public readonly Color32 color;

	public readonly bool localize;

	public LabelInfo(string key, object arguments, Color32 color, bool localize)
	{
		this.key = key;
		this.arguments = arguments;
		this.color = color;
		this.localize = localize;
	}

	public LabelInfo(string key, object arguments, Color32 color)
	{
		this.key = key;
		this.arguments = arguments;
		this.color = color;
		localize = true;
	}

	public LabelInfo(string key, Color32 color, bool localize)
	{
		this.key = key;
		this.color = color;
		this.localize = localize;
	}

	public LabelInfo(string key, Color32 color)
	{
		this.key = key;
		this.color = color;
		localize = true;
	}

	public LabelInfo(string key, bool localize)
	{
		this.key = key;
		this.localize = localize;
		color = InstanceBehavior<GlobalReferences>.Instance.colors.white;
	}

	public LabelInfo(string key)
	{
		this.key = key;
		color = InstanceBehavior<GlobalReferences>.Instance.colors.white;
		localize = true;
	}
}
