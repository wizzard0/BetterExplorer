﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.ComponentModel;
using mshtml;
using System.Runtime.InteropServices;
using System.Reflection;



namespace WpfDocumentPreviewer {

	/// <summary>
	/// Interaction logic for PreviewerControl.xaml
	/// </summary>
	public partial class PreviewerControl : UserControl, INotifyPropertyChanged {

		#region Implement INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;
		public void RaisePropertyChanged(string propertyName) {
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion

		private Bitmap imageSrc;
		private List<string> Images = new List<string>(new string[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".wmf" });

		public ImageSource ImageSource { get { return IconFromFileName(fileName); } }

		private string fileName;
		public string FileName {
			get { return System.IO.Path.GetFileName(fileName); }
			set {
				fileName = value;
				SetFileName(fileName);
				if (!String.IsNullOrEmpty(fileName)) {
					RaisePropertyChanged("ImageSource");
				}
				RaisePropertyChanged("FileName");
			}
		}

		/*
		public static ImageSource BitmapFromUri(Uri source) {
			var bitmap = new BitmapImage();
			bitmap.BeginInit();
			bitmap.UriSource = source;
			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			bitmap.EndInit();
			return bitmap;
		}
		*/

		public PreviewerControl() {
			InitializeComponent();
			this.Unloaded += PreviewerControl_Unloaded;
		}

		private void SetFileName(string fileName) {
			if (!String.IsNullOrEmpty(fileName)) {

				Guid? previewGuid = Guid.Empty;
				if (previewHandlerHost1.Open(fileName, out previewGuid) == false) {
					if ((previewGuid != null && previewGuid.Value != Guid.Empty) || !Images.Contains(System.IO.Path.GetExtension(fileName))) {
						wb1.Visibility = Visibility.Visible;
						var activeX = wb1.GetType().InvokeMember("ActiveXInstance",
							BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
							null, wb1, new object[] { }) as SHDocVw.WebBrowser;
						activeX.FileDownload += activeX_FileDownload;
						wb1.Navigate(fileName);
						imgh1.Visibility = System.Windows.Visibility.Collapsed;
						wfh1.Visibility = Visibility.Collapsed;
					}
					else {
						wb1.Visibility = Visibility.Collapsed;
						imageSrc = new Bitmap(fileName);

						img1.Image = imageSrc;
						imgh1.Visibility = System.Windows.Visibility.Visible;
						wfh1.Visibility = Visibility.Collapsed;
					}
				}
				else {
					imgh1.Visibility = System.Windows.Visibility.Collapsed;
					wb1.Visibility = Visibility.Collapsed;
					wfh1.Visibility = Visibility.Visible;
				}
			}
			else {
				Guid? previewGuid = Guid.Empty;
				previewHandlerHost1.Open(fileName, out previewGuid);
				wb1.Visibility = Visibility.Collapsed;
				wfh1.Visibility = Visibility.Visible;
				imgh1.Visibility = System.Windows.Visibility.Collapsed;
				img1.Image = null;
				if (imageSrc != null)
					imageSrc.Dispose();
			}

		}

		void activeX_FileDownload(bool ActiveDocument, ref bool Cancel) {
			if (!ActiveDocument) {
				Cancel = true;
				this.FileName = null;
			}
		}



		void PreviewerControl_Unloaded(object sender, RoutedEventArgs e) {
			previewHandlerHost1.Dispose();
		}

		internal BitmapSource IconFromFileName(string fileName) {
			BitmapImage bmpImage = new BitmapImage();
			if (fileName != null && fileName.Contains(".")) {
				try {
					System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(fileName);
					Bitmap bmp = icon.ToBitmap();
					MemoryStream strm = new MemoryStream();
					bmp.Save(strm, System.Drawing.Imaging.ImageFormat.Png);
					bmpImage.BeginInit();
					strm.Seek(0, SeekOrigin.Begin);
					bmpImage.StreamSource = strm;
					bmpImage.EndInit();
				}
				catch { }
			}

			return bmpImage;
		}

		private void wb1_LoadCompleted(object sender, NavigationEventArgs e) {
			if (wb1.Document == null) return;
			try {
				mshtml.IHTMLDocument2 doc = (mshtml.IHTMLDocument2)wb1.Document;
				if (doc.title == "Navigation Canceled") {
					wb1.Visibility = Visibility.Collapsed;
					wfh1.Visibility = Visibility.Visible;
				}
			}
			catch { }

		}


	}
}
