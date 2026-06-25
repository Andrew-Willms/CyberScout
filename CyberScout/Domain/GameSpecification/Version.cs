using System.Diagnostics.CodeAnalysis;

namespace Domain.GameSpecification;



public record struct Version {

	[SetsRequiredMembers]
	public Version(uint major, uint minor, uint patch, string description = "") {
		MajorNumber = major;
		MinorNumber = minor;
		PatchNumber = patch;
		Description = description;
	}

	public required uint MajorNumber { get; init; }

	public required uint MinorNumber { get; init; }

	public required uint PatchNumber { get; init; }

	public string Description { get; init; } = "";

}