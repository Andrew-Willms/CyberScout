using System.IO;
using Comms.Serialization;
using Domain.GameSpecification;
using GameMakerWpf.Domain.Editors;
using Microsoft.Win32;
using Newtonsoft.Json;
using UtilitiesLibrary.Results;
using static GameMakerWpf.AppManagement.IPublisher;

namespace GameMakerWpf.AppManagement;



public interface IPublisher {

	public IPublishOldResult Publish(GameEditor gameEditor);

	public interface IPublishOldResult : IOldResult {

		public new class OldSuccess : IOldResult.OldSuccess, IPublishOldResult;

		public class Aborted : OldError, IPublishOldResult;

		public class GameEditorCouldNotBeConvertedToGameSpecification : OldError, IPublishOldResult;

		public class GameSpecificationCouldNotBeConvertedToSaveData : OldError, IPublishOldResult;

		public class SaveLocationDoesNotExist : OldError, IPublishOldResult;

		public class SaveLocationCouldNotBeWrittenTo : OldError, IPublishOldResult;

	}

}



public class FilePublisher : IPublisher {

	private static SaveFileDialog SaveFileDialog => new() {
		Title = "Select a file name and location for the published Game Specification.",
		Filter = "CyberScout Game Specification (*.cgs)|*.cgs"
	};

	public IPublishOldResult Publish(GameEditor gameEditor) {

		GameSpec? gameSpecSpecification = gameEditor.ToGameSpecification();

		if (gameSpecSpecification is null) {
			return new IPublishOldResult.GameEditorCouldNotBeConvertedToGameSpecification();
		}

		SaveFileDialog saveFileDialog = SaveFileDialog;
		bool? proceed = saveFileDialog.ShowDialog();

		if (proceed is null or false) {
			return new IPublishOldResult.Aborted();
		}

		string filePath = saveFileDialog.FileName;
		string[] filePathPieces = filePath.Split("\\");
		string folderPath = string.Join("\\", filePathPieces[..^1]);

		if (!Directory.Exists(folderPath)) {
			return new IPublishOldResult.SaveLocationDoesNotExist();
		}

		string serializedGameSpecification;
		try {
			serializedGameSpecification = JsonConvert.SerializeObject(gameSpecSpecification, JsonSettings.JsonSerializerSettings);

		} catch {
			return new IPublishOldResult.GameSpecificationCouldNotBeConvertedToSaveData();
		}

		try {
			File.WriteAllText(filePath, serializedGameSpecification);

		} catch {
			return new IPublishOldResult.SaveLocationCouldNotBeWrittenTo();
		}

		return new IPublishOldResult.OldSuccess();
	}

}