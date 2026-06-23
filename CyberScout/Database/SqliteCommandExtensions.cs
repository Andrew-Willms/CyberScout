using System.Runtime.CompilerServices;
using Database.Results;
using Microsoft.Data.Sqlite;
using OneOf;
using Willmsy.AsyncTryResult;

namespace Database;



public static class SqliteCommandExtensions {

	/// <summary> Executes an <see cref="SqliteCommand"/> as a non-query, expecting a specific number of modifications to occur. </summary>
	/// <param name="command"> The <see cref="SqliteCommand"/> to be executed as a non-query. </param>
	/// <param name="expectedModifications"> The expected number of modifications performed by the <see cref="SqliteCommand"/>. An <see cref="WrongNumberOfModificationsError"/> error is returned if the actual result differs. </param>
	/// <param name="callerFilePath"> The file path of the caller, supplied by <see cref="CallerFilePathAttribute"/>, used to construct a <see cref="DataStoreError"/> if one occurs. </param>
	/// <param name="callerMemberName"> The name of the caller, supplied by <see cref="CallerMemberNameAttribute"/>, used to construct a <see cref="DataStoreError"/> if one occurs. </param>
	/// <param name="commandName"> The expression for <paramref name="command"/>, provided by <see cref="CallerArgumentExpressionAttribute"/>, used to construct a <see cref="DataStoreError"/> if one occurs. </param>
	/// <returns> A <see cref="DataStoreError"/> if an error occured, otherwise <see langword="null"/>. </returns>
	public static async Task<DataStoreError?> ExecuteNonQueryAndExpect(
		this SqliteCommand command,
		int expectedModifications,
		[CallerFilePath] string callerFilePath = "",
		[CallerMemberName] string callerMemberName = "",
		[CallerArgumentExpression(nameof(command))] string? commandName = null) {

		try {
			int modifications = await command.ExecuteNonQueryAsync();

			if (modifications != expectedModifications) {
				return new WrongNumberOfModificationsError {
					CommandText = command.CommandText,
					ExpectedModifications = expectedModifications,
					ActualModifications = modifications,
					CallerFilePath = callerFilePath,
					CallerMemberName = callerMemberName,
					CommandName = commandName
				};
			}

			return null;

		} catch (Exception exception) {
			return DataStoreError.FromException(exception, command, callerFilePath, callerMemberName, commandName);
		}
	}



	/// <summary> Executes an <see cref="SqliteCommand"/> as a scalar. </summary>
	/// <typeparam name="T"> The result of the <see cref="SqliteCommand"/>. A <see cref="ParseError"/> is returned if the result cannot be converted to type <typeparamref name="T"/>. </typeparam>
	/// <param name="command"> The <see cref="SqliteCommand"/> to be executed as a scalar. </param>
	/// <param name="callerFilePath"> The file path of the caller, supplied by <see cref="CallerFilePathAttribute"/>, used to construct a <see cref="DataStoreError"/> if one occurs. </param>
	/// <param name="callerMemberName"> The name of the caller, supplied by <see cref="CallerMemberNameAttribute"/>, used to construct a <see cref="DataStoreError"/> if one occurs. </param>
	/// <param name="commandName"> The expression for <paramref name="command"/>, provided by <see cref="CallerArgumentExpressionAttribute"/>, used to construct a <see cref="DataStoreError"/> if one occurs. </param>
	/// <returns> The result of the <see cref="SqliteCommand"/> or an <see cref="DataStoreError"/>. </returns>
	public static async Task<AsyncTryValueResult<T, DataStoreError>> TryExecuteScalar<T>(
		this SqliteCommand command,
		[CallerFilePath] string callerFilePath = "",
		[CallerMemberName] string callerMemberName = "",
		[CallerArgumentExpression(nameof(command))] string? commandName = null)
		where T : struct {

		try {
			object? result = await command.ExecuteScalarAsync();

			if (result is T value) {
				return value;
			}

			return new ParseError {
				CommandText = command.CommandText,
				ExpectedType = typeof(T).FullName,
				ActualType = result?.GetType().FullName,
				StringValue = result?.ToString(),
				CallerFilePath = callerFilePath,
				CallerMemberName = callerMemberName,
				CommandName = commandName
			};

		} catch (Exception exception) {
			return DataStoreError.FromException(exception, command, callerFilePath, callerMemberName, commandName);
		}
	}

}