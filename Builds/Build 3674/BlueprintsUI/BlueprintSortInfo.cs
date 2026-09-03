using System.Collections.Generic;
using Blueprints;

namespace BlueprintsUI;

public class BlueprintSortInfo
{
	private BlueprintFilter _buildingTypeFilter;

	private BlueprintFilter _buildingSizeFilter;

	private BlueprintFilter _businessTypeFilter;

	private BlueprintFilter _buildVersionFilter;

	private SortByOption _sortByOption;

	private string _searchQuery;

	public bool HasChanged { get; set; }

	public BlueprintFilter BuildingTypeFilter => _buildingTypeFilter;

	public BlueprintFilter BuildingSizeFilter => _buildingSizeFilter;

	public BlueprintFilter BusinessTypeFilter => _businessTypeFilter;

	public BlueprintFilter BuildVersionFilter => _buildVersionFilter;

	public SortByOption SortByOption => _sortByOption;

	public string SearchQuery => _searchQuery;

	public bool HasBuildVersionFilterActive => IsFilterActive(_buildVersionFilter);

	public void SetBuildingTypeFilter(BlueprintFilter filter)
	{
		SetField(ref _buildingTypeFilter, filter);
	}

	public void SetBuildingSizeFilter(BlueprintFilter filter)
	{
		SetField(ref _buildingSizeFilter, filter);
	}

	public void SetBusinessTypeFilter(BlueprintFilter filter)
	{
		SetField(ref _businessTypeFilter, filter);
	}

	public void SetBuildVersionFilter(BlueprintFilter filter)
	{
		SetField(ref _buildVersionFilter, filter);
	}

	public void SetSearchQuery(string query)
	{
		SetField(ref _searchQuery, query);
	}

	public void SetSortByOption(SortByOption option)
	{
		SetField(ref _sortByOption, option);
	}

	public List<string> GetBuildingTypeActiveTags()
	{
		return GetActiveTags(BuildingTypeFilter);
	}

	public List<string> GetBuildingSizeActiveTags()
	{
		return GetActiveTags(BuildingSizeFilter);
	}

	public List<string> GetBusinessTypeActiveTags()
	{
		return GetActiveTags(BusinessTypeFilter);
	}

	public bool MatchesBuildVersion(int buildNumber)
	{
		if (!HasBuildVersionFilterActive)
		{
			return true;
		}
		string versionByBuildNumber = GameVersion.GetVersionByBuildNumber(buildNumber, useBlueprintPreVersionSystem: true);
		if (string.IsNullOrEmpty(versionByBuildNumber))
		{
			return false;
		}
		foreach (BlueprintFilterOption filterOption in _buildVersionFilter.filterOptions)
		{
			if (filterOption.toggled && filterOption is BlueprintBuildVersionFilterOption blueprintBuildVersionFilterOption && blueprintBuildVersionFilterOption.IsMatch(versionByBuildNumber))
			{
				return true;
			}
		}
		return false;
	}

	private static List<string> GetActiveTags(BlueprintFilter filter)
	{
		List<string> list = new List<string>();
		if (filter.allFilterToggle.isOn)
		{
			return list;
		}
		foreach (BlueprintFilterOption filterOption in filter.filterOptions)
		{
			if (filterOption.toggled)
			{
				list.Add(filterOption.Tag);
			}
		}
		return list;
	}

	private static bool IsFilterActive(BlueprintFilter filter)
	{
		if (filter == null || filter.allFilterToggle == null || filter.allFilterToggle.isOn)
		{
			return false;
		}
		foreach (BlueprintFilterOption filterOption in filter.filterOptions)
		{
			if (filterOption.toggled && !(filterOption is BlueprintAllFilterOption))
			{
				return true;
			}
		}
		return false;
	}

	public void SortBlueprints(ref List<Blueprint> blueprints)
	{
		foreach (Blueprint blueprint in blueprints)
		{
			blueprint.FetchSteamInfo();
		}
		blueprints.Sort(delegate(Blueprint a, Blueprint b)
		{
			bool flag = a.metadata.blueprintType == BlueprintType.SavedLocally;
			bool flag2 = b.metadata.blueprintType == BlueprintType.SavedLocally;
			return (flag != flag2) ? (flag ? 1 : (-1)) : (SortByOption switch
			{
				SortByOption.Popularity => b.downloads.CompareTo(a.downloads), 
				SortByOption.Rating => b.rating.CompareTo(a.rating), 
				SortByOption.UploadDate => b.releaseDate.CompareTo(a.releaseDate), 
				_ => 0, 
			});
		});
	}

	private void SetField<T>(ref T field, T value)
	{
		if (!EqualityComparer<T>.Default.Equals(field, value))
		{
			field = value;
			HasChanged = true;
		}
	}
}
