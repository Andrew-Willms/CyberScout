using static GameMakerWpf.AppManagement.ISaver.ISaveAsOldResult;

namespace GameMakerWpf.DisplayData.Errors.ErrorData.AppManagerErrors;



public static class SaveAsErrors {

	public static ErrorDisplayData SaveLocationIsInvalid(SaveLocationIsInvalid error) => new() {
		Caption = "Save Location Invalid",
		Message = "The Game Project does not have a valid save location. Try using \"SaveAs\"."
	};

}