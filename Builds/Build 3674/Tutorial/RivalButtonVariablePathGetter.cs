using BigAmbitions.Rivals;
using RoboRyanTron.SearchableEnum;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/VariablePathGetters/RivalButtonVariablePathGetter")]
public class RivalButtonVariablePathGetter : TutorialPointerVariablePathGetter
{
	private enum RivalFirstType
	{
		FirstMessage,
		FirstAttack,
		FirstActive
	}

	[SerializeField]
	[SearchableEnum]
	private RivalFirstType firstType;

	[SerializeField]
	private string buttonNamePrefix = "RivalLeaderboardButton_";

	public override string GetVariablePath()
	{
		SpecialRival specialRival = firstType switch
		{
			RivalFirstType.FirstMessage => RivalsHelper.GetFirstMessageRival(), 
			RivalFirstType.FirstAttack => RivalsHelper.GetFirstAttackRival(), 
			RivalFirstType.FirstActive => RivalsHelper.GetFirstActiveRival(), 
			_ => null, 
		};
		if (!(specialRival == null))
		{
			return buttonNamePrefix + specialRival.rivalData.rivalName;
		}
		return string.Empty;
	}
}
