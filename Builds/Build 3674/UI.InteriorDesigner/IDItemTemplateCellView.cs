using System.Collections.Generic;
using BaTable;
using Extensions;
using UnityEngine;

namespace UI.InteriorDesigner;

public class IDItemTemplateCellView : BaTableCellView<IDItemTemplatesModel>
{
	[SerializeField]
	private Transform itemTemplate;

	private readonly List<IDItemTemplateBase> _itemTemplates = new List<IDItemTemplateBase>();

	protected override void Awake()
	{
		base.Awake();
		_itemTemplates.Capacity = 9;
		itemTemplate.ResetTemplate();
		for (int i = 0; i < 9; i++)
		{
			Transform transform = Object.Instantiate(itemTemplate, base.transform);
			transform.gameObject.SetActive(value: false);
			_itemTemplates.Add(transform.GetComponent<IDItemTemplateBase>());
		}
	}

	public override void SetData(IDItemTemplatesModel data)
	{
		for (int i = 0; i < 9; i++)
		{
			if (i < data.itemTemplates.Count)
			{
				_itemTemplates[i].gameObject.SetActive(value: true);
				_itemTemplates[i].SetUp(data.itemTemplates[i]);
			}
			else
			{
				_itemTemplates[i].gameObject.SetActive(value: false);
			}
		}
	}
}
