using System.Diagnostics;
using Microsoft.Maui.Controls;
using BooleanInputDataCollector = ScoutingApp.DataCollectors.BooleanInputDataCollector;
using IntegerInputDataCollector = ScoutingApp.DataCollectors.IntegerInputDataCollector;
using SelectionInputDataCollector = ScoutingApp.DataCollectors.SelectionInputDataCollector;
using TextInputDataCollector = ScoutingApp.DataCollectors.TextInputDataCollector;

namespace ScoutingApp.Views.DataTemplateSelectors;



public class InputDataTemplateSelector : DataTemplateSelector {

	public DataTemplate BooleanDataFieldTemplate { get; set; } = null!;
	public DataTemplate TextDataFieldTemplate { get; set; } = null!;
	public DataTemplate IntegerDataFieldTemplate { get; set; } = null!;
	public DataTemplate MultiIntegerDataFieldTemplate { get; set; } = null!;
	public DataTemplate SelectionDataFieldTemplate { get; set; } = null!;

	protected override DataTemplate OnSelectTemplate(object item, BindableObject container) {

		return item switch {
			BooleanInputDataCollector => BooleanDataFieldTemplate,
			TextInputDataCollector => TextDataFieldTemplate,
			IntegerInputDataCollector => IntegerDataFieldTemplate,
			SelectionInputDataCollector => SelectionDataFieldTemplate,
			_ => throw new UnreachableException()
		};
	}

}



public class NullOrValueTemplateSelector : DataTemplateSelector {

	public required DataTemplate NullTemplate { get; init; } = null!;
	public required DataTemplate ValueTemplate { get; init; } = null!;

	protected override DataTemplate OnSelectTemplate(object? item, BindableObject container) {

		return item is null ? NullTemplate : ValueTemplate;
	}

}