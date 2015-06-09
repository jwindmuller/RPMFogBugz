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
using System.Collections;
using System.Windows.Controls.Primitives;

namespace RPMFogBugz
{
	/// <summary>
	/// Interaction logic for Cases.xaml
	/// </summary>
	public partial class CasesWindow : Window
	{
		private List<CaseInformation> data;
		
		private FogBugz fb
		{
			get
			{
				return FogBugz.getInstance();
			}
		}
		private ListCollectionView grouped;
		
		CollectionViewSource dataSource;
		public CasesWindow()
		{
			InitializeComponent();
			dataSource = (CollectionViewSource)FindResource("CaseList");
			this.fb.didUpdateCases += fb_didUpdateCases;
			this.fb.updateCasesData();
		}

		private void fb_didUpdateCases(object sender, EventArgs e)
		{
			this.updateList();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.WindowState = WindowState.Minimized;
		}

		private WindowState storedWindowState = WindowState.Normal;
		void Window_StateChanged(object sender, EventArgs args)
		{
			if (WindowState == WindowState.Minimized)
			{
				Hide();
			}
			else
			{
				Show();
				Activate();
				storedWindowState = WindowState;
			}
		}

		private void updateList()
		{
			
			this.data = this.fb.cases;
			string searchTerm = this.SearchBox.Text.Trim().ToLower();
			if (searchTerm != "filter...")
			{
				this.data = this.data.Where(x => x.title.ToLower().Contains(searchTerm)).ToList();
			}
			this.grouped = new ListCollectionView(this.data);
			this.grouped.GroupDescriptions.Add(new PropertyGroupDescription("milestone"));
			this.CasesList.ItemsSource = this.grouped; 
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

		private void LogoutButton_Click(object sender, RoutedEventArgs e)
		{
			this.fb.stopTrackingWork();
			this.fb.logout();
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			this.fb.updateCasesData();
		}

		private void TrackWorkButton_Click(object sender, RoutedEventArgs e)
		{
			Hyperlink link = (Hyperlink)e.OriginalSource;
			CaseInformation caseInfo = link.DataContext as CaseInformation;
			
			this.StopWorkButton.Visibility = System.Windows.Visibility.Visible;
			this.fb.startTrackingWork(caseInfo.number);
		}

		private void StopWorkButton_Click(object sender, RoutedEventArgs e)
		{
			this.fb.stopTrackingWork();
			this.StopWorkButton.Visibility = System.Windows.Visibility.Hidden;
		}
	}
}
