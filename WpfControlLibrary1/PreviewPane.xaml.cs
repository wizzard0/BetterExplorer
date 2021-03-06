﻿using BExplorer.Shell.Interop;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BExplorer.Shell;

namespace BetterExplorerControls {
	/*
	Make this work like the normal File Info Pop-up
	
	File -> Right-Click -> Properties
	*/


	//Finish!!!!

	/// <summary> Interaction logic for PreviewPane.xaml </summary>
	public partial class DetailsPane : UserControl, INotifyPropertyChanged {
		//private ShellItem[] SelectedItems;
		public event PropertyChangedEventHandler PropertyChanged;
		public ShellView Browser;
		//private BitmapSource Thumbnail { get; set; }
		private ShellItem SelectedItem { get { return this.Browser != null && this.Browser.GetSelectedCount() > 0 ? this.Browser.SelectedItems[0] : null; } }
		//private String DisplayName { get { return SelectedItem != null ? SelectedItem.DisplayName : String.Empty; } }
		//private String FileSize { get { return "FileSize: Not Coded"; } }
		//private String FileCreated { get { return "FileCreated: Not Coded"; } }
		//private String FileModified { get { return "FileModified: Not Coded"; } }


		//private String FileType {
		//	get {
		//		if (SelectedItem	 == null) {
		//			return String.Empty;
		//		}

		//		return SelectedItem.GetPropertyValue(new PROPERTYKEY() { fmtid = Guid.Parse("B725F130-47EF-101A-A5F1-02608C9EEBAC"), pid = 4 }, typeof(String)).Value.ToString();
		//	}
		//}




		public DetailsPane() {
			InitializeComponent();
			DataContext = this;
			this.Loaded += (sender, e) => this.SizeChanged += PreviewPane_SizeChanged; ;
		}

		private void Setup_PreviewPane() {
			Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)(() => {
				if (SelectedItem != null) {
					if (Browser.SelectedItems.Count == 0) return;
					var File = new System.IO.FileInfo(Browser.SelectedItems[0].ParsingName);

					this.SelectedItem.Thumbnail.CurrentSize = new System.Windows.Size(this.ActualHeight - 20, this.ActualHeight - 20);
					this.SelectedItem.Thumbnail.FormatOption = BExplorer.Shell.Interop.ShellThumbnailFormatOption.Default;
					this.SelectedItem.Thumbnail.RetrievalOption = BExplorer.Shell.Interop.ShellThumbnailRetrievalOption.Default;
					icon.Source = this.SelectedItem.Thumbnail.BitmapSource;


					txtDisplayName.Text = File.Name;
					txtFileType.Text = File.Extension;
					txtFileSize.Text = File.Length.ToString();
					txtFileCreated.Text = File.CreationTime.ToLongDateString();
					txtFileModified.Text = File.LastWriteTime.ToLongDateString();
				}
			}));
		}


		private void PreviewPane_SizeChanged(object sender, SizeChangedEventArgs e) {
			if (this.ActualHeight == 0)
				return;

			Setup_PreviewPane();
			//Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)(() =>
			//{

			/*
			ShellItem selectedItemsFirst = null;
			if (this.Browser != null && this.Browser.GetSelectedCount() == 1) {
				selectedItemsFirst = this.Browser.SelectedItems.First();
				selectedItemsFirst.Thumbnail.CurrentSize = new System.Windows.Size(this.ActualHeight - 20, this.ActualHeight - 20);
				selectedItemsFirst.Thumbnail.FormatOption = BExplorer.Shell.Interop.ShellThumbnailFormatOption.Default;
				selectedItemsFirst.Thumbnail.RetrievalOption = BExplorer.Shell.Interop.ShellThumbnailRetrievalOption.Default;
				icon.Source = selectedItemsFirst.Thumbnail.BitmapSource;


				txtDisplayName.Text = DisplayName;
			}
			*/

			//}));
		}
		public void FillPreviewPane(ShellView browser) {
			if (this.Browser == null)
				this.Browser = browser;

			Setup_PreviewPane();
			/*
			Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)(() => {
				if (this.Browser.GetSelectedCount() == 1) {
					this.SelectedItems = this.Browser.SelectedItems.ToArray();
					this.SelectedItems[0].Thumbnail.CurrentSize = new System.Windows.Size(this.ActualHeight - 20, this.ActualHeight - 20);
					this.SelectedItems[0].Thumbnail.FormatOption = BExplorer.Shell.Interop.ShellThumbnailFormatOption.Default;
					this.SelectedItems[0].Thumbnail.RetrievalOption = BExplorer.Shell.Interop.ShellThumbnailRetrievalOption.Default;
					icon.Source = this.SelectedItems[0].Thumbnail.BitmapSource;

					txtDisplayName.Text = DisplayName;
				}
			}));
			*/
		}
	}
}