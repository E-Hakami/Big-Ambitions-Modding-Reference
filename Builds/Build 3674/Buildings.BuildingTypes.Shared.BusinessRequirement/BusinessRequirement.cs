using System.Linq;
using BigAmbitions.Items;
using Entities;
using HGAttributes;
using NaughtyAttributes;
using UnityEngine;

namespace Buildings.BuildingTypes.Shared.BusinessRequirement;

[CreateAssetMenu(menuName = "BigAmbitions/BusinessRequirement")]
public class BusinessRequirement : ScriptableObject
{
	public string businessRequirementName;

	[SerializeField]
	private string helpLink;

	[SerializeField]
	[ShowIf("MustInputTodoTaskItemName")]
	[AutocompleteDropdown("Items")]
	protected string todoTaskItemName;

	[SerializeField]
	[ShowIf("MustInputLocalizeKey")]
	protected string localizeKey;

	public virtual string GetLocalizeKey()
	{
		if (!MustInputLocalizeKey())
		{
			return todoTaskItemName;
		}
		return localizeKey;
	}

	public virtual string GetHelpLink(BuildingRegistration registration)
	{
		return helpLink;
	}

	public virtual TodoTaskType GetTodoTaskType()
	{
		return TodoTaskType.UnusedVar;
	}

	public virtual string GetTodoTaskItemName()
	{
		return todoTaskItemName;
	}

	public virtual string[] GetItems()
	{
		return null;
	}

	public virtual ItemType GetItemType()
	{
		return (ItemType)0;
	}

	protected virtual bool MustInputLocalizeKey()
	{
		return true;
	}

	protected virtual bool MustInputTodoTaskItemName()
	{
		return true;
	}

	public virtual bool IsRequirementMet(BuildingRegistration registration)
	{
		return false;
	}

	public virtual string[] GetRequiredItemsForTutorialPointers(BuildingRegistration registration)
	{
		return new string[1] { GetItems().First() };
	}

	public virtual string[] GetRequiredItemsForTutorial(BuildingRegistration registration)
	{
		return GetItems();
	}
}
