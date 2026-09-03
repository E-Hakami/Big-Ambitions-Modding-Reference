using System;
using System.Collections.Generic;
using System.Linq;
using Entities;
using Extensions;
using Localizor;
using UI.Dialog;
using UI.Notification;

namespace Dialogs;

public class HealthInsuranceNegotiationDialog : Dialog
{
	public static HealthInsurancePlanOffer planOffer;

	private float _bestPlayerOffer;

	private float _currentPlayerOffer;

	private float _previousPlayerOffer;

	private readonly Negotiator _negotiator;

	public HealthInsuranceNegotiationDialog()
	{
		_negotiator = new Negotiator(100, planOffer);
		_currentPlayerOffer = planOffer.initialOfferPrice;
		DialogController.current.ShowEntry(Start());
	}

	private DialogEntry Start()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_health_insurance_negotiation_start", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			messageData = "dialog_health_insurance_negotiation_start".Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = delegate
			{
				DialogController.current.ShowEntry(ShowPlayerOfferInput());
			}
		};
	}

	private DialogEntry ShowPlayerOfferInput()
	{
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Input,
			ConfirmTextOverride = "dialog_send_offer_button".Localize(),
			CancelTextOverride = "dialog_decline_button",
			InputTemplate = DialogEntry.InputTemplateName.PlayerOffer,
			OnConfirm = OnPlayerOfferSet,
			OnCancel = CancelOffer,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call"),
			OnVisible = delegate
			{
				PlayerOfferSettings inputComponent = DialogController.current.GetInputComponent<PlayerOfferSettings>();
				inputComponent.SetInitialAmount(_currentPlayerOffer);
				inputComponent.SetAllowNegative(allowNegative: false);
				inputComponent.FocusInput();
			}
		};
	}

	private DialogEntry OnPlayerOfferSet()
	{
		PlayerOfferSettings inputComponent = DialogController.current.GetInputComponent<PlayerOfferSettings>();
		float amount = 0f;
		if (!inputComponent.GetAmount(ref amount) || amount <= 0f)
		{
			Notifications.ShowError("common_notification_invalid_amount");
			return null;
		}
		_currentPlayerOffer = amount;
		var (flag, text) = _negotiator.EvaluateOffer(_currentPlayerOffer);
		if (flag)
		{
			planOffer.AcceptOffer(_currentPlayerOffer);
			Dictionary<string, string> messageData = new Dictionary<string, string> { 
			{
				"amount",
				_currentPlayerOffer.ToCurrencyFormat()
			} };
			DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_health_insurance_player_offer", messageData, read: true));
			DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_health_insurance_accepted_player_offer", null, read: true));
			return new DialogEntry
			{
				messageData = text.Localize(),
				OnVisible = DialogController.current.FinishDialog
			};
		}
		if (_negotiator.IsDeclinedByMood)
		{
			planOffer.DeclineOffer();
			Dictionary<string, string> messageData2 = new Dictionary<string, string> { 
			{
				"amount",
				amount.ToCurrencyFormat()
			} };
			DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_health_insurance_player_offer", messageData2, read: true));
			DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_health_insurance_declined_player_offer", null, read: true));
			return new DialogEntry
			{
				messageData = "dialog_health_insurance_declined_player_offer".Localize(),
				OnVisible = DialogController.current.FinishDialog
			};
		}
		return ShowFeedback(text);
	}

	private DialogEntry DoCounterOffer(NegotiationOptions[] options)
	{
		DialogEntry dialogEntry = new DialogEntry();
		if (options.Length == 2)
		{
			dialogEntry.messageData = "dialog_negotiation_final_counter_offer".Localize(new
			{
				counterOffer = ((float)Math.Round(_negotiator.GetCounterOffer(), 2)).ToCurrencyFormat()
			});
		}
		else
		{
			dialogEntry.messageData = "dialog_negotiation_counter_offer".Localize(new
			{
				counterOffer = ((float)Math.Round(_negotiator.GetCounterOffer(), 2)).ToCurrencyFormat()
			});
		}
		if (options.Contains(NegotiationOptions.Negotiate))
		{
			dialogEntry.SecondOptionTextOverride = "dialog_negotiate_button";
			dialogEntry.OnSecondOption = NegotiateCounterOffer;
		}
		if (options.Contains(NegotiationOptions.Accept))
		{
			dialogEntry.ConfirmTextOverride = "dialog_accept_button".Localize();
			dialogEntry.OnConfirm = AcceptCounterOffer;
		}
		if (options.Contains(NegotiationOptions.Decline))
		{
			dialogEntry.CancelTextOverride = "dialog_decline_button";
			dialogEntry.OnCancel = DeclineCounterOffer;
		}
		return dialogEntry;
	}

	private DialogEntry AcceptCounterOffer()
	{
		Dictionary<string, string> messageData = new Dictionary<string, string> { 
		{
			"amount",
			_negotiator.GetCounterOffer().ToCurrencyFormat()
		} };
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_health_insurance_player_offer", messageData, read: true));
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_health_insurance_accepted_player_offer", null, read: true));
		return new DialogEntry
		{
			messageData = "dialog_negotiation_accepted_glad_you_accepted".Localize(),
			OnVisible = delegate
			{
				planOffer.AcceptOffer(_negotiator.GetCounterOffer());
				DialogController.current.FinishDialog();
			}
		};
	}

	private void DeclineCounterOffer()
	{
		Dictionary<string, string> messageData = new Dictionary<string, string> { 
		{
			"amount",
			_negotiator.GetCounterOffer().ToCurrencyFormat()
		} };
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_health_insurance_player_offer", messageData, read: true));
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_negotiation_player_declined_counter_offer", null, read: true));
		planOffer.DeclineOffer();
		DialogController.current.FinishDialog();
	}

	private DialogEntry NegotiateCounterOffer()
	{
		return new DialogEntry
		{
			messageData = "dialog_negotiation_negotiate_counter_offer".Localize(),
			OnVisible = delegate
			{
				DialogController.current.ShowEntry(ShowPlayerOfferInput());
			}
		};
	}

	private DialogEntry ShowFeedback(string feedbackKey)
	{
		var (icon, secondaryIcon) = NegotiationHelper.GetMoodIcon(_negotiator);
		return new DialogEntry
		{
			messageData = feedbackKey.Localize(),
			Template = DialogEntry.TemplateType.Text,
			OnVisible = delegate
			{
				DialogController.current.ShowEntry(DoCounterOffer(_negotiator.GetOptions()));
			},
			Icon = icon,
			SecondaryIcon = secondaryIcon
		};
	}

	private void CancelOffer()
	{
		planOffer.accepted = false;
		planOffer.negotiationFinished = true;
		DialogController.current.CancelDialog();
	}
}
