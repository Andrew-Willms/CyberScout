using OneOf;

namespace UtilitiesLibrary.Results;



// TODO: make my own source generator that can handle classes with the same name but different numbers of type parameters
// Also the GenerateOneOf attribute does not work transitively through ErrorUnion

[GenerateOneOf]
public partial class ErrorUnion1<TError1>
	: OneOfBase<TError1>
	where TError1 : Error {

	// TODO this shouldn't be needed because ErrorUnion inherits from Error
	public static implicit operator Error(
		ErrorUnion1<TError1> error) {

		return error.Match<Error>(
			error1 => error1);
	}

}

[GenerateOneOf]
public partial class ErrorUnion2<TError1, TError2>
	: OneOfBase<TError1, TError2>
	where TError1 : Error
	where TError2 : Error {

	public static implicit operator Error(
		ErrorUnion2<TError1, TError2> error) {

		return error.Match<Error>(
			error1 => error1,
			error2 => error2);
	}

}

[GenerateOneOf]
public partial class ErrorUnion3<TError1, TError2, TError3>
	: OneOfBase<TError1, TError2, TError3>
	where TError1 : Error
	where TError2 : Error
	where TError3 : Error {

	public static implicit operator Error(
		ErrorUnion3<TError1, TError2, TError3> error) {

		return error.Match<Error>(
			error1 => error1,
			error2 => error2,
			error3 => error3);
	}

}

[GenerateOneOf]
public partial class ErrorUnion4<TError1, TError2, TError3, TError4>
	: OneOfBase<TError1, TError2, TError3, TError4>
	where TError1 : Error
	where TError2 : Error
	where TError3 : Error 
	where TError4 : Error {

	public static implicit operator Error(
		ErrorUnion4<TError1, TError2, TError3, TError4> error) {

		return error.Match<Error>(
			error1 => error1,
			error2 => error2,
			error3 => error3,
			error4 => error4);
	}

}