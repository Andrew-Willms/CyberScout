using System.Data;
using System.IO;
using Comms.Serialization;
using GameMakerWpf.Domain.EditingData;
using Microsoft.Win32;
using Newtonsoft.Json;
using UtilitiesLibrary.Optional;
using UtilitiesLibrary.Results;
using static GameMakerWpf.AppManagement.ISaver;

namespace GameMakerWpf.AppManagement;



public interface ISaver {

	public interface ISaveOldResult : IOldResult {

		public class OldSuccess : IOldResult.OldSuccess, ISaveOldResult;

		public class NoSaveOldLocationSpecified : OldError, ISaveOldResult;

		public class GameEditingDataCouldNotBeConvertedToSaveOldData : OldError, ISaveOldResult;

		public class SaveOldLocationInaccessible : OldError, ISaveOldResult;

	}

	public interface ISaveAsOldResult : IOldResult {

		public class OldSuccess : IOldResult.OldSuccess, ISaveAsOldResult;

		public class Aborted : OldError, ISaveAsOldResult;

		public class SaveLocationIsInvalid : OldError, ISaveAsOldResult;

	}

	public interface IOpenOldResult : IOldResult<GameEditingData> {

		public class OldSuccess : IOldResult<GameEditingData>.OldSuccess, IOpenOldResult;

		public class Aborted : OldError, IOpenOldResult;

		public class SaveLocationInaccessible : OldError, IOpenOldResult;
		
		public class SavedDataCouldNotBeConvertedToGameEditingData : OldError, IOpenOldResult;

	}



	public bool ProjectHasSaveLocation { get; }

	public ISaveOldResult Save(GameEditingData gameEditingData);

	public ISaveAsOldResult SetSaveLocation();

	public IOpenOldResult Open();

}



public class Saver : ISaver {

	public bool ProjectHasSaveLocation => FilePath.HasValue;

	private Optional<string> FilePath = Optional.NoValue;

	public ISaveOldResult Save(GameEditingData gameEditingData) {

		if (!FilePath.HasValue) {
			return new ISaveOldResult.NoSaveOldLocationSpecified();
		}

		string serializedProject;
		try {
			serializedProject = JsonConvert.SerializeObject(gameEditingData, JsonSettings.JsonSerializerSettings);

		} catch {
			return new ISaveOldResult.GameEditingDataCouldNotBeConvertedToSaveOldData();
		}

		try {
			File.WriteAllText(FilePath.Value, serializedProject);

		} catch {
			return new ISaveOldResult.SaveOldLocationInaccessible();
		}

		return new ISaveOldResult.OldSuccess();
	}

	public ISaveAsOldResult SetSaveLocation() {

		SaveFileDialog saveFileDialog = SaveFileDialog;

		bool? proceed = saveFileDialog.ShowDialog();

		if (proceed is null or false) {
			return new ISaveAsOldResult.Aborted();
		}

		string filePath = saveFileDialog.FileName;
		string[] filePathPieces = filePath.Split("\\");
		string folderPath = string.Join("\\", filePathPieces[..^1]);

		if (!Directory.Exists(folderPath)) {
			return new ISaveAsOldResult.SaveLocationIsInvalid();
		}

		FilePath = filePath.Optionalize();

		return new ISaveAsOldResult.OldSuccess();
	}

	public IOpenOldResult Open() {

		OpenFileDialog openFileDialog = OpenFileDialog;

		bool? proceed = openFileDialog.ShowDialog();

		if (proceed is null or false) {
			return new IOpenOldResult.Aborted();
		}

		string filePath = openFileDialog.FileName;
		string serializedGameEditingData;

		try {
			serializedGameEditingData = File.ReadAllText(filePath);

		} catch {
			return new IOpenOldResult.SaveLocationInaccessible();
		}

		try {
			GameEditingData newGameEditingData =
				JsonConvert.DeserializeObject<GameEditingData>(serializedGameEditingData, JsonSettings.JsonSerializerSettings)
				?? throw new NoNullAllowedException();

			FilePath = filePath.Optionalize();
			return new IOpenOldResult.OldSuccess { Value = newGameEditingData};

		} catch {
			return new IOpenOldResult.SavedDataCouldNotBeConvertedToGameEditingData();
		}
	}



	private static OpenFileDialog OpenFileDialog => new() {
		Title = "Select a file to open.",
		Filter = "CyberScout Game Project (*.cgp)|*.cgp"
	};

	private static SaveFileDialog SaveFileDialog => new() {
		Title = "Select a file name and location for the project to be saved.",
		Filter = "CyberScout Game Project (*.cgp)|*.cgp"
	};

}