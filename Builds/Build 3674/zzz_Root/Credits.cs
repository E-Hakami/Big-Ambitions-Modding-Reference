using System;
using System.Collections.Generic;
using BigAmbitions.InputSystem;
using Extensions;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;

public class Credits : MonoBehaviour
{
	[Serializable]
	public class CreditsEntry
	{
		public string name;

		public string[] names;
	}

	public List<CreditsEntry> creditsEntries;

	public Transform creditsCategoryTemplate;

	public Transform creditsEntryTemplate;

	public Transform spacerTemplate;

	private Transform _content;

	private void Start()
	{
		_content = creditsCategoryTemplate.parent;
		creditsCategoryTemplate.ResetTemplate();
		creditsEntryTemplate.ResetTemplate();
		spacerTemplate.ResetTemplate();
		foreach (CreditsEntry creditsEntry in creditsEntries)
		{
			Transform obj = UnityEngine.Object.Instantiate(creditsCategoryTemplate, _content);
			obj.GetComponent<TextLocalizationComponent>().Key = creditsEntry.name;
			obj.gameObject.SetActive(value: true);
			string[] names = creditsEntry.names;
			foreach (string text in names)
			{
				Transform obj2 = UnityEngine.Object.Instantiate(creditsEntryTemplate, _content);
				obj2.GetComponent<TMP_Text>().text = text;
				obj2.gameObject.SetActive(value: true);
			}
			UnityEngine.Object.Instantiate(spacerTemplate, _content).gameObject.SetActive(value: true);
		}
	}

	private void Update()
	{
		if (PlayerAction.Cancel.Pressed())
		{
			Close();
		}
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
		InstanceBehavior<MainMenuController>.Instance.startView.SetActive(value: true);
	}
}
