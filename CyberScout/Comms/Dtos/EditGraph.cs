using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OneOf;
using UtilitiesLibrary.Collections;
using Willmsy.AsyncTryResult;

namespace Comms.Dtos;



public record EditGraph {

	public required string OriginalDeviceId { get; init; }

	public required long OriginalMatchId { get; init; }

	public MatchDataDto? OriginalMatch { get; init; }

	public required ReadOnlyList<SubGraph> Graphs { get; init; }



	private EditGraph() { }

	public static CreateEditGraphResult Create(List<MatchDataDto> matchDataDtos) {

		if (matchDataDtos.Count == 0) {
			return new EmptyMatchesListError();
		}

		List<MatchDataDto> duplicateMatches = matchDataDtos.Duplicates();
		if (duplicateMatches.Count != 0) {
			return new DuplicateMatchError { Duplicate = duplicateMatches };
		}

		MatchDataDto? originalMatch = null;
		string originalDeviceId = matchDataDtos[0].OriginalDeviceId;
		long originalMatchId = matchDataDtos[0].OriginalMatchId;

		if (matchDataDtos.Any(x => x.OriginalDeviceId != originalDeviceId || x.OriginalMatchId != originalMatchId)) {
			return new MismatchedOriginalMatchError();
		}

		List <MatchDataDto> unplacedNodes = matchDataDtos.ToList();
		List<SubGraph> graphs = [];

		while (unplacedNodes.Count > 0) {

			SubGraph subGraph = new();

			// Iterate backwards because it's easier to remove from the end of a list.
			int i = unplacedNodes.Count - 1;
			while (i >= 0) {

				MatchDataDto nodeToPlace = unplacedNodes[i];

				// If the node doesn't belong in the graph keep looking through the nodes.
				if (!subGraph.TryAdd(nodeToPlace, out SubGraph? newGraph)) {
					i--;
					continue;
				}

				if (nodeToPlace.DeviceId == originalDeviceId && nodeToPlace.MatchId == originalMatchId) {

					if (originalMatch is not null) {
						return new MultipleOriginalMatchesError { First = originalMatch, Second = nodeToPlace };
					}

					originalMatch = nodeToPlace;
				}

				// If the node does belong in the graph:
				subGraph = newGraph; // update the graph,
				unplacedNodes.RemoveAt(i); // remove the node from the list of nodes to place, and
				i = unplacedNodes.Count - 1; // reset the index to zero because we need to check every unplaced node against the updated graph.
			}

			graphs.Add(subGraph);
		}

		return new EditGraph {
			OriginalDeviceId = originalDeviceId,
			OriginalMatchId = originalMatchId,
			OriginalMatch = originalMatch,
			Graphs = graphs.ToReadOnly()
		};
	}

}



public record SubGraph {

	public ReadOnlyList<MatchDataDto> Nodes { get; init; }

	private ReadOnlyDictionary<MatchDataDto, MatchDataDto> Parents;

	private ReadOnlyDictionary<MatchDataDto, MatchDataDto> Children;



	public SubGraph() {
		Nodes = ReadOnlyList.Empty;
		Parents = ReadOnlyDictionary<MatchDataDto, MatchDataDto>.Empty;
		Children = ReadOnlyDictionary<MatchDataDto, MatchDataDto>.Empty;
	}

	public SubGraph(MatchDataDto node) {
		Nodes = node.ReadOnlyListify();
		Parents = ReadOnlyDictionary<MatchDataDto, MatchDataDto>.Empty;
		Children = ReadOnlyDictionary<MatchDataDto, MatchDataDto>.Empty;
	}

	public bool TryAdd(MatchDataDto node, [NotNullWhen(true)] out SubGraph? newGraph) {
		throw new NotImplementedException();
	}

}






public record CreateEditGraphResult : AsyncTryResult<EditGraph, CreateEditGraphError> {

	public CreateEditGraphResult(EditGraph value) : base(value) { }

	public CreateEditGraphResult(CreateEditGraphError error) : base(error) { }

	public static implicit operator CreateEditGraphResult(EditGraph value) {
		return new(value);
	}

	public static implicit operator CreateEditGraphResult(CreateEditGraphError error) {
		return new(error);
	}

	public static implicit operator CreateEditGraphResult(EmptyMatchesListError error) {
		return new(error);
	}

	public static implicit operator CreateEditGraphResult(DuplicateMatchError error) {
		return new(error);
	}

	public static implicit operator CreateEditGraphResult(MismatchedOriginalMatchError error) {
		return new(error);
	}

	public static implicit operator CreateEditGraphResult(MultipleOriginalMatchesError error) {
		return new(error);
	}

}

[GenerateOneOf]
public partial class CreateEditGraphError : OneOfBase<
	EmptyMatchesListError,
	DuplicateMatchError,
	MismatchedOriginalMatchError,
	MultipleOriginalMatchesError
>;


public record EmptyMatchesListError;

public record DuplicateMatchError {

	public required List<MatchDataDto> Duplicate { get; init; }

}

public record MismatchedOriginalMatchError;

public record MultipleOriginalMatchesError {

	public required MatchDataDto First { get; init; }

	public required MatchDataDto Second { get; init; }

}