using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Comms.Dtos;
using Database.Results;
using Microsoft.Data.Sqlite;
using OneOf;
using SqliteUtilities;
using Willmsy.AsyncTryResult;

namespace Database.Sqlite;



public enum RecordType {
	Game,
	Event,
	Match
}



public class SqliteIndexerVersion1 {

	private readonly SqliteConnection Connection;

	public SqliteIndexerVersion1(SqliteConnection connection) {
		Connection = connection;
	}



	public async Task<SetRecordMetaDataResult> SetGameIndexMetaData(string deviceId, long recordId, MatchIndexMetaData metaData) {
		return await SetRecordMetaData(deviceId, recordId, metaData, RecordType.Game);
	}

	public async Task<SetRecordMetaDataResult> SetEventIndexMetaData(string deviceId, long recordId, MatchIndexMetaData metaData) {
		return await SetRecordMetaData(deviceId, recordId, metaData, RecordType.Event);
	}

	public async Task<SetRecordMetaDataResult> SetMatchIndexMetaData(string deviceId, long recordId, MatchIndexMetaData metaData) {
		return await SetRecordMetaData(deviceId, recordId, metaData, RecordType.Match);
	}

	private async Task<SetRecordMetaDataResult> SetRecordMetaData(string deviceId, long recordId, RecordMetaData metaData, RecordType type) {

		GetSuperRangeResult superRangeResult = await GetSuperRangeAround(deviceId, recordId, type);
		if (superRangeResult.IsFailure) {
			return superRangeResult.Error;
		}
		SuperRange existingRanges = superRangeResult.Value;

		SuperRange? updatedRangeSet = existingRanges.OverwriteIndexAndSimplify(recordId, metaData);
		if (updatedRangeSet is null) {
			return new RangeOperationError();
		}

		foreach (Ranges range in existingRanges.Ranges) {

			DeleteRangeFromIndexResult deleteResult = await DeleteRangeFromIndex(deviceId, range);
			if (deleteResult.IsFailure) {
				return deleteResult.Error;
			}
		}

		foreach (Ranges range in updatedRangeSet.Ranges) {

			AddRangeToIndexResult addResult = await AddRangeToIndex(deviceId, range);
			if (addResult.IsFailure) {
				return addResult.Error;
			}
		}

		return Success.Instance;
	}



	public async Task<BulkSetRecordMetaData> SetMatchIndexMetaData(EventDto eventDto, MatchIndexMetaData metaData) {
		throw new NotImplementedException();
	}

	public async Task<BulkSetRecordMetaData?> SetMatchIndexMetaData(GameDto gameDto, MatchIndexMetaData metaData) {
		throw new NotImplementedException();
	}



	public async Task<ResetIndexError> ResetGameIndex() {
		return await ResetIndex(RecordType.Game);
	}

	public async Task<ResetIndexError> ResetEventIndex() {
		return await ResetIndex(RecordType.Event);
	}

	public async Task<ResetIndexError> ResetMatchIndex() {
		return await ResetIndex(RecordType.Match);
	}

	private async Task<ResetIndexError> ResetIndex(RecordType type) {

		string tableName = type switch {
			RecordType.Game => nameof(Tables.GameIndex),
			RecordType.Event => nameof(Tables.EventIndex),
			RecordType.Match => nameof(Tables.MatchIndex),
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};

		SqliteCommand deleteRecordRange = new($"DELETE FROM \"{tableName}\";", Connection);

		if (await deleteRecordRange.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError deleteRecordRangeError) {
			return await RollbackError<ExecuteNonQueryAndExpectError>.TryRollback(deleteRecordRangeError, Connection);
		}

		return Success.Instance;
	}



