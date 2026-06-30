using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UtilitiesLibrary.Optional;
using UtilitiesLibrary.Results;

namespace UtilitiesLibrary.Collections;



public static class CollectionExtensions {

	public static List<T> Listify<T>(this T item) {

		return [item];
	}

	public static ReadOnlyList<T> ToReadOnly<T>(this IEnumerable<T> enumerable) {

		return new(enumerable);
	}
	
	public static void AddIfNotNull<T>(this List<T> list, T? newValue) {

		if (newValue is null) {
			return;
		}

		list.Add(newValue);
	}

	public static void AddIfHasValue<T>(this List<T> list, Optional<T> newValue) {

		if (newValue.HasValue) {
			list.Add(newValue.Value);
		}
	}

	public static void AddValueIfIsSuccess<T>(this List<T> list, IOldResult<T> oldResult) {

		if (oldResult is IOldResult<T>.OldSuccess success) {
			list.Add(success.Value);
		}
	}

	public static IEnumerable<T> AppendIfNotNull<T>(this IEnumerable<T> enumerable, T? newValue) {

		return newValue is null ? enumerable : enumerable.Append(newValue);
	}

	public static IEnumerable<TTarget?> SelectIfNotNull<TCollection, TTarget>
		(this IEnumerable<TCollection> enumerable, Func<TCollection, TTarget?> selector) {

		return enumerable.Select(selector).Where(x => x is not null);
	}

	// todo rename selector to transformer?
	public static IEnumerable<TTarget> SelectIfHasValue<TCollection, TTarget>
		(this IEnumerable<TCollection> enumerable, Func<TCollection, Optional<TTarget>> selector) {

		return enumerable.Where(x => selector(x).HasValue).Select(x => selector(x).Value);

		//return enumerable.Select(selector).Where(x => x.HasValue).Select(x => x.Value);
	}

	public static void AddIfUnique<T>(this List<T> list, T item) {
		if (!list.Contains(item)) {
			list.Add(item);
		}
	}

	public static void AddUniqueItems<T>(this List<T> list, IEnumerable<T> newItems) {

		foreach (T item in newItems) {
			list.AddIfUnique(item);
		}
	}



	public static ReadOnlyList<T> ReadOnlyListify<T>(this T item) {

		return new List<T> { item }.ToReadOnly();
	}



	public static void Foreach<T>(this IEnumerable<T> enumerable, Action<T> predicate) {

		foreach (T item in enumerable) {
			predicate(item);
		}
	}



	public static bool IsEmpty<T>(this IEnumerable<T> enumerable) {

		return !enumerable.Any();
	}

	public static bool None<T>(this IEnumerable<T> enumerable, T value) where T : IComparable {

		return enumerable.Count(x => x.CompareTo(value) == 0) == 0;
	}

