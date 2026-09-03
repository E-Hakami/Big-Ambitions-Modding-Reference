using BigAmbitions.Rivals;
using RoboRyanTron.SearchableEnum;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/VariablePathGetters/RivalNameVariablePathGetter")]
public class RivalNameVariablePathGetter : TutorialPointerVariablePathGetter
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
			return specialRival.rivalData.rivalName;
		}
		return string.Empty;
	}
}
