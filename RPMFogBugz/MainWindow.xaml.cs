using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
//using System.Windows.Input;
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
		private string appName = "RpmFogBugz";
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

		private CasesWindow _casesWindow = null;
		private CasesWindow casesWindow
		{
			get
			{
				if (_casesWindow == null)
				{
					_casesWindow = new CasesWindow();
					_casesWindow.Closed += casesWindow_Closed;
					_casesWindow.cases = this.cases;
				}
				return _casesWindow;
			}
		}

		private ContextMenuStrip contextMenu;
		private NotifyIcon notifyIcon;
		public MainWindow()
		{
			InitializeComponent();
		}

		private List<CaseInformation> cases;

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			notifyIcon = new NotifyIcon();
			notifyIcon.BalloonTipText = "I'm here";
			notifyIcon.Text = "RPM :)";
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

			this.prePopulateLoginForm();
		}

		private void prePopulateLoginForm()
		{
			Credential userData = CredentialManager.ReadCredential(this.appName);
			if (userData != null)
			{
				this.EmailTextBox.Text = userData.UserName;
				this.PasswordTextBox.Password = userData.Password;
			}
		}

        private void updateCasesData()
        {
            string[] cols = new string[] { "ixBug", "sTitle", "sProject" };

            XElement doc = this.sendRequest(
                string.Format(
                    "?token={0}&cmd=search&q={1}&cols={2}",
                    this.token,
                    "assignedto:\"joaquin\" project:\"development\"",
                    string.Join(",", cols)
                )
            );

            FogBugzError error = this.checkError(doc);
            if (error != null)
            {
                // TODO: report error
                return;
            }

            this.cases = (from caseEl in doc.Descendants("case")
                          select new
                          {
                              CaseNumber = caseEl.Attribute("ixBug").Value,
                              CaseTitle = (from titleEl in caseEl.Descendants("sTitle") select titleEl).First().Value,
                              CaseProject = (from projectEl in caseEl.Descendants("sProject") select projectEl).First().Value
                          }).AsEnumerable()
                          .Select(
                             c => new CaseInformation(c.CaseTitle, int.Parse(c.CaseNumber), this.baseUrl + '?' + c.CaseNumber, c.CaseProject)
                          ).ToList();
        }

		private void updateContextMenu() {
			

			this.contextMenu.Items.Clear();
            ToolStripItem refreshItem = this.contextMenu.Items.Add("Reload");
			refreshItem.Click += ContextItemSelected;

			ToolStripItem logOutItem = this.contextMenu.Items.Add("Log out");
			logOutItem.Click += ContextItemSelected;

			
		}

		private void ContextItemSelected(object sender, EventArgs e)
		{
			if (sender.ToString() == "Reload")
			{
                this.updateCasesData();
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
                this.casesWindow.Close();
                return;
			}
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

			if (this.RememberMeCheckBox.IsChecked == true)
			{
				CredentialManager.WriteCredential(this.appName, this.email, this.password);
			}
			else
			{
				CredentialManager.WriteCredential(this.appName, "", "");
			}

			IEnumerable<string> tokens = from tokenEl in doc.Descendants("token")
										 select (string)tokenEl;
			this.token = tokens.First();

			this.updateContextMenu();

			WindowState = WindowState.Minimized;
		}

		void notifyIcon_Click(object sender, EventArgs e)
		{
			MouseEventArgs clickEvent = (MouseEventArgs)e;
			if (clickEvent.Button == MouseButtons.Right)
			{
				this.contextMenu.Show(System.Windows.Forms.Control.MousePosition);
			}
			else
			{
                this.casesWindow.Show();
				this.casesWindow.WindowState = WindowState.Normal;
			}
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
			bool shouldShowTrayIcon = !this.IsVisible;
			ShowTrayIcon(shouldShowTrayIcon);
			this.prePopulateLoginForm();
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
            //this.casesWindow.Close();
            if (this.notifyIcon != null)
            {
                this.notifyIcon.Dispose();
            }
			this.Close();
            System.Windows.Application.Current.Shutdown();
		}

		private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
			{
				this.DragMove();
			}
		}

		private void casesWindow_Closed(object sender, EventArgs e)
		{
			this._casesWindow = null;
		}
	}
}
