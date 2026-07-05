using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Domain.Data;
using OneOf;
using UtilitiesLibrary.Results;
using Willmsy.AsyncTryResult;

namespace Comms.Dtos;



public class MatchDataDto {

	public required MatchData Data { get; init; }

	public required string DeviceId { get; init; }

	public required long MatchId { get; init; }

	public required string OriginalDeviceId { get; init; }

	public required long OriginalMatchId { get; init; }

	// TODO create a DeviceId type
	public required List<(string deviceId, long matchdId)> Parents { get; init; }

	// TODO figure out if this needs escaping
	public string ParentsAsText => string.Join(';', Parents.Select(parent => $"{parent.deviceId}, {parent.matchdId}"));

	public required string GameDeviceId { get; init; }

	public required long GameId { get; init; }

	// TODO move this information to somewhere central
	// I decided that MatchData shouldn't actually point to an EventSchedule object.
	// EventSchedules only exist to provide suggestions for team numbers once you pick a match and alliance.
	// Having MatchData objects point to EventSchedule objects require a bunch of additional validation on MatchData objects.
	// Otherwise, it would create the possibility for invalid MatchData objects.
	// For MatchDataDtos to point to EventSchedule objects (or to have an EventDeviceId and EventMetaDataId) then devices
	// would need to share events before they can share matches. This would likely increase the size of MatchDataQr codes.
	// MatchDataDtos need an EventCode property so that they can be indexed by event in the database.
	// Obviously I don't want to create the possibility of invalid data so it will simply return the EventCode from the MatchData property.
	// The model object can scan for MatchData with an EventCode that doesn't correspond to an EventSchedule and for matches where the teams don't match the event schedule.
	// These can be raised as errors within the Model interface.
	public string EventCode => Data.EventCode;



	private MatchDataDto() { }

	public static CreateMatchDataDtoResult Create(
		MatchData matchData,
		string deviceId,
		long matchId,
		string originalDeviceId,
		long originalMatchId,
		List<(string deviceId, long matchId)> parents,
		string gameDeviceId,
		long gameId) {

		// The current match cannot be before the original match.
		if (deviceId == originalDeviceId && matchId < originalMatchId) {
			return new MatchBeforeOriginalMatchError(deviceId, matchId, originalDeviceId, originalMatchId);
		}

		// If the current match is the original match there must be no parents.
		if (deviceId == originalDeviceId && matchId == originalMatchId && parents.Count != 0) {
			return new OriginalMatchHasParentsError();
		}

		// If the current match isn't the original match there must be at least one parent.
		if ((deviceId != originalDeviceId || matchId != originalMatchId) && parents.Count == 0) {
			return new EditedMatchHasNoParentsError();
		}

		foreach ((string deviceId, long matchId) parent in parents) {

			// The current match must be after parent match.
			if (deviceId == parent.deviceId && matchId <= parent.matchId) {
				return new MatchNotAfterParentMatchError();
			}

			// A parent match cannot be before the original match.
			if (parent.deviceId == originalDeviceId && parent.matchId < originalMatchId) {
				return new ParentMatchBeforeOriginalMatchError();
			}
		}

		return new MatchDataDto {
			Data = matchData,
			DeviceId = deviceId,
			MatchId = matchId,
			OriginalDeviceId = originalDeviceId,
			OriginalMatchId = originalMatchId,
			Parents = parents,
			GameDeviceId = gameDeviceId,
			GameId = gameId
		};
	}

	public static ParentsFromTextResult ParentsFromText(string parentsAsText) {

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

			int nextCommaPosition = parentsAsText.IndexOf(',', parentStartIndex);

			if (nextCommaPosition == -1) {
				return new NoCommaInParentTextError(parentsAsText);
			}

			string parentDeviceId = parentsAsText.Substring(parentStartIndex, nextCommaPosition - parentStartIndex);
			string parentMatchIdText = parentsAsText.Substring(nextCommaPosition + 1, parentEndIndex - (nextCommaPosition + 1));

			if (!long.TryParse(parentMatchIdText, out long parentMatchId)) {
				return new CoundNotParseMatchIndexError(parentsAsText);
			}

			parents.Add((parentDeviceId, parentMatchId));

			parentStartIndex = parentEndIndex + 1;
		}

