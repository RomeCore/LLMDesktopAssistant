using LLMDesktopAssistant.LLM.MVVM.Attachments;
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

namespace LLMDesktopAssistant.LLM.MVVM.Attachments
{
	public partial class AttachmentsManagerView : UserControl
	{
		public AttachmentsManagerView()
		{
			InitializeComponent();

			this.AllowDrop = true;
			this.Drop += OnDrop;
		}

		private void OnDrop(object sender, DragEventArgs e)
		{
			if (DataContext is AttachmentsManagerViewModel vm)
			{
				vm.AcceptDrop(e);
			}
		}
	}
}
