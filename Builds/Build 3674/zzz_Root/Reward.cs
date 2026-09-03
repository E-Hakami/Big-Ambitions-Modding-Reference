using Localizor.LanguageChangeEvent;
using UnityEngine;

public abstract class Reward : ScriptableObject
{
	[SerializeField]
	private string title;

	public abstract void OnComplete();

	public virtual LanguageChangeEventDataHolder GetTitle()
	{
		return LanguageChangeEventDataHolder.Create(title);
	}
}
