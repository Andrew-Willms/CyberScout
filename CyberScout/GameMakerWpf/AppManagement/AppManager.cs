using System.ComponentModel;
using System.Diagnostics;
using GameMakerWpf.DisplayData.Errors.ErrorData.AppManagerErrors;
using GameMakerWpf.Domain.Data;
using GameMakerWpf.Domain.Editors;
using GameMakerWpf.Domain.Editors.DataFieldEditors;
using Microsoft.Extensions.DependencyInjection;
using WPFUtilities;
using static GameMakerWpf.AppManagement.ISavePrompter;
using static GameMakerWpf.AppManagement.ISaver.ISaveAsOldResult;
using static GameMakerWpf.AppManagement.ISaver.ISaveOldResult;

namespace GameMakerWpf.AppManagement;



public abstract class AppManagerDependent : DependentControl<IAppManager> {

	protected override IAppManager SingletonGetter => App.ServiceProvider.GetRequiredService<IAppManager>();

}



public interface IAppManager : INotifyPropertyChanged {

	public DataFieldEditor? SelectedDataField { get; set; }

	public GameEditor GameEditor { get; }

	public void ApplicationStartup();

	public void SaveGameProject();

	public void SaveGameProjectAs();

	public void OpenGameProject();

	public void NewGameProject();

	public void Publish();

}



public class AppManager : IAppManager, INotifyPropertyChanged {

	public GameEditor GameEditor {
		get;
		private set {
			value.AnythingChanged.Subscribe(() => ProjectIsSaved = false);
			field = value;
			OnPropertyChanged(nameof(GameEditor));
		}
	} = null!;

	public DataFieldEditor? SelectedDataField {
		get;
		set {
			field = value;
			OnPropertyChanged(nameof(SelectedDataField));
		}
	}

	private IMainView MainView => App.ServiceProvider.GetRequiredService<IMainView>();
	private static IErrorPresenter ErrorPresenter => App.ServiceProvider.GetRequiredService<IErrorPresenter>();
	private readonly ISaver Saver = App.ServiceProvider.GetRequiredService<ISaver>();
	private static ISavePrompter SavePrompter => App.ServiceProvider.GetRequiredService<ISavePrompter>();
	private readonly IPublisher Publisher = App.ServiceProvider.GetRequiredService<IPublisher>();

	private bool ProjectIsSaved { get; set; } = true;



	public AppManager() {

		GameEditor = new(DefaultEditingDataValues.DefaultGameEditingData);
	}

	public void ApplicationStartup() {
		MainView.Show();
	}



	private void PromptIfUnsaved(out bool cancelOperation) {

		if (ProjectIsSaved) {
			cancelOperation = false;
			return;
		}

		SavePromptResult savePromptResult = SavePrompter.PromptSave();

		switch (savePromptResult) {

			case SavePromptResult.Cancel:
				cancelOperation = true;
				return;

			case SavePromptResult.SaveAndContinue:
				SaveGameProject();
				cancelOperation = false;
				return;

			case SavePromptResult.ContinueWithoutSaving:
				cancelOperation = false;
				return;

			default:
				throw new InvalidEnumArgumentException();
		}
	}

	public void SaveGameProject() {

		if (!Saver.ProjectHasSaveLocation) {
			SaveGameProjectAs();
			return;
		}

		ISaver.ISaveOldResult oldResult = Saver.Save(GameEditor.ToEditingData());

		switch (oldResult) {

			case ISaver.ISaveOldResult.OldSuccess:
				ProjectIsSaved = true;
				return;

			case NoSaveOldLocationSpecified error:
				ErrorPresenter.DisplayError(error, SaveErrors.NoSaveLocationSpecified);
				break;

			case SaveOldLocationInaccessible error:
				ErrorPresenter.DisplayError(error, SaveErrors.SaveLocationInaccessible);
				break;

			case GameEditingDataCouldNotBeConvertedToSaveOldData error:
				ErrorPresenter.DisplayError(error, SaveErrors.GameEditingDataCouldNotBeConvertedToSaveData);
				break;

			default:
				throw new UnreachableException();
		}
	}

	public void SaveGameProjectAs() {

		ISaver.ISaveAsOldResult oldResult = Saver.SetSaveLocation();

		switch (oldResult) {

			case UtilitiesLibrary.Results.OldSuccess:
				break;

			case Aborted:
				return;

			case SaveLocationIsInvalid error:
				ErrorPresenter.DisplayError(error, SaveAsErrors.SaveLocationIsInvalid);
				return;

			default:
				throw new UnreachableException();
		}

		SaveGameProject();
	}

	public void OpenGameProject() {

		PromptIfUnsaved(out bool cancelOperation);
		if (cancelOperation) {
			return;
		}

		ISaver.IOpenOldResult openOldResult = Saver.Open();

		switch (openOldResult) {

			case ISaver.IOpenOldResult.OldSuccess newGameEditingData:
				GameEditor = new(newGameEditingData.Value);
				return;

			case ISaver.IOpenOldResult.Aborted:
				return;

			case ISaver.IOpenOldResult.SaveLocationInaccessible error:
				ErrorPresenter.DisplayError(error, OpenError.SaveLocationInaccessible);
				return;

			case ISaver.IOpenOldResult.SavedDataCouldNotBeConvertedToGameEditingData error:
				ErrorPresenter.DisplayError(error, OpenError.SavedDataCouldNotBeConvertedToGameEditingData);
				return;

			default:
				throw new UnreachableException();
		}
	}

	public void NewGameProject() {

		PromptIfUnsaved(out bool cancelOperation);
		if (cancelOperation) {
			return;
		}

		GameEditor = new(DefaultEditingDataValues.DefaultGameEditingData);
		ProjectIsSaved = true;
	}

	public void Publish() {

		IPublisher.IPublishOldResult oldResult = Publisher.Publish(GameEditor);

		switch (oldResult) {

			case UtilitiesLibrary.Results.OldSuccess:
				break;

			case IPublisher.IPublishOldResult.Aborted:
				return;

			case IPublisher.IPublishOldResult.GameEditorCouldNotBeConvertedToGameSpecification error:
				ErrorPresenter.DisplayError(error, PublishErrors.GameEditorCouldNotBeConvertedToGameSpecification);
				return;

			case IPublisher.IPublishOldResult.GameSpecificationCouldNotBeConvertedToSaveData error:
				ErrorPresenter.DisplayError(error, PublishErrors.GameSpecificationCouldNotBeConvertedToSaveData);
				return;

			case IPublisher.IPublishOldResult.SaveLocationDoesNotExist error:
				ErrorPresenter.DisplayError(error, PublishErrors.SaveLocationDoesNotExist);
				return;

			case IPublisher.IPublishOldResult.SaveLocationCouldNotBeWrittenTo error:
				ErrorPresenter.DisplayError(error, PublishErrors.SaveLocationCouldNotBeWrittenTo);
				return;

			default:
				throw new UnreachableException();
		}

	}



	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged(string propertyName) {
		PropertyChanged?.Invoke(this, new(propertyName));
	}

}