using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LLMDesktopAssistant.Core.LLM.MVVM
{
	public partial class UserInputView : UserControl
	{
		public UserInputView()
		{
			InitializeComponent();

			this.AllowDrop = true;
			this.Drop += OnDrop;
		}

		private async void OnDrop(object sender, DragEventArgs e)
		{
			if (DataContext is UserInputViewModel vm)
			{
				await vm.AcceptDropAsync(e);
			}
		}
	}
}