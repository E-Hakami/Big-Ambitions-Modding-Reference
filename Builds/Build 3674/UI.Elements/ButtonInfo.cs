using System;

namespace UI.Elements;

public class ButtonInfo
{
	public readonly string name;

	public readonly string key;

	public readonly object arguments;

	public readonly string color;

	public readonly Action onClick;

	public readonly PlayerAction playerAction;

	public readonly bool interactable;

	public ButtonInfo(string name, string key, object arguments, string color, Action onClick, PlayerAction playerAction, bool interactable)
	{
		this.name = name;
		this.key = key;
		this.arguments = arguments;
		this.color = color;
		this.onClick = onClick;
		this.playerAction = playerAction;
		this.interactable = interactable;
	}

	public ButtonInfo(string name, string key, object arguments, string color, Action onClick, PlayerAction playerAction)
	{
		this.name = name;
		this.key = key;
		this.arguments = arguments;
		this.color = color;
		this.onClick = onClick;
		this.playerAction = playerAction;
		interactable = true;
	}

	public ButtonInfo(string name, string key, string color, Action onClick, PlayerAction playerAction)
	{
		this.name = name;
		this.key = key;
		this.color = color;
		this.onClick = onClick;
		this.playerAction = playerAction;
		interactable = true;
	}

	public ButtonInfo(string name, string key, string color, Action onClick, PlayerAction playerAction, bool interactable)
	{
		this.name = name;
		this.key = key;
		this.color = color;
		this.onClick = onClick;
		this.playerAction = playerAction;
		this.interactable = interactable;
	}
}
