﻿
using BExplorer.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace BetterExplorerControls
{
  /// <summary>
  /// Interaction logic for PreviewPane.xaml
  /// </summary>
  public partial class PreviewPane : UserControl, INotifyPropertyChanged
  {
    public ShellView Browser;
    private BitmapSource _thumbnail;
    private ShellItem[] SelectedItems;
    public BitmapSource Thumbnail { 
      get{
        return _thumbnail;
      }
    }

    
    public String DisplayName
    {
      get
      {
		  if (this.Browser == null) 
			  return String.Empty;
        if (this.Browser.GetSelectedCount() > 0)
        {
          return this.Browser.SelectedItems[0].DisplayName;
        }
        else
        {
          return String.Empty;
        }
      }
    }

    public String FileType
    {
      get
      {
				//FIXME: fix this here
        //if (this.Browser.SelectedItems.Count() > 0)
        //{
        //  return this.Browser.SelectedItems[0].Properties.System.ItemTypeText != null ? this.Browser.SelectedItems[0].Properties.System.ItemTypeText.Value : String.Empty;
        //}
        //else
        //{
        //  return String.Empty;
        //}
				return string.Empty;
      }
    }
    public PreviewPane()
    {
      InitializeComponent();
      DataContext = this;
      this.Loaded += PreviewPane_Loaded;
    }

    void PreviewPane_Loaded(object sender, RoutedEventArgs e)
    {
      this.SizeChanged += PreviewPane_SizeChanged;
    }


    void PreviewPane_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      //Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)(() =>
      //{
        ShellItem selectedItemsFirst = null;
				if (this.Browser != null && this.Browser.GetSelectedCount() == 1)
        {
          selectedItemsFirst = this.Browser.SelectedItems.First();
          selectedItemsFirst.Thumbnail.CurrentSize = new Size(this.ActualHeight - 20, this.ActualHeight - 20);
          icon.Source = selectedItemsFirst.Thumbnail.BitmapSource;
        }

      //}));
    }


    public void FillPreviewPane(ShellView browser)
    {
      Dispatcher.BeginInvoke(DispatcherPriority.Background, (ThreadStart)(() =>
          {
            if (this.Browser == null)
              this.Browser = browser;

						if (this.Browser.GetSelectedCount() == 1)
						{
                this.SelectedItems = this.Browser.SelectedItems.ToArray();
                this.SelectedItems[0].Thumbnail.CurrentSize = new Size(this.ActualHeight - 20, this.ActualHeight - 20);
                icon.Source = this.SelectedItems[0].Thumbnail.BitmapSource;
            }

            this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("DisplayName"));
            this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("FileType"));
            
          }));
      
    }

    public event PropertyChangedEventHandler PropertyChanged;
  }
}
