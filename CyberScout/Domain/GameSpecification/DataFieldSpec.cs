using System;
using System.Linq;
using Domain.DataCollectors;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Optional;

namespace Domain.GameSpecification;



// TODO consider making this a closed class hierarchy
public abstract record DataFieldSpec {

	public required string Name { get; init; }

	public abstract DataField ToDataField();

}

public record BooleanDataFieldSpec : DataFieldSpec {

	public required bool InitialValue { get; init; }

	public override DataField ToDataField() {
		return new BooleanDataField(this);
	}

}

public record TextDataFieldSpec : DataFieldSpec {

	public required string InitialValue { get; init; } = string.Empty;

	public required bool MustNotBeEmpty { get; init; }

	public required bool MustNotBeInitialValue { get; init; }

	public override DataField ToDataField() {
		return new TextDataField(this);
	}

}

public record IntegerDataFieldSpec : DataFieldSpec {

	public required int InitialValue { get; init; }

	public required int MinValue { get; init; } = int.MinValue;

	public required int MaxValue { get; init; } = int.MaxValue;

	public override DataField ToDataField() {
		return new IntegerDataField(this);
	}

}

public record SelectionDataFieldSpec : DataFieldSpec, IEquatable<SelectionDataFieldSpec> {

	public required ReadOnlyList<string> Options { get; init; }

	// Todo validate the initial value
	public required Optional<string> InitialValue { get; init; }

	public required bool RequiresValue { get; init; }

	public override DataField ToDataField() {
		return new SelectionDataField(this);
	}

	public virtual bool Equals(SelectionDataFieldSpec? other) {

		if (other is null) {
			return false;
		}

		if (ReferenceEquals(this, other)) {
			return true;
		}

		return Options.SequenceEqual(other.Options) && RequiresValue == other.RequiresValue;
	}

	public override int GetHashCode() {
		return HashCode.Combine(Options, RequiresValue);
	}
}