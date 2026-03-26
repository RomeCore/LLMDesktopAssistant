using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace LLMDesktopAssistant.LLM.MVVM
{
	public class MessageViewModelBase : ViewModelBase
	{
		private readonly BranchedMessage branchedMessage;
		private readonly IChatOperationService chatOperator;

		public ICommand RegenerateCommand { get; }
		public ICommand SwitchBranchCommand { get; }

		public IEnumerable<int> BranchIndices =>
			Enumerable.Range(1, branchedMessage.AvailableBranchesCount);

		public int SelectedBranchIndex
		{
			get => branchedMessage.SelectedBranchIndex + 1;
			set => chatOperator.SwitchBranch(branchedMessage.MessageIndex, value - 1);
		}
		public int AvailableBranchesCount => branchedMessage.AvailableBranchesCount;
		public int PreviousBranchIndex => SelectedBranchIndex - 1;
		public int NextBranchIndex => SelectedBranchIndex + 1;
		public bool BranchSelectionAvailable => branchedMessage.AvailableBranchesCount > 1;
		public Visibility BranchSelectionVisibility => BranchSelectionAvailable ? Visibility.Visible : Visibility.Collapsed;

		public virtual void OnRemoved()
		{
		}

		public MessageViewModelBase(BranchedMessage branchedMessage, Chat chat)
		{
			this.branchedMessage = branchedMessage;
			chatOperator = chat.Services.GetRequiredService<IChatOperationService>();

			RegenerateCommand = new RelayCommand(() =>
			{
				chatOperator.RegenerateOrResendMessageAsync(branchedMessage.MessageIndex);
			});

			SwitchBranchCommand = new RelayCommand<int>(branchIndex =>
			{
				chatOperator.SwitchBranch(branchedMessage.MessageIndex, branchIndex - 1);
			},
			branchIndex => branchIndex - 1 >= 0 && branchIndex - 1 < branchedMessage.AvailableBranchesCount);
		}
	}
}