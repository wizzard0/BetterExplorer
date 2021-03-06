﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace BetterExplorer {

	//TODO: Make all updating private and handled in [Updater]
	partial class MainWindow {
		private BackgroundWorker UpdaterWorker;
		private int UpdateCheckType;

		private void CheckForUpdate(bool ShowUpdateUI = true) {
			this.UpdaterWorker = new BackgroundWorker();
			this.UpdaterWorker.WorkerSupportsCancellation = true;
			this.UpdaterWorker.WorkerReportsProgress = true;
			this.UpdaterWorker.DoWork += new DoWorkEventHandler(UpdaterWorker_DoWork);

			if (!this.UpdaterWorker.IsBusy)
				this.UpdaterWorker.RunWorkerAsync(ShowUpdateUI);
			else if (ShowUpdateUI)
				MessageBox.Show("Update in progress! Please wait!");

			// var informalVersion = (Assembly.GetExecutingAssembly().GetCustomAttributes(
			//typeof(AssemblyInformationalVersionAttribute), false).FirstOrDefault() as AssemblyInformationalVersionAttribute).InformationalVersion;
		}

		private void UpdaterWorker_DoWork(object sender, DoWorkEventArgs e) {
			Updater updater = new Updater("http://update.better-explorer.com/update.xml", 5, UpdateCheckType == 1);
			if (updater.LoadUpdateFile()) {
				if ((bool)e.Argument) {
					Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
						(Action)(() => {
							UpdateWizard updateWizzard = new UpdateWizard(updater);
							updateWizzard.ShowDialog(this.GetWin32Window());
						}));
				}
				else {
					Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
						(Action)(() => {
							stiUpdate.Content = FindResource("stUpdateAvailableCP").ToString().Replace("VER", updater.AvailableUpdates[0].Version);
							stiUpdate.Foreground = System.Windows.Media.Brushes.Red;
						}));
				}
			}
			else {
				Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
						(Action)(() => {
							stiUpdate.Content = FindResource("stUpdateNotAvailableCP").ToString();
							stiUpdate.Foreground = System.Windows.Media.Brushes.Black;
							if ((bool)e.Argument)
								MessageBox.Show(FindResource("stUpdateNotAvailableCP").ToString());
						}));
			}

			Utilities.SetRegistryValue("LastUpdateCheck", DateTime.Now.ToBinary(), RegistryValueKind.QWord);
			LastUpdateCheck = DateTime.Now;
		}

		private void updateCheckTimer_Tick(object sender, EventArgs e) {
			if (DateTime.Now.Subtract(LastUpdateCheck).Days >= UpdateCheckInterval) {
				CheckForUpdate(false);
			}
		}
	}
}