	public static bool None<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate) {

		return !enumerable.Any(predicate);
	}

	public static bool OnlyOne<T>(this IEnumerable<T> enumerable) {

		return enumerable.Count() == 1;
	}

	public static bool OnlyOne<T>(this IEnumerable<T> enumerable, T value) where T : IComparable {

		return enumerable.Count(x => x.CompareTo(value) == 0) == 1;
	}

	public static bool OnlyOne<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate) {

		return enumerable.Count(predicate) == 1;
	}

	public static bool Multiple<T>(this IEnumerable<T> enumerable) {

		return enumerable.Count() > 1;
	}

	public static bool Multiple<T>(this IEnumerable<T> enumerable, T value) where T : IComparable {

		return enumerable.Count(x => x.CompareTo(value) == 0) > 1;
	}

	public static bool Multiple<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate) {

		return enumerable.Count(predicate) > 1;
	}



	public static IEnumerable<(T first, T second)> Pair<T>(this IEnumerable<T> enumerable1, IEnumerable<T> enumerable2) {

		T[] array1 = enumerable1 as T[] ?? enumerable1.ToArray();
		T[] array2 = enumerable2 as T[] ?? enumerable2.ToArray();

		if (array1.Length != array2.Length) {
			throw new InvalidOperationException();
		}

		return array1.Zip(array2);
	}



	public static string CharArrayToString(this IEnumerable<char> enumerable) {

		return enumerable.Aggregate("", (current, character) => current + character);
	}



	public static ReadOnlyKeysDictionary<TKey, TValue> ToReadOnlyKeysDictionary<TKey, TValue>(
		this IEnumerable<TKey> keys, TValue defaultValue) where TKey : notnull {

		return new ReadOnlyKeysDictionary<TKey, TValue>(keys, defaultValue);
	}



	public static void PruneToUnion<T>(this List<T> list, IEnumerable<T> other) {

		T[] uniqueValuesInList = list.Where(item => !other.Contains(item)).ToArray();

		foreach (T item in uniqueValuesInList) {
			list.Remove(item);
		}
	}

	public static void PruneEntriesFrom<T>(this List<T> list, IEnumerable<T> other) {

		T[] duplicates = list.Where(other.Contains).ToArray();

		foreach (T duplicate in duplicates) {
			list.Remove(duplicate);
		}
	}

	public static void PruneEntriesFrom<T1, T2>(this List<T1> list, IEnumerable<T2> other, Func<T1, T2, bool> predicate) {

		T1[] duplicates = list.Where(item => other.Any(x => predicate(item, x))).ToArray();

		foreach (T1 item in duplicates) {
			list.Remove(item);
		}
	}

	public static void PruneEntriesFrom<T1, T2>(this List<T1> list, IEnumerable<T2> other, Func<T2, T1> selector) {

		IEnumerable<T1> transformedOther = other.Select(selector).ToArray();

		T1[] duplicates = list.Where(transformedOther.Contains).ToArray();

		foreach (T1 item in duplicates) {
			list.Remove(item);
		}
	}

	public static void PruneToUnionAndDispose<T>(this List<T> list, IEnumerable<T> other) where T : IDisposable {

		T[] uniqueValuesInList = list.Where(item => !other.Contains(item)).ToArray();

		foreach (T item in uniqueValuesInList) {
			list.RemoveAndDispose(item);
		}
	}

	public static bool RemoveAndDispose<T>(this List<T> list, T item) where T : IDisposable {

		bool result = list.Remove(item);

		if (result) {
			item.Dispose();
		}

		return result;
	}



	public static bool ContainsDuplicates<T>(this IEnumerable<T> enumerable) {
		throw new NotImplementedException();
	}

	public static List<T> Duplicates<T>(this IEnumerable<T> enumerable) {

		List<T> duplicates = [];
		T[] array = enumerable as T[] ?? enumerable.ToArray();

		for (int i = 0; i < array.Length; i++) {

			if (duplicates.Contains(array.ElementAt(i))) {
				continue;
			}

			for (int j = i + 1; j < array.Length; j++) {

				if (array[j]!.Equals(array[i])) {
					duplicates.Add(array[i]);
					break;
				}
			}
		}

		return duplicates;
	}



	public static string StringJoinCustom<T>(this IEnumerable<T> enumerable, string separator, string lastSeparator, string onlyTwoSeparator) {
		
		T[] array = enumerable as T[] ?? enumerable.ToArray();

		switch (array.Length) {
			case 0:
				return "";
			case 1:
				return array[0]!.ToString()!;
			case 2:
				return $"{array[0]}{onlyTwoSeparator}{array[1]}";
		}

		StringBuilder stringBuilder = new();

		for (int i = 0; i < array.Length - 1; i++) {

			stringBuilder.Append(array[i]);
			stringBuilder.Append(separator);
		}

		stringBuilder.Append(lastSeparator);
		stringBuilder.Append(array[^1]);

		return stringBuilder.ToString();
	}

}