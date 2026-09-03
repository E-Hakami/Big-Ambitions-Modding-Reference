using System.Linq;
using DG.Tweening;
using Dialogs;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dialog;

public class SlotMachineUI : MonoBehaviour
{
	[SerializeField]
	private Icon[] icons;

	[SerializeField]
	private Image[] rows;

	public void SetIcon(int row, SlotMachineDialog.SlotElement element)
	{
		Icon icon = icons.FirstOrDefault((Icon x) => x.name == element.ToStringFast());
		if (icon == null)
		{
			Debug.LogError("Icon " + element.ToStringFast() + " not found for Slot Machine UI");
			return;
		}
		rows[row].sprite = icon.sprite;
		rows[row].DOFade(1f, 0.25f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
	}
}
