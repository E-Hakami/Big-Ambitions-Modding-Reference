// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.Mods.ModOption
using UnityEngine;

public abstract class ModOption
{
	public string Id { get; }

	public string Label { get; }

	public string ModId { get; internal set; }

	protected ModOption(string id, string label)
	{
		Id = id;
		Label = label;
	}

	public virtual void SpawnUi(Transform parent, string modId)
	{
		Debug.LogError("[ModOptions] " + GetType().Name + " for mod '" + modId + "' has no built-in renderer and did not override SpawnUi; option will not appear in the menu.");
	}
}
