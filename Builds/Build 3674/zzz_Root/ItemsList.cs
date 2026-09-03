using System.Collections.Generic;
using TMPro;
using UI.Components;
using UnityEngine;

public class ItemsList : MonoBehaviour
{
	[SerializeField]
	private TMP_Text windowTitle;

	[SerializeField]
	private InputField searchInputField;

	[SerializeField]
	private Transform entriesContainer;

	private readonly List<ItemsListEntry> _entriesList = new List<ItemsListEntry>();

	public void Toggle(bool newState)
	{
		base.gameObject.SetActive(newState);
	}

	public void SetTitle(string title)
	{
		windowTitle.text = title;
	}

	public void AddEntry(ItemsListEntry entry)
	{
		_entriesList.Add(entry);
		entry.transform.SetParent(entriesContainer, worldPositionStays: false);
	}

	public void OnSearchBarTextChanged()
	{
		string value = searchInputField.tmpInputField.text.ToLower();
		if (string.IsNullOrEmpty(value))
		{
			foreach (ItemsListEntry entries in _entriesList)
			{
				entries.Show();
			}
			return;
		}
		foreach (ItemsListEntry entries2 in _entriesList)
		{
			if (entries2.NameLocalized.ToLower().Contains(value))
			{
				entries2.Show();
			}
			else
			{
				entries2.Hide();
			}
		}
	}

	public void Clear()
	{
		foreach (ItemsListEntry entries in _entriesList)
		{
			entries.Remove();
		}
		_entriesList.Clear();
		searchInputField.ClearText();
	}
}
