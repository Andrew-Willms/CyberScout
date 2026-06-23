using OneOf;

namespace Database.Results;



[GenerateOneOf]
public partial class DataStoreResult<TSuccess> : OneOfBase<TSuccess, DataStoreError>;