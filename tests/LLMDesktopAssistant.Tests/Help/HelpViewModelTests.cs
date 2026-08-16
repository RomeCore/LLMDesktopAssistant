using System.Reflection;
using LLMDesktopAssistant.Help;
using LLMDesktopAssistant.Settings.Application;

namespace LLMDesktopAssistant.Tests.Help;

public class HelpViewModelTests
{
	private readonly HelpViewModel _viewModel;

	public HelpViewModelTests()
	{
		ApplicationSettingsAccessor.SetApplicationSettings(new ApplicationSettings());
		_viewModel = new HelpViewModel(CreateStore());
	}


	[Fact]
	public void LinkClicked_NavigatesFromRootDocument()
	{
		_viewModel.SelectedNode = FindNode(_viewModel, "getting-started");

		Assert.True(_viewModel.LinkClicked(new Uri("chat.md", UriKind.Relative)));
		Assert.Equal("chat", _viewModel.SelectedNode?.NodePath);
	}

	[Fact]
	public void LinkClicked_NavigatesFromRootDocumentToNestedDocument()
	{
		_viewModel.SelectedNode = FindNode(_viewModel, "getting-started");

		Assert.True(_viewModel.LinkClicked(new Uri("chat/agents.md", UriKind.Relative)));
		Assert.Equal("chat/agents", _viewModel.SelectedNode?.NodePath);
	}

	[Fact]
	public void LinkClicked_ResolvesSiblingRelativeToCurrentDocument()
	{
		_viewModel.SelectedNode = FindNode(_viewModel, "tools/metatools");

		Assert.True(_viewModel.LinkClicked(new Uri("scripting.md", UriKind.Relative)));
		Assert.Equal("tools/scripting", _viewModel.SelectedNode?.NodePath);
	}

	[Fact]
	public void LinkClicked_ResolvesParentRelativeToCurrentDocument()
	{
		_viewModel.SelectedNode = FindNode(_viewModel, "tools/metatools");

		Assert.True(_viewModel.LinkClicked(new Uri("../tools.md", UriKind.Relative)));
		Assert.Equal("tools", _viewModel.SelectedNode?.NodePath);
	}

	[Fact]
	public void LinkClicked_StripsFragment()
	{
		_viewModel.SelectedNode = FindNode(_viewModel, "getting-started");

		Assert.True(_viewModel.LinkClicked(new Uri("chat.md#intro", UriKind.Relative)));
		Assert.Equal("chat", _viewModel.SelectedNode?.NodePath);
	}

	[Fact]
	public void LinkClicked_ReturnsFalseForAbsoluteUri()
	{
		_viewModel.SelectedNode = FindNode(_viewModel, "getting-started");

		Assert.False(_viewModel.LinkClicked(new Uri("https://example.com/page.md")));
	}

	[Fact]
	public void LinkClicked_ReturnsFalseForUnknownDocument()
	{
		_viewModel.SelectedNode = FindNode(_viewModel, "getting-started");

		Assert.False(_viewModel.LinkClicked(new Uri("unknown.md", UriKind.Relative)));
	}

	[Theory]
	[InlineData("![a](assets/img.png)", "![a](avares://LLMDesktopAssistant/Assets/help/assets/img.png)")]
	[InlineData("![a](chat/photo.jpg)", "![a](avares://LLMDesktopAssistant/Assets/help/chat/photo.jpg)")]
	[InlineData("![a](assets/anim.gif)", "![a](avares://LLMDesktopAssistant/Assets/help/assets/anim.gif)")]
	[InlineData("![a](assets/icon.svg)", "![a](avares://LLMDesktopAssistant/Assets/help/assets/icon.svg)")]
	[InlineData("![a](assets/img.webp)", "![a](avares://LLMDesktopAssistant/Assets/help/assets/img.webp)")]
	[InlineData("[link](chat.md)", "[link](chat.md)")]
	[InlineData("![a](https://example.com/x.png)", "![a](https://example.com/x.png)")]
	public void ReplaceImageLinks(string input, string expected)
	{
		var method = typeof(HelpViewModel).GetMethod("ReplaceImageLinks",
			BindingFlags.NonPublic | BindingFlags.Static);
		var result = (string)method!.Invoke(null, new object[] { input })!;

		Assert.Equal(expected, result);
	}

	private static HelpDocumentStore CreateStore()
	{
		// The tree is built manually so that navigation tests do not depend on the
		// embedded help resources of the application assembly.
		var store = new HelpDocumentStore(typeof(HelpViewModelTests).Assembly);
		var root = store.Root;

		var chat = AddNode(root, "chat");
		AddNode(chat, "chat/agents");
		AddNode(chat, "chat/memory");

		AddNode(root, "getting-started");

		var tools = AddNode(root, "tools");
		AddNode(tools, "tools/metatools");
		AddNode(tools, "tools/scripting");

		return store;
	}

	private static HelpDocumentNode AddNode(HelpDocumentNode parent, string path)
	{
		var node = new HelpDocumentNode(path, path);
		parent.Children.Add(node);
		return node;
	}

	private static HelpDocumentNode FindNode(HelpViewModel viewModel, string path) =>
		FindNode(viewModel.RootNodes, path);

	private static HelpDocumentNode FindNode(IEnumerable<HelpDocumentNode> nodes, string path)
	{
		foreach (var node in nodes)
		{
			if (string.Equals(node.NodePath, path, StringComparison.OrdinalIgnoreCase))
				return node;
			if (FindNode(node.Children, path) is { } found)
				return found;
		}

		return null!;
	}
}
