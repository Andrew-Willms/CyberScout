namespace Database.Sqlite;



public static class Tables {

	// -------- General --------

	public static class DatabaseVersion {
		public const string Version = "Version";
	}

	public static class Scout {
		public const string Name = "Name";
	}

	public static class KnownDevices {
		public const string DeviceId = "DeviceId";
		public const string DeviceName = "DeviceName";
		public const string PublicKey = "PublicKey";
	}

	// -------- Game --------

	public static class GameIdSequence {
		public const string LastUsedId = "LastUsedId";
	}

	public static class GameIndex {
		public const string DeviceId = "DeviceId";
		public const string StartIndex = "StartIndex";
		public const string EndIndex = "EndIndex";
		public const string Status = "Status";
	}

	public static class Games {
		public const string DeviceId = "DeviceId";
		public const string GameId = "GameId";
		public const string TimePublished = "TimePublished";
		public const string Data = "Data";
		public const string MajorVersion = "MajorVersion";
		public const string MinorVersion = "MinorVersion";
		public const string PatchVersion = "PatchVersion";
	}

	// -------- Event --------

	public static class EventIdSequence {
		public const string LastUsedId = "LastUsedId";
	}

	public static class EventIndex {
		public const string DeviceId = "DeviceId";
		public const string StartIndex = "StartIndex";
		public const string EndIndex = "EndIndex";
		public const string Status = "Status";
	}

	// Every device with an internet connection will likely create an event from TBA and then will share this event to other devices.
	// This will result in a decent number of records being shared with little purpose. However, each even should only be about the same
	// amount of data as a match. I don't think it will meaningfully slow things down, and it's very convenient to treat everything the same.
	public static class EventMetaData {
		public const string DeviceId = "DeviceId";
		public const string MetaDataId = "MetaDataId";
		public const string DataId = "DataId";
		public const string TimePublished = "TimePublished";
		public const string Source = "Source";
	}

	public static class EventData {
		public const string EventDataId = "EventDataId";
		public const string Data = "Data";
	}

	// -------- Match --------

	public static class MatchIdSequence {
		public const string LastUsedId = "LastUsedId";
	}

	public static class MatchIndex {
		public const string DeviceId = "DeviceId";
		public const string StartIndex = "StartIndex";
		public const string EndIndex = "EndIndex";
		public const string Status = "Status";
		public const string GameDeviceId = "GameDeviceId";
		public const string GameId = "GameId";
		public const string EventDataId = "EventDataId";
	}

	public static class MatchData {
		public const string DeviceId = "DeviceId";
		public const string MatchId = "MatchId";

		// OriginalDeviceId and OriginalRecordId are not foreign keys because I do not want to require
		// a device to have the original match data in order to have descendant match data.
		public const string OriginalDeviceId = "OriginalDeviceId";
		public const string OriginalMatchId = "OriginalMatchId";

		public const string GameDeviceId = "GameDeviceId";
		public const string GameId = "GameId";
		public const string EventDeviceId = "EventDeviceId";
		public const string EventMetaDataId = "EventMetaDataId";
		public const string Data = "Data";
	}

	public static class EditGraphVertices {
		public const string ChildDeviceId = "ChildDeviceId";
		public const string ChildMatchId = "ChildMatchId";

		// ParentDeviceId and ParentRecordId are not foreign keys because I do not want to require
		// a device to have the parent match data in order to have descendant match data.
		public const string ParentDeviceId = "ParentDeviceId";
		public const string ParentMatchId = "ParentMatchId";

		// OriginalDeviceId and OriginalRecordId are not foreign keys because I do not want to require
		// a device to have the original match data in order to have descendant match data.
		public const string OriginalDeviceId = "OriginalDeviceId";
		public const string OriginalMatchId = "OriginalMatchId";

		public const string Comment = "Comment";
	}

}