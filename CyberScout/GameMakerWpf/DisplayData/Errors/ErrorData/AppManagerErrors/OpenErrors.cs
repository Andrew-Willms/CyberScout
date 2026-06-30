using static GameMakerWpf.AppManagement.ISaver.IOpenOldResult;

namespace GameMakerWpf.DisplayData.Errors.ErrorData.AppManagerErrors;



public static class OpenError {

	public static ErrorDisplayData SaveLocationInaccessible(SaveLocationInaccessible error) => new() {
		Caption = "Save Location Inaccessible",
		Message = "The save location specified could not be accessed. Verify that the location you specified exists. " +
		          "Alternately, try opening a different GameProject using \"Open\"."
	};

	public static ErrorDisplayData SavedDataCouldNotBeConvertedToGameEditingData(SavedDataCouldNotBeConvertedToGameEditingData error) => new() {
		Caption = "Saved Data Could not Be Converted",
		Message = "The saved data could not be converted into a GameProject. Verify that you specified the correct save location. " +
		          "Alternately, try opening a different GameProject using \"Open\". " +
		          "Please contact the creators of CyberScout if you receive this error."
	};

}