using System.Collections.Generic;
using System.Linq;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Results;

namespace Comms.Serialization;



public class Parents {

	public static string ToText(ReadOnlyList<(string deviceId, long matchId)> parents) {
		return string.Join(';', parents.Select(parent => $"{parent.deviceId}:{parent.matchId}"));
	}

	public static Result<List<(string deviceId, long matchId)>> FromText(string parentsAsText) {

		List<(string deviceId, long matchId)> parents = [];

		if (string.IsNullOrEmpty(parentsAsText)) {
			return parents;
		}

		int parentStartIndex = 0;
		int parentEndIndex = 0;
		while (parentEndIndex < parentsAsText.Length) {

			parentEndIndex = parentsAsText.IndexOf(';', parentStartIndex);

			if (parentEndIndex == -1) {
				parentEndIndex = parentsAsText.Length;
			}

			int nextColonPosition = parentsAsText.IndexOf(':', parentStartIndex);

			if (nextColonPosition == -1) {
				return new AdHocError("No colon found in parent text.", ("parentsAsText", parentsAsText));
			}

			string parentDeviceId = parentsAsText.Substring(parentStartIndex, nextColonPosition - parentStartIndex);
			string parentMatchIdText = parentsAsText.Substring(nextColonPosition + 1, parentEndIndex - (nextColonPosition + 1));

			if (!long.TryParse(parentMatchIdText, out long parentMatchId)) {
				return new AdHocError("Could not parse matchId as a long.", ("parentsAsText", parentsAsText));
			}

			parents.Add((parentDeviceId, parentMatchId));

			parentStartIndex = parentEndIndex + 1;
		}

		return parents;
	}

}