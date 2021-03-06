﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using BExplorer.Shell;

namespace Wpf.Controls {

	[TemplatePart(Name = "PART_CloseButton", Type = typeof(ButtonBase))]
	public class TabItem : System.Windows.Controls.TabItem {

		#region Properties
		public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(object), typeof(TabItem), new UIPropertyMetadata(null));
		public static readonly DependencyProperty AllowDeleteProperty = DependencyProperty.Register("AllowDelete", typeof(bool), typeof(TabItem), new UIPropertyMetadata(true));

		public List<String> SelectedItems = new List<String>();
		//TODO: Fix [log] The Navigation Log starts out null!
		public NavigationLog log = new NavigationLog();
		public ShellItem ShellObject { get; set; }
		public ContextMenu mnu { get; set; }


		/// <summary>
		///     Used by the TabPanel for sizing
		/// </summary>
		internal Dimension Dimension { get; set; }

		/// <summary>
		/// Provides a place to display an Icon on the Header and on the DropDown Context Menu
		/// </summary>
		public object Icon {
			get { return (object)GetValue(IconProperty); }
			set { SetValue(IconProperty, value); }
		}

		/// <summary>
		/// Allow the Header to be Deleted by the end user
		/// </summary>
		public bool AllowDelete {
			get { return (bool)GetValue(AllowDeleteProperty); }
			set { SetValue(AllowDeleteProperty, value); }
		}


		#endregion Properties

		static TabItem() {
			DefaultStyleKeyProperty.OverrideMetadata(typeof(Wpf.Controls.TabItem), new FrameworkPropertyMetadata(typeof(Wpf.Controls.TabItem)));
		}

		/// <summary>
		/// OnApplyTemplate override
		/// </summary>
		public override void OnApplyTemplate() {
			base.OnApplyTemplate();

			// wire up the CloseButton's Click event if the button exists
			ButtonBase button = this.Template.FindName("PART_CloseButton", this) as ButtonBase;
			if (button != null) {
				button.PreviewMouseLeftButtonDown += (sender, e) => {
					// get the parent tabcontrol
					TabControl tc = Helper.FindParentControl<TabControl>(this);
					if (tc == null)
						return;
					// remove this tabitem from the parent tabcontrol
					tc.RemoveTabItem(this);
				};
			}

			this.PreviewMouseDoubleClick += TabItem_PreviewMouseDoubleClick;
			this.MouseRightButtonUp += TabItem_MouseRightButtonUp;
		}

		/// <summary>
		/// OnMouseEnter, Create and Display a Tooltip
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e) {
			base.OnMouseEnter(e);

			this.ToolTip = this.ShellObject.GetDisplayName(BExplorer.Shell.Interop.SIGDN.DESKTOPABSOLUTEEDITING);
			e.Handled = true;
		}

		/// <summary>
		/// OnMouseLeave, remove the Tooltip
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e) {
			base.OnMouseLeave(e);

			this.ToolTip = null;
			e.Handled = true;
		}

		private void TabItem_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			e.Handled = true;
		}

		private void TabItem_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			TabControl tc = Helper.FindParentControl<TabControl>(this);
			if (tc == null) return;

			TabItem firstItem = tc.Items.OfType<TabItem>().FirstOrDefault();
			this.mnu = new ContextMenu();

			Action<string, RoutedEventHandler> Worker = (x, y) => {
				MenuItem Item = new MenuItem();
				Item.Header = x;
				Item.Click += y;
				this.mnu.Items.Add(Item);
			};

			Worker("Close current tab", new RoutedEventHandler((owner, a) => tc.RemoveTabItem(this)));

			Worker("Close all tabs", new RoutedEventHandler(
				(owner, a) => {
					foreach (TabItem tabItem in tc.Items.OfType<TabItem>().ToArray()) {
						if (!tabItem.Equals(firstItem)) tc.RemoveTabItem(tabItem);
					}
				}));

			Worker("Close all other tabs", new RoutedEventHandler((owner, a) => tc.CloseAllTabsButThis(this)));

			this.mnu.Items.Add(new Separator());

			Worker("New tab", new RoutedEventHandler((owner, a) => tc.NewTab()));
			Worker("Clone tab", new RoutedEventHandler((owner, a) => tc.CloneTabItem(this)));

			this.mnu.Items.Add(new Separator());

			//TODO: fix reopetab
			MenuItem miundocloser = new MenuItem();
			miundocloser.Header = "Undo close tab";
			miundocloser.IsEnabled = tc.ReopenableTabs.Count > 0;
			miundocloser.Tag = "UCTI";
			miundocloser.Click += new RoutedEventHandler((owner, a) => tc.ReOpenTab(tc.ReopenableTabs.Last()));

			this.mnu.Items.Add(miundocloser);
			this.mnu.Items.Add(new Separator());

			//TODO: Fix Context Menu Item [Open in new window]
			Worker("Open in new window", new RoutedEventHandler(
				(owner, a) => {
					System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName, this.ShellObject.ParsingName + " /nw");
					tc.RemoveTabItem(this);
				}));

			if (this.mnu != null && this.mnu.Items.Count > 0) {
				this.mnu.Placement = PlacementMode.Bottom;
				this.mnu.PlacementTarget = this;
				this.mnu.IsOpen = true;
			}
		}
	}
}