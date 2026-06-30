using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UtilitiesLibrary.Results;

namespace UtilitiesLibrary.Collections;



// TODO find a better or more specific name for this. Possibly involving "IndirectAdd"
public class ObservableList<TItem, TAdd> : INotifyCollectionChanged, IEnumerable<TItem> {

	private readonly List<TItem> Collection = new();
	public TItem this[int index] => Collection[index];

	public Action<TItem>? OnAdd { private get; init; }
	public Action<TItem>? OnRemove { private get; init; }

	public required Func<TAdd, TItem> Adder { private get; init; }



	public void Add(TAdd intermediateItem) {

		TItem newItem = Adder.Invoke(intermediateItem);

		Collection.Add(newItem);
		CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, newItem));

		OnAdd?.Invoke(newItem);
	}

	public IListRemoveOldResult<TItem> Remove(TItem toRemove) {

		if (!Collection.Contains(toRemove)) {
			return new IListRemoveOldResult<TItem>.ItemNotFound();
		}

		int index = Collection.IndexOf(toRemove);
		if (!Collection.Remove(toRemove)) {
			return new IListRemoveOldResult<TItem>.OtherFailure();
		}

		OnRemove?.Invoke(toRemove);
		CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Remove, toRemove, index));
		return new IListRemoveOldResult<TItem>.OldSuccess();
	}



	public IEnumerator<TItem> GetEnumerator() {
		return Collection.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}



	public event NotifyCollectionChangedEventHandler? CollectionChanged;

}



public interface IListRemoveOldResult : IOldResult;

// TODO: Move to its own file
public interface IListRemoveOldResult<T> : IListRemoveOldResult {

	public class OldSuccess : IOldResult.OldSuccess, IListRemoveOldResult<T> { }

	public class ItemNotFound : OldError, IListRemoveOldResult<T> { }

	public class OtherFailure : OldError, IListRemoveOldResult<T> { }

}