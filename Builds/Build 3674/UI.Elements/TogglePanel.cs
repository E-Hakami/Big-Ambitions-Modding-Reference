using UnityEngine;

namespace UI.Elements;

public class TogglePanel : MonoBehaviour
{
	[SerializeField]
	private GameObject panel;

	public bool IsOpen => panel.activeSelf;

	public void Toggle()
	{
		if (IsOpen)
		{
			Close();
		}
		else
		{
			Open();
		}
	}

	public void Open()
	{
		if (!IsOpen)
		{
			OnOpen();
			panel.SetActive(value: true);
		}
	}

	public void Close()
	{
		if (IsOpen)
		{
			OnClose();
			panel.SetActive(value: false);
		}
	}

	protected virtual void OnOpen()
	{
	}

	protected virtual void OnClose()
	{
	}
}
