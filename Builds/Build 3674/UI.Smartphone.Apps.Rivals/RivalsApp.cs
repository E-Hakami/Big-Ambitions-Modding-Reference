using BigAmbitions.Rivals;
using JimmysUnityUtilities;
using UI.Smartphone.Apps.Rivals.Tables;
using UnityEngine;

namespace UI.Smartphone.Apps.Rivals;

public class RivalsApp : InstanceBehavior<RivalsApp>
{
	[SerializeField]
	private RivalLeaderboard leaderboard;

	[SerializeField]
	private SelectedRivalUI selectedRivalUI;

	[Header("Tables")]
	[SerializeField]
	private RivalBusinessesTable businessTable;

	[SerializeField]
	private RivalRealEstateTable realEstateTable;

	public RivalLeaderboardData selectedRival;

	private void OnEnable()
	{
		RivalsHelper.RefreshRivals();
		leaderboard.Load();
	}

	private void OnDisable()
	{
		GameEvent.Invoke("ba:gameevent_closedrivalsapp");
	}

	public void ShowRival(RivalLeaderboardData data)
	{
		selectedRival = data;
		GameEvent.Invoke("ba:gameevent_rivalselectedinapp");
		leaderboard.SelectButtonByName(data.entryName);
		selectedRivalUI.ShowRival(data);
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			businessTable.Load(data.ownedBusinesses);
			realEstateTable.Load(data.ownedBuildings);
		});
	}
}
