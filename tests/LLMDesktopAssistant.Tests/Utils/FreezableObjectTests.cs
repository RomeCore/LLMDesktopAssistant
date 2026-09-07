namespace LLMDesktopAssistant.Tests.Utils
{
	public class FreezableObjectTests
	{
		private class TestFreezable : Freezable
		{
			public int Value
			{
				get;
				set => SetProperty(ref field, value);
			}
		}

		private class TestFreezableWithNotFrozen : Freezable
		{
			public int Value
			{
				get;
				set => SetProperty(ref field, value);
			}

			[NotFrozen]
			public string? NotFrozen1
			{
				get;
				set => SetProperty(ref field, value);
			}

			public string? NotFrozen2
			{
				get;
				set => SetProperty(ref field, value);
			}

			protected override IEnumerable<string> NotFrozenProperties => ["NotFrozen2"];
		}

		private class TestFreezableWithNotFrozenDescendant : TestFreezableWithNotFrozen
		{
			[NotFrozen]
			public string? NotFrozen3
			{
				get;
				set => SetProperty(ref field, value);
			}
		}

		[Fact]
		public void Freezable_BlocksChangesAfterFreezing()
		{
			var testFreezable = new TestFreezable
			{
				Value = 10
			};
			Assert.False(testFreezable.IsFrozen);
			Assert.Equal(10, testFreezable.Value);

			testFreezable.Value = 20;
			Assert.False(testFreezable.IsFrozen);
			Assert.Equal(20, testFreezable.Value);

			testFreezable.Freeze();
			Assert.True(testFreezable.IsFrozen);

			Assert.Throws<InvalidOperationException>(() => testFreezable.Value = 30);
			Assert.Equal(20, testFreezable.Value);
		}

		[Fact]
		public void Freezable_NotFreezedProperties_AreNormal()
		{
			var testFreezable = new TestFreezableWithNotFrozen
			{
				Value = 10,
				NotFrozen1 = "abc",
				NotFrozen2 = "def"
			};
			Assert.False(testFreezable.IsFrozen);

			testFreezable.Freeze();
			Assert.True(testFreezable.IsFrozen);

			Assert.Throws<InvalidOperationException>(() => testFreezable.Value = 20);
			testFreezable.NotFrozen1 = "ghi";
			testFreezable.NotFrozen2 = "jkl";
			Assert.Equal(10, testFreezable.Value);
			Assert.Equal("ghi", testFreezable.NotFrozen1);
			Assert.Equal("jkl", testFreezable.NotFrozen2);
		}

		[Fact]
		public void Freezable_NotFreezedProperties_AreNormal_Descendant()
		{
			var testFreezable = new TestFreezableWithNotFrozenDescendant
			{
				Value = 10,
				NotFrozen1 = "abc",
				NotFrozen2 = "def",
				NotFrozen3 = "ghi"
			};
			Assert.False(testFreezable.IsFrozen);

			testFreezable.Freeze();
			Assert.True(testFreezable.IsFrozen);

			Assert.Throws<InvalidOperationException>(() => testFreezable.Value = 20);
			testFreezable.NotFrozen1 = "ghi";
			testFreezable.NotFrozen2 = "jkl";
			testFreezable.NotFrozen3 = "mno";
			Assert.Equal(10, testFreezable.Value);
			Assert.Equal("ghi", testFreezable.NotFrozen1);
			Assert.Equal("jkl", testFreezable.NotFrozen2);
			Assert.Equal("mno", testFreezable.NotFrozen3);
		}

		[Fact]
		public void Freezable_DoesNotSerialize_IsFrozenProperty_Json()
		{
			var testFreezable = new TestFreezable
			{
				Value = 10
			};
			var expectedObject = new
			{
				Value = 10
			};

			var freezableJson = System.Text.Json.JsonSerializer.Serialize(testFreezable);
			var expectedJson = System.Text.Json.JsonSerializer.Serialize(expectedObject);

			Assert.Equal(expectedJson, freezableJson);
		}

		[Fact]
		public void Freezable_DoesNotSerialize_IsFrozenProperty_Bson()
		{
			var testFreezable = new TestFreezable
			{
				Value = 10
			};
			var expectedObject = new
			{
				Value = 10
			};

			var freezableJson = LiteDB.BsonMapper.Global.Serialize(testFreezable);
			var expectedJson = LiteDB.BsonMapper.Global.Serialize(expectedObject);

			Assert.Equal(expectedJson, freezableJson);
		}

		[Fact]
		public void Freezable_DoesNotSerialize_IsFrozenProperty_Yaml()
		{
			var testFreezable = new TestFreezable
			{
				Value = 10
			};
			var expectedObject = new
			{
				Value = 10
			};

			var serializer = new YamlDotNet.Serialization.SerializerBuilder().Build();
			var freezableJson = serializer.Serialize(testFreezable);
			var expectedJson = serializer.Serialize(expectedObject);

			Assert.Equal(expectedJson, freezableJson);
		}
	}
}
