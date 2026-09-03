using System;
using BehaviorDesigner.Runtime;
using BigAmbitions.Items;
using BigAmbitions.Tags;

[Serializable]
public class SharedItemTag : SharedVariable<string>
{
	private int _tagIndex = -1;

	private string _cachedTag;

	private bool _triedToGetTagIndex;

	public string[] AllWithTag
	{
		get
		{
			if (string.IsNullOrEmpty(mValue))
			{
				return Array.Empty<string>();
			}
			if (!string.Equals(_cachedTag, mValue, StringComparison.Ordinal))
			{
				_cachedTag = mValue;
				_tagIndex = -1;
				_triedToGetTagIndex = false;
			}
			if (!_triedToGetTagIndex)
			{
				_tagIndex = TagDatabaseHelper.TryGetTagIndex(mValue, "ItemTagDatabase");
				_triedToGetTagIndex = _tagIndex != -1;
			}
			if (_tagIndex != -1)
			{
				return ItemsGetter.GetAllItemNamesByTag(_tagIndex);
			}
			return Array.Empty<string>();
		}
	}

	public override string ToString()
	{
		return mValue;
	}

	public static implicit operator SharedItemTag(string value)
	{
		return new SharedItemTag
		{
			mValue = value
		};
	}
}
