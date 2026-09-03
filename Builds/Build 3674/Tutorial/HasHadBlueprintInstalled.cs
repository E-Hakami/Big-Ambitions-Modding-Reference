using System.Linq;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Blueprints/HasHadBlueprintInstalled")]
public class HasHadBlueprintInstalled : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		return SaveGameManager.Current.Transactions.Any((Transaction x) => x.transactionType == "ba:transaction_interiorinstallation");
	}
}
