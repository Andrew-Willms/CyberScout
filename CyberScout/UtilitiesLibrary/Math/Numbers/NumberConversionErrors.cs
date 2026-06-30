using System.Numerics;
using UtilitiesLibrary.Results;

namespace UtilitiesLibrary.Math.Numbers;



public class InvalidCharactersOldError : OldError { }

public class ArgumentNullOldError : OldError { }

public class ValueTooLargeOldError : OldError { }

public class ValueTooSmallOldError : OldError { }

public class ValueIsNotWholeNumberOldError : OldError { }

public class ValueIsNotPositiveOldError : OldError { }



public interface IIntegerToPrimitiveOldResult<T> : IOldResult<T> where T : INumber<T> {

	public class OldSuccess : IOldResult<T>.OldSuccess, IIntegerToPrimitiveOldResult<T> { }

	public class ValueBelowMin : OldError, IIntegerToPrimitiveOldResult<T> { }

	public class ValueAboveMax : OldError, IIntegerToPrimitiveOldResult<T> { }

}

public interface INumberToPrimitiveOldResult<T> : IOldResult<T> where T : INumber<T> {

	public class OldSuccess : IOldResult<T>.OldSuccess, INumberToPrimitiveOldResult<T> { }

	public class ValueBelowMin : OldError, INumberToPrimitiveOldResult<T> { }

	public class ValueAboveMax : OldError, INumberToPrimitiveOldResult<T> { }

	//public class DecimalsCannotBeRepresented : IResult<T>.Error, IIntegerToPrimitiveResult<T> { }

}