	private async Task<GetRangeResult> GetRange(string deviceId, long recordId, RecordType type) {

		SqliteCommand getIndexRange = new(
			$"""
			 SELECT * FROM "{nameof(Tables.MatchIndex)}"
			 WHERE "{Tables.MatchIndex.DeviceId}" = @DeviceId
			   AND "{Tables.MatchIndex.StartIndex}" <= @Index
			   AND "{Tables.MatchIndex.EndIndex}" >= @Index
			 """,
			Connection);

		getIndexRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = deviceId });
		getIndexRange.Parameters.Add(new("@Index", SqliteType.Integer) { Value = recordId });

		SqliteDataReader reader = await getIndexRange.ExecuteReaderAsync();

		throw new NotImplementedException();
	}

	private async Task<GetSuperRangeResult> GetSuperRangeAround(string deviceId, long recordId, RecordType type) {

		GetRangeResult containingRangeResult = await GetRange(deviceId, recordId, type);
		if (containingRangeResult.IsFailure) {
			return new GetContainingRangeError(containingRangeResult.Error, deviceId, recordId, type);
		}

		Ranges containingRange = containingRangeResult.Value;
		List<Ranges> relevantRanges = new(3);

		// If the index is at the very start of the containingRange and isn't the first possible index (0) then we need to check the preceding range.
		if (containingRange.Start == recordId && recordId != 0) {

			GetRangeResult precedingRangeResult = await GetRange(deviceId, recordId - 1, type);

			if (precedingRangeResult.IsFailure) {
				return new GetPrecedingRangeError(precedingRangeResult.Error, deviceId, recordId, type);
			}

			relevantRanges.Add(precedingRangeResult.Value);
		}

		// The containing range is always relevant.
		relevantRanges.Add(containingRange);

		// If the index is at the very end of the containingRange and isn't the last possible index (2^63) then we need to check the subsequent range.
		if (recordId == containingRange.End && recordId != long.MaxValue) {

			GetRangeResult subsequentRangeResult = await GetRange(deviceId, recordId - 1, type);

			if (subsequentRangeResult.IsFailure) {
				return new GetSubsequentRangeError(subsequentRangeResult.Error, deviceId, recordId, type);
			}

			relevantRanges.Add(subsequentRangeResult.Value);
		}

		// Create the RangeSet.
		SuperRange? superRange = SuperRange.Create(relevantRanges);
		if (superRange is null) {
			return new RangeOperationError();
		}

		return superRange;
	}

	private async Task<AddRangeToIndexResult> AddRangeToIndex(string deviceId, Ranges range) {

		SqliteCommand addRecordRange;

		switch (range.MetaData) {

			case GameIndexMetaData:
				addRecordRange = new(
					$"""
					 INSERT INTO "{nameof(Tables.GameIndex)}" (
					     "{Tables.MatchIndex.DeviceId}",
					     "{Tables.MatchIndex.StartIndex}",
					     "{Tables.MatchIndex.EndIndex}",
					     "{Tables.MatchIndex.Status}"
					 )
					 VALUES (
					     @DeviceId,
					     @StartIndex,
					     @EndIndex,
					     @Status
					 );
					 """,
					Connection);

				addRecordRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = deviceId });
				addRecordRange.Parameters.Add(new("@StartIndex", SqliteType.Integer) { Value = range.Start });
				addRecordRange.Parameters.Add(new("@EndIndex", SqliteType.Integer) { Value = range.End });
				addRecordRange.Parameters.Add(new("@Status", SqliteType.Text) { Value = range.MetaData.Status });
				break;

			case EventIndexMetaData:
				addRecordRange = new(
					$"""
					 INSERT INTO "{nameof(Tables.EventIndex)}" (
					     "{Tables.MatchIndex.DeviceId}",
					     "{Tables.MatchIndex.StartIndex}",
					     "{Tables.MatchIndex.EndIndex}",
					     "{Tables.MatchIndex.Status}"
					 )
					 VALUES (
					     @DeviceId,
					     @StartIndex,
					     @EndIndex,
					     @Status
					 );
					 """,
					Connection);

				addRecordRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = deviceId });
				addRecordRange.Parameters.Add(new("@StartIndex", SqliteType.Integer) { Value = range.Start });
				addRecordRange.Parameters.Add(new("@EndIndex", SqliteType.Integer) { Value = range.End });
				addRecordRange.Parameters.Add(new("@Status", SqliteType.Text) { Value = range.MetaData.Status });
				break;

			case MatchIndexMetaData metaData:

				addRecordRange = new(
					$"""
					 INSERT INTO "{nameof(Tables.MatchIndex)}" (
					     "{Tables.MatchIndex.DeviceId}",
					     "{Tables.MatchIndex.StartIndex}",
					     "{Tables.MatchIndex.EndIndex}",
					     "{Tables.MatchIndex.Status}",
					     "{Tables.MatchIndex.GameDeviceId}",
					     "{Tables.MatchIndex.GameId}",
					     "{Tables.MatchIndex.EventDataId}"
					 )
					 VALUES (
					     @DeviceId,
					     @StartIndex,
					     @EndIndex,
					     @Status,
					     @GameDeviceId,
					     @GameId,
					     @EventDataId,
					 );
					 """,
					Connection);

				addRecordRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = deviceId });
				addRecordRange.Parameters.Add(new("@StartIndex", SqliteType.Integer) { Value = range.Start });
				addRecordRange.Parameters.Add(new("@EndIndex", SqliteType.Integer) { Value = range.End });
				addRecordRange.Parameters.Add(new("@Status", SqliteType.Text) { Value = metaData.Status });
				addRecordRange.Parameters.Add(new("@GameDeviceId", SqliteType.Text) { Value = metaData.GameDeviceId });
				addRecordRange.Parameters.Add(new("@GameId", SqliteType.Text) { Value = metaData.GameId });
				addRecordRange.Parameters.Add(new("@EventDataId", SqliteType.Text) { Value = metaData.EventDeviceId });
				break;

			default:
				throw new UnreachableException(); // TODO removed with closed class hierarchy
		}

		if (await addRecordRange.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError addRecordRangeError) {
			return await RollbackError<InsertDataResult>.TryRollback(addRecordRangeError, Connection);
		}

		return Success.Instance;
	}

	private async Task<DeleteRangeFromIndexResult> DeleteRangeFromIndex(string deviceId, Ranges range) {

		string tableName = range.MetaData switch {
			GameIndexMetaData => nameof(Tables.GameIndex),
			EventIndexMetaData => nameof(Tables.EventIndex),
			MatchIndexMetaData => nameof(Tables.MatchIndex),
			_ => throw new UnreachableException() // TODO removed with closed class hierarchy
		};

		SqliteCommand deleteRecordRange = new(
			$"""
			 DELETE FROM "{tableName}"
			 WHERE "{Tables.MatchIndex.DeviceId}" = @DeviceId
			   AND "{Tables.MatchIndex.StartIndex}" = @StartIndex
			   AND "{Tables.MatchIndex.EndIndex}" = @EndIndex
			   AND "{Tables.MatchIndex.Status}" = @Status;
			 """,
			Connection);

		deleteRecordRange.Parameters.Add(new("@DeviceId", SqliteType.Text) { Value = deviceId });
		deleteRecordRange.Parameters.Add(new("@StartIndex", SqliteType.Integer) { Value = range.Start });
		deleteRecordRange.Parameters.Add(new("@EndIndex", SqliteType.Integer) { Value = range.End });
		deleteRecordRange.Parameters.Add(new("@Status", SqliteType.Text) { Value = range.MetaData.Status });

		if (await deleteRecordRange.ExecuteNonQueryAndExpect(1) is ExecuteNonQueryAndExpectError deleteRecordRangeError) {
			return await RollbackError<DeleteDataError>.TryRollback(deleteRecordRangeError, Connection);
		}

		return Success.Instance;
	}

}





