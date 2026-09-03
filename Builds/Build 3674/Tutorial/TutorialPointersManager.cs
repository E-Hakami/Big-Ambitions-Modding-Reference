using System;
using System.Collections.Generic;
using UI.Guiders;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Tutorial;

public static class TutorialPointersManager
{
	private const string UiRootPath = "Canvases/";

	private static GameObject TutorialPointerTemplateWorld;

	private static GameObject TutorialPointerTemplateUi;

	private static Transform UiRoot;

	private static readonly List<TutorialPointer> CurrentQuestEntryPointers = new List<TutorialPointer>();

	private static bool Initialized;

	public static void Init()
	{
		if (!Initialized)
		{
			Initialized = true;
			SetUiRoot();
			TutorialPointerTemplateWorld = Addressables.LoadAssetAsync<GameObject>("TutorialPointerTemplateWorld").WaitForCompletion();
			TutorialPointerTemplateUi = Addressables.LoadAssetAsync<GameObject>("TutorialPointerTemplateUi").WaitForCompletion();
		}
	}

	public static void UpdateTutorialPointers(QuestEntry questEntry, DirectionGuiderType guiderType)
	{
		foreach (TutorialPointer currentQuestEntryPointer in CurrentQuestEntryPointers)
		{
			if (currentQuestEntryPointer.guiderType == guiderType && currentQuestEntryPointer != null)
			{
				UnityEngine.Object.Destroy(currentQuestEntryPointer.gameObject);
			}
		}
		CurrentQuestEntryPointers.RemoveAll((TutorialPointer x) => x.guiderType == guiderType);
		if (questEntry != null)
		{
			int num = questEntry.TutorialPointersData.Length;
			for (int num2 = 0; num2 < num; num2++)
			{
				TutorialPointerData tutorialPointerData = questEntry.TutorialPointersData[num2];
				TutorialPointer component = UnityEngine.Object.Instantiate(GetTutorialPointerTemplate(tutorialPointerData.Type)).GetComponent<TutorialPointer>();
				component.Hide();
				component.SetGuiderType(guiderType);
				component.data = tutorialPointerData;
				component.enabled = true;
				CurrentQuestEntryPointers.Add(component);
			}
		}
	}

	public static RectTransform FindUiRectByPath(string path)
	{
		if (UiRoot == null)
		{
			SetUiRoot();
		}
		if (UiRoot == null || string.IsNullOrEmpty(path))
		{
			return null;
		}
		Transform transform;
		if (!path.StartsWith("Canvases/"))
		{
			transform = UiRoot.transform.Find(path);
		}
		else
		{
			Transform uiRoot = UiRoot;
			int length = "Canvases/".Length;
			transform = uiRoot.Find(path.Substring(length, path.Length - length));
		}
		Transform transform2 = transform;
		if (!(transform2 != null))
		{
			return null;
		}
		return transform2.transform as RectTransform;
	}

	private static void SetUiRoot()
	{
		GameObject gameObject = GameObject.Find("Canvases/");
		if (gameObject != null)
		{
			UiRoot = gameObject.transform;
		}
	}

	private static GameObject GetTutorialPointerTemplate(TutorialPointerType type)
	{
		return type switch
		{
			TutorialPointerType.Ui => TutorialPointerTemplateUi, 
			TutorialPointerType.World => TutorialPointerTemplateWorld, 
			_ => throw new ArgumentOutOfRangeException("type", type, null), 
		};
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		CurrentQuestEntryPointers.Clear();
		Initialized = false;
		TutorialPointerTemplateWorld = null;
		TutorialPointerTemplateUi = null;
		UiRoot = null;
	}
}
