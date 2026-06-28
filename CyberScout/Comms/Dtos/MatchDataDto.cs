using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Domain.Data;
using OneOf;
using Willmsy.AsyncTryResult;

namespace Comms.Dtos;



public class MatchDataDto {

	public required MatchData MatchData { get; init; }

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

	public required string EventDeviceId { get; init; }

	public required long EventMetaDataId { get; init; }



	private MatchDataDto() { }

	public static CreateMatchDataDtoResult Create(
		MatchData matchData,
		string deviceId,
		long matchId,
		string originalDeviceId,
		long originalMatchId,
		List<(string deviceId, long matchId)> parents,
		string gameDeviceId,
		long gameId,
		string eventDeviceId,
		long eventMetaDataId) {

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
			MatchData = matchData,
			DeviceId = deviceId,
			MatchId = matchId,
			OriginalDeviceId = originalDeviceId,
			OriginalMatchId = originalMatchId,
			Parents = parents,
			GameDeviceId = gameDeviceId,
			GameId = gameId,
			EventDeviceId = eventDeviceId,
			EventMetaDataId = eventMetaDataId
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
	ParentMatchBeforeOriginalMatchError
>;

public readonly record struct MatchBeforeOriginalMatchError {

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

public record OriginalMatchHasParentsError;

public record EditedMatchHasNoParentsError;

public record MatchNotAfterParentMatchError;

public record ParentMatchBeforeOriginalMatchError;



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
	CoundNotParseMatchIndexError
>;

public record NoCommaInParentTextError(string ParentsText);

public record CoundNotParseMatchIndexError(string ParentsText);