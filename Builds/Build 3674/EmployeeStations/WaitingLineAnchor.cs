using UnityEngine;

namespace EmployeeStations;

public class WaitingLineAnchor : MonoBehaviour
{
	[SerializeField]
	private Renderer meshRenderer;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color defaultColor;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color hoverColor;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color selectedColor;

	private static readonly int EmissiveColor = Shader.PropertyToID("_EmissiveColor");

	public int Index { get; private set; }

	private void Start()
	{
		SetDefaultColor();
	}

	public void OnIoEnter()
	{
		SetHoverColor();
	}

	public void OnIoExit()
	{
		SetDefaultColor();
	}

	public void Init(int anchorIndex)
	{
		Index = anchorIndex;
	}

	public void SetDefaultColor()
	{
		SetColor(defaultColor);
	}

	public void SetHoverColor()
	{
		SetColor(hoverColor);
	}

	public void SetSelectedColor()
	{
		SetColor(selectedColor);
	}

	private void SetColor(Color color)
	{
		meshRenderer.material.SetColor(EmissiveColor, color);
	}
}
