using System.IO;
using Extensions;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Smartphone.Apps.BizMan;

public class LogoShapes : MonoBehaviour
{
	[SerializeField]
	private LogoShapesList playerShapesList;

	[SerializeField]
	private LogoShapesList builtInShapesList;

	[HideInInspector]
	public UnityEvent<string> onSelectShape = new UnityEvent<string>();

	private string _selectedShape;

	private void Awake()
	{
		playerShapesList.onSelectShape.AddListener(SelectShape);
		playerShapesList.onDeleteShape.AddListener(RefreshLogoShapes);
		builtInShapesList.onSelectShape.AddListener(SelectShape);
	}

	public void BrowseFiles()
	{
		if (!Directory.Exists(LogoHelper.GetCustomIconsFolderPath()))
		{
			Directory.CreateDirectory(LogoHelper.GetCustomIconsFolderPath());
		}
		FolderHelper.OpenFolder(LogoHelper.GetCustomIconsFolderPath());
	}

	public void RefreshLogoShapes()
	{
		onSelectShape.Invoke(null);
		playerShapesList.SetUp(_selectedShape);
		builtInShapesList.SetUp(_selectedShape);
	}

	public void SetSelectedShape(string shape)
	{
		_selectedShape = shape;
		playerShapesList.SetSelectedShape(shape);
		builtInShapesList.SetSelectedShape(shape);
	}

	private void SelectShape(string shape)
	{
		SetSelectedShape(shape);
		onSelectShape.Invoke(shape);
	}
}
