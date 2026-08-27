using LLMDesktopAssistant;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Tests.Utils;

/// <summary>
/// Tests the compatibility of <see cref="ChangeTracker"/> with <see cref="ObservableDictionary{TKey, TValue}"/>.
/// </summary>
public class ChangeTrackerTests
{
	private sealed class ObservableItem : NotifyPropertyChanged
	{
		private int _value;

		public int Value
		{
			get => _value;
			set => SetProperty(ref _value, value);
		}
	}

	private sealed class Holder : NotifyPropertyChanged
	{
		private ObservableDictionary<string, int>? _dictionary;

		public ObservableDictionary<string, int>? Dictionary
		{
			get => _dictionary;
			set => SetProperty(ref _dictionary, value);
		}
	}

	private sealed class TrackedContext
	{
		public ObservableDictionary<string, int> Dict { get; } = new();

		public ChangeTracker Tracker { get; }

		public int Changed;

		public TrackedContext()
		{
			Tracker = new ChangeTracker(Dict, () => Changed++);
		}
	}

	private static TrackedContext CreateTracked() => new();

	private sealed class TrackedItemContext
	{
		public ObservableDictionary<string, ObservableItem> Dict { get; } = new();

		public ChangeTracker Tracker { get; }

		public int Changed;

		public TrackedItemContext()
		{
			Tracker = new ChangeTracker(Dict, () => Changed++);
		}
	}

	// ========== Basic operations ==========

	[Fact]
	public void Add_FiresOnChanged()
	{
		var ctx = CreateTracked();

		ctx.Dict.Add("a", 1);

		Assert.Equal(1, ctx.Changed);
	}

	[Fact]
	public void IndexerSet_NewKey_FiresOnChanged()
	{
		var ctx = CreateTracked();

		ctx.Dict["a"] = 1;

		Assert.Equal(1, ctx.Changed);
	}

	[Fact]
	public void IndexerSet_ExistingKey_FiresOnChanged()
	{
		var ctx = CreateTracked();
		ctx.Dict.Add("a", 1);
		ctx.Changed = 0;

		ctx.Dict["a"] = 2;

		Assert.Equal(1, ctx.Changed);
	}

	[Fact]
	public void Remove_FiresOnChanged()
	{
		var ctx = CreateTracked();
		ctx.Dict.Add("a", 1);
		ctx.Changed = 0;

		ctx.Dict.Remove("a");

		Assert.Equal(1, ctx.Changed);
	}

	[Fact]
	public void Remove_MissingKey_DoesNotFire()
	{
		var ctx = CreateTracked();

		ctx.Dict.Remove("missing");

		Assert.Equal(0, ctx.Changed);
	}

	[Fact]
	public void TryAdd_ExistingKey_DoesNotFire()
	{
		var ctx = CreateTracked();
		ctx.Dict.Add("a", 1);
		ctx.Changed = 0;

		ctx.Dict.TryAdd("a", 2);

		Assert.Equal(0, ctx.Changed);
	}

	[Fact]
	public void Clear_FiresOnChanged()
	{
		var ctx = CreateTracked();
		ctx.Dict.Add("a", 1);
		ctx.Dict.Add("b", 2);
		ctx.Changed = 0;

		ctx.Dict.Clear();

		Assert.Equal(1, ctx.Changed);
	}

	// ========== Range operations ==========

	[Fact]
	public void AddRange_FiresOnce()
	{
		var ctx = CreateTracked();

		ctx.Dict.AddRange(new[] { new KeyValuePair<string, int>("a", 1), new KeyValuePair<string, int>("b", 2) });

		Assert.Equal(1, ctx.Changed);
	}

	[Fact]
	public void AddRange_Throwing_DoesNotFire()
	{
		var ctx = CreateTracked();

		Assert.Throws<ArgumentException>(() => ctx.Dict.AddRange(new[] { new KeyValuePair<string, int>("a", 1), new KeyValuePair<string, int>("a", 2) }));

		Assert.Equal(0, ctx.Changed);
	}