public record SetRecordMetaDataResult : AsyncTryValueResult<Success, SetRecordMetaDataError> {

	public SetRecordMetaDataResult(Success value) : base(value) { }

	public SetRecordMetaDataResult(SetRecordMetaDataError error) : base(error) { }

	public static implicit operator SetRecordMetaDataResult(Success success) {
		return new(success);
	}

	public static implicit operator SetRecordMetaDataResult(SetRecordMetaDataError error) {
		return new(error);
	}

	public static implicit operator SetRecordMetaDataResult(GetSuperRangeError error) {
		return new(error);
	}

	public static implicit operator SetRecordMetaDataResult(RangeOperationError error) {
		return new(error);
	}

	public static implicit operator SetRecordMetaDataResult(DeleteRangeFromIndexError error) {
		return new(error);
	}

	public static implicit operator SetRecordMetaDataResult(AddRangeToIndexError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class SetRecordMetaDataError : OneOfBase<
	GetSuperRangeError,
	RangeOperationError,
	DeleteRangeFromIndexError,
	AddRangeToIndexError
>;


public record BulkSetRecordMetaData : AsyncTryValueResult<Success, BulkSetRecordMetaDataError> {

	public BulkSetRecordMetaData(Success value) : base(value) { }

	public BulkSetRecordMetaData(BulkSetRecordMetaDataError error) : base(error) { }

}

public class BulkSetRecordMetaDataError;



public record ResetIndexError : AsyncTryValueResult<Success, RollbackError<ExecuteNonQueryAndExpectError>> {

	public ResetIndexError(Success value) : base(value) { }

	public ResetIndexError(RollbackError<ExecuteNonQueryAndExpectError> error) : base(error) { }

	public static implicit operator ResetIndexError(Success success) {
		return new(success);
	}

	public static implicit operator ResetIndexError(RollbackError<ExecuteNonQueryAndExpectError> error) {
		return new(error);
	}

}



public record GetRangeResult : AsyncTryResult<Ranges, GetRangeError> {

	public GetRangeResult(Ranges value) : base(value) { }

	public GetRangeResult(GetRangeError error) : base(error) { }

	public static implicit operator GetRangeResult(GetRangeError error) {
		return new(error);
	}

}

//[GenerateOneOf]
public /*partial*/ class GetRangeError /*: OneOfBase<>*/; // todo



public record GetSuperRangeResult : AsyncTryResult<SuperRange, GetSuperRangeError> {

	public GetSuperRangeResult(SuperRange value) : base(value) { }

	public GetSuperRangeResult(GetSuperRangeError error) : base(error) { }

	public static implicit operator GetSuperRangeResult(SuperRange value) {
		return new(value);
	}

	public static implicit operator GetSuperRangeResult(GetSuperRangeError error) {
		return new(error);
	}

	public static implicit operator GetSuperRangeResult(GetPrecedingRangeError error) {
		return new(error);
	}

	public static implicit operator GetSuperRangeResult(GetContainingRangeError error) {
		return new(error);
	}

	public static implicit operator GetSuperRangeResult(GetSubsequentRangeError error) {
		return new(error);
	}

	public static implicit operator GetSuperRangeResult(RangeOperationError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class GetSuperRangeError : OneOfBase<
	SuperRange,
	GetPrecedingRangeError,
	GetContainingRangeError,
	GetSubsequentRangeError,
	RangeOperationError
>;

public record GetPrecedingRangeError {

	public required GetRangeError Error { get; init; }

	public required string DeviceId { get; init; }

	public required long RecordId { get; init; }

	public required RecordType Type { get; init; }

	[SetsRequiredMembers]
	public GetPrecedingRangeError(GetRangeError error, string deviceId, long recordId, RecordType type) {
		Error = error;
		DeviceId = deviceId;
		RecordId = recordId;
		Type = type;
	}

}

public record GetContainingRangeError {

	public required GetRangeError Error { get; init; }

	public required string DeviceId { get; init; }

	public required long RecordId { get; init; }

	public required RecordType Type { get; init; }

	[SetsRequiredMembers]
	public GetContainingRangeError(GetRangeError error, string deviceId, long recordId, RecordType type) {
		Error = error;
		DeviceId = deviceId;
		RecordId = recordId;
		Type = type;
	}

}

public record GetSubsequentRangeError {

	public required GetRangeError Error { get; init; }

	public required string DeviceId { get; init; }

	public required long RecordId { get; init; }

	public required RecordType Type { get; init; }

	[SetsRequiredMembers]
	public GetSubsequentRangeError(GetRangeError error, string deviceId, long recordId, RecordType type) {
		Error = error;
		DeviceId = deviceId;
		RecordId = recordId;
		Type = type;
	}

}



public record AddRangeToIndexResult : AsyncTryValueResult<Success, AddRangeToIndexError> {

	public AddRangeToIndexResult(Success value) : base(value) { }

	public AddRangeToIndexResult(AddRangeToIndexError error) : base(error) { }

	public static implicit operator AddRangeToIndexResult(Success success) {
		return new(success);
	}

	public static implicit operator AddRangeToIndexResult(AddRangeToIndexError error) {
		return new(error);
	}

	public static implicit operator AddRangeToIndexResult(RollbackError<InsertDataResult> error) {
		return new AddRangeToIndexError(error);
	}

}

public record AddRangeToIndexError(RollbackError<InsertDataResult> Error);



public record DeleteRangeFromIndexResult : AsyncTryValueResult<Success, DeleteRangeFromIndexError> {

	public DeleteRangeFromIndexResult(Success value) : base(value) { }

	public DeleteRangeFromIndexResult(DeleteRangeFromIndexError error) : base(error) { }

	public static implicit operator DeleteRangeFromIndexResult(Success success) {
		return new(success);
	}

	public static implicit operator DeleteRangeFromIndexResult(DeleteRangeFromIndexError error) {
		return new(error);
	}

	public static implicit operator DeleteRangeFromIndexResult(RollbackError<DeleteDataError> error) {
		return new DeleteRangeFromIndexError(error);
	}

}

public record DeleteRangeFromIndexError(RollbackError<DeleteDataError> Error);