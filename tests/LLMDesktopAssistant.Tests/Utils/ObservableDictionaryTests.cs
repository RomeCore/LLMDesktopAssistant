using System.Collections;
using System.Collections.Specialized;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Tests.Utils;

public class ObservableDictionaryTests
{
	private sealed class EventRecorder
	{
		public List<NotifyCollectionChangedEventArgs> Collection { get; } = [];
		public List<string> Properties { get; } = [];

		public void Attach(ObservableDictionary<string, int> dict)
		{
			dict.CollectionChanged += (_, e) => Collection.Add(e);
			dict.PropertyChanged += (_, e) => Properties.Add(e.PropertyName ?? "");
		}
	}

	private static KeyValuePair<string, int> Kv(string key, int value) => new(key, value);

	// ========== Constructors ==========

	[Fact]
	public void DefaultCtor_IsEmpty()
	{
		var dict = new ObservableDictionary<string, int>();

		Assert.Empty(dict);
		Assert.False(dict.Any);
	}

	[Fact]
	public void Ctor_FromDictionary_CopiesEntries()
	{
		var source = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
		var dict = new ObservableDictionary<string, int>(source);

		Assert.Equal(2, dict.Count);
		Assert.Equal(1, dict["a"]);
		Assert.Equal(2, dict["b"]);
	}

	[Fact]
	public void Ctor_FromEnumerable_CopiesEntries()
	{
		var source = new[] { Kv("a", 1), Kv("b", 2) };
		var dict = new ObservableDictionary<string, int>(source);

		Assert.Equal(2, dict.Count);
		Assert.Equal(1, dict["a"]);
	}

	[Fact]
	public void Ctor_FromDictionaryWithComparer_UsesComparer()
	{
		var source = new Dictionary<string, int> { ["a"] = 1 };
		var dict = new ObservableDictionary<string, int>(source, StringComparer.OrdinalIgnoreCase);

		Assert.Equal(1, dict["A"]);
	}

	[Fact]
	public void Ctor_FromEnumerableWithComparer_UsesComparer()
	{
		var source = new[] { Kv("a", 1) };
		var dict = new ObservableDictionary<string, int>(source, StringComparer.OrdinalIgnoreCase);

		Assert.Equal(1, dict["A"]);
	}

	[Fact]
	public void Ctor_WithComparer_ExposesComparer()
	{
		var dict = new ObservableDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		Assert.Equal(StringComparer.OrdinalIgnoreCase, dict.Comparer);
	}

