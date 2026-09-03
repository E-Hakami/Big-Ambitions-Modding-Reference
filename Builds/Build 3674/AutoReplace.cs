using UI.Components.AutoHide;
using UnityEngine;

public class AutoReplace : AutoHideBase
{
	[Header("Replace")]
	[SerializeField]
	private GameObject whenShownObject;

	[SerializeField]
	private GameObject whenHiddenObject;

	protected override void OnHideChange(bool hide)
	{
		whenShownObject.SetActive(hide);
		whenHiddenObject.SetActive(!hide);
	}
}
