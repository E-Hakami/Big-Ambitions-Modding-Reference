using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using BigAmbitions.DayNightCycle;
using BigAmbitions.SaveSystem.Legacy.CompatParsers;
using Buildings.Office.Headquarters;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Entities;

[Serializable]
public class TextMessage : IDeserializationCallback
{
	[Serializable]
	[Obsolete]
	public class MessageData
	{
		public string amount;

		public string amount2;

		public string businessName;

		public string businessName2;

		public string businessName3;

		public string businessType;

		public string selectedEmployee;

		public int startingDay;

		public int campaignDurationInDays;

		public int impressionsPerDay;

		public int amountOfCandidates;

		public string skillKey;

		public int days;

		public string itemName;

		public int startingHour;

		public int endingHour;

		public string vehicleTypeName;

		public string hour;

		public string minute;

		public int day;

		public string marketingType;

		public string agencyBusinessName;

		public Address address;

		public string autoTowServiceOption;

		public string text;

		public string investmentFund;

		public string employeeName;

		public HealthInsurancePlanType healthPlanType;

		public string jobDemandName;

		public string rivalName;

		public string products;

		public List<BusinessDeliveryInfo> deliveryInfoList;

		public string buildingType;

		public string sizeInfo;

		public List<ContextButtonData> contextButtonData;

		public List<string> listOfLabels;

		public Taxes taxes;
	}

	[Serializable]
	public class ContextAction
	{
		[Serializable]
		public enum ContextActionType
		{
			NotUsed,
			HealthInsurancePlanOffer,
			SalaryNegotiation
		}

		public ContextActionType type;

		public string employeeInstanceId;

		public string healthPlanOfferId;

		public string salaryNegotiationId;
	}

	[Serializable]
	public struct ContextButtonData(string key, ContextButton.BackgroundColor backgroundColor, UnityAction onClick = null)
	{
		public string key = key;

		public ContextButton.BackgroundColor backgroundColor = backgroundColor;

		public UnityAction onClick = onClick;
	}

	[FormerlySerializedAs("messageType")]
	public string messageKey;

	public Timestamp timestamp;

	public bool read;

	public bool isFromPlayer;

	public bool isNewInteraction;

	public ContextAction contextAction;

	public bool isSpecialMessage;

	public Dictionary<string, string> messageData;

	public AdditionalMessageData additionalData;

	[SerializeField]
	[Obsolete("Since EA 0.11")]
	private MessageData data;

	public TextMessage()
	{
	}

	public TextMessage(string messageKey, Dictionary<string, string> messageData = null, bool read = false, bool isNewInteraction = false, bool isSpecialMessage = false, AdditionalMessageData additionalData = null)
	{
		this.messageKey = messageKey;
		this.messageData = messageData;
		this.read = read;
		this.isNewInteraction = isNewInteraction;
		this.isSpecialMessage = isSpecialMessage;
		this.additionalData = additionalData;
		timestamp = TimeHelper.Now();
	}

	public void OnDeserialization(object sender)
	{
		if (data != null && (messageData == null || messageData.Count == 0))
		{
			messageData = MessageDataParser.ParseData(data);
		}
		if (data != null && additionalData == null)
		{
			additionalData = MessageDataParser.ParseAdditionalData(data);
		}
		if (data != null && (messageData != null || additionalData != null))
		{
			data = null;
		}
		if (messageData == null)
		{
			messageData = new Dictionary<string, string>();
		}
	}
}
