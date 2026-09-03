using System.Collections.Generic;
using UnityEngine;

namespace UI.Components;

public class SelectorGroup : MonoBehaviour
{
	private readonly List<ISelectable> _selectors = new List<ISelectable>();

	private ISelectable _selectedSelectable;

	public void Register(ISelectable selectable)
	{
		if (selectable != null && !_selectors.Contains(selectable))
		{
			_selectors.Add(selectable);
		}
	}

	public void Unregister(ISelectable selectable)
	{
		if (_selectedSelectable == selectable)
		{
			_selectors.Remove(selectable);
		}
	}

	public void Select(ISelectable selected)
	{
		_selectedSelectable = selected;
		foreach (ISelectable selector in _selectors)
		{
			if (selector == selected)
			{
				selector.OnSelected();
			}
			else
			{
				selector.OnDeselected();
			}
		}
	}
}
