using System.Collections;
using System.Collections.Immutable;

namespace Purview.Telemetry.SourceGenerator.Records;

public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
	where T : IEquatable<T>
{
	readonly ImmutableArray<T> _array;

	public EquatableArray(ImmutableArray<T> array) => _array = array;

	public int Length => _array.IsDefault ? 0 : _array.Length;

	public bool IsEmpty => Length == 0;

	public T this[int index] => _array[index];

	public bool Equals(EquatableArray<T> other)
	{
		if (_array.IsDefault && other._array.IsDefault)
			return true;
		if (_array.IsDefault || other._array.IsDefault)
			return false;
		if (_array.Length != other._array.Length)
			return false;

		for (var i = 0; i < _array.Length; i++)
		{
			if (!_array[i].Equals(other._array[i]))
				return false;
		}

		return true;
	}

	public override bool Equals(object? obj) =>
		obj is EquatableArray<T> other && Equals(other);

	public override int GetHashCode()
	{
		if (_array.IsDefault)
			return 0;

		var hash = 0;
		foreach (var item in _array)
		{
			unchecked
			{
				hash = (hash * 397) ^ item.GetHashCode();
			}
		}

		return hash;
	}

	public ImmutableArray<T> AsImmutableArray() => _array.IsDefault ? [] : _array;

	public IEnumerator<T> GetEnumerator() =>
		((IEnumerable<T>)AsImmutableArray()).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) =>
		left.Equals(right);

	public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) =>
		!left.Equals(right);

	public static implicit operator EquatableArray<T>(ImmutableArray<T> array) => new(array);

	public static implicit operator ImmutableArray<T>(EquatableArray<T> array) =>
		array.AsImmutableArray();
}