		return parents;
	}

}

public record CreateMatchDataDtoResult : AsyncTryResult<MatchDataDto, CreateMatchDataDtoError> {

	public CreateMatchDataDtoResult(MatchDataDto value) : base(value) { }

	public CreateMatchDataDtoResult(CreateMatchDataDtoError error) : base(error) { }

	public static implicit operator CreateMatchDataDtoResult(MatchDataDto value) {
		return new(value);
	}

	public static implicit operator CreateMatchDataDtoResult(CreateMatchDataDtoError error) {
		return new(error);
	}

	public static implicit operator CreateMatchDataDtoResult(MatchBeforeOriginalMatchError error) {
		return new(error);
	}

	public static implicit operator CreateMatchDataDtoResult(OriginalMatchHasParentsError error) {
		return new(error);
	}

	public static implicit operator CreateMatchDataDtoResult(EditedMatchHasNoParentsError error) {
		return new(error);
	}

	public static implicit operator CreateMatchDataDtoResult(MatchNotAfterParentMatchError error) {
		return new(error);
	}

	public static implicit operator CreateMatchDataDtoResult(ParentMatchBeforeOriginalMatchError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class CreateMatchDataDtoError : OneOfBase<
	MatchBeforeOriginalMatchError,
	OriginalMatchHasParentsError,
	EditedMatchHasNoParentsError,
	MatchNotAfterParentMatchError,
	ParentMatchBeforeOriginalMatchError> {

	public static implicit operator Error(CreateMatchDataDtoError error) {
		return error.Match<Error>(
			error1 => error1,
			error2 => error2,
			error3 => error3,
			error4 => error4,
			error5 => error5);
	}

}

public record MatchBeforeOriginalMatchError : Error {

	public required string DeviceId { get; init; }

	public required long MatchId { get; init; }

	public required string OriginalDeviceId { get; init; }

	public required long OriginalMatchId { get; init; }

	[SetsRequiredMembers]
	public MatchBeforeOriginalMatchError(string deviceId, long matchId, string originalDeviceId, long originalMatchId) {
		DeviceId = deviceId;
		MatchId = matchId;
		OriginalDeviceId = originalDeviceId;
		OriginalMatchId = originalMatchId;
	}

}

public record OriginalMatchHasParentsError : Error;

public record EditedMatchHasNoParentsError : Error;

public record MatchNotAfterParentMatchError : Error;

public record ParentMatchBeforeOriginalMatchError : Error;



public record ParentsFromTextResult : AsyncTryResult<List<(string deviceId, long matchId)>, ParentsFromTextError> {

	public ParentsFromTextResult(List<(string deviceId, long matchId)> value) : base(value) { }

	public ParentsFromTextResult(ParentsFromTextError error) : base(error) { }

	public static implicit operator ParentsFromTextResult(List<(string deviceId, long matchId)> value) {
		return new(value);
	}

	public static implicit operator ParentsFromTextResult(ParentsFromTextError error) {
		return new(error);
	}

	public static implicit operator ParentsFromTextResult(NoCommaInParentTextError error) {
		return new(error);
	}

	public static implicit operator ParentsFromTextResult(CoundNotParseMatchIndexError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class ParentsFromTextError : OneOfBase<
	NoCommaInParentTextError,
	CoundNotParseMatchIndexError> {

	public static implicit operator Error(ParentsFromTextError error) {
		return error.Match<Error>(
			error1 => error1,
			error2 => error2);
	}

}

public record NoCommaInParentTextError(string ParentsText) : Error;

public record CoundNotParseMatchIndexError(string ParentsText) : Error;