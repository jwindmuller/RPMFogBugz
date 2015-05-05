# RPMFogBugz

Quickly get BugzID: ### for commit messages.

It uses the FogBugz API to login and get your currently assigned cases and shows a system tray icon with that list. 

Clicking a case copies the BugzID to the clipboard.

## Setup

In `RPMFogBugz/MainWindow.xaml.cs` change `baseUrl` to your organization's FogBugz URL.