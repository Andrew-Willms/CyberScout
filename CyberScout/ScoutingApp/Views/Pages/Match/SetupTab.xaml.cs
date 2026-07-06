using System;
using System.Linq;
using Domain.Data;
using Domain.DataCollectors;
using Domain.MatchData;
using Microsoft.Maui.Controls;
using ScoutingApp.AppManagement;
using UtilitiesLibrary.Collections;
using UtilitiesLibrary.Optional;

namespace ScoutingApp.Views.Pages.Match; 



public partial class SetupTab : ContentPage {

	public static string Route => "Setup";

	private AppManager AppManager { get; }

	public uint? MatchNumber {

		get => AppManager.ActiveMatchData.MatchNumber.HasValue
			? AppManager.ActiveMatchData.MatchNumber.Value
			: null;

		set => AppManager.ActiveMatchData.MatchNumber = value is not null
			? ((uint)value).Optionalize()
			: Optional.NoValue;
	}

	public uint? ReplayNumber {

		get => AppManager.ActiveMatchData.ReplayNumber.HasValue
			? AppManager.ActiveMatchData.ReplayNumber.Value
			: null;

		set => AppManager.ActiveMatchData.ReplayNumber = value is not null
			? ((uint)value).Optionalize()
			: Optional.NoValue;
	}

	public MatchType[] MatchTypes { get; } = Enum.GetValues<MatchType>();

	public MatchType? MatchType {
		get => AppManager.ActiveMatchData.MatchType.HasValue
			? AppManager.ActiveMatchData.MatchType.Value
			: null;

		set => AppManager.ActiveMatchData.MatchType = value is not null
				? ((MatchType) value).Optionalize()
				: Optional.NoValue;
	}

	public uint? TeamNumber {

		get => AppManager.ActiveMatchData.TeamNumber.HasValue
			? AppManager.ActiveMatchData.TeamNumber.Value
			: null;

		set => AppManager.ActiveMatchData.TeamNumber = value is not null
			? ((uint)value).Optionalize()
			: Optional.NoValue;
	}

	public int Alliance {
		get => AppManager.ActiveMatchData.Alliance is { HasValue: true, Value: var value } && value < Alliances.Count
			? (int)value
			: -1;

		set => AppManager.ActiveMatchData.Alliance = value is not -1 && Alliances.Count > value
			? ((uint)value).Optionalize() 
			: Optional.NoValue;
	}

	public ReadOnlyList<string> Alliances => AppManager.ActiveMatchData.GameSpecification.Alliances.Select(x => x.Name).ToReadOnly();

	public ReadOnlyList<InputDataCollector> Inputs => AppManager.ActiveMatchData.SetupTabInputs;

	public SetupTab(AppManagerGetter appManagerGetter) {

		// TODO: add graceful error handling
		AppManager = appManagerGetter.Instance ?? throw new AppManagerNotCreatedException(typeof(SetupTab));

		// ReSharper disable once ExplicitCallerInfoArgument - refresh all
		AppManager.OnMatchStarted.Subscribe(() => OnPropertyChanged(""));

		InitializeComponent();
		BindingContext = this;
	}

}