using BigAmbitions.Items;
using UnityEngine;

namespace Tutorial.ItemOrderingConditions;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/ItemOrderingCondition/OrderByAttachableSurface")]
public class OrderByAttachableSurface : ItemOrderingComparison
{
	public override int Comparison(string a, string b)
	{
		bool value = ItemsGetter.GetByName(a).type.HasFlag(ItemType.AttachableWorkSurface);
		return ItemsGetter.GetByName(b).type.HasFlag(ItemType.AttachableWorkSurface).CompareTo(value);
	}
}
