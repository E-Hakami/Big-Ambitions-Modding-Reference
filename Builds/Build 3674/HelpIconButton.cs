using UnityEngine;

public class HelpIconButton : MonoBehaviour
{
	public string linkSlug;

	public void OnClick()
	{
		if (!string.IsNullOrEmpty(linkSlug))
		{
			InstanceBehavior<HelpSystem>.Instance.Toggle(show: true, linkSlug.ToLower());
		}
	}
}
