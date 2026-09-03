using BigAmbitions.Items;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class UpdateSomeItemsAttachmentsToNewAttachments : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
			{
				if (value.itemName == "ba:itemname_table1")
				{
					foreach (AttachableChild stackedItem in value.stackedItems)
					{
						if (stackedItem.attachmentIndex >= 0 && stackedItem.attachmentIndex <= 2)
						{
							stackedItem.attachmentIndex = 1;
						}
						else if (stackedItem.attachmentIndex >= 3)
						{
							stackedItem.attachmentIndex -= 2;
						}
					}
				}
				else if (value.itemName == "ba:itemname_roundtable")
				{
					foreach (AttachableChild stackedItem2 in value.stackedItems)
					{
						if (stackedItem2.attachmentIndex < 5)
						{
							stackedItem2.attachmentIndex = 0;
						}
						else
						{
							stackedItem2.attachmentIndex -= 4;
						}
					}
				}
				else if (value.itemName == "ba:itemname_officedesk1")
				{
					foreach (AttachableChild stackedItem3 in value.stackedItems)
					{
						if (stackedItem3.attachmentIndex >= 0 && stackedItem3.attachmentIndex <= 1)
						{
							stackedItem3.attachmentIndex = 0;
						}
						else if (stackedItem3.attachmentIndex == 2)
						{
							stackedItem3.attachmentIndex = 1;
						}
						else if (stackedItem3.attachmentIndex == 3)
						{
							stackedItem3.attachmentIndex = 2;
						}
					}
				}
				else if (value.itemName == "ba:itemname_officedesk2left")
				{
					foreach (AttachableChild stackedItem4 in value.stackedItems)
					{
						if (stackedItem4.attachmentIndex >= 0 && (stackedItem4.attachmentIndex <= 3 || stackedItem4.attachmentIndex == 5 || stackedItem4.attachmentIndex == 6))
						{
							stackedItem4.attachmentIndex = 0;
						}
						else if (stackedItem4.attachmentIndex == 4)
						{
							stackedItem4.attachmentIndex = 1;
						}
						else if (stackedItem4.attachmentIndex == 7)
						{
							stackedItem4.attachmentIndex = 2;
						}
					}
				}
				else
				{
					if (!(value.itemName == "ba:itemname_officedesk2right"))
					{
						continue;
					}
					foreach (AttachableChild stackedItem5 in value.stackedItems)
					{
						if (stackedItem5.attachmentIndex >= 0 && (stackedItem5.attachmentIndex <= 3 || stackedItem5.attachmentIndex == 5 || stackedItem5.attachmentIndex == 6))
						{
							stackedItem5.attachmentIndex = 0;
						}
						else if (stackedItem5.attachmentIndex == 4)
						{
							stackedItem5.attachmentIndex = 1;
						}
						else if (stackedItem5.attachmentIndex == 7)
						{
							stackedItem5.attachmentIndex = 2;
						}
					}
				}
			}
		}
	}
}
