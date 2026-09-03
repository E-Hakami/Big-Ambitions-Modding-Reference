using System;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.SoundSystem;
using Blueprints;
using BlueprintsUI;
using Dancing;
using Dialogs;
using Entities;
using Enums;
using Parking.UndergroundParking;
using PlayerActivity;
using PlayerActivity.Activities.Paid;
using UI;
using UI.Notification;
using UnityEngine;

public static class EnumHelpers
{
	public static string ToStringFast(this ItemType value)
	{
		return value switch
		{
			ItemType.Decoration => "Decoration", 
			ItemType.RetailProduct => "RetailProduct", 
			ItemType.ServiceProduct => "ServiceProduct", 
			ItemType.PointOfSale => "PointOfSale", 
			ItemType.EmployeeWorkstation => "EmployeeWorkstation", 
			ItemType.StorageShelf => "StorageShelf", 
			ItemType.ShowcaseShelf => "ShowcaseShelf", 
			ItemType.FactoryMachine => "FactoryMachine", 
			ItemType.ActivityItem => "ActivityItem", 
			ItemType.Security => "Security", 
			ItemType.RadioSource => "RadioSource", 
			ItemType.LightSource => "LightSource", 
			ItemType.Seat => "Seat", 
			ItemType.ForSale => "ForSale", 
			ItemType.Seasonal => "Seasonal", 
			ItemType.JobDemand => "JobDemand", 
			ItemType.BusinessRequirement => "BusinessRequirement", 
			ItemType.Desk => "Desk", 
			ItemType.AttachableWorkSurface => "AttachableWorkSurface", 
			ItemType.SpecialBusiness => "SpecialBusiness", 
			ItemType.WorkoutMachine => "WorkoutMachine", 
			ItemType.Table => "Table", 
			ItemType.FlatDecoration => "FlatDecoration", 
			ItemType.Computer => "Computer", 
			ItemType.Sink => "Sink", 
			ItemType.Toilet => "Toilet", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this SoundType value)
	{
		return value switch
		{
			SoundType.FootstepAsphalt => "FootstepAsphalt", 
			SoundType.FootstepWood => "FootstepWood", 
			SoundType.FootstepCarpet => "FootstepCarpet", 
			SoundType.WallPainting => "WallPainting", 
			SoundType.FootstepTile => "FootstepTile", 
			SoundType.FootstepGrass => "FootstepGrass", 
			SoundType.ObjectPickup => "ObjectPickup", 
			SoundType.ObjectPutDown => "ObjectPutDown", 
			SoundType.ObjectRotate => "ObjectRotate", 
			SoundType.ObjectSnap => "ObjectSnap", 
			SoundType.ElevatorDoorClose => "ElevatorDoorClose", 
			SoundType.ShoppingStoreEnter => "ShoppingStoreEnter", 
			SoundType.DoorOpen => "DoorOpen", 
			SoundType.DoorClose => "DoorClose", 
			SoundType.PaperbagGrabbed => "PaperbagGrabbed", 
			SoundType.ShoppingBasketPickup => "ShoppingBasketPickup", 
			SoundType.PurchaseSuccess => "PurchaseSuccess", 
			SoundType.MopOneShot => "MopOneShot", 
			SoundType.Eating => "Eating", 
			SoundType.MopPickup => "MopPickup", 
			SoundType.ElevatorDoorOpen => "ElevatorDoorOpen", 
			SoundType.NpcWhistle => "NpcWhistle", 
			SoundType.NpcFemaleCough => "NpcFemaleCough", 
			SoundType.NpcMaleCough => "NpcMaleCough", 
			SoundType.BedLayDown => "BedLayDown", 
			SoundType.BedStandUp => "BedStandUp", 
			SoundType.ChairSitDown => "ChairSitDown", 
			SoundType.ChairStandUp => "ChairStandUp", 
			SoundType.GlassDoorOpen => "GlassDoorOpen", 
			SoundType.GlassDoorClose => "GlassDoorClose", 
			SoundType.MetroAmbience => "MetroAmbience", 
			SoundType.CardboardBoxPutDown => "CardboardBoxPutDown", 
			SoundType.HandTruckEnter => "HandTruckEnter", 
			SoundType.HandTruckExit => "HandTruckExit", 
			SoundType.CarDoorOpenClose => "CarDoorOpenClose", 
			SoundType.CarRefuel => "CarRefuel", 
			SoundType.CarRepair => "CarRepair", 
			SoundType.GarageDoorOpen => "GarageDoorOpen", 
			SoundType.GarageDoorClose => "GarageDoorClose", 
			SoundType.BlackJackStart => "BlackJackStart", 
			SoundType.RouletteStart => "RouletteStart", 
			SoundType.SlotMachineStart => "SlotMachineStart", 
			SoundType.SlotMachineJackpot => "SlotMachineJackpot", 
			SoundType.SlotMachineWin => "SlotMachineWin", 
			SoundType.SlotMachineLoose => "SlotMachineLoose", 
			SoundType.BlackJackCard => "BlackJackCard", 
			SoundType.NpcMaleYawn => "NpcMaleYawn", 
			SoundType.NpcFemaleYawn => "NpcFemaleYawn", 
			SoundType.NpcMaleDisappointed => "NpcMaleDisappointed", 
			SoundType.NpcFemaleDisappointed => "NpcFemaleDisappointed", 
			SoundType.NpcMaleProducerInteraction => "NpcMaleProducerInteraction", 
			SoundType.NpcFemaleProducerInteraction => "NpcFemaleProducerInteraction", 
			SoundType.DrinkingSoda => "DrinkingSoda", 
			SoundType.AddProductToBasket => "AddProductToBasket", 
			SoundType.CarBrokenStartupSmall => "CarBrokenStartupSmall", 
			SoundType.CarBrokenStartupBig => "CarBrokenStartupBig", 
			SoundType.CarFuelEmptyStartupSmall => "CarFuelEmptyStartupSmall", 
			SoundType.CarFuelEmptyStartupBig => "CarFuelEmptyStartupBig", 
			SoundType.HandTruckAddBox => "HandTruckAddBox", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this SignType value)
	{
		return value switch
		{
			SignType.Type1 => "Type1", 
			SignType.Type2 => "Type2", 
			SignType.Type3 => "Type3", 
			SignType.Type4 => "Type4", 
			SignType.Type5 => "Type5", 
			SignType.Type6 => "Type6", 
			SignType.Type7 => "Type7", 
			SignType.Type8 => "Type8", 
			SignType.Type9 => "Type9", 
			SignType.Type10 => "Type10", 
			SignType.Type11 => "Type11", 
			SignType.Type12 => "Type12", 
			SignType.Type13 => "Type13", 
			SignType.Type14 => "Type14", 
			SignType.Type15 => "Type15", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this SubwayStationName value)
	{
		return value switch
		{
			SubwayStationName.GarmentDistrictEastStation => "GarmentDistrictEastStation", 
			SubwayStationName.GarmentDistrictWestStation => "GarmentDistrictWestStation", 
			SubwayStationName.GarmentDistrictHarborStation => "GarmentDistrictHarborStation", 
			SubwayStationName.HellsKitchenNorthStation => "HellsKitchenNorthStation", 
			SubwayStationName.HellsKitchenSouthStation => "HellsKitchenSouthStation", 
			SubwayStationName.HellsKitchenEastStation => "HellsKitchenEastStation", 
			SubwayStationName.HellsKitchenWestStation => "HellsKitchenWestStation", 
			SubwayStationName.MurrayHillNorthStation => "MurrayHillNorthStation", 
			SubwayStationName.MurrayHillSouthStation => "MurrayHillSouthStation", 
			SubwayStationName.MurrayHillEastStation => "MurrayHillEastStation", 
			SubwayStationName.MidtownTimesSquareStation => "MidtownTimesSquareStation", 
			SubwayStationName.MidtownStPatrickStation => "MidtownStPatrickStation", 
			SubwayStationName.MidtownMadisonParkStation => "MidtownMadisonParkStation", 
			SubwayStationName.MidtownNorthWestStation => "MidtownNorthWestStation", 
			SubwayStationName.LowerManhattanCenterStation => "LowerManhattanCenterStation", 
			SubwayStationName.IndustryCityCenterStation => "IndustryCityCenterStation", 
			SubwayStationName.LowerManhattanNorthStation => "LowerManhattanNorthStation", 
			SubwayStationName.IndustryCityHarborStation => "IndustryCityHarborStation", 
			SubwayStationName.IndustryCitySunsetParkStation => "IndustryCitySunsetParkStation", 
			SubwayStationName.TheHamptonsStation => "TheHamptonsStation", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this DialogEntry.InputTemplateName value)
	{
		return value switch
		{
			DialogEntry.InputTemplateName.None => "None", 
			DialogEntry.InputTemplateName.RecruitmentSettings => "RecruitmentSettings", 
			DialogEntry.InputTemplateName.BankInvestment => "BankInvestment", 
			DialogEntry.InputTemplateName.BankLoan => "BankLoan", 
			DialogEntry.InputTemplateName.RecruitmentDeadline => "RecruitmentDeadline", 
			DialogEntry.InputTemplateName.MarketingCampaignSettings => "MarketingCampaignSettings", 
			DialogEntry.InputTemplateName.DeliveryContractSettings => "DeliveryContractSettings", 
			DialogEntry.InputTemplateName.DeliveryContractsList => "DeliveryContractsList", 
			DialogEntry.InputTemplateName.ImportPartnershipSettings => "ImportPartnershipSettings", 
			DialogEntry.InputTemplateName.AutoTowServiceSettings => "AutoTowServiceSettings", 
			DialogEntry.InputTemplateName.FurnitureDeliverySettings => "FurnitureDeliverySettings", 
			DialogEntry.InputTemplateName.SlotMachine => "SlotMachine", 
			DialogEntry.InputTemplateName.Roulette => "Roulette", 
			DialogEntry.InputTemplateName.RouletteBetSettings => "RouletteBetSettings", 
			DialogEntry.InputTemplateName.Blackjack => "Blackjack", 
			DialogEntry.InputTemplateName.BlackjackBetSettings => "BlackjackBetSettings", 
			DialogEntry.InputTemplateName.RecruitmentCampaignsList => "RecruitmentCampaignsList", 
			DialogEntry.InputTemplateName.HealthInsurancePartnershipSettings => "HealthInsurancePartnershipSettings", 
			DialogEntry.InputTemplateName.PlayerOffer => "PlayerOffer", 
			DialogEntry.InputTemplateName.InteriorInstallationFirmDesignSettings => "InteriorInstallationFirmDesignSettings", 
			DialogEntry.InputTemplateName.SalaryOffer => "SalaryOffer", 
			DialogEntry.InputTemplateName.MovingServiceSettings => "MovingServiceSettings", 
			DialogEntry.InputTemplateName.MovingServiceContractsList => "MovingServiceContractsList", 
			DialogEntry.InputTemplateName.FurnitureDeliveriesList => "FurnitureDeliveriesList", 
			DialogEntry.InputTemplateName.InstallationContractsList => "InstallationContractsList", 
			DialogEntry.InputTemplateName.VehicleContractSettings => "VehicleContractSettings", 
			DialogEntry.InputTemplateName.VehicleDeliveryContractsList => "VehicleDeliveryContractsList", 
			DialogEntry.InputTemplateName.FoodDeliverySettings => "FoodDeliverySettings", 
			DialogEntry.InputTemplateName.FoodDeliveriesList => "FoodDeliveriesList", 
			DialogEntry.InputTemplateName.PrivateDriverManageContract => "PrivateDriverManageContract", 
			DialogEntry.InputTemplateName.PrivateDriverManageVehicles => "PrivateDriverManageVehicles", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this MarketingTypeName value)
	{
		return value switch
		{
			MarketingTypeName.SmallInternet => "SmallInternet", 
			MarketingTypeName.MediumInternet => "MediumInternet", 
			MarketingTypeName.LargeInternet => "LargeInternet", 
			MarketingTypeName.SmallBillboard => "SmallBillboard", 
			MarketingTypeName.MediumBillboard => "MediumBillboard", 
			MarketingTypeName.LargeBillboard => "LargeBillboard", 
			MarketingTypeName.None => "None", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this DayOfWeek value)
	{
		return value switch
		{
			DayOfWeek.Sunday => "Sunday", 
			DayOfWeek.Monday => "Monday", 
			DayOfWeek.Tuesday => "Tuesday", 
			DayOfWeek.Wednesday => "Wednesday", 
			DayOfWeek.Thursday => "Thursday", 
			DayOfWeek.Friday => "Friday", 
			DayOfWeek.Saturday => "Saturday", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this DiplomaName value)
	{
		return value switch
		{
			DiplomaName.Undefined => "Undefined", 
			DiplomaName.FoodSafetyCourse => "FoodSafetyCourse", 
			DiplomaName.AutoMechanicEducation => "AutoMechanicEducation", 
			DiplomaName.BasicHr => "BasicHr", 
			DiplomaName.MarketDemands => "MarketDemands", 
			DiplomaName.Headquarters => "Headquarters", 
			DiplomaName.OfficeBusinesses => "OfficeBusinesses", 
			DiplomaName.ProductManufacturing => "ProductManufacturing", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this ItemPanelMetaView value)
	{
		return value switch
		{
			ItemPanelMetaView.None => "None", 
			ItemPanelMetaView.Shopping => "Shopping", 
			ItemPanelMetaView.Vehicle => "Vehicle", 
			ItemPanelMetaView.Cleaning => "Cleaning", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this Gender value)
	{
		return value switch
		{
			Gender.Male => "Male", 
			Gender.Female => "Female", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this Priority value)
	{
		return value switch
		{
			Priority.Low => "Low", 
			Priority.Medium => "Medium", 
			Priority.High => "High", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this AppName value)
	{
		return value switch
		{
			AppName.Persona => "Persona", 
			AppName.Contacts => "Contacts", 
			AppName.MyEmployees => "MyEmployees", 
			AppName.BizMan => "BizMan", 
			AppName.EconoView => "EconoView", 
			AppName.VoogleMaps => "VoogleMaps", 
			AppName.MarketInsider => "MarketInsider", 
			AppName.Rivals => "Rivals", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this FontFace value)
	{
		return value switch
		{
			FontFace.Exo2 => "Exo2", 
			FontFace.BebasNeue => "BebasNeue", 
			FontFace.BonheurRoyale => "BonheurRoyale", 
			FontFace.NotoSerif => "NotoSerif", 
			FontFace.Rubik => "Rubik", 
			FontFace.Anton => "Anton", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this DayOfWeekOrdered value)
	{
		return value switch
		{
			DayOfWeekOrdered.Monday => "Monday", 
			DayOfWeekOrdered.Tuesday => "Tuesday", 
			DayOfWeekOrdered.Wednesday => "Wednesday", 
			DayOfWeekOrdered.Thursday => "Thursday", 
			DayOfWeekOrdered.Friday => "Friday", 
			DayOfWeekOrdered.Saturday => "Saturday", 
			DayOfWeekOrdered.Sunday => "Sunday", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this MarketEventType value)
	{
		return value switch
		{
			MarketEventType.BusinessOpened => "BusinessOpened", 
			MarketEventType.BusinessClosed => "BusinessClosed", 
			MarketEventType.Hype => "Hype", 
			MarketEventType.ProductShortage => "ProductShortage", 
			MarketEventType.ProductBackorder => "ProductBackorder", 
			MarketEventType.LargePlayerPurchase => "LargePlayerPurchase", 
			MarketEventType.MaxProvidersExceeded => "MaxProvidersExceeded", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this LogType value)
	{
		return value switch
		{
			LogType.Error => "Error", 
			LogType.Assert => "Assert", 
			LogType.Warning => "Warning", 
			LogType.Log => "Log", 
			LogType.Exception => "Exception", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this SlotMachineDialog.SlotElement value)
	{
		return value switch
		{
			SlotMachineDialog.SlotElement.DoubleDiamond => "DoubleDiamond", 
			SlotMachineDialog.SlotElement.Seven => "Seven", 
			SlotMachineDialog.SlotElement.Cherry => "Cherry", 
			SlotMachineDialog.SlotElement.Orange => "Orange", 
			SlotMachineDialog.SlotElement.Apple => "Apple", 
			SlotMachineDialog.SlotElement.BarSingle => "BarSingle", 
			SlotMachineDialog.SlotElement.BarDouble => "BarDouble", 
			SlotMachineDialog.SlotElement.BarTriple => "BarTriple", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this RouletteDialog.RouletteColor value)
	{
		return value switch
		{
			RouletteDialog.RouletteColor.Red => "Red", 
			RouletteDialog.RouletteColor.Black => "Black", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this CasinoMessageUI.CasinoMessage value)
	{
		return value switch
		{
			CasinoMessageUI.CasinoMessage.casino_message_welcome => "casino_message_welcome", 
			CasinoMessageUI.CasinoMessage.casino_message_trip_over => "casino_message_trip_over", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this NotificationType value)
	{
		return value switch
		{
			NotificationType.Success => "Success", 
			NotificationType.Warning => "Warning", 
			NotificationType.Error => "Error", 
			NotificationType.Info => "Info", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this AnimationType value)
	{
		return value switch
		{
			AnimationType.UsingCashRegister => "UsingCashRegister", 
			AnimationType.ConsumeFood => "ConsumeFood", 
			AnimationType.ThrowingTrash => "ThrowingTrash", 
			AnimationType.UsingProducer => "UsingProducer", 
			AnimationType.Unused4 => "Unused4", 
			AnimationType.Faint => "Faint", 
			AnimationType.UsingDesktopComputer => "UsingDesktopComputer", 
			AnimationType.BoxingPunch => "BoxingPunch", 
			AnimationType.PressAction => "PressAction", 
			AnimationType.SweatRemove => "SweatRemove", 
			AnimationType.EatIceCream => "EatIceCream", 
			AnimationType.SquatAction => "SquatAction", 
			AnimationType.StretchArms => "StretchArms", 
			AnimationType.PushUpJump => "PushUpJump", 
			AnimationType.Drink => "Drink", 
			AnimationType.DJEncouraging => "DJEncouraging", 
			AnimationType.StoringAJacket => "StoringAJacket", 
			AnimationType.ShampooHair => "ShampooHair", 
			AnimationType.ChemsHair => "ChemsHair", 
			AnimationType.CutHair => "CutHair", 
			AnimationType.StyleHair => "StyleHair", 
			AnimationType.ShampooHairExit => "ShampooHairExit", 
			AnimationType.ChemsHairExit => "ChemsHairExit", 
			AnimationType.CutHairExit => "CutHairExit", 
			AnimationType.StyleHairExit => "StyleHairExit", 
			AnimationType.ConsumeDrink => "ConsumeDrink", 
			AnimationType.UsingLaptop => "UsingLaptop", 
			AnimationType.UsingScorpioGaming => "UsingScorpioGaming", 
			AnimationType.VictoryStanding => "VictoryStanding", 
			AnimationType.DefeatStanding => "DefeatStanding", 
			AnimationType.VictorySitting => "VictorySitting", 
			AnimationType.DefeatSitting => "DefeatSitting", 
			AnimationType.DealerBlackjack => "DealerBlackjack", 
			AnimationType.DealerRoulette => "DealerRoulette", 
			AnimationType.None => "None", 
			AnimationType.GymTrainerCheering => "GymTrainerCheering", 
			AnimationType.IDontThinkSo => "IDontThinkSo", 
			AnimationType.Calisthenics04 => "Calisthenics04", 
			AnimationType.Calisthenics0501 => "Calisthenics0501", 
			AnimationType.Calisthenics0502 => "Calisthenics0502", 
			AnimationType.Calisthenics0601 => "Calisthenics0601", 
			AnimationType.Calisthenics0602 => "Calisthenics0602", 
			AnimationType.HammerHitting => "HammerHitting", 
			AnimationType.RidingAttraction => "RidingAttraction", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this PlayerAction value)
	{
		return value switch
		{
			PlayerAction.Move => "Move", 
			PlayerAction.Interact => "Interact", 
			PlayerAction.SecondaryInteract => "SecondaryInteract", 
			PlayerAction.SpecialInteract => "SpecialInteract", 
			PlayerAction.Cancel => "Cancel", 
			PlayerAction.RotateLeft => "RotateLeft", 
			PlayerAction.RotateRight => "RotateRight", 
			PlayerAction.Zoom => "Zoom", 
			PlayerAction.CursorMovement => "CursorMovement", 
			PlayerAction.CursorScroll => "CursorScroll", 
			PlayerAction.Confirm => "Confirm", 
			PlayerAction.NextOption => "NextOption", 
			PlayerAction.PreviousOption => "PreviousOption", 
			PlayerAction.Click => "Click", 
			PlayerAction.RightClick => "RightClick", 
			PlayerAction.Menu => "Menu", 
			PlayerAction.ToggleRunning => "ToggleRunning", 
			PlayerAction.SliderLeft => "SliderLeft", 
			PlayerAction.SliderRight => "SliderRight", 
			PlayerAction.SkipSong => "SkipSong", 
			PlayerAction.Sell => "Sell", 
			PlayerAction.Sleep => "Sleep", 
			PlayerAction.OpenNotifications => "OpenNotifications", 
			PlayerAction.Pause => "Pause", 
			PlayerAction.AutoRun => "AutoRun", 
			PlayerAction.OpenHelp => "OpenHelp", 
			PlayerAction.OpenBugReport => "OpenBugReport", 
			PlayerAction.QuickSave => "QuickSave", 
			PlayerAction.OpenMap => "OpenMap", 
			PlayerAction.OpenBizman => "OpenBizman", 
			PlayerAction.ReverseCarCam => "ReverseCarCam", 
			PlayerAction.PerformActionWithoutConfirm => "PerformActionWithoutConfirm", 
			PlayerAction.FastMapPan => "FastMapPan", 
			PlayerAction.SelectMultipleElements => "SelectMultipleElements", 
			PlayerAction.SnapFreePlacement => "SnapFreePlacement", 
			PlayerAction.InvertItemRotation => "InvertItemRotation", 
			PlayerAction.VehicleRightBlinker => "VehicleRightBlinker", 
			PlayerAction.VehicleLeftBlinker => "VehicleLeftBlinker", 
			PlayerAction.RotateItem => "RotateItem", 
			PlayerAction.VehicleHorn => "VehicleHorn", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this LogoSize value)
	{
		return value switch
		{
			LogoSize.SquareSign => "SquareSign", 
			LogoSize.WideSign => "WideSign", 
			LogoSize.Billboard => "Billboard", 
			LogoSize.Billboard2x1 => "Billboard2x1", 
			LogoSize.Billboard4x1 => "Billboard4x1", 
			LogoSize.Billboard1x2 => "Billboard1x2", 
			LogoSize.Billboard1x4 => "Billboard1x4", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this Transaction.AmountOption value)
	{
		return value switch
		{
			Transaction.AmountOption.All => "All", 
			Transaction.AmountOption.Positive => "Positive", 
			Transaction.AmountOption.Negative => "Negative", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this BoatTypeName value)
	{
		return value switch
		{
			BoatTypeName.Speedboat => "Speedboat", 
			BoatTypeName.Yacht => "Yacht", 
			BoatTypeName.LuxuryYacht => "LuxuryYacht", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this FootStepSoundType value)
	{
		return value switch
		{
			FootStepSoundType.Asphalt => "Asphalt", 
			FootStepSoundType.Wood => "Wood", 
			FootStepSoundType.Carpet => "Carpet", 
			FootStepSoundType.Tile => "Tile", 
			FootStepSoundType.Grass => "Grass", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this SteamAPI.DLC value)
	{
		return value switch
		{
			SteamAPI.DLC.None => "None", 
			SteamAPI.DLC.Silver => "Silver", 
			SteamAPI.DLC.Gold => "Gold", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this Floor value)
	{
		return value switch
		{
			Floor.Parking => "Parking", 
			Floor.Building => "Building", 
			Floor.Exit => "Exit", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this PermanentAnimationType value)
	{
		return value switch
		{
			PermanentAnimationType.Drunk => "Drunk", 
			PermanentAnimationType.TalkingPhone => "TalkingPhone", 
			PermanentAnimationType.TextingPhone => "TextingPhone", 
			PermanentAnimationType.Squats => "Squats", 
			PermanentAnimationType.Crunches => "Crunches", 
			PermanentAnimationType.Boxing => "Boxing", 
			PermanentAnimationType.Press => "Press", 
			PermanentAnimationType.Running => "Running", 
			PermanentAnimationType.PushUpIdle => "PushUpIdle", 
			PermanentAnimationType.DJ => "DJ", 
			PermanentAnimationType.Sitting => "Sitting", 
			PermanentAnimationType.SittingOnHairdresserWashChair => "SittingOnHairdresserWashChair", 
			PermanentAnimationType.SittingOnHairdresserChair => "SittingOnHairdresserChair", 
			PermanentAnimationType.Laying => "Laying", 
			PermanentAnimationType.ShampooHairIdle => "ShampooHairIdle", 
			PermanentAnimationType.ChemsHairIdle => "ChemsHairIdle", 
			PermanentAnimationType.CutHairIdle => "CutHairIdle", 
			PermanentAnimationType.StyleHairIdle => "StyleHairIdle", 
			PermanentAnimationType.SitDeliveryTruck => "SitDeliveryTruck", 
			PermanentAnimationType.UsingRoulette => "UsingRoulette", 
			PermanentAnimationType.UsingBlackjack => "UsingBlackjack", 
			PermanentAnimationType.UsingSlotMachine => "UsingSlotMachine", 
			PermanentAnimationType.ReadingABook => "ReadingABook", 
			PermanentAnimationType.DealerBlackjackIdle => "DealerBlackjackIdle", 
			PermanentAnimationType.DealerRouletteIdle => "DealerRouletteIdle", 
			PermanentAnimationType.JumpingOnTrampoline => "JumpingOnTrampoline", 
			PermanentAnimationType.CrunchesOnFitBall => "CrunchesOnFitBall", 
			PermanentAnimationType.KettlebellPull => "KettlebellPull", 
			PermanentAnimationType.Deadlift => "Deadlift", 
			PermanentAnimationType.CleaningIdle => "CleaningIdle", 
			PermanentAnimationType.Cleaning => "Cleaning", 
			PermanentAnimationType.HoldingIceCream => "HoldingIceCream", 
			PermanentAnimationType.ConsumingFoodSitting => "ConsumingFoodSitting", 
			PermanentAnimationType.Showering => "Showering", 
			PermanentAnimationType.UsingSink => "UsingSink", 
			PermanentAnimationType.Calisthenics02 => "Calisthenics02", 
			PermanentAnimationType.Calisthenics03 => "Calisthenics03", 
			PermanentAnimationType.CalisthenicsIdle04 => "CalisthenicsIdle04", 
			PermanentAnimationType.CalisthenicsIdle0501 => "CalisthenicsIdle0501", 
			PermanentAnimationType.CalisthenicsIdle0502 => "CalisthenicsIdle0502", 
			PermanentAnimationType.CalisthenicsIdle0601 => "CalisthenicsIdle0601", 
			PermanentAnimationType.CalisthenicsIdle0602 => "CalisthenicsIdle0602", 
			PermanentAnimationType.LayingInLoungerChair => "LayingInLoungerChair", 
			PermanentAnimationType.HammerIdle => "HammerIdle", 
			PermanentAnimationType.Bathing => "Bathing", 
			PermanentAnimationType.PlayingGuitar => "PlayingGuitar", 
			PermanentAnimationType.Singing => "Singing", 
			PermanentAnimationType.PlayingBucketDrums => "PlayingBucketDrums", 
			PermanentAnimationType.Juggling => "Juggling", 
			PermanentAnimationType.CaricaturePainting => "CaricaturePainting", 
			PermanentAnimationType.HoldingAnItem => "HoldingAnItem", 
			PermanentAnimationType.FactoryWorking => "FactoryWorking", 
			PermanentAnimationType.SittingOnCinemaChair => "SittingOnCinemaChair", 
			PermanentAnimationType.LayingInTowel01 => "LayingInTowel01", 
			PermanentAnimationType.LayingInDeckChair => "LayingInDeckChair", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this SystemRequirement.SystemRequirements value)
	{
		return value switch
		{
			SystemRequirement.SystemRequirements.CPUSpeed => "CPUSpeed", 
			SystemRequirement.SystemRequirements.Ram => "Ram", 
			SystemRequirement.SystemRequirements.Vram => "Vram", 
			SystemRequirement.SystemRequirements.DedicatedGPU => "DedicatedGPU", 
			SystemRequirement.SystemRequirements.ShaderLevel => "ShaderLevel", 
			SystemRequirement.SystemRequirements.ComputeShader => "ComputeShader", 
			SystemRequirement.SystemRequirements.FolderAccess => "FolderAccess", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this PlayerPref value)
	{
		return value switch
		{
			PlayerPref.ControlMode => "ControlMode", 
			PlayerPref.InvertRotation => "InvertRotation", 
			PlayerPref.RunByDefaultIndoors => "RunByDefaultIndoors", 
			PlayerPref.VehicleMouseInput => "VehicleMouseInput", 
			PlayerPref.SteeringAssist => "SteeringAssist", 
			PlayerPref.RadioVolume => "RadioVolume", 
			PlayerPref.GlobalVolume => "GlobalVolume", 
			PlayerPref.MenuMusicVolume => "MenuMusicVolume", 
			PlayerPref.SfxVolume => "SfxVolume", 
			PlayerPref.AiStoreMusicVolume => "AiStoreMusicVolume", 
			PlayerPref.Locale => "Locale", 
			PlayerPref.uiZooming => "uiZooming", 
			PlayerPref.use12h => "use12h", 
			PlayerPref.useImperial => "useImperial", 
			PlayerPref.GameSpeed => "GameSpeed", 
			PlayerPref.NumberFormat => "NumberFormat", 
			PlayerPref.allowTracking => "allowTracking", 
			PlayerPref.SeasonalDecorations => "SeasonalDecorations", 
			PlayerPref.ControlHints => "ControlHints", 
			PlayerPref.RadioStation => "RadioStation", 
			PlayerPref.MaxAutoSavesPerGame => "MaxAutoSavesPerGame", 
			PlayerPref.MinutesBetweenAutoSaves => "MinutesBetweenAutoSaves", 
			PlayerPref.antiAliasingSetting => "antiAliasingSetting", 
			PlayerPref.vSyncAndFPSLimitV2 => "vSyncAndFPSLimitV2", 
			PlayerPref.LowDetailCityMap => "LowDetailCityMap", 
			PlayerPref.hbaoQuality => "hbaoQuality", 
			PlayerPref.showFps => "showFps", 
			PlayerPref.particleQuality => "particleQuality", 
			PlayerPref.textureQuality => "textureQuality", 
			PlayerPref.gamma => "gamma", 
			PlayerPref.shadows => "shadows", 
			PlayerPref.LastSaveGameName => "LastSaveGameName", 
			PlayerPref.ShowWelcomeScreen => "ShowWelcomeScreen", 
			PlayerPref.PlayerEmail => "PlayerEmail", 
			PlayerPref.shownSystemRequirementWarning => "shownSystemRequirementWarning", 
			PlayerPref.ShowDataTrackingPopup => "ShowDataTrackingPopup", 
			PlayerPref.LastPlayedVersion => "LastPlayedVersion", 
			PlayerPref.LatestCrashDate => "LatestCrashDate", 
			PlayerPref.HasAutoSubscribedBlueprints09 => "HasAutoSubscribedBlueprints09", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this EntertainType value)
	{
		return value switch
		{
			EntertainType.Play => "Play", 
			EntertainType.WatchTV => "WatchTV", 
			EntertainType.DJ => "DJ", 
			EntertainType.Read => "Read", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this WorkoutType value)
	{
		return value switch
		{
			WorkoutType.Jump => "Jump", 
			WorkoutType.Run => "Run", 
			WorkoutType.SitUps => "SitUps", 
			WorkoutType.Boxing => "Boxing", 
			WorkoutType.Squats => "Squats", 
			WorkoutType.BenchPressing => "BenchPressing", 
			WorkoutType.PullUps => "PullUps", 
			WorkoutType.KettlebellPull => "KettlebellPull", 
			WorkoutType.Deadlift => "Deadlift", 
			WorkoutType.Calisthenics02 => "Calisthenics02", 
			WorkoutType.Calisthenics03 => "Calisthenics03", 
			WorkoutType.Calisthenics04 => "Calisthenics04", 
			WorkoutType.Calisthenics05 => "Calisthenics05", 
			WorkoutType.Calisthenics0601 => "Calisthenics0601", 
			WorkoutType.Calisthenics0602 => "Calisthenics0602", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this SleepEnvironmentType value)
	{
		return value switch
		{
			SleepEnvironmentType.Bed => "Bed", 
			SleepEnvironmentType.Car => "Car", 
			SleepEnvironmentType.Boat => "Boat", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this HealthInsurancePlanType value)
	{
		return value switch
		{
			HealthInsurancePlanType.Bronze => "Bronze", 
			HealthInsurancePlanType.Silver => "Silver", 
			HealthInsurancePlanType.Gold => "Gold", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this AppearanceElementType value)
	{
		return value switch
		{
			AppearanceElementType.Gender => "Gender", 
			AppearanceElementType.Hair => "Hair", 
			AppearanceElementType.HairAccessory => "HairAccessory", 
			AppearanceElementType.Head => "Head", 
			AppearanceElementType.HeadAccessory => "HeadAccessory", 
			AppearanceElementType.Torso => "Torso", 
			AppearanceElementType.TorsoAccessory => "TorsoAccessory", 
			AppearanceElementType.Legs => "Legs", 
			AppearanceElementType.LegsAccessory => "LegsAccessory", 
			AppearanceElementType.Feet => "Feet", 
			AppearanceElementType.FeetAccessory => "FeetAccessory", 
			AppearanceElementType.Eyes => "Eyes", 
			AppearanceElementType.Mouth => "Mouth", 
			AppearanceElementType.Nose => "Nose", 
			AppearanceElementType.Beard => "Beard", 
			AppearanceElementType.Eyebrows => "Eyebrows", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this BlueprintCategory value)
	{
		return value switch
		{
			BlueprintCategory.Gallery => "Gallery", 
			BlueprintCategory.MyLibrary => "MyLibrary", 
			BlueprintCategory.DevBusinessLayouts => "DevBusinessLayouts", 
			BlueprintCategory.DevInteriorDesigns => "DevInteriorDesigns", 
			BlueprintCategory.Feedback => "Feedback", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this DataElement value)
	{
		return value switch
		{
			DataElement.Price => "Price", 
			DataElement.BuildingType => "BuildingType", 
			DataElement.BuildingSize => "BuildingSize", 
			DataElement.InteriorScore => "InteriorScore", 
			DataElement.Workstations => "Workstations", 
			DataElement.StorageShelves => "StorageShelves", 
			DataElement.PalletShelves => "PalletShelves", 
			DataElement.PointsOfSales => "PointsOfSales", 
			DataElement.BusinessTypeName => "BusinessTypeName", 
			DataElement.CreatorSteamUsername => "CreatorSteamUsername", 
			DataElement.BuildNumber => "BuildNumber", 
			DataElement.BlueprintVersion => "BlueprintVersion", 
			DataElement.CompatBlueprint => "CompatBlueprint", 
			DataElement.RequiredModIds => "RequiredModIds", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this DanceType value)
	{
		return value switch
		{
			DanceType.Dance1 => "Dance1", 
			DanceType.Dance2 => "Dance2", 
			DanceType.Dance3 => "Dance3", 
			DanceType.Dance4 => "Dance4", 
			DanceType.Dance5 => "Dance5", 
			DanceType.Dance6 => "Dance6", 
			DanceType.Dance7 => "Dance7", 
			DanceType.Dance8 => "Dance8", 
			DanceType.Dance9 => "Dance9", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this SortByOption value)
	{
		return value switch
		{
			SortByOption.Popularity => "Popularity", 
			SortByOption.Rating => "Rating", 
			SortByOption.UploadDate => "UploadDate", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this RestEnvironmentType value)
	{
		return value switch
		{
			RestEnvironmentType.OutsideBench => "OutsideBench", 
			RestEnvironmentType.Chair => "Chair", 
			RestEnvironmentType.OutsideChair => "OutsideChair", 
			RestEnvironmentType.Bench => "Bench", 
			RestEnvironmentType.DeckChair => "DeckChair", 
			RestEnvironmentType.BeachTowel => "BeachTowel", 
			RestEnvironmentType.SunLounger => "SunLounger", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this InteriorDesignerAction value)
	{
		return value switch
		{
			InteriorDesignerAction.OpenPackageTool => "OpenPackageTool", 
			InteriorDesignerAction.OpenHandTool => "OpenHandTool", 
			InteriorDesignerAction.OpenEyedropperTool => "OpenEyedropperTool", 
			InteriorDesignerAction.OpenPaletteTool => "OpenPaletteTool", 
			InteriorDesignerAction.OpenSellTool => "OpenSellTool", 
			InteriorDesignerAction.OpenFurnitureTool => "OpenFurnitureTool", 
			InteriorDesignerAction.OpenDuplicateTool => "OpenDuplicateTool", 
			InteriorDesignerAction.OpenWallTool => "OpenWallTool", 
			InteriorDesignerAction.OpenFloorTool => "OpenFloorTool", 
			InteriorDesignerAction.OpenProducerTool => "OpenProducerTool", 
			InteriorDesignerAction.OpenQueueTool => "OpenQueueTool", 
			InteriorDesignerAction.OpenSecurityTool => "OpenSecurityTool", 
			InteriorDesignerAction.Undo => "Undo", 
			InteriorDesignerAction.Redo => "Redo", 
			InteriorDesignerAction.OpenTimeOfDayTool => "OpenTimeOfDayTool", 
			InteriorDesignerAction.OpenBlueprintPanel => "OpenBlueprintPanel", 
			InteriorDesignerAction.ToggleWalls => "ToggleWalls", 
			InteriorDesignerAction.ShowUpperFloor => "ShowUpperFloor", 
			InteriorDesignerAction.ShowLowerFloor => "ShowLowerFloor", 
			InteriorDesignerAction.SpecialBehavior => "SpecialBehavior", 
			InteriorDesignerAction.Apply => "Apply", 
			InteriorDesignerAction.Exit => "Exit", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}

	public static string ToStringFast(this PaidActivityType value)
	{
		return value switch
		{
			PaidActivityType.SwingChairs => "SwingChairs", 
			PaidActivityType.SpinningCups => "SpinningCups", 
			PaidActivityType.Twister => "Twister", 
			PaidActivityType.BigStrikers => "BigStrikers", 
			PaidActivityType.FerrisWheel => "FerrisWheel", 
			_ => throw new ArgumentOutOfRangeException("value", value, null), 
		};
	}
}
