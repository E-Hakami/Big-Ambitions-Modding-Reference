using System;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;

public class FixLegacyJobDemandMessageData : ICompatibilityFix
{
	private const string JobDemandDataKey = "jobDemandName";

	private const string LegacyJobDemandPrefix = "jobdemand_";

	private const string LocalizationPrefix = "ba:";

	public void Apply(GameInstance gameInstance)
	{
		foreach (Contact contact in gameInstance.Contacts)
		{
			if (contact?.messagesQueue == null)
			{
				continue;
			}
			foreach (TextMessage item in contact.messagesQueue)
			{
				if (item?.messageData != null && item.messageData.TryGetValue("jobDemandName", out var value) && value != null && value.StartsWith("jobdemand_", StringComparison.Ordinal))
				{
					item.messageData["jobDemandName"] = "ba:" + value;
				}
			}
		}
	}
}
