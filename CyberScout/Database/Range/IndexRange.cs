namespace Database.Range;



public record IndexRange {

	public required long Start { get; init; }

	public required long End { get; init; }

	public required RecordStatus Status { get; init; }

	public bool Contains(long index) {
		return Start <= index && index <= End;
	}

}



public enum RecordStatus {
	Stored,
	Ignored
}