	[Fact]
	public void Ctor_NullDictionary_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => new ObservableDictionary<string, int>((IDictionary<string, int>)null!));
	}

	[Fact]
	public void Ctor_NullEnumerable_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => new ObservableDictionary<string, int>((IEnumerable<KeyValuePair<string, int>>)null!));
	}

	[Fact]
	public void Ctor_DuplicateKeys_Throws()
	{
		var source = new[] { Kv("a", 1), Kv("a", 2) };

		Assert.Throws<ArgumentException>(() => new ObservableDictionary<string, int>(source));
	}

	// ========== Indexer ==========

	[Fact]
	public void Indexer_Get_ReturnsValue()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 42 });

		Assert.Equal(42, dict["a"]);
	}

	[Fact]
	public void Indexer_Get_MissingKey_Throws()
	{
		var dict = new ObservableDictionary<string, int>();

		Assert.Throws<KeyNotFoundException>(() => dict["missing"]);
	}

	[Fact]
	public void Indexer_Set_NewKey_RaisesAdd()
	{
		var dict = new ObservableDictionary<string, int>();
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict["a"] = 1;

		Assert.Single(dict);
		Assert.True(dict.ContainsKey("a"));
		var e = Assert.Single(recorder.Collection);
		Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
		var pair = Assert.IsType<KeyValuePair<string, int>>(e.NewItems![0]);
		Assert.Equal("a", pair.Key);
		Assert.Equal(1, pair.Value);
	}

	[Fact]
	public void Indexer_Set_ExistingKey_RaisesReplace()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict["a"] = 2;

		Assert.Single(dict);
		var e = Assert.Single(recorder.Collection);
		Assert.Equal(NotifyCollectionChangedAction.Replace, e.Action);
		var newPair = Assert.IsType<KeyValuePair<string, int>>(e.NewItems![0]);
		var oldPair = Assert.IsType<KeyValuePair<string, int>>(e.OldItems![0]);
		Assert.Equal(2, newPair.Value);
		Assert.Equal(1, oldPair.Value);
	}

	[Fact]
	public void Indexer_NonGeneric_Works()
	{
		IDictionary dict = new ObservableDictionary<string, int>();

		dict["a"] = 5;
		Assert.Equal(5, dict["a"]);

		dict["a"] = 6;
		Assert.Equal(6, dict["a"]);
	}

	// ========== Add ==========

	[Fact]
	public void Add_NewKey_AddsAndRaisesEvent()
	{
		var dict = new ObservableDictionary<string, int>();
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict.Add("a", 1);

		Assert.Single(dict);
		Assert.True(dict.ContainsKey("a"));
		var e = Assert.Single(recorder.Collection);
		Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
		Assert.Equal("a", Assert.IsType<KeyValuePair<string, int>>(e.NewItems![0]).Key);
	}

	[Fact]
	public void Add_DuplicateKey_Throws()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

		Assert.Throws<ArgumentException>(() => dict.Add("a", 2));
	}

	[Fact]
	public void Add_KeyValuePair_Adds()
	{
		var dict = new ObservableDictionary<string, int>();

		dict.Add(Kv("a", 1));

		Assert.Equal(1, dict["a"]);
	}

	[Fact]
	public void Add_NonGeneric_Adds()
	{
		IDictionary dict = new ObservableDictionary<string, int>();

		dict.Add("a", 1);

		Assert.Single(dict);
		Assert.Equal(1, ((ObservableDictionary<string, int>)dict)["a"]);
	}

	[Fact]
	public void TryAdd_NewKey_ReturnsTrueAndRaisesEvent()
	{
		var dict = new ObservableDictionary<string, int>();
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		var result = dict.TryAdd("a", 1);

		Assert.True(result);
		Assert.Single(dict);
		Assert.Single(recorder.Collection);
	}

	[Fact]
	public void TryAdd_ExistingKey_ReturnsFalseWithoutEvent()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		var result = dict.TryAdd("a", 2);

		Assert.False(result);
		Assert.Equal(1, dict["a"]);
		Assert.Empty(recorder.Collection);
	}

	// ========== AddRange ==========

	[Fact]
	public void AddRange_Empty_DoesNotRaiseEvents()
	{
		var dict = new ObservableDictionary<string, int>();
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict.AddRange([]);

		Assert.Empty(recorder.Collection);
	}

	[Fact]
	public void AddRange_AddsAllAndRaisesSingleEvent()
	{
		var dict = new ObservableDictionary<string, int>();
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict.AddRange(new[] { Kv("a", 1), Kv("b", 2), Kv("c", 3) });

		Assert.Equal(3, dict.Count);
		var e = Assert.Single(recorder.Collection);
		Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
		Assert.Equal(3, e.NewItems!.Count);
	}

	[Fact]
	public void AddRange_PreferReset_RaisesReset()
	{
		var dict = new ObservableDictionary<string, int> { PreferResetForRangeOperations = true };
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict.AddRange(new[] { Kv("a", 1), Kv("b", 2) });

		var e = Assert.Single(recorder.Collection);
		Assert.Equal(NotifyCollectionChangedAction.Reset, e.Action);
	}

	[Fact]
	public void AddRange_DuplicateInside_ThrowsAtomically()
	{
		var dict = new ObservableDictionary<string, int>();
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		Assert.Throws<ArgumentException>(() => dict.AddRange(new[] { Kv("a", 1), Kv("a", 2) }));

		Assert.Empty(dict);
		Assert.Empty(recorder.Collection);
	}

	[Fact]
	public void AddRange_ExistingKey_ThrowsAtomically()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["dup"] = 1 });
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		Assert.Throws<ArgumentException>(() => dict.AddRange(new[] { Kv("x", 1), Kv("dup", 2) }));

		Assert.Single(dict);
		Assert.False(dict.ContainsKey("x"));
		Assert.Empty(recorder.Collection);
	}

	[Fact]
	public void AddRange_Null_Throws()
	{
		var dict = new ObservableDictionary<string, int>();

		Assert.Throws<ArgumentNullException>(() => dict.AddRange(null!));
	}

	// ========== Remove ==========

	[Fact]
	public void Remove_ExistingKey_RemovesAndRaisesEvent()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		var result = dict.Remove("a");

		Assert.True(result);
		Assert.Empty(dict);
		Assert.False(dict.ContainsKey("a"));
		var e = Assert.Single(recorder.Collection);
		Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);
		var pair = Assert.IsType<KeyValuePair<string, int>>(e.OldItems![0]);
		Assert.Equal("a", pair.Key);
		Assert.Equal(1, pair.Value);
	}

	[Fact]
	public void Remove_MissingKey_ReturnsFalseWithoutEvent()
	{
		var dict = new ObservableDictionary<string, int>();
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		var result = dict.Remove("missing");

		Assert.False(result);
		Assert.Empty(recorder.Collection);
	}

	[Fact]
	public void Remove_WithOutValue_ReturnsRemovedValue()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 42 });

		var result = dict.Remove("a", out var value);

		Assert.True(result);
		Assert.Equal(42, value);
		Assert.Empty(dict);
	}

	[Fact]
	public void Remove_WithOutValue_MissingKey_ReturnsFalse()
	{
		var dict = new ObservableDictionary<string, int>();

		var result = dict.Remove("missing", out var value);

		Assert.False(result);
		Assert.Equal(0, value);
	}

	[Fact]
	public void Remove_KeyValuePair_Matching_Removes()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

		var result = dict.Remove(Kv("a", 1));

		Assert.True(result);
		Assert.Empty(dict);
	}

	[Fact]
	public void Remove_KeyValuePair_WrongValue_DoesNotRemove()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		var result = dict.Remove(Kv("a", 999));

		Assert.False(result);
		Assert.Single(dict);
		Assert.Empty(recorder.Collection);
	}

	[Fact]
	public void Remove_KeyValuePair_MissingKey_ReturnsFalse()
	{
		var dict = new ObservableDictionary<string, int>();

		Assert.False(dict.Remove(Kv("missing", 1)));
	}

	[Fact]
	public void Remove_NonGeneric_Removes()
	{
		IDictionary dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

		dict.Remove("a");

		Assert.Empty(dict);
	}

	[Fact]
	public void Remove_NonGeneric_WrongType_IsNoOp()
	{
		IDictionary dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

		dict.Remove(123);

		Assert.Single(dict);
	}

	[Fact]
	public void RemoveRange_RemovesFoundAndRaisesSingleEvent()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2, ["c"] = 3 });
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict.RemoveRange(new[] { "a", "c", "missing" });

		Assert.Single(dict);
		Assert.True(dict.ContainsKey("b"));
		var e = Assert.Single(recorder.Collection);
		Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);
		Assert.Equal(2, e.OldItems!.Count);
	}

	[Fact]
	public void RemoveRange_PreferReset_RaisesReset()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 })
		{
			PreferResetForRangeOperations = true
		};
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict.RemoveRange(new[] { "a", "b" });

		var e = Assert.Single(recorder.Collection);
		Assert.Equal(NotifyCollectionChangedAction.Reset, e.Action);
	}

	[Fact]
	public void RemoveRange_Empty_DoesNotRaiseEvents()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict.RemoveRange([]);

		Assert.Single(dict);
		Assert.Empty(recorder.Collection);
	}

	[Fact]
	public void RemoveRange_Null_Throws()
	{
		var dict = new ObservableDictionary<string, int>();

		Assert.Throws<ArgumentNullException>(() => dict.RemoveRange(null!));
	}

	// ========== GetOrAdd ==========

	[Fact]
	public void GetOrAdd_ExistingKey_ReturnsValueWithoutEvent()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		var result = dict.GetOrAdd("a", 999);

		Assert.Equal(1, result);
		Assert.Empty(recorder.Collection);
	}

	[Fact]
	public void GetOrAdd_NewKey_AddsAndRaisesEvent()
	{
		var dict = new ObservableDictionary<string, int>();
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		var result = dict.GetOrAdd("a", 42);

		Assert.Equal(42, result);
		Assert.Equal(42, dict["a"]);
		Assert.Single(recorder.Collection);
	}

	[Fact]
	public void GetOrAdd_WithFactory_ExistingKey_DoesNotCallFactory()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });
		var factoryCalled = false;

		var result = dict.GetOrAdd("a", _ =>
		{
			factoryCalled = true;
			return 999;
		});

		Assert.Equal(1, result);
		Assert.False(factoryCalled);
	}

	[Fact]
	public void GetOrAdd_WithFactory_NewKey_CallsFactoryAndRaisesEvent()
	{
		var dict = new ObservableDictionary<string, int>();
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		var result = dict.GetOrAdd("a", key => key.Length);

		Assert.Equal(1, result);
		Assert.Equal(1, dict["a"]);
		Assert.Single(recorder.Collection);
	}

	[Fact]
	public void GetOrAdd_NullFactory_Throws()
	{
		var dict = new ObservableDictionary<string, int>();

		Assert.Throws<ArgumentNullException>(() => dict.GetOrAdd("a", null!));
	}

	// ========== AddOrUpdate ==========

	[Fact]
	public void AddOrUpdate_NewKey_AddsAndRaisesAdd()
	{
		var dict = new ObservableDictionary<string, int>();
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		var result = dict.AddOrUpdate("a", 10, (_, old) => old + 1);

		Assert.Equal(10, result);
		var e = Assert.Single(recorder.Collection);
		Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
	}

	[Fact]
	public void AddOrUpdate_ExistingKey_UpdatesAndRaisesReplace()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		var result = dict.AddOrUpdate("a", 10, (_, old) => old + 1);

		Assert.Equal(2, result);
		Assert.Equal(2, dict["a"]);
		var e = Assert.Single(recorder.Collection);
		Assert.Equal(NotifyCollectionChangedAction.Replace, e.Action);
		var oldPair = Assert.IsType<KeyValuePair<string, int>>(e.OldItems![0]);
		var newPair = Assert.IsType<KeyValuePair<string, int>>(e.NewItems![0]);
		Assert.Equal(1, oldPair.Value);
		Assert.Equal(2, newPair.Value);
	}

	[Fact]
	public void AddOrUpdate_WithAddFactory_NewKey_UsesFactory()
	{
		var dict = new ObservableDictionary<string, int>();

		var result = dict.AddOrUpdate("abc", key => key.Length, (_, old) => old);

		Assert.Equal(3, result);
	}

	[Fact]
	public void AddOrUpdate_WithAddFactory_ExistingKey_DoesNotCallAddFactory()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 5 });
		var addFactoryCalled = false;

		var result = dict.AddOrUpdate("a",
			key =>
			{
				addFactoryCalled = true;
				return 1;
			},
			(_, old) => old * 2);

		Assert.Equal(10, result);
		Assert.False(addFactoryCalled);
	}

	[Fact]
	public void AddOrUpdate_UpdateFactory_ReceivesKeyAndOldValue()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });
		string? receivedKey = null;
		int receivedOld = 0;

		dict.AddOrUpdate("a", 0, (key, old) =>
		{
			receivedKey = key;
			receivedOld = old;
			return old;
		});

		Assert.Equal("a", receivedKey);
		Assert.Equal(1, receivedOld);
	}

	[Fact]
	public void AddOrUpdate_NullUpdateFactory_Throws()
	{
		var dict = new ObservableDictionary<string, int>();

		Assert.Throws<ArgumentNullException>(() => dict.AddOrUpdate("a", 1, null!));
	}

	[Fact]
	public void AddOrUpdate_NullFactories_Throw()
	{
		var dict = new ObservableDictionary<string, int>();

		Assert.Throws<ArgumentNullException>(() => dict.AddOrUpdate("a", null!, (_, old) => old));
		Assert.Throws<ArgumentNullException>(() => dict.AddOrUpdate("a", key => key.Length, null!));
	}

	// ========== Lookups ==========

	[Fact]
	public void ContainsKey_ReturnsCorrectResult()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

		Assert.True(dict.ContainsKey("a"));
		Assert.False(dict.ContainsKey("missing"));
	}

	[Fact]
	public void ContainsKey_NonGeneric_Works()
	{
		IDictionary dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

		Assert.True(dict.Contains("a"));
		Assert.False(dict.Contains("missing"));
		Assert.False(dict.Contains(123));
	}

	[Fact]
	public void TryGetValue_ExistingKey_ReturnsTrueAndValue()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 42 });

		var result = dict.TryGetValue("a", out var value);

		Assert.True(result);
		Assert.Equal(42, value);
	}

	[Fact]
	public void TryGetValue_MissingKey_ReturnsFalseAndDefault()
	{
		var dict = new ObservableDictionary<string, int>();

		var result = dict.TryGetValue("missing", out var value);

		Assert.False(result);
		Assert.Equal(0, value);
	}

	[Fact]
	public void Contains_Pair_ChecksKeyAndValue()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

		Assert.Contains(Kv("a", 1), dict);
		Assert.DoesNotContain(Kv("a", 2), dict);
		Assert.DoesNotContain(Kv("missing", 1), dict);
	}

	// ========== Clear ==========

	[Fact]
	public void Clear_Empty_DoesNotRaiseEvents()
	{
		var dict = new ObservableDictionary<string, int>();
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict.Clear();

		Assert.Empty(recorder.Collection);
	}

	[Fact]
	public void Clear_NonEmpty_ClearsAndRaisesRemove()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict.Clear();

		Assert.Empty(dict);
		Assert.False(dict.Any);
		var e = Assert.Single(recorder.Collection);
		Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);
		Assert.Equal(2, e.OldItems!.Count);
	}

	[Fact]
	public void Clear_PreferReset_RaisesReset()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 })
		{
			PreferResetForRangeOperations = true
		};
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict.Clear();

		var e = Assert.Single(recorder.Collection);
		Assert.Equal(NotifyCollectionChangedAction.Reset, e.Action);
	}

	// ========== Reset ==========

	[Fact]
	public void Reset_WithItems_ReplacesContent()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["old"] = 1 });
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict.Reset(new[] { Kv("new1", 10), Kv("new2", 20) });

		Assert.Equal(2, dict.Count);
		Assert.False(dict.ContainsKey("old"));
		Assert.Equal(10, dict["new1"]);
		var e = Assert.Single(recorder.Collection);
		Assert.Equal(NotifyCollectionChangedAction.Replace, e.Action);
		Assert.Single(e.OldItems!);
		Assert.Equal(2, e.NewItems!.Count);
	}

	[Fact]
	public void Reset_EmptyDictionary_RaisesAdd()
	{
		var dict = new ObservableDictionary<string, int>();
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict.Reset(new[] { Kv("a", 1) });

		var e = Assert.Single(recorder.Collection);
		Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
	}

	[Fact]
	public void Reset_FromIDictionary_Works()
	{
		var dict = new ObservableDictionary<string, int>();
		var source = new Dictionary<string, int> { ["a"] = 1 };

		dict.Reset(source);

		Assert.Equal(1, dict["a"]);
	}

	[Fact]
	public void Reset_PreferReset_RaisesReset()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 })
		{
			PreferResetForRangeOperations = true
		};
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict.Reset(new[] { Kv("b", 2) });

		var e = Assert.Single(recorder.Collection);
		Assert.Equal(NotifyCollectionChangedAction.Reset, e.Action);
	}

	[Fact]
	public void Reset_EmptyItems_Clears()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

		dict.Reset([]);

		Assert.Empty(dict);
	}

	[Fact]
	public void Reset_Null_Throws()
	{
		var dict = new ObservableDictionary<string, int>();

		Assert.Throws<ArgumentNullException>(() => dict.Reset((IEnumerable<KeyValuePair<string, int>>)null!));
		Assert.Throws<ArgumentNullException>(() => dict.Reset((IDictionary<string, int>)null!));
	}

	[Fact]
	public void Reset_DuplicateKeys_Throws()
	{
		var dict = new ObservableDictionary<string, int>();

		Assert.Throws<ArgumentException>(() => dict.Reset(new[] { Kv("a", 1), Kv("a", 2) }));
	}

	// ========== Keys / Values ==========

	[Fact]
	public void Keys_ReturnsSnapshotOfKeys()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });

		var keys = dict.Keys;

		Assert.Equal(2, keys.Count);
		Assert.Contains("a", keys);
		Assert.Contains("b", keys);
	}

	[Fact]
	public void Keys_IsSnapshot_NotAffectedByLaterChanges()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });
		var keys = dict.Keys;

		dict.Add("b", 2);
		dict.Remove("a");

		Assert.Single(keys);
		Assert.Contains("a", keys);
		Assert.DoesNotContain("b", keys);
	}

	[Fact]
	public void Values_ReturnsSnapshotOfValues()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });

		var values = dict.Values;

		Assert.Equal(2, values.Count);
		Assert.Contains(1, values);
		Assert.Contains(2, values);
	}

	[Fact]
	public void Values_IsSnapshot_NotAffectedByLaterChanges()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });
		var values = dict.Values;

		dict["a"] = 999;

		Assert.Single(values);
		Assert.Contains(1, values);
		Assert.DoesNotContain(999, values);
	}

	[Fact]
	public void Keys_NonGeneric_Works()
	{
		IDictionary dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

		var keys = dict.Keys;

		Assert.Single(keys);
		Assert.Contains("a", keys.Cast<object>());
	}

	[Fact]
	public void Values_NonGeneric_Works()
	{
		IDictionary dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

		var values = dict.Values;

		Assert.Single(values);
		Assert.Contains(1, values.Cast<object>());
	}

	[Fact]
	public void Keys_ReadOnlyInterface_IsEnumerable()
	{
		IReadOnlyDictionary<string, int> dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

		var keys = dict.Keys.ToList();

		Assert.Equal(["a"], keys);
	}

	// ========== Enumeration ==========

	[Fact]
	public void GetEnumerator_YieldsAllPairs()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });

		var pairs = dict.ToList();

		Assert.Equal(2, pairs.Count);
		Assert.Contains(Kv("a", 1), pairs);
		Assert.Contains(Kv("b", 2), pairs);
	}

	[Fact]
	public void SnapshotEnumerator_IsNotAffectedByModifications()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });

		using var enumerator = dict.GetEnumerator();
		Assert.True(enumerator.MoveNext());

		dict.Add("c", 3);
		dict.Remove("a");

		var remaining = 0;
		while (enumerator.MoveNext())
			remaining++;

		Assert.Equal(1, remaining);
	}

	[Fact]
	public void NonSnapshotEnumerator_ThrowsOnModification()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 })
		{
			UseSnapshotEnumeration = false
		};

		using var enumerator = dict.GetEnumerator();
		Assert.True(enumerator.MoveNext());

		dict.Add("b", 2);

		Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
	}

	[Fact]
	public void GetSnapshotEnumerator_AlwaysReturnsSnapshot()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 })
		{
			UseSnapshotEnumeration = false
		};

		using var enumerator = dict.GetSnapshotEnumerator();
		Assert.True(enumerator.MoveNext());
		dict.Add("b", 2);
		Assert.False(enumerator.MoveNext());
	}

	[Fact]
	public void NonGenericEnumerator_ReturnsDictionaryEntries()
	{
		IDictionary dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

		var enumerator = dict.GetEnumerator();
		Assert.True(enumerator.MoveNext());
		var entry = Assert.IsType<DictionaryEntry>(enumerator.Current);

		Assert.Equal("a", entry.Key);
		Assert.Equal(1, entry.Value);
		Assert.Equal("a", enumerator.Key);
		Assert.Equal(1, enumerator.Value);
	}

	// ========== CopyTo ==========

	[Fact]
	public void CopyTo_CopiesPairs()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });
		var array = new KeyValuePair<string, int>[3];

		dict.CopyTo(array, 1);

		Assert.Equal(default, array[0]);
		Assert.Contains(array[1], new[] { Kv("a", 1), Kv("b", 2) });
		Assert.Contains(array[2], new[] { Kv("a", 1), Kv("b", 2) });
	}

	[Fact]
	public void CopyTo_NonGeneric_CopiesPairs()
	{
		ICollection dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });
		var array = new KeyValuePair<string, int>[1];

		dict.CopyTo(array, 0);

		Assert.Equal(Kv("a", 1), array[0]);
	}

	// ========== Interface contracts ==========

	[Fact]
	public void Implements_IDictionary_Contract()
	{
		IDictionary<string, int> dict = new ObservableDictionary<string, int>();

		Assert.False(dict.IsReadOnly);
		Assert.Empty(dict);
		dict.Add("a", 1);
		Assert.True(dict.ContainsKey("a"));
		Assert.True(dict.Remove("a"));
	}

	[Fact]
	public void Implements_IReadOnlyDictionary_Contract()
	{
		IReadOnlyDictionary<string, int> dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

		Assert.Single(dict);
		Assert.Equal(1, dict["a"]);
		Assert.True(dict.TryGetValue("a", out var value));
		Assert.Equal(1, value);
		Assert.True(dict.ContainsKey("a"));
		Assert.Equal(["a"], dict.Keys);
		Assert.Equal([1], dict.Values);
	}

	[Fact]
	public void Implements_ICollection_Contract()
	{
		ICollection<KeyValuePair<string, int>> dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });

		Assert.False(dict.IsReadOnly);
		Assert.True(dict.Contains(Kv("a", 1)));
		Assert.True(dict.Remove(Kv("a", 1)));
		Assert.Empty(dict);
	}

	[Fact]
	public void NonGenericFlags_AreSet()
	{
		IDictionary dict = new ObservableDictionary<string, int>();

		Assert.False(dict.IsFixedSize);
		Assert.False(dict.IsReadOnly);
		Assert.True(dict.IsSynchronized);
		Assert.NotNull(dict.SyncRoot);
	}

	// ========== PropertyChanged ==========

	[Fact]
	public void Add_RaisesPropertyChanged()
	{
		var dict = new ObservableDictionary<string, int>();
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict.Add("a", 1);

		Assert.Contains("Count", recorder.Properties);
		Assert.Contains("Any", recorder.Properties);
		Assert.Contains("Keys", recorder.Properties);
		Assert.Contains("Values", recorder.Properties);
		Assert.Contains("Item[]", recorder.Properties);
	}

	[Fact]
	public void Replace_RaisesPropertyChanged()
	{
		var dict = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["a"] = 1 });
		var recorder = new EventRecorder();
		recorder.Attach(dict);

		dict["a"] = 2;

		Assert.Contains("Item[]", recorder.Properties);
	}

	// ========== Comparer ==========

	[Fact]
	public void CustomComparer_IsUsedForKeyLookup()
	{
		var dict = new ObservableDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		dict["Key"] = 1;

		Assert.True(dict.ContainsKey("key"));
		Assert.Equal(1, dict["KEY"]);
		Assert.Throws<ArgumentException>(() => dict.Add("KEY", 2));
	}

	// ========== Thread safety ==========

	[Fact]
	public async Task ConcurrentAdds_AreSafe()
	{
		var dict = new ObservableDictionary<int, int>();

		var tasks = Enumerable.Range(0, 4).Select(thread => Task.Run(() =>
		{
			for (int i = 0; i < 500; i++)
				dict.TryAdd(thread * 1000 + i, i);
		})).ToArray();

		await Task.WhenAll(tasks);

		Assert.Equal(2000, dict.Count);
		Assert.Equal(499, dict[1499]);
	}

	[Fact]
	public async Task ConcurrentAddAndRemove_AreSafe()
	{
		var dict = new ObservableDictionary<int, int>();

		var addTask = Task.Run(() =>
		{
			for (int i = 0; i < 1000; i++)
				dict.TryAdd(i, i);
		});
		var removeTask = Task.Run(async () =>
		{
			for (int i = 0; i < 1000; i++)
			{
				dict.Remove(i);
				await Task.Yield();
			}
		});

		await Task.WhenAll(addTask, removeTask);

		Assert.True(dict.Count <= 1000);
	}

	[Fact]
	public async Task SnapshotKeys_WhileConcurrentlyModified_IsSafe()
	{
		var dict = new ObservableDictionary<int, int>();

		var addTask = Task.Run(() =>
		{
			for (int i = 0; i < 1000; i++)
				dict.TryAdd(i, i);
		});
		var readTask = Task.Run(() =>
		{
			for (int i = 0; i < 100; i++)
				_ = dict.Keys;
		});

		await Task.WhenAll(addTask, readTask);

		Assert.Equal(1000, dict.Count);
	}
}
