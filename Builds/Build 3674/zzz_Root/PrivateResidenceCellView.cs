using BaTable;
using Localizor.LanguageChangeEvent;
using Streets;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PrivateResidenceCellView : BaTableCellView<PrivateResidenceCellView.PrivateResidenceModel>
{
	public class PrivateResidenceModel
	{
		public string Id;

		public Address Address;

		public int Size;

		public PrivateResidenceModel(Address address, int size)
		{
			Address = address;
			Size = size;
		}
	}

	[SerializeField]
	private GameObject hoverOutline;

	public Button destinationButton;

	public TextLocalizationComponent address;

	public TMP_Text size;

	public override void SetData(PrivateResidenceModel data)
	{
		destinationButton.onClick.RemoveAllListeners();
		destinationButton.onClick.AddListener(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.SetDestination(data.Address);
		});
		address.SetValue(data.Address.ToFormattedString(), clearKey: true);
		size.text = data.Size.ToFormattedArea();
	}

	public override void ResetVisuals()
	{
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
	}

	public override void VisualizeSelected(bool selected)
	{
	}
}
