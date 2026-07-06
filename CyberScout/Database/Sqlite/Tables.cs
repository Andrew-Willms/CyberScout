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

	// TODO move to a central information location.
	// Each type of record has its own sequence so that records of each type are more contiguous in the indexes.
	public static class GameIdSequence {
		public const string LastUsedId = "LastUsedId";
	}

	public static class GameIndex {
		public const string DeviceId = "DeviceId";
		public const string StartIndex = "StartIndex";
		public const string EndIndex = "EndIndex";
		public const string Status = "Status";
	}

	public static class GameData {
		public const string DeviceId = "DeviceId";
		public const string GameId = "GameId";
		public const string Data = "Data";
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

	public static class EventData {
		public const string DeviceId = "DeviceId";
		public const string EventId = "EventId";
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
		public const string EventCode = "EventCode";
	}

	public static class MatchData {
		public const string DeviceId = "DeviceId";
		public const string MatchId = "MatchId";

		// OriginalDeviceId and OriginalRecordId are not foreign keys because I do not want to require
		// a device to have the original match data in order to have descendant match data.
		public const string OriginalDeviceId = "OriginalDeviceId";
		public const string OriginalMatchId = "OriginalMatchId";
		public const string ParentsAsText = "ParentsAsText";

		public const string GameDeviceId = "GameDeviceId";
		public const string GameId = "GameId";

		// EventDeviceId and EventMetaDataId are foreign keys. This means that a device must have an event before it can
		// accept any matches made from that event. This may result in some friction when sharing the first match from a 
		// device via QR code. In most cases this friction should be able to be eliminated by having devices pinging the
		// central server for event data (which will ping TBA and create event data authored by the central server that
		// can then be used by all devices) instead of having each device ping TBA individually (resulting in each device
		// having uniquely authored event data).
		public const string EventCode = "EventCode";
		public const string Data = "Data";
	}

}