	[Fact]
	public void RemoveRange_FiresOnce()
	{
		var ctx = CreateTracked();
		ctx.Dict.Add("a", 1);
		ctx.Dict.Add("b", 2);
		ctx.Changed = 0;

		ctx.Dict.RemoveRange(new[] { "a", "b" });

		Assert.Equal(1, ctx.Changed);
	}

	[Fact]
	public void Reset_FiresOnChanged()
	{
		var ctx = CreateTracked();
		ctx.Dict.Add("a", 1);
		ctx.Changed = 0;

		ctx.Dict.Reset(new[] { new KeyValuePair<string, int>("b", 2), new KeyValuePair<string, int>("c", 3) });

		Assert.Equal(1, ctx.Changed);
	}

	[Fact]
	public void GetOrAdd_NewKey_FiresOnChanged()
	{
		var ctx = CreateTracked();

		ctx.Dict.GetOrAdd("a", 1);

		Assert.Equal(1, ctx.Changed);
	}

	[Fact]
	public void GetOrAdd_ExistingKey_DoesNotFire()
	{
		var ctx = CreateTracked();
		ctx.Dict.Add("a", 1);
		ctx.Changed = 0;

		ctx.Dict.GetOrAdd("a", 2);

		Assert.Equal(0, ctx.Changed);
	}

	[Fact]
	public void AddOrUpdate_NewKey_FiresAdd()
	{
		var ctx = CreateTracked();

		ctx.Dict.AddOrUpdate("a", 1, (_, old) => old + 1);

		Assert.Equal(1, ctx.Changed);
	}

	[Fact]
	public void AddOrUpdate_ExistingKey_FiresReplace()
	{
		var ctx = CreateTracked();
		ctx.Dict.Add("a", 1);
		ctx.Changed = 0;

		ctx.Dict.AddOrUpdate("a", 1, (_, old) => old + 1);

		Assert.Equal(1, ctx.Changed);
	}

	// ========== Lifecycle ==========

	[Fact]
	public void Dispose_StopsTracking()
	{
		var dict = new ObservableDictionary<string, int>();
		var changed = 0;
		var tracker = new ChangeTracker(dict, () => changed++);

		tracker.Dispose();
		dict.Add("a", 1);

		Assert.Equal(0, changed);
	}

	[Fact]
	public void Dispose_StopsTracking_AfterReset()
	{
		var dict = new ObservableDictionary<string, int>();
		var changed = 0;
		var tracker = new ChangeTracker(dict, () => changed++);

		dict.Reset(new[] { new KeyValuePair<string, int>("a", 1) });
		Assert.Equal(1, changed);

		tracker.Dispose();
		dict.Add("b", 2);

		Assert.Equal(1, changed);
	}

	// ========== Nested tracking ==========

	[Fact]
	public void NestedDictionary_PropertyChanges_AreTracked()
	{
		var holder = new Holder();
		var dict = new ObservableDictionary<string, int>();
		var changed = 0;
		using var tracker = new ChangeTracker(holder, () => changed++);

		holder.Dictionary = dict; // SetProperty -> onChanged
		Assert.Equal(1, changed);

		dict.Add("a", 1);
		Assert.Equal(2, changed);
	}

	[Fact]
	public void ReplacingDictionaryProperty_StopsTrackingOldAndTracksNew()
	{
		var holder = new Holder();
		var oldDict = new ObservableDictionary<string, int>();
		var newDict = new ObservableDictionary<string, int>();
		var changed = 0;
		using var tracker = new ChangeTracker(holder, () => changed++);

		holder.Dictionary = oldDict;
		holder.Dictionary = newDict; // untrack old, track new
		changed = 0;

		oldDict.Add("a", 1); // old dictionary is no longer tracked
		Assert.Equal(0, changed);

		newDict.Add("b", 2);
		Assert.Equal(1, changed);
	}

	[Fact]
	public void TrackerDoesNotModifyDictionary()
	{
		var ctx = CreateTracked();

		ctx.Tracker.Dispose();

		Assert.Empty(ctx.Dict);
	}

	// ========== Deep tracking of values ==========

	[Fact]
	public void DeepChanges_InsideValues_AreTracked()
	{
		var ctx = new TrackedItemContext();
		var item = new ObservableItem();
		ctx.Dict.Add("item", item);
		ctx.Changed = 0;

		item.Value = 42;

		Assert.Equal(1, ctx.Changed);
	}

	[Fact]
	public void ReplaceValue_UntracksOldAndTracksNew()
	{
		var ctx = new TrackedItemContext();
		var oldItem = new ObservableItem();
		var newItem = new ObservableItem();
		ctx.Dict.Add("item", oldItem);
		ctx.Dict["item"] = newItem; // Replace
		ctx.Changed = 0;

		oldItem.Value = 1;
		Assert.Equal(0, ctx.Changed);

		newItem.Value = 2;
		Assert.Equal(1, ctx.Changed);
	}

	[Fact]
	public void Remove_UntracksValue()
	{
		var ctx = new TrackedItemContext();
		var item = new ObservableItem();
		ctx.Dict.Add("item", item);
		ctx.Dict.Remove("item");
		ctx.Changed = 0;

		item.Value = 1;

		Assert.Equal(0, ctx.Changed);
	}

	[Fact]
	public void RemoveRange_UntracksValues()
	{
		var ctx = new TrackedItemContext();
		var item1 = new ObservableItem();
		var item2 = new ObservableItem();
		ctx.Dict.Add("a", item1);
		ctx.Dict.Add("b", item2);
		ctx.Dict.RemoveRange(new[] { "a", "b" });
		ctx.Changed = 0;

		item1.Value = 1;
		item2.Value = 2;

		Assert.Equal(0, ctx.Changed);
	}

	[Fact]
	public void Clear_UntracksValues()
	{
		var ctx = new TrackedItemContext();
		var item = new ObservableItem();
		ctx.Dict.Add("item", item);
		ctx.Dict.Clear();
		ctx.Changed = 0;

		item.Value = 1;

		Assert.Equal(0, ctx.Changed);
	}

	[Fact]
	public void Reset_UntracksOldValuesAndTracksNew()
	{
		var ctx = new TrackedItemContext();
		var oldItem = new ObservableItem();
		var newItem = new ObservableItem();
		ctx.Dict.Add("item", oldItem);
		ctx.Dict.Reset(new[] { new KeyValuePair<string, ObservableItem>("item", newItem) });
		ctx.Changed = 0;

		oldItem.Value = 1;
		Assert.Equal(0, ctx.Changed);

		newItem.Value = 2;
		Assert.Equal(1, ctx.Changed);
	}

	[Fact]
	public void GetOrAdd_ExistingValue_StaysTracked()
	{
		var ctx = new TrackedItemContext();
		var item = new ObservableItem();
		ctx.Dict.Add("item", item);
		ctx.Dict.GetOrAdd("item", new ObservableItem());
		ctx.Changed = 0;

		item.Value = 42;

		Assert.Equal(1, ctx.Changed);
	}

	[Fact]
	public void Dispose_StopsDeepTracking()
	{
		var ctx = new TrackedItemContext();
		var item = new ObservableItem();
		ctx.Dict.Add("item", item);
		ctx.Tracker.Dispose();
		ctx.Changed = 0;

		item.Value = 42;

		Assert.Equal(0, ctx.Changed);
	}

	[Fact]
	public void DuplicateReference_Value_IsUntrackedAfterFirstRemoval()
	{
		// Known ChangeTracker limitation: the same value object stored under multiple keys
		// is untracked when the first key is removed, even though it is still present.
		var ctx = new TrackedItemContext();
		var item = new ObservableItem();
		ctx.Dict.Add("a", item);
		ctx.Dict.Add("b", item);
		ctx.Dict.Remove("a");
		ctx.Changed = 0;

		item.Value = 1;

		Assert.Equal(0, ctx.Changed);
	}
}
