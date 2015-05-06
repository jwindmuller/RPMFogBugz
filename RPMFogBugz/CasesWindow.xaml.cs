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
using System.Windows.Shapes;
using System.Diagnostics;

namespace RPMFogBugz
{
	/// <summary>
	/// Interaction logic for Cases.xaml
	/// </summary>
	public partial class CasesWindow : Window
	{
		private List<CaseInformation> data;
		private List<CaseInformation> _cases;
		public List<CaseInformation> cases
		{
			set
			{
				_cases = value;
				this.updateList();
			}
			get {
				return _cases;
			}
		}
		CollectionViewSource dataSource;
		public CasesWindow()
		{
			InitializeComponent();
			dataSource = (CollectionViewSource)FindResource("CaseList");
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.WindowState = WindowState.Minimized;
		}

		private void updateList()
		{
			this.data = this.cases;
			string searchTerm = this.SearchBox.Text.Trim().ToLower();
			if (searchTerm != "filter...")
			{
				this.data = this.data.Where(x => x.title.ToLower().Contains(searchTerm)).ToList();
			}
			this.dataSource.Source = this.data;
		}

		private void CopyButton_Click(object sender, RoutedEventArgs e)
		{
			Hyperlink link = (Hyperlink)e.OriginalSource;
			
			CaseInformation caseInfo = link.DataContext as CaseInformation;

			System.Windows.Clipboard.SetText(string.Format(
				"YOUR_COMMIT_MSG\n\nBugzID: {0}\n{1}",
				caseInfo.number,
				caseInfo.url
			));
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			Hyperlink link = (Hyperlink)e.OriginalSource;
			Process.Start(link.NavigateUri.AbsoluteUri);
		}

		private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
			{
				this.DragMove();
			}
		}

		private void TextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			if (this.SearchBox.Text == "Filter...")
			{
				this.SearchBox.Text = "";
			}
		}

		private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (this.SearchBox.Text == "")
			{
				this.SearchBox.Text = "Filter...";
			}
		}

		private void SearchBox_KeyUp(object sender, KeyEventArgs e)
		{
			this.updateList();
		}
	}
}
