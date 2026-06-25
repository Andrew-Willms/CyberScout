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

	public interface ISaveResult : IResult {

		public class Success : IResult.Success, ISaveResult;

		public class NoSaveLocationSpecified : Error, ISaveResult;

		public class GameEditingDataCouldNotBeConvertedToSaveData : Error, ISaveResult;

		public class SaveLocationInaccessible : Error, ISaveResult;

	}

	public interface ISaveAsResult : IResult {

		public class Success : IResult.Success, ISaveAsResult;

		public class Aborted : Error, ISaveAsResult;

		public class SaveLocationIsInvalid : Error, ISaveAsResult;

	}

	public interface IOpenResult : IResult<GameEditingData> {

		public class Success : IResult<GameEditingData>.Success, IOpenResult;

		public class Aborted : Error, IOpenResult;

		public class SaveLocationInaccessible : Error, IOpenResult;
		
		public class SavedDataCouldNotBeConvertedToGameEditingData : Error, IOpenResult;

	}



	public bool ProjectHasSaveLocation { get; }

	public ISaveResult Save(GameEditingData gameEditingData);

	public ISaveAsResult SetSaveLocation();

	public IOpenResult Open();

}



public class Saver : ISaver {

	public bool ProjectHasSaveLocation => FilePath.HasValue;

	private Optional<string> FilePath = Optional.NoValue;

	public ISaveResult Save(GameEditingData gameEditingData) {

		if (!FilePath.HasValue) {
			return new ISaveResult.NoSaveLocationSpecified();
		}

		string serializedProject;
		try {
			serializedProject = JsonConvert.SerializeObject(gameEditingData, JsonSettings.JsonSerializerSettings);

		} catch {
			return new ISaveResult.GameEditingDataCouldNotBeConvertedToSaveData();
		}

		try {
			File.WriteAllText(FilePath.Value, serializedProject);

		} catch {
			return new ISaveResult.SaveLocationInaccessible();
		}

		return new ISaveResult.Success();
	}

	public ISaveAsResult SetSaveLocation() {

		SaveFileDialog saveFileDialog = SaveFileDialog;

		bool? proceed = saveFileDialog.ShowDialog();

		if (proceed is null or false) {
			return new ISaveAsResult.Aborted();
		}

		string filePath = saveFileDialog.FileName;
		string[] filePathPieces = filePath.Split("\\");
		string folderPath = string.Join("\\", filePathPieces[..^1]);

		if (!Directory.Exists(folderPath)) {
			return new ISaveAsResult.SaveLocationIsInvalid();
		}

		FilePath = filePath.Optionalize();

		return new ISaveAsResult.Success();
	}

	public IOpenResult Open() {

		OpenFileDialog openFileDialog = OpenFileDialog;

		bool? proceed = openFileDialog.ShowDialog();

		if (proceed is null or false) {
			return new IOpenResult.Aborted();
		}

		string filePath = openFileDialog.FileName;
		string serializedGameEditingData;

		try {
			serializedGameEditingData = File.ReadAllText(filePath);

		} catch {
			return new IOpenResult.SaveLocationInaccessible();
		}

		try {
			GameEditingData newGameEditingData =
				JsonConvert.DeserializeObject<GameEditingData>(serializedGameEditingData, JsonSettings.JsonSerializerSettings)
				?? throw new NoNullAllowedException();

			FilePath = filePath.Optionalize();
			return new IOpenResult.Success { Value = newGameEditingData};

		} catch {
			return new IOpenResult.SavedDataCouldNotBeConvertedToGameEditingData();
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