using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Tests.Utils
{
	public class ObservableCollectionTests
	{
		[Fact]
		public void ReadOnlyObservableCollection_FiresEvents()
		{
			var original = new RangeObservableCollection<int>();

			var readOnly = new ReadOnlyObservableCollection<int>(original);

			int count = 0;
			readOnly.CollectionChanged += (s, e) =>
			{
				count++;
			};

			original.Add(1);
			original.Add(2);
			original.RemoveAt(0);

			Assert.Equal(3, count);
		}

		[Fact]
		public void ReadOnlyObservableCollection_Chain_FiresEvents()
		{
			var original = new RangeObservableCollection<int>();

			var readOnly1 = new ReadOnlyObservableCollection<int>(original);
			var readOnly2 = new ReadOnlyObservableCollection<int>(readOnly1);
			var readOnly3 = new ReadOnlyObservableCollection<int>(readOnly2);

			int count = 0;
			readOnly3.CollectionChanged += (s, e) =>
			{
				count++;
			};

			original.Add(1);
			original.Add(2);
			original.RemoveAt(0);

			Assert.Equal(3, count);
		}
	}
}
