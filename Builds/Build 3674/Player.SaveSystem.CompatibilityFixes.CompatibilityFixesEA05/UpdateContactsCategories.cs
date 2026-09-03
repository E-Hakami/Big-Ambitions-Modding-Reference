using Entities;
using UI.Smartphone.Apps.Contacts;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class UpdateContactsCategories : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (Contact contact in gameInstance.Contacts)
		{
			string text = contact.description.ToLower();
			if (text.Contains("employee_contact_description"))
			{
				contact.category = ContactCategoryName.Employees;
				continue;
			}
			if (text.Contains("businesstype"))
			{
				if (text.Contains("recruitmentagency") || text.Contains("marketingagency"))
				{
					contact.category = ContactCategoryName.Business;
				}
				else if (text.Contains("wholesalestore") || text.Contains("importexport"))
				{
					contact.category = ContactCategoryName.ImportsAndGoods;
				}
				else if (text.Contains("appliancestore") || text.Contains("officesupplystore") || text.Contains("furniturestore") || text.Contains("interiorinstallationfirm"))
				{
					contact.category = ContactCategoryName.FurnitureAndEquipment;
				}
				else if (text.Contains("bank"))
				{
					contact.category = ContactCategoryName.Finance;
				}
				continue;
			}
			if (text.Contains("hospital_health_insurance_manager"))
			{
				contact.category = ContactCategoryName.Business;
				continue;
			}
			string text2 = contact.id.ToLower();
			if (text.Contains("government"))
			{
				if (text2.Contains("internal_revenue_service"))
				{
					contact.category = ContactCategoryName.Finance;
				}
				else
				{
					contact.category = ContactCategoryName.General;
				}
			}
		}
	}
}
