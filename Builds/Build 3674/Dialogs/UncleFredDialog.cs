using Entities;
using Helpers;
using Localizor;

namespace Dialogs;

public class UncleFredDialog : Dialog
{
	public UncleFredDialog()
	{
		if (!TutorialHelper.IsTutorialEnabled())
		{
			DialogController.current.ShowEntry(StartReenableTutorial());
		}
		else if (TutorialHelper.currentQuest.checkpointData != null)
		{
			DialogController.current.ShowEntry(StartNextCheckpoint());
		}
		else
		{
			DialogController.current.ShowEntry(StartDisableTutorial());
		}
	}

	private DialogEntry StartNextCheckpoint()
	{
		string uncleFredDialogStart = TutorialHelper.currentQuest.checkpointData.uncleFredDialogStart;
		DialogController.current.contact.SendMessage(new TextMessage(uncleFredDialogStart, null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = uncleFredDialogStart.Localize(),
			Template = DialogEntry.TemplateType.Text,
			ConfirmTextOverride = "dialog_uncle_fred_next_checkpoint_button".Localize(),
			CancelTextOverride = "dialog_uncle_fred_first_cancel_dialog_button",
			OnConfirm = GoToNextCheckpoint,
			OnCancel = DialogController.current.CancelDialog
		};
	}

	private DialogEntry GoToNextCheckpoint()
	{
		GameEvent.Invoke("ba:gameevent_calledunclefred");
		string uncleFredDialogEnd = TutorialHelper.currentQuest.checkpointData.uncleFredDialogEnd;
		return new DialogEntry
		{
			messageData = uncleFredDialogEnd.Localize(),
			OnVisible = DialogController.current.CancelDialog
		};
	}

	private DialogEntry StartDisableTutorial()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_uncle_fred_cancel_tutorial_start_dialog", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "ba:messagetype_dialog_uncle_fred_cancel_tutorial_start_dialog".Localize(),
			Template = DialogEntry.TemplateType.Text,
			ConfirmTextOverride = "dialog_uncle_fred_cancel_tutorial_button".Localize(),
			CancelTextOverride = "dialog_uncle_fred_first_cancel_dialog_button",
			OnConfirm = ConfirDisableTutorial,
			OnCancel = DialogController.current.CancelDialog
		};
	}

	private DialogEntry StartReenableTutorial()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_uncle_fred_reenable_tutorial_start_dialog", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "ba:messagetype_dialog_uncle_fred_reenable_tutorial_start_dialog".Localize(),
			Template = DialogEntry.TemplateType.Text,
			ConfirmTextOverride = "dialog_uncle_fred_reenable_tutorial_button".Localize(),
			CancelTextOverride = "dialog_uncle_fred_cancel_reenable_button",
			OnConfirm = ReenableTutorial,
			OnCancel = DialogController.current.CancelDialog
		};
	}

	private DialogEntry ConfirDisableTutorial()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_uncle_fred_cancel_tutorial_confirm_dialog", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "ba:messagetype_dialog_uncle_fred_cancel_tutorial_confirm_dialog".Localize(),
			Template = DialogEntry.TemplateType.Text,
			ConfirmTextOverride = "dialog_uncle_fred_cancel_tutorial_button".Localize(),
			CancelTextOverride = "dialog_uncle_fred_second_cancel_dialog_button",
			OnConfirm = DisableTutorial,
			OnCancel = DialogController.current.CancelDialog
		};
	}

	private DialogEntry DisableTutorial()
	{
		TutorialHelper.DisableTutorial();
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_uncle_fred_cancel_tutorial_end_dialog", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "ba:messagetype_dialog_uncle_fred_cancel_tutorial_end_dialog".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = DialogController.current.FinishDialog
		};
	}

	private DialogEntry ReenableTutorial()
	{
		TutorialHelper.EnableTutorial();
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_uncle_fred_reenable_tutorial_end_dialog", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "ba:messagetype_dialog_uncle_fred_reenable_tutorial_end_dialog".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = DialogController.current.FinishDialog
		};
	}
}
