using static GameMakerWpf.AppManagement.IPublisher.IPublishOldResult;

namespace GameMakerWpf.DisplayData.Errors.ErrorData.AppManagerErrors; 



public static class PublishErrors {

	public static ErrorDisplayData GameEditorCouldNotBeConvertedToGameSpecification(GameEditorCouldNotBeConvertedToGameSpecification error) => new() {
		Caption = "Save Location Inaccessible",
		Message = "The save location specified could not be accessed. Verify that the location you specified exists. " +
		          "Alternately, try opening a different GameProject using \"Open\"."
	};

	public static ErrorDisplayData GameSpecificationCouldNotBeConvertedToSaveData(GameSpecificationCouldNotBeConvertedToSaveData error) => new() {
		Caption = "Save Location Inaccessible",
		Message = "The save location specified could not be accessed. Verify that the location you specified exists. " +
		          "Alternately, try opening a different GameProject using \"Open\"."
	};

	public static ErrorDisplayData SaveLocationDoesNotExist(SaveLocationDoesNotExist error) => new() {
		Caption = "Save Location Inaccessible",
		Message = "The save location specified could not be accessed. Verify that the location you specified exists. " +
		          "Alternately, try opening a different GameProject using \"Open\"."
	};

	public static ErrorDisplayData SaveLocationCouldNotBeWrittenTo(SaveLocationCouldNotBeWrittenTo error) => new() {
		Caption = "Save Location Inaccessible",
		Message = "The save location specified could not be accessed. Verify that the location you specified exists. " +
		          "Alternately, try opening a different GameProject using \"Open\"."
	};

}