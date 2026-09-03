using Dialogs;
using Entities;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace Buildings.BuildingTypes.Special.MovingCompany;

public class MovingContractEntry : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent infoText;

	[SerializeField]
	private Button cancelButton;

	private MovingServiceContract _cachedMovingServiceContract;

	public void SetupEntry(MovingServiceContract movingServiceContract, string originBusinessName, string destinationBusinessName)
	{
		_cachedMovingServiceContract = movingServiceContract;
		var arguments = new
		{
			originBusinessName = originBusinessName,
			destinationBusinessName = destinationBusinessName,
			day = TimeHelper.GetDayOfWeek(movingServiceContract.movingDay).GetLocalizeKey(),
			number = movingServiceContract.movingDay,
			hour = movingServiceContract.movingHour.GetFormattedTime()
		};
		infoText.SetData("ba:messagetype_dialog_moving_service_contracts_list_info".Localize(arguments));
		cancelButton.onClick.AddListener(OnCancel);
	}

	private void OnCancel()
	{
		if (_cachedMovingServiceContract != null && DialogController.current.dialog is MovingServiceDialog movingServiceDialog)
		{
			movingServiceDialog.OnCancelMovingContract(_cachedMovingServiceContract).ShowEntry();
		}
	}
}
