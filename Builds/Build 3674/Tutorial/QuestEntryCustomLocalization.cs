using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace Tutorial;

public abstract class QuestEntryCustomLocalization : ScriptableObject
{
	public abstract LanguageChangeEventDataHolder GetLocalization(string localizeKey);

	public virtual void Init()
	{
	}

	public virtual void Dispose()
	{
	}

	public virtual bool IsDynamic()
	{
		return false;
	}
}
