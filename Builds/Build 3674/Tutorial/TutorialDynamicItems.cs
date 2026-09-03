using System.Collections.Generic;
using BigAmbitions.Items;
using Helpers;

namespace Tutorial;

public class TutorialDynamicItems
{
	public readonly List<string[]> dynamicItems = new List<string[]>();

	public readonly List<bool> dynamicItemsFulfilled = new List<bool>();

	public bool invalid;

	private readonly List<bool> _dynamicItemsIsNeededForAttachedWorkSurfaceRequirement = new List<bool>();

	private readonly Dictionary<string, int> _itemCount = new Dictionary<string, int>();

	private int _minimumAmount = 1;

	public void SetMinimumAmount(int amount)
	{
		_minimumAmount = amount;
	}

	public void Reset()
	{
		dynamicItems.Clear();
		dynamicItemsFulfilled.Clear();
		_dynamicItemsIsNeededForAttachedWorkSurfaceRequirement.Clear();
		invalid = false;
		_minimumAmount = 1;
	}

	public void ResetFulfilled()
	{
		for (int i = 0; i < dynamicItemsFulfilled.Count; i++)
		{
			dynamicItemsFulfilled[i] = false;
		}
	}

	public void AddCollection(IEnumerable<string> items, bool isNeededForAttachedWorkSurfaceRequirement = false)
	{
		if (items == null)
		{
			return;
		}
		if (items is string[] item)
		{
			dynamicItems.Add(item);
			dynamicItemsFulfilled.Add(item: false);
			_dynamicItemsIsNeededForAttachedWorkSurfaceRequirement.Add(isNeededForAttachedWorkSurfaceRequirement);
			return;
		}
		List<string> list = new List<string>(items);
		if (list.Count > 0)
		{
			dynamicItems.Add(list.ToArray());
			dynamicItemsFulfilled.Add(item: false);
			_dynamicItemsIsNeededForAttachedWorkSurfaceRequirement.Add(isNeededForAttachedWorkSurfaceRequirement);
		}
	}

	public bool ContainsUnfulfilled(string itemName)
	{
		for (int i = 0; i < dynamicItems.Count; i++)
		{
			if (dynamicItemsFulfilled[i])
			{
				continue;
			}
			string[] array = dynamicItems[i];
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j] == itemName)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool NoItemsRemaining()
	{
		foreach (bool item in dynamicItemsFulfilled)
		{
			if (!item)
			{
				return false;
			}
		}
		return true;
	}

	public List<string> GetDynamicItemsFulfilled()
	{
		List<string> list = new List<string>();
		for (int i = 0; i < dynamicItems.Count; i++)
		{
			if (dynamicItemsFulfilled[i])
			{
				list.Add(dynamicItems[i][0]);
			}
		}
		return list;
	}

	public void CheckItem(string itemName, int amount = 1)
	{
		if (amount != 0)
		{
			if (_minimumAmount == 1)
			{
				CheckItemWithoutAmount(itemName);
			}
			else
			{
				CheckItemWithAmount(itemName, amount);
			}
		}
	}

	private void CheckItemWithoutAmount(string itemName)
	{
		int num = -1;
		for (int num2 = dynamicItems.Count - 1; num2 >= 0; num2--)
		{
			string[] array = dynamicItems[num2];
			if (!dynamicItemsFulfilled[num2])
			{
				for (int i = 0; i < array.Length; i++)
				{
					string text = array[i];
					if (itemName != text)
					{
						continue;
					}
					SetCollectionFulfilled(array, num2, i);
					if (_dynamicItemsIsNeededForAttachedWorkSurfaceRequirement[num2])
					{
						if (num == -1)
						{
							num = GetNumberOfWorkSurfaceAttachmentPoints(itemName);
						}
						num--;
						if (num > 0)
						{
							break;
						}
					}
					return;
				}
			}
		}
	}

	private static int GetNumberOfWorkSurfaceAttachmentPoints(string itemName)
	{
		int num = 0;
		AttachmentPoint[] attachmentPoints = PrefabHelper.LoadItemControllerFromPrefab(itemName).AttachmentPoints;
		for (int i = 0; i < attachmentPoints.Length; i++)
		{
			if (attachmentPoints[i].AttachmentPointType == AttachmentPointType.WorkSurface)
			{
				num++;
			}
		}
		return num;
	}

	private void CheckItemWithAmount(string itemName, int amount)
	{
		for (int num = dynamicItems.Count - 1; num >= 0; num--)
		{
			string[] array = dynamicItems[num];
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i];
				if (!(itemName != text))
				{
					if (!_itemCount.TryAdd(itemName, amount))
					{
						_itemCount[itemName] += amount;
					}
					if (_itemCount[itemName] >= _minimumAmount)
					{
						SetCollectionFulfilled(array, num, i);
						break;
					}
				}
			}
		}
	}

	private void SetCollectionFulfilled(string[] collection, int collectionIndex, int itemIndex)
	{
		ref string reference = ref collection[0];
		ref string reference2 = ref collection[itemIndex];
		string text = collection[itemIndex];
		string text2 = collection[0];
		reference = text;
		reference2 = text2;
		dynamicItemsFulfilled[collectionIndex] = true;
	}
}
