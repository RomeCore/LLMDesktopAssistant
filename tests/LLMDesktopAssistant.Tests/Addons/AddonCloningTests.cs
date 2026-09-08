using LLMDesktopAssistant.Addons;

namespace LLMDesktopAssistant.Tests.Addons
{
	public class AddonCloningTests
	{
		private class TestCloneableAddon : AddonBase<TestCloneableAddon>
		{
			public string AdditionalProperty
			{
				get => field ??= string.Empty;
				set => SetProperty(ref field, value);
			}
		}

		private class TestNotCloneableAddon : AddonBase<TestNotCloneableAddon>
		{
			internal TestNotCloneableAddon()
			{
			}
		}

		[Fact]
		public void CloneableAddon_CloningWorks()
		{
			var original = new TestCloneableAddon
			{
				Name = "test-addon",
				Description = "This is a test addon",
				Body = "*Body*",
				AdditionalProperty = "Additional Value"
			};
			original.Freeze();

			var cloned = original.Clone();
			Assert.False(cloned.IsFrozen);

			Assert.Equal(original.Name, cloned.Name);
			Assert.Equal(original.Description, cloned.Description);
			Assert.Equal(original.Body, cloned.Body);
			Assert.Equal(original.AdditionalProperty, cloned.AdditionalProperty);
			Assert.Equal(original.HomeDirectory, cloned.HomeDirectory); // Property not set in original, should be default
			Assert.NotSame(original, cloned);
		}

		[Fact]
		public void NonCloneableAddon_CloningThrowsException()
		{
			var addon = new TestNotCloneableAddon();
			Assert.Throws<InvalidOperationException>(() => addon.Clone());
		}
	}
}
