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
		
		private FogBugz fb {
			get
			{
				return FogBugz.getInstance();
			}
		}

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

		private CasesWindow _casesWindow = null;
		private CasesWindow casesWindow
		{
			get
			{
				if (_casesWindow == null)
				{
					_casesWindow = new CasesWindow();
					_casesWindow.Closed += casesWindow_Closed;
				}
				return _casesWindow;
			}
		}

		private ContextMenuStrip contextMenu;
		private NotifyIcon notifyIcon;

		public MainWindow()
		{
			InitializeComponent();
			this.fb.didLogout += fb_didLogout;
		}

		private void fb_didLogout(object sender, EventArgs e)
		{
			this.casesWindow.Close();
			this.Show();
			this.WindowState = WindowState.Normal;
		}

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
				this.fb.updateCasesData();
				return;
			}
			if (sender.ToString() == "Log out")
			{
				this.fb.logout();
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

			if (!this.fb.login(this.email, this.password))
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

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
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
