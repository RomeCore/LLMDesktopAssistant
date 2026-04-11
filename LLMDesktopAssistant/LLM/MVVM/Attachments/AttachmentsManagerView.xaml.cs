using LLMDesktopAssistant.Core.LLM.MVVM.Attachments;
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

namespace LLMDesktopAssistant.Core.LLM.MVVM.Attachments
{
	public partial class AttachmentsManagerView : UserControl
	{
		public AttachmentsManagerView()
		{
			InitializeComponent();

			this.AllowDrop = true;
			this.Drop += OnDrop;
		}

		private void OnUrlInputKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && DataContext is AttachmentsManagerViewModel vm)
			{
				vm.AddUrlCommand.Execute(null);
				e.Handled = true;
			}
		}

		private void OnDragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop) ||
				e.Data.GetDataPresent(DataFormats.Text))
			{
				e.Effects = DragDropEffects.Copy;
				DropOverlay.Visibility = Visibility.Visible;
			}
			else
			{
				e.Effects = DragDropEffects.None;
			}

			e.Handled = true;
		}

		private void OnDragLeave(object sender, DragEventArgs e)
		{
			DropOverlay.Visibility = Visibility.Collapsed;
		}

		private void OnDrop(object sender, DragEventArgs e)
		{
			DropOverlay.Visibility = Visibility.Collapsed;

			if (DataContext is AttachmentsManagerViewModel vm)
			{
				vm.AcceptDrop(e);
			}

			e.Handled = true;
		}
	}
}
