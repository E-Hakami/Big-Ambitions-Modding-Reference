using UI.Components.AutoHide;

public class AutoHide : AutoHideBase
{
	protected override void OnHideChange(bool hide)
	{
		contentToCheck.gameObject.SetActive(hide);
	}
}
