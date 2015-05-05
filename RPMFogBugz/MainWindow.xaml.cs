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
using System.Windows.Forms;
using System.Drawing;

using System.Net.Http;

using System.Xml.Linq;

namespace RPMFogBugz
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string token = "";
		private string email
		{
			get {
				return this.EmailTextBox.Text.Trim();
			}
		}
		private string password
		{
			get
			{
				return this.PasswordTextBox.Password;
			}
		}

		private string baseUrl = "https://rpm.fogbugz.com/";
		private HttpClient _client = null;
		private HttpClient client
		{
			get
			{
				if (_client == null)
				{
					_client = new HttpClient();
					_client.BaseAddress = new Uri(this.baseUrl + "api.asp");
				}
				return _client;
			}
		}

		private ContextMenuStrip contextMenu;
		private NotifyIcon notifyIcon;
		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			notifyIcon = new NotifyIcon();
			notifyIcon.BalloonTipText = "I'm here";
			notifyIcon.Text = "Test";
			notifyIcon.BalloonTipTitle = "RPM FogBugz";
			notifyIcon.Click += new EventHandler(notifyIcon_Click);
			try
			{
				notifyIcon.Icon = new Icon("Resources/TrayIcon.ico");
				notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			contextMenu = new ContextMenuStrip();
			ToolStripItem test = contextMenu.Items.Add("Update Cases");
			test.Click += ContextItemSelected;
		}

		private void updateContextMenu() {
			string[] cols = new string[] {"ixBug", "sTitle"};

			XElement doc = this.sendRequest(
				string.Format(
					"?token={0}&cmd=search&q={1}&cols={2}", 
					this.token,
					"assignedto:\"joaquin\" project:\"development\"",
					string.Join(",", cols)
				)
			);

			var cases = from caseEl in doc.Descendants("case")
						select new
						{
							CaseNumber = caseEl.Attribute("ixBug").Value,
							CaseTitle  = (from titleEl in caseEl.Descendants("sTitle") select titleEl).First().Value
						};

			contextMenu.Items.Clear();
			foreach (var foundCase in cases)
			{
				ToolStripItem caseItem = contextMenu.Items.Add(
					string.Format("BugzID: {0}\n{1}", foundCase.CaseNumber, foundCase.CaseTitle)
				);
				caseItem.Click += ContextItemSelected;
			}

			ToolStripItem refreshItem = contextMenu.Items.Add("Reload");
			refreshItem.Click += ContextItemSelected;

			ToolStripItem logOutItem = contextMenu.Items.Add("Log out");
			logOutItem.Click += ContextItemSelected;
		}

		private void ContextItemSelected(object sender, EventArgs e)
		{
			Console.WriteLine("Item Selected");
			this.updateContextMenu();
			if (sender.ToString() == "Reload")
			{
				return;
			}
			if (sender.ToString() == "Log out")
			{
				this.token = "";
				this.EmailTextBox.Clear();
				this.PasswordTextBox.Clear();
				this.contextMenu.Items.Clear();
				this.Show();
				this.WindowState = WindowState.Normal;
				return;
			}
			var parts = sender.ToString().Split('\n');
			System.Windows.Clipboard.SetText(string.Format(
				"{0}\n{1}",
				parts[0],
				this.baseUrl + "?" + parts[0].Replace("BugzID: ", "")
			));
		}

		private void LoginButton_Click(object sender, RoutedEventArgs e)
		{
			this.ErrorContainer.Visibility = Visibility.Hidden;

			XElement doc = this.sendRequest(
				string.Format("?cmd=logon&email={0}&password={1}", this.email, this.password)
			);
			FogBugzError error = this.checkError(doc);
			if (error != null)
			{
				this.ErrorContainer.Visibility = Visibility.Visible;
				return;
			}

			IEnumerable<string> tokens = from tokenEl in doc.Descendants("token")
										 select (string)tokenEl;
			this.token = tokens.First();

			this.updateContextMenu();

			WindowState = WindowState.Minimized;
		}

		void notifyIcon_Click(object sender, EventArgs e)
		{
			if (this.token == "")
			{
				this.Show();
				this.WindowState = WindowState.Normal;
				return;
			}
			this.contextMenu.Show(System.Windows.Forms.Control.MousePosition);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			notifyIcon.Dispose();
			notifyIcon = null;
		}

		private WindowState storedWindowState = WindowState.Normal;
		void Window_StateChanged(object sender, EventArgs args)
		{
			if (WindowState == WindowState.Minimized)
			{
				Hide();
				if (notifyIcon != null)
				{
					notifyIcon.ShowBalloonTip(5000);
				}
			}
			else
			{
				Show();
				storedWindowState = WindowState;
			}
		}

		private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			CheckTrayIcon();
		}

		void CheckTrayIcon()
		{
			ShowTrayIcon(!IsVisible);
		}

		void ShowTrayIcon(bool show)
		{
			if (notifyIcon != null)
			{
				notifyIcon.Visible = show;
			}
		}

		XElement sendRequest(string query)
		{
			Task<HttpResponseMessage> request = this.client.GetAsync(query);
			Task.WaitAll(request);

			Task<string> bodyRead = request.Result.Content.ReadAsStringAsync();
			Task.WaitAll(bodyRead);

			return XElement.Parse(bodyRead.Result);
		}

		FogBugzError checkError(XElement doc)
		{
			IEnumerable<XElement> errors = doc.Descendants("error");
			if (errors.Count() == 0)
			{
				return null;
			}
			XElement error = errors.First();
			if (error == null)
			{
				return null;
			}
			int code = int.Parse( error.Attribute("code").Value );
			string description = error.Value;
			return new FogBugzError(code, description);
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				this.DragMove();
			}
		}

	}
}
