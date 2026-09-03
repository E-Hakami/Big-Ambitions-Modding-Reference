using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Blueprints;
using Buildings;
using Buildings.BuildingTypes.Shared;
using BusinessLayoutSets;
using Entities;
using Helpers;
using UI.Smartphone.Apps.Contacts;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class RegenerateWronglyGeneratedAIBusinessEmployees : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		List<string> list = new List<string>();
		List<string> currentSkills = new List<string>();
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => !string.IsNullOrEmpty(x.businessOwnerRivalId)))
		{
			list.Clear();
			currentSkills.Clear();
			List<string> workStations = GetWorkStations(item);
			if (workStations != null)
			{
				list = (from x in item.CalculateNeededSkillForce(workStations)
					select x.skillName).Distinct().ToList();
				currentSkills = item.aiEmployees.Select((AiBusinessEmployeeData x) => x.primarySkillName).Distinct().ToList();
				if (list.Count != currentSkills.Count || !list.All((string x) => currentSkills.Contains(x)))
				{
					RemoveContactDataFromEmployees(gameInstance, item);
					item.GenerateAiBusinessEmployees();
				}
			}
		}
	}

	private static void RemoveContactDataFromEmployees(GameInstance gameInstance, BuildingRegistration buildingRegistration)
	{
		foreach (AiBusinessEmployeeData employee in buildingRegistration.aiEmployees)
		{
			Contact contact = SaveGameManager.Current.Contacts.Find((Contact x) => x.id == employee.GetEmployeeInstance().characterData.name && x.description == "employee_contact_description");
			if (contact != null)
			{
				ContactsApp.RemoveContextRelatedDataFromMessages(contact);
				gameInstance.Contacts.Remove(contact);
			}
		}
	}

	private static List<string> GetWorkStations(BuildingRegistration buildingRegistration)
	{
		List<string> list = new List<string>();
		Building building = BuildingHelper.GetBuilding(buildingRegistration.Address);
		BusinessLayoutSet orLoadBusinessLayoutSet = BusinessLayoutSetHelper.GetOrLoadBusinessLayoutSet(buildingRegistration.businessTypeName, new BuildingSizeInfo(building), buildingRegistration.Layout);
		if (orLoadBusinessLayoutSet == null)
		{
			return null;
		}
		foreach (BusinessLayoutSets.Item item in orLoadBusinessLayoutSet.Items)
		{
			BigAmbitions.Items.Item byName = ItemsGetter.GetByName(item.itemName);
			if ((byName.type & ItemType.EmployeeWorkstation) != 0)
			{
				list.Add(byName.itemName);
			}
		}
		return list;
	}
}
