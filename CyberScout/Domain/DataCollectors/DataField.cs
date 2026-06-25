using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Domain.GameSpecification;
using UtilitiesLibrary.Optional;
using Event = UtilitiesLibrary.SimpleEvent.Event;

namespace Domain.DataCollectors;



public abstract class DataField : INotifyPropertyChanged {

	public DataFieldSpec Specification { get; }

	public string Name => Specification.Name;

	public abstract object BaseValue { get; }

	public readonly Event OnValueChange = new();

	public abstract List<string> Errors { get; }

	protected DataField(DataFieldSpec specification) {
		Specification = specification;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged(string propertyName) {
		PropertyChanged?.Invoke(this, new(propertyName));
	}

}



public class BooleanDataField : DataField {

	public new BooleanDataFieldSpec Specification { get; }

	public bool Value {
		get;
		set {
			field = value;
			OnValueChange.Invoke();
			OnPropertyChanged(nameof(Value));
			OnPropertyChanged(nameof(Errors));
		}
	}

	public override object BaseValue => Value;

	public override List<string> Errors => [];

	public BooleanDataField(BooleanDataFieldSpec specification) : base(specification) {
		Value = specification.InitialValue;
		Specification = specification;
	}

}



public class TextDataField : DataField {

	public new TextDataFieldSpec Specification { get; }

	public string Value {
		get;
		set {
			field = value;
			OnValueChange.Invoke();
			OnPropertyChanged(nameof(Value));
			OnPropertyChanged(nameof(Errors));
		}
	}

	public override object BaseValue => Value;

	public override List<string> Errors {
		get {
			List<string> errors = [];

			if (Value == string.Empty &&
			    (Specification.MustNotBeEmpty ||
			     Specification is { MustNotBeInitialValue: true, InitialValue: "" })) {

				errors.Add($"The data field \"{Name}\" is empty.");
			}

			if (Specification.MustNotBeInitialValue && Value == Specification.InitialValue) {

				errors.Add($"The data field \"{Name}\" must not be the default value {Specification.InitialValue}.");
			}

			return errors;
		}
	}

	public TextDataField(TextDataFieldSpec specification) : base(specification) {
		Value = specification.InitialValue;
		Specification = specification;
	}

}



public class IntegerDataField : DataField {

	public new IntegerDataFieldSpec Specification { get; }

	public int Value {
		get;
		set {
			field = value;
			OnValueChange.Invoke();
			OnPropertyChanged(nameof(Value));
			OnPropertyChanged(nameof(Errors));
		}
	}

	public override object BaseValue => Value;

	public override List<string> Errors {
		get {
			List<string> errors = [];

			if (Value > Specification.MaxValue) {
				errors.Add($"The data field \"{Name}\" ist set to {Value} which is greater than it's maximum value of {Specification.MaxValue}.");
			}

			if (Value < Specification.MinValue) {
				errors.Add($"The data field \"{Name}\" ist set to {Value} which is less than it's minimum value of {Specification.MinValue}.");
			}

			return errors;
		}
	}

	public IntegerDataField(IntegerDataFieldSpec specification) : base(specification) {
		Value = specification.InitialValue;
		Specification = specification;
	}

}



public class SelectionDataField : DataField {

	public new SelectionDataFieldSpec Specification { get; }

	// TODO rework this to use an integer index instead???
	public Optional<string> Value {
		get;
		set {
			field = value;
			OnValueChange.Invoke();
			OnPropertyChanged(nameof(Value));
			OnPropertyChanged(nameof(Errors));
		}
	}

	public override object BaseValue => Value;

	public override List<string> Errors {
		get {
			if (Specification.RequiresValue && (Value == Optional.NoValue || Value.Value == string.Empty)) {
				return [ $"The data field \"{Name}\" requires a value." ];
			}

			// TODO: I have to add these extra cases because somehow the value gets set to an empty string, maybe look into seeing
			// if I can force the not set value to be Optional.NoValue
			if (Value != Optional.NoValue && Value.Value != string.Empty && !Specification.Options.Contains(Value.Value)) {
				return [$"The data field \"{Name}\" does not contain the specified value \"{Value.Value}\""];
			}

			return [];
		}
	}

	public SelectionDataField(SelectionDataFieldSpec specification) : base(specification) {
		Value = specification.InitialValue;
		Specification = specification;
	}

}