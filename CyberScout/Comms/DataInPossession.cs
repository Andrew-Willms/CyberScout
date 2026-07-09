using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Comms;



public record DataInPossession {

	public ReadOnlyDictionary<string, List<IndexRange>> Test { get; init; }


}