using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime.Tasks;
using Entities;

[TaskCategory("Big Ambitions/Order")]
public class ProcessOrderEntryByName : BehaviorDesigner.Runtime.Tasks.Action
{
	public string[] itemNames;

	public SharedItemTag itemTag;

	[SharedRequired]
	public SharedCustomer sharedCustomer;

	public override void OnStart()
	{
		IEnumerable<string> enumerable = itemTag?.AllWithTag;
		IEnumerable<string> first = enumerable ?? Enumerable.Empty<string>();
		string[] second = itemNames ?? Array.Empty<string>();
		HashSet<string> hashSet = new HashSet<string>(first.Concat(second));
		foreach (OrderEntry entry in sharedCustomer.Value.order.entries)
		{
			if (hashSet.Contains(entry.itemName))
			{
				entry.processed = true;
			}
		}
	}
}
