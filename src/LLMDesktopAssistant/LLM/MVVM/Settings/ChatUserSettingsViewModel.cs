using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.Users;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// ViewModel for the chat user settings tab. Users are listed as cards and are
	/// created/edited through the <see cref="EditUserDialogViewModel"/> dialog.
	/// </summary>
	[ViewModelFor(typeof(ChatUserSettingsView))]
	public class ChatUserSettingsViewModel : ViewModelBase
	{
		/// <summary>
		/// Gets the collection of users.
		/// </summary>
		public RangeObservableCollection<UserInformation> Users { get; }

		/// <summary>
		/// Gets the command that opens the "Add user" dialog.
		/// </summary>
		public IAsyncRelayCommand AddCommand { get; }

		/// <summary>
		/// Gets the command that opens the "Edit user" dialog for the given user.
		/// </summary>
		public IAsyncRelayCommand<UserInformation> EditCommand { get; }

		/// <summary>
		/// Gets the command that removes the given user. The last user cannot be removed.
		/// </summary>
		public IRelayCommand<UserInformation> DeleteCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatUserSettingsViewModel"/> class.
		/// </summary>
		/// <param name="users">The collection of users to manage.</param>
		public ChatUserSettingsViewModel(RangeObservableCollection<UserInformation> users)
		{
			Users = users;

			AddCommand = new AsyncRelayCommand(AddAsync);
			EditCommand = new AsyncRelayCommand<UserInformation>(EditAsync);
			DeleteCommand = new RelayCommand<UserInformation>(Delete);
		}

		private async Task AddAsync()
		{
			var vm = new EditUserDialogViewModel(null);
			var result = await DialogManager.ShowDialogAsync(vm);

			if (result is true && vm.Result is { } user)
				Users.Add(user);
		}

		private async Task EditAsync(UserInformation? user)
		{
			if (user == null)
				return;

			await DialogManager.ShowDialogAsync(new EditUserDialogViewModel(user));
		}

		private void Delete(UserInformation? user)
		{
			if (user == null || Users.Count <= 1)
				return;

			Users.Remove(user);
		}
	}
}
