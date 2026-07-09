using System;
using System.Linq;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Optional;

namespace Domain.GameSpecification;



// TODO consider making this a closed class hierarchy
public abstract record DataFieldSpec {

	public required string Name { get; init; }

}

public record BooleanDataFieldSpec : DataFieldSpec {

	public required bool InitialValue { get; init; }

}

public record TextDataFieldSpec : DataFieldSpec {

	public required string InitialValue { get; init; } = string.Empty;

	public required bool MustNotBeEmpty { get; init; }

	public required bool MustNotBeInitialValue { get; init; }

}

public record IntegerDataFieldSpec : DataFieldSpec {

	public required int InitialValue { get; init; }

	public required int MinValue { get; init; } = int.MinValue;

	public required int MaxValue { get; init; } = int.MaxValue;

}

public record SelectionDataFieldSpec : DataFieldSpec, IEquatable<SelectionDataFieldSpec> {

	// TODO sequence equals attribute
	public required ReadOnlyList<string> Options { get; init; }

	// Todo validate the initial value
	public required Optional<string> InitialValue { get; init; }

	public required bool RequiresValue { get; init; }

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