using UnityEngine;

namespace Tutorial;

public class HelpSlugOverrider : ScriptableObject
{
	public virtual string GetTargetHelpSlug()
	{
		return null;
	}
}
