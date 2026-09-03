using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LogoShapesList : MonoBehaviour
{
	[SerializeField]
	private bool isPlayerList;

	[SerializeField]
	private Transform shapeTemplate;

	public UnityEvent<string> onSelectShape = new UnityEvent<string>();

	public UnityEvent onDeleteShape = new UnityEvent();

	private readonly Dictionary<string, Transform> _entries = new Dictionary<string, Transform>();

	private GameObject _previousHighlight;

	private int _selectedChildIndex = -1;

	private string _selectedShape;

	private void Awake()
	{
		Transform transform = shapeTemplate.Find("Selected");
		if ((bool)transform)
		{
			_selectedChildIndex = transform.GetSiblingIndex();
		}
		shapeTemplate.gameObject.SetActive(value: false);
	}

	public void SetUp(string initialSelectedShape)
	{
		_selectedShape = initialSelectedShape;
		ResetTemplate();
		StopAllCoroutines();
		StartCoroutine(LoadShapes());
	}

	public void SetSelectedShape(string shape)
	{
		_selectedShape = shape;
		if (shape != null && _entries.TryGetValue(shape, out var value))
		{
			SetSelectedEntry(value);
		}
		else
		{
			ClearSelectedEntry();
		}
	}

	private IEnumerator LoadShapes()
	{
		List<string> shapes;
		if (isPlayerList)
		{
			string customIconsFolderPath = LogoHelper.GetCustomIconsFolderPath();
			if (!Directory.Exists(customIconsFolderPath))
			{
				yield break;
			}
			shapes = Directory.GetFiles(customIconsFolderPath, "*.png").Select(Path.GetFileNameWithoutExtension).ToList();
		}
		else
		{
			yield return new WaitForSecondsRealtime(0.1f);
			shapes = LogoHelper.AvailableIcons;
		}
		if (shapes == null)
		{
			yield break;
		}
		for (int index = 0; index < shapes.Count; index++)
		{
			SetUpLogoShape(shapes[index]);
			if (!isPlayerList && index % 2 == 0)
			{
				yield return null;
			}
		}
	}

	private void SetUpLogoShape(string shape)
	{
		Sprite logoSprite = LogoHelper.GetLogoSprite(shape);
		if (!logoSprite)
		{
			return;
		}
		Transform entry = Object.Instantiate(shapeTemplate, shapeTemplate.parent);
		entry.name = shapeTemplate.name;
		entry.GetComponent<Image>().sprite = logoSprite;
		entry.GetComponent<Button>().onClick.AddListener(delegate
		{
			SelectShape(shape, entry);
		});
		if (isPlayerList && (bool)entry.Find("RemoveButton"))
		{
			entry.Find("RemoveButton").GetComponent<Button>().onClick.AddListener(delegate
			{
				RemoveCustomIcon(shape);
			});
		}
		entry.gameObject.SetActive(value: true);
		_entries[shape] = entry;
		if (shape == _selectedShape)
		{
			SetSelectedEntry(entry);
		}
	}

	private void SelectShape(string shape, Transform entry)
	{
		SetSelectedEntry(entry);
		onSelectShape.Invoke(shape);
	}

	private void SetSelectedEntry(Transform entry)
	{
		ClearSelectedEntry();
		if (_selectedChildIndex >= 0 && entry.childCount > _selectedChildIndex)
		{
			_previousHighlight = entry.GetChild(_selectedChildIndex).gameObject;
			_previousHighlight.SetActive(value: true);
		}
	}

	private void ClearSelectedEntry()
	{
		if ((bool)_previousHighlight)
		{
			_previousHighlight.SetActive(value: false);
		}
		_previousHighlight = null;
	}

	private void RemoveCustomIcon(string shape)
	{
		LogoHelper.RemoveCustomIcon(shape);
		onDeleteShape.Invoke();
	}

	private void ResetTemplate()
	{
		_entries.Clear();
		_previousHighlight = null;
		foreach (Transform item in shapeTemplate.parent)
		{
			if (item.name == shapeTemplate.name && item != shapeTemplate)
			{
				Object.Destroy(item.gameObject);
			}
		}
	}
}
