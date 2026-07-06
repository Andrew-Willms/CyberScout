using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Comms.Dtos;
using Comms.Serialization;
using Database;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using ScoutingApp.AppManagement;
using ZXing.Net.Maui;

namespace ScoutingApp.Views.Pages.Flyout;



public partial class QrCodeScanner : ContentPage {

	public static string Route => "QrCodeScanner";

	private AppManager AppManager { get; }
	private IErrorPresenter ErrorPresenter { get; }

	public int QrCodeCount {
		get;
		set {
			field = value;
			OnPropertyChanged();
		}
	}

	public string LastQrCodeScanned {
		get;
		set {
			field = value;
			OnPropertyChanged();
		}
	} = "";



	public QrCodeScanner(AppManagerGetter appManagerGetter, IDataStore dataStore, IErrorPresenter errorPresenter) {

		// TODO: add graceful error handling
		AppManager = appManagerGetter.Instance ?? throw new AppManagerNotCreatedException(typeof(QrCodeScanner));
		ErrorPresenter = errorPresenter;

		BindingContext = this;
		InitializeComponent();

		QrCodeReader.Options = new() {
			Formats = BarcodeFormats.TwoDimensional,
			TryHarder = true,
			AutoRotate = true,
			Multiple = false
		};
	}



	private void CameraBarcodeReaderView_OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e) {

		string? qrCodeString = e.Results.FirstOrDefault(IsValidQrCode)?.Value;

		if (qrCodeString is null) {
			return;
		}

		if (AppManager.GameSpecification is null) {
			ErrorPresenter.DisplayError("Game Specification Null",
				"The GameSpecification is null, this shouldn't be the case.");
			return;
		}

		MatchDataDto? matchDataDto = MatchDataDtoToCsv.Deserialize(qrCodeString, AppManager.GameSpecification);

		if (matchDataDto is null) {
			ErrorPresenter.DisplayError("Invalid QR Code", "The QR code data could not be converted into a match.");
			return;
		}

		ImportMatchDataResult saveResult = AppManager.DataStore.ImportMatchData(matchDataDto).Result;

		MainThread.BeginInvokeOnMainThread(() => {

			saveResult.Switch(
				success => {
					QrCodeCount++;
					LastQrCodeScanned = qrCodeString;
				},
				duplicate => LastQrCodeScanned = "Duplicate",
				// TODO probably display this as an inline error instead of in the QR code text box
				// doesn't need to be a popup error because it's not a serious error
				rollbackError => ErrorPresenter.DisplayError(
					"Error and roll-back failure",
					$"Exception of type '{rollbackError.FirstException.GetType()}' with the message:\r\n{rollbackError.FirstException.Message}" +
					$"{(rollbackError.FirstException.InnerException is null
						? string.Empty
						: $"\r\n\r\nInner exception of type '{rollbackError.FirstException.InnerException.GetType()}' " +
						  $"with message:\r\n{rollbackError.FirstException.InnerException.Message}")}\r\n\r\n" +
					$"A roll-back was attempted resulting in an exception of type" +
					$"'{rollbackError.RollbackException.GetType()}' with the message:\r\n{rollbackError.RollbackException.Message}" +
					$"{(rollbackError.RollbackException.InnerException is null
						? string.Empty
						: $"\r\n\r\nInner exception of type '{rollbackError.RollbackException.InnerException.GetType()}' " +
						  $"with message:\r\n{rollbackError.RollbackException.InnerException.Message}")}"),
				exception => ErrorPresenter.DisplayError(
					"Error scanning match",
					$"Exception of type '{exception.GetType()}' with the message:\r\n{exception.Message}" +
					$"{(exception.InnerException is null
							? string.Empty
							: $"\r\n\r\nInner exception of type '{exception.InnerException.GetType()}' " +
							  $"with message:\r\n{exception.InnerException.Message}")}")
			);
		});
	}

	private static bool IsValidQrCode(BarcodeResult qrCode) {

		return
			!qrCode.Value.All(character => character is '0') &&
			qrCode.Value.Length > 25;
	}

	private async void ExportButton_OnClicked(object? sender, EventArgs e) {

		try {
			if (AppManager.GameSpecification is null) {
				ErrorPresenter.DisplayError("Game Specification Null",
					"The GameSpecification is null, this shouldn't be the case.");
				return;
			}

			GetMatchDataResult matchDataResult = await AppManager.DataStore.GetMatchData();

			string? error = matchDataResult.Match<string?>(
				success => null,
				exception =>
					$"An exception occured with the type '{exception.GetType()}' and message:\r\n\r\n {exception.Message} \r\n\r\n" +
					$"There is an inner exception of type '{exception.InnerException?.GetType()}' and message:\r\n\r\n {exception.InnerException?.Message}",
				matchDataDeserializationError =>
					$"There was an error deserializing the match data. The serialized match data is:\r\n\r\n" +
					$"{matchDataDeserializationError.SerializedMatchData}",
				invalidEditIdsError =>
					$"Edit IDs must both have values or must both be null. The EditDeviceId is '{invalidEditIdsError.EditOfRecord}' " +
					$"but the EditRecordId is '{invalidEditIdsError.EditOfRecord}'."
			);

			if (error is not null) {
				ErrorPresenter.DisplayError("Error Loading Match Data",
					"Could not load the match data from the database while trying to export to CSV.");
				return;
			}

			List<MatchDataDto> matchData = matchDataResult.AsT0;

			StringBuilder stringBuilder = new(MatchDataToCsv.GetCsvHeaders(AppManager.GameSpecification));
			foreach (MatchDataDto matchDataDto in matchData) {
				stringBuilder.Append('\n');
				stringBuilder.Append(MatchDataToCsv.Serialize(matchDataDto.Data));
			}

			string saveDirectory =
				Android.App.Application.Context.GetExternalFilesDir(Android.OS.Environment.DirectoryDocuments)!
					.AbsolutePath;
			string path = Path.Combine(saveDirectory, $"Match Data {DateTime.Now:yyyy-MM-dd HH_mm_ss}.csv");
			await File.WriteAllTextAsync(path, stringBuilder.ToString());

		} catch (Exception exception) {

			ErrorPresenter.DisplayError(
				"Error exporting data.",
				$"Exception of type '{exception.GetType()}' with the message:\r\n{exception.Message}" +
				$"{(exception.InnerException is null
					? string.Empty
					: $"\r\n\r\nInner exception of type '{exception.InnerException.GetType()}' " +
					  $"with message:\r\n{exception.InnerException.Message}")}");
		}
	}

}