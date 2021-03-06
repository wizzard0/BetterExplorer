﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BExplorer.Shell.Interop;
using Microsoft.Win32;

namespace BExplorer.Shell {

	#region Substructures and classes

	/// <summary> Specifies how list items are displayed in a <see cref="ShellView" /> control. </summary>
	public enum ShellViewStyle {

		/// <summary> Items appear in a grid and icon size is 256x256 </summary>
		ExtraLargeIcon,

		/// <summary> Items appear in a grid and icon size is 96x96 </summary>
		LargeIcon,

		/// <summary> Each item appears as a full-sized icon with a label below it. </summary>
		Medium,

		/// <summary> Each item appears as a small icon with a label to its right. </summary>
		SmallIcon,

		/// <summary>
		/// Each item appears as a small icon with a label to its right. Items are arranged in columns.
		/// </summary>
		List,

		/// <summary>
		/// Each item appears on a separate line with further information about each item arranged
		/// in columns. The left-most column contains a small icon and label.
		/// </summary>
		Details,

		/// <summary> Each item appears with a thumbnail picture of the file's content. </summary>
		Thumbnail,

		/// <summary>
		/// Each item appears as a full-sized icon with the item label and file information to the
		/// right of it.
		/// </summary>
		Tile,

		/// <summary>
		/// Each item appears in a thumbstrip at the bottom of the control, with a large preview of
		/// the seleted item appearing above.
		/// </summary>
		Thumbstrip,

		/// <summary> Each item appears in a item that occupy the whole view width </summary>
		Content,
	}

	public class NavigatedEventArgs : EventArgs, IDisposable {
		private ShellItem m_Folder;

		public void Dispose() {
			if (m_Folder != null) {
				m_Folder.Dispose();
				m_Folder = null;
			}
		}

		public NavigatedEventArgs(ShellItem folder) {
			m_Folder = folder;
		}

		/// <summary> The folder that is navigated to. </summary>
		public ShellItem Folder {
			get { return m_Folder; }
			set { m_Folder = value; }
		}
	}

	/// <summary> Provides information for the <see cref="ShellView.Navigating" /> event. </summary>
	public class NavigatingEventArgs : EventArgs, IDisposable {
		private bool m_Cancel;

		private ShellItem m_Folder;

		public void Dispose() {
			if (m_Folder != null) {
				m_Folder.Dispose();
				m_Folder = null;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NavigatingEventArgs"/>
		/// class.
		/// </summary>
		///
		/// <param name="folder">
		/// The folder being navigated to.
		/// </param>
		public NavigatingEventArgs(ShellItem folder) {
			m_Folder = folder;
		}

		/// <summary> Gets/sets a value indicating whether the navigation should be cancelled. </summary>
		public bool Cancel {
			get { return m_Cancel; }
			set { m_Cancel = value; }
		}

		/// <summary> The folder being navigated to. </summary>
		public ShellItem Folder {
			get { return m_Folder; }
			set { m_Folder = value; }
		}
	}

	public class ItemDisplayedEventArgs : EventArgs, IDisposable {
		public ShellItem DisplayedItem { get; private set; }
		public int DisplayedItemIndex { get; private set; }

		public ItemDisplayedEventArgs(ShellItem item, int index) {
			this.DisplayedItem = item;
			this.DisplayedItemIndex = index;
		}

		#region IDisposable Members

		public void Dispose() {
			if (DisplayedItem != null) {
				DisplayedItem.Dispose();
				DisplayedItem = null;
			}
		}

		#endregion IDisposable Members
	}

	public enum ItemUpdateType {
		Renamed,
		Created,
		Deleted,
		Updated
	}

	public class ItemUpdatedEventArgs : EventArgs {
		public ItemUpdateType UpdateType { get; private set; }
		public ShellItem PreviousItem { get; private set; }
		public ShellItem NewItem { get; private set; }

		public int NewItemIndex { get; private set; }

		public ItemUpdatedEventArgs(ItemUpdateType type, ShellItem newItem, ShellItem previousItem, int index) {
			this.UpdateType = type;
			this.NewItem = newItem;
			this.PreviousItem = previousItem;
			this.NewItemIndex = index;
		}
	}

	public class ViewChangedEventArgs : EventArgs {
		private Int32 m_ThumbnailSize;
		private ShellViewStyle m_View;

		public ViewChangedEventArgs(ShellViewStyle view, Int32? thumbnailSize) {
			m_View = view;
			if (thumbnailSize != null)
				m_ThumbnailSize = thumbnailSize.Value;
		}

		/// <summary> The current ViewStyle </summary>
		public ShellViewStyle CurrentView {
			get { return m_View; }
			set { m_View = value; }
		}

		public Int32 ThumbnailSize {
			get { return m_ThumbnailSize; }
			set { m_ThumbnailSize = value; }
		}
	}

	#endregion Substructures and classes

	/// <summary> The ShellFileListView class that visualise contents of a directory </summary>
	public partial class ShellView : UserControl {

		#region Event Handler

		private void OnViewChanged(ViewChangedEventArgs e) {
			if (ViewStyleChanged != null) {
				ViewStyleChanged(this, e);
			}
		}

		private async void OnNavigated(NavigatedEventArgs e) {
			if (Navigated != null) {
				Navigated(this, e);
			}
			//this.FolderSizes.Clear();
			//LPCSHCOLUMNINIT lpi = new LPCSHCOLUMNINIT();
			//lpi.wszFolder = e.Folder.ParsingName + "\0";
			//if (ICP != null)
			//{
			//	await Task.Run(() =>
			//	{
			//		this.BeginInvoke(new MethodInvoker(() =>
			//		{
			//			this.FolderSizes.Clear();
			//			ICP.Initialize(lpi);
			//			foreach (var item in this.CurrentFolder)
			//			{
			//				var pn = item.ParsingName;
			//				LPCSHCOLUMNID lid = new LPCSHCOLUMNID();
			//				lid.fmtid = Guid.Parse("04DAAD08-70EF-450E-834A-DCFAF9B48748");
			//				lid.pid = 0;
			//				LPCSHCOLUMNDATA ldata = new LPCSHCOLUMNDATA();
			//				ldata.dwFileAttributes = item.IsFolder ? (uint)FileAttributes.Directory : 0;
			//				//ldata.dwFileAttributes = (uint)FileAttributes.Directory;
			//				if (!item.IsFolder)
			//				{
			//					ldata.pwszExt = Path.GetExtension(item.ParsingName);
			//				}
			//				ldata.wszFile = pn + "\0";
			//				object o = 0;
			//				this.ICP.GetItemData(lid, ldata, out o);
			//				if (o != null)
			//				{
			//					if (this.FolderSizes.ContainsKey(pn))
			//						this.FolderSizes[pn] = o.ToString();
			//					else
			//						this.FolderSizes.TryAdd(pn, o.ToString());
			//				}

			// Thread.Sleep(1); //Application.DoEvents();

			//			}
			//		}));
			//	});
			//}
		}

		/// <summary> Occurs when the control gains focus </summary>
		public event EventHandler GotFocus;

		public event EventHandler<ItemDisplayedEventArgs> ItemDisplayed;

		private void OnItemDisplayed(ShellItem item, int index) {
			if (ItemDisplayed != null) {
				ItemDisplayed(this, new ItemDisplayedEventArgs(item, index));
			}
		}

		public event EventHandler<NavigatingEventArgs> Navigating;

		/// <summary> Triggers the Navigating event. </summary>
		public virtual void OnNavigating(NavigatingEventArgs ea) {
			EventHandler<NavigatingEventArgs> handler = Navigating;
			if (handler != null)
				handler(this, ea);
		}

		/// <summary> Occurs when the control loses focus </summary>
		public event EventHandler LostFocus;

		/// <summary> Occurs when the <see cref="ShellView" /> control navigates to a new folder. </summary>
		public event EventHandler<NavigatedEventArgs> Navigated;

		/// <summary>
		/// Occurs when the <see cref="ShellView"/>'s current selection
		/// changes.
		/// </summary>
		///
		/// <remarks>
		/// <b>Important:</b> When <see cref="ShowWebView"/> is set to
		/// <see langref="true"/>, this event will not occur. This is due to
		/// a limitation in the underlying windows control.
		/// </remarks>
		public event EventHandler SelectionChanged;

		public event EventHandler<ItemUpdatedEventArgs> ItemUpdated;

		public event EventHandler<ViewChangedEventArgs> ViewStyleChanged;

		public event EventHandler<NavigatedEventArgs> ItemMiddleClick;

		/// <summary>
		/// Occurs when the user right-clicks on the blank area of the column header area
		/// </summary>
		public event MouseEventHandler ColumnHeaderRightClick;

		/// <summary> Raised whenever a key is pressed </summary>
		public new event KeyEventHandler KeyDown;

		/// <summary>
		/// Raised whenever a key is pressed, with the intention of doing a key jump. Please use
		/// <see cref="KeyDown" /> to catch when any key is pressed.
		/// </summary>
		public event KeyEventHandler KeyJumpKeyDown;

		/// <summary> Raised when the timer finishes for the Key Jump timer. </summary>
		public event EventHandler KeyJumpTimerDone;

		#endregion Event Handler

		#region Public Members

		public Boolean IsGroupsEnabled { get; set; }

		public ToolTip ToolTip;

		public List<string> RecommendedPrograms(string ext) {
			List<string> progs = new List<string>();

			using (RegistryKey rk = Registry.ClassesRoot.OpenSubKey(ext + @"\OpenWithList")) {
				if (rk != null) {
					foreach (string item in rk.GetSubKeyNames()) {
						progs.Add(item);
					}
				}
			}

			using (RegistryKey rk = Registry.ClassesRoot.OpenSubKey(ext + @"\OpenWithProgids")) {
				if (rk != null) {
					foreach (string item in rk.GetValueNames())
						progs.Add(item);
				}
			}

			return progs;
		}

		/// <summary>
		/// Returns the index of the first item whose display name starts with the search string.
		/// </summary>
		/// <param name="search"> The string for which to search for. </param>
		/// <returns> The index of an item within the list view. </returns>
		public int GetFirstIndexOf(string search) {
			return GetFirstIndexOf(search, 0);
		}

		/// <summary> Returns the key jump string as it currently is. </summary>
		public string KeyJumpString {
			get { return _keyjumpstr; }
		}
		public List<Collumns> AllAvailableColumns = new List<Collumns>();
		public List<Collumns> Collumns = new List<Collumns>();
		public List<ListViewGroupEx> Groups = new List<ListViewGroupEx>();
		public ShellNotifications Notifications = new ShellNotifications();

		/// <summary>
		/// Gets a value indicating whether a previous page in navigation history is available,
		/// which allows the <see cref="ShellView.NavigateBack" /> method to succeed.
		/// </summary>
		[Browsable(false)]
		public bool CanNavigateBack {
			get { return History.CanNavigateBack; }
		}

		/// <summary>
		/// Gets a value indicating whether a subsequent page in navigation history is available,
		/// which allows the <see cref="ShellView.NavigateForward" /> method to succeed.
		/// </summary>
		[Browsable(false)]
		public bool CanNavigateForward {
			get { return History.CanNavigateForward; }
		}

		/// <summary>
		/// Gets a value indicating whether the folder currently being browsed by the <see
		/// cref="ShellView" /> has parent folder which can be navigated to by calling <see
		/// cref="NavigateParent" />.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool CanNavigateParent {
			get {
				return m_CurrentFolder != ShellItem.Desktop;
			}
		}

		/// <summary>
		/// Gets/sets a <see cref="ShellItem" /> describing the folder currently being browsed by
		/// the <see cref="ShellView" />.
		/// </summary>
		[Browsable(false)]
		public ShellItem CurrentFolder {
			get { return m_CurrentFolder; }
			set {
				//if (value != m_CurrentFolder)
				//{
				//	Navigate(value);
				//}
				m_CurrentFolder = value;
			}
		}

		// <summary> Gets the <see cref="ShellView" />'s navigation history. </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ShellHistory History { get; private set; }

		public int IconSize { get; private set; }

		public List<ShellItem> Items { get; set; }

		public Dictionary<ShellItem, int> ItemsHashed { get; set; }

		public int LastSortedColumnIndex { get; set; }

		public SortOrder LastSortOrder { get; set; }

		public Collumns LastGroupCollumn { get; set; }

		public SortOrder LastGroupOrder { get; set; }

		public IntPtr LVHandle { get; set; }

		public List<LVItemColor> LVItemsColorCodes { get; set; }

		public List<ShellItem> SelectedItems {
			get {
				List<ShellItem> selItems = new List<ShellItem>();
				foreach (var index in this.SelectedIndexes) {
					selItems.Add(this.Items[index]);
					DraggedItemIndexes.Add(index);
				}
				return selItems;
			}
		}

		public List<int> SelectedIndexes {
			get {
				List<int> selItems = new List<int>();
				int iStart = -1;
				LVITEMINDEX lvi = new LVITEMINDEX();
				while (lvi.iItem != -1) {
					lvi.iItem = iStart;
					lvi.iGroup = this.GetGroupIndex(iStart);
					User32.SendMessage(this.LVHandle, LVM.GETNEXTITEMINDEX, ref lvi, LVNI.LVNI_SELECTED);
					iStart = lvi.iItem;
					if (lvi.iItem != -1) {
						selItems.Add(lvi.iItem);
					}
				}

				return selItems;
			}
		}

		public Boolean ShowCheckboxes {
			get {
				return _showCheckBoxes;
			}
			set {
				if (value) {
					User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SetExtendedStyle, (int)ListViewExtendedStyles.CheckBoxes, (int)ListViewExtendedStyles.CheckBoxes);
					User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SetExtendedStyle, (int)ListViewExtendedStyles.LVS_EX_AUTOCHECKSELECT, (int)ListViewExtendedStyles.LVS_EX_AUTOCHECKSELECT);
				}
				else {
					User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SetExtendedStyle, (int)ListViewExtendedStyles.CheckBoxes, 0);
					User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SetExtendedStyle, (int)ListViewExtendedStyles.LVS_EX_AUTOCHECKSELECT, 0);
				}
				_showCheckBoxes = value;
			}
		}

		public Boolean ShowHidden {
			get {
				return _ShowHidden;
			}
			set {
				_ShowHidden = value;
				this.RefreshContents();
			}
		}
		/// <summary> Gets or sets how items are displayed in the control. </summary>
		[DefaultValue(ShellViewStyle.Medium), Category("Appearance")]
		public ShellViewStyle View {
			get { return m_View; }
			set {
				m_View = value;
				var iconsize = 16;
				switch (value) {
					case ShellViewStyle.ExtraLargeIcon:
						User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETVIEW, (int)LV_VIEW.LV_VIEW_ICON, 0);
						ResizeIcons(256);
						iconsize = 256;
						RefreshItemsCountInternal();
						break;

					case ShellViewStyle.LargeIcon:
						User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETVIEW, (int)LV_VIEW.LV_VIEW_ICON, 0);
						ResizeIcons(96);
						iconsize = 96;
						RefreshItemsCountInternal();
						break;

					case ShellViewStyle.Medium:
						User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETVIEW, (int)LV_VIEW.LV_VIEW_ICON, 0);
						ResizeIcons(48);
						iconsize = 48;
						RefreshItemsCountInternal();
						break;

					case ShellViewStyle.SmallIcon:
						User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETVIEW, (int)LV_VIEW.LV_VIEW_SMALLICON, 0);
						ResizeIcons(16);
						iconsize = 16;
						RefreshItemsCountInternal();
						break;

					case ShellViewStyle.List:
						User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETVIEW, (int)LV_VIEW.LV_VIEW_LIST, 0);
						ResizeIcons(16);
						iconsize = 16;
						RefreshItemsCountInternal();
						break;

					case ShellViewStyle.Details:
						User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETVIEW, (int)LV_VIEW.LV_VIEW_DETAILS, 0);
						ResizeIcons(16);
						iconsize = 16;
						RefreshItemsCountInternal();
						break;

					case ShellViewStyle.Thumbnail:
						break;

					case ShellViewStyle.Tile:
						User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETVIEW, (int)LV_VIEW.LV_VIEW_TILE, 0);
						ResizeIcons(48);
						iconsize = 48;
						RefreshItemsCountInternal();
						//LVTILEVIEWINFO tvi = new LVTILEVIEWINFO();
						//tvi.cbSize = Marshal.SizeOf(typeof(LVTILEVIEWINFO));
						//tvi.dwMask = (int)LVTVIM.LVTVIM_COLUMNS | (int)LVTVIM.LVTVIM_TILESIZE;
						//tvi.dwFlags = (int)LVTVIF.LVTVIF_AUTOSIZE;
						//tvi.cLines = 4;
						//var a = User32.SendMessage(this.LVHandle, (int)BExplorer.Shell.Interop.MSG.LVM_SETTILEVIEWINFO, 0, tvi);
						break;

					case ShellViewStyle.Thumbstrip:
						break;

					case ShellViewStyle.Content:
						break;

					default:
						break;
				}
				if (value != ShellViewStyle.Details)
					AutosizeAllColumns(-2);
				else
					AutosizeAllColumns(-1);
				OnViewChanged(new ViewChangedEventArgs(value, iconsize));
			}
		}

		public int CurrentRefreshedItemIndex = -1;	
		#endregion Public Members

		#region Private Members

		//private ShellHistory m_History;
		//private int _iconSize;
		private Boolean _showCheckBoxes = false;

		private Boolean _ShowHidden;
		private Boolean _IsInRenameMode = false;
		private System.Windows.Forms.Timer _ResetTimer = new System.Windows.Forms.Timer();
		private Thread MaintenanceThread;
		private List<Int32> DraggedItemIndexes = new List<int>();
		private System.Windows.Forms.Timer _KeyJumpTimer = new System.Windows.Forms.Timer();
		private string _keyjumpstr = "";
		private ShellItem _kpreselitem = null;
		private LVIS _IsDragSelect = 0;
		private BackgroundWorker bw = new BackgroundWorker();
		private ConcurrentDictionary<int, Bitmap> cache = new ConcurrentDictionary<int, Bitmap>();
		private Thread _IconCacheLoadingThread;
		private Boolean Cancel = false;
		private Bitmap ExeFallBack16;
		private Bitmap ExeFallBack256;
		private Bitmap ExeFallBack32;
		private Bitmap ExeFallBack48;
		private Bitmap Shield16;
		private Bitmap Shield256;
		private Bitmap Shield32;
		private Bitmap Shield48;
		private int ShieldIconIndex;
		private ImageList extra = new ImageList(ImageListSize.ExtraLarge);
		private List<int> IndexesWithThumbnail = new List<int>();
		private bool IsDoubleNavFinished = false;
		private ImageList jumbo = new ImageList(ImageListSize.Jumbo);
		private ImageList large = new ImageList(ImageListSize.Large);
		private ShellItem m_CurrentFolder;
		private ShellViewStyle m_View;
		private SyncQueue<int> overlayQueue = new SyncQueue<int>(3000);
		private Dictionary<int, int> overlays = new Dictionary<int, int>();
		private Thread _OverlaysLoadingThread;
		private Thread _ShieldLoadingThread;
		private System.Windows.Forms.Timer selectionTimer = new System.Windows.Forms.Timer();
		private Dictionary<int, int> shieldedIcons = new Dictionary<int, int>();
		private SyncQueue<int> shieldQueue = new SyncQueue<int>(3000);
		private ImageList small = new ImageList(ImageListSize.SystemSmall);
		private Thread _IconLoadingThread;
		private Thread _UpdateSubitemValuesThread;
		private ConcurrentDictionary<int, ConcurrentDictionary<Collumns, object>> SubItems = new ConcurrentDictionary<int, ConcurrentDictionary<Collumns, object>>();
		private SyncQueue<int> ThumbnailsForCacheLoad = new SyncQueue<int>(5000);
		private SyncQueue<Tuple<int, int, PROPERTYKEY>> ItemsForSubitemsUpdate = new SyncQueue<Tuple<int, int, PROPERTYKEY>>(5000);
		private List<int> cachedIndexes = new List<int>();
		private ConcurrentBag<Tuple<int, PROPERTYKEY, object>> SubItemValues = new ConcurrentBag<Tuple<int, PROPERTYKEY, object>>();
		private ManualResetEvent resetEvent = new ManualResetEvent(true);
		private SyncQueue<int> waitingThumbnails = new SyncQueue<int>(3000);
		private List<int> _CuttedIndexes = new List<int>();
		private int LastI = 0;
		private int CurrentI = 0;
		private const int SW_SHOW = 5;
		private const uint SEE_MASK_INVOKEIDLIST = 12;
		private int _LastSelectedIndexByDragDrop = -1;
		
		#endregion Private Members

		#region Initializer

		/// <summary> Main constructor </summary>
		public ShellView() {
			this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque | ControlStyles.OptimizedDoubleBuffer | ControlStyles.EnableNotifyMessage, true);
			InitializeComponent();
			this.Items = new List<ShellItem>();
			this.LVItemsColorCodes = new List<LVItemColor>();
			this.AllAvailableColumns = this.AvailableColumns();
			this.AllowDrop = true;
			_IconLoadingThread = new Thread(_IconsLoadingThreadRun) { IsBackground = true, Priority = ThreadPriority.AboveNormal };
			_IconLoadingThread.Start();
			_IconCacheLoadingThread = new Thread(_IconCacheLoadingThreadRun) { IsBackground = true, Priority = ThreadPriority.Normal };
			_IconCacheLoadingThread.SetApartmentState(ApartmentState.STA);
			_IconCacheLoadingThread.Start();
			_OverlaysLoadingThread = new Thread(_OverlaysLoadingThreadRun) { IsBackground = true, Priority = ThreadPriority.BelowNormal };
			_OverlaysLoadingThread.Start();
			_ShieldLoadingThread = new Thread(_ShieldLoadingThreadRun) { IsBackground = true, Priority = ThreadPriority.BelowNormal };
			_ShieldLoadingThread.Start();
			_UpdateSubitemValuesThread = new Thread(_UpdateSubitemValuesThreadRun) { IsBackground = true, Priority = ThreadPriority.Normal };
			_UpdateSubitemValuesThread.Start();
			History = new ShellHistory();
			_ResetTimer.Interval = 200;
			_ResetTimer.Tick += resetTimer_Tick;

			Shell32.SHSTOCKICONINFO defIconInfo = new Shell32.SHSTOCKICONINFO() { cbSize = (uint)Marshal.SizeOf(typeof(Shell32.SHSTOCKICONINFO)) };

			Shell32.SHGetStockIconInfo(Shell32.SHSTOCKICONID.SIID_APPLICATION, Shell32.SHGSI.SHGSI_SYSICONINDEX, ref defIconInfo);
			ExeFallBack48 = extra.GetIcon(defIconInfo.iSysIconIndex).ToBitmap();
			ExeFallBack256 = jumbo.GetIcon(defIconInfo.iSysIconIndex).ToBitmap();
			ExeFallBack16 = small.GetIcon(defIconInfo.iSysIconIndex).ToBitmap();

			Shell32.SHGetStockIconInfo(Shell32.SHSTOCKICONID.SIID_SHIELD, Shell32.SHGSI.SHGSI_SYSICONINDEX, ref defIconInfo);
			Shield48 = extra.GetIcon(defIconInfo.iSysIconIndex).ToBitmap();
			Shield256 = jumbo.GetIcon(defIconInfo.iSysIconIndex).ToBitmap();
			Shield16 = small.GetIcon(defIconInfo.iSysIconIndex).ToBitmap();
			ShieldIconIndex = defIconInfo.iSysIconIndex;

			this.KeyDown += ShellView_KeyDown;
			this.MouseUp += ShellView_MouseUp;
			this.GotFocus += ShellView_GotFocus;
		}

		#endregion Initializer

		#region Events

		private void selectionTimer_Tick(object sender, EventArgs e) {
			if (MouseButtons != System.Windows.Forms.MouseButtons.Left) {
				//RedrawWindow();
				OnSelectionChanged();
				if (KeyJumpTimerDone != null) {
					KeyJumpTimerDone(this, EventArgs.Empty);
				}
				(sender as System.Windows.Forms.Timer).Stop();
			}
		}

		private void _KeyJumpTimer_Tick(object sender, EventArgs e) {
			if (KeyJumpTimerDone != null) {
				KeyJumpTimerDone(this, EventArgs.Empty);
			}

			_KeyJumpTimer.Enabled = false;

			//process key jump
			DeSelectAllItems();
			int startindex = 0;
			if (_kpreselitem != null) {
				if (_kpreselitem.GetDisplayName(SIGDN.NORMALDISPLAY).ToUpperInvariant().StartsWith(_keyjumpstr.ToUpperInvariant())) {
					startindex = Items.IndexOf(_kpreselitem) + 1;
				}
			}

			int selind = GetFirstIndexOf(_keyjumpstr, startindex);
			if (selind != -1) {
				SelectItemByIndex(selind, true);
			}

			_keyjumpstr = "";
		}

		private void resetTimer_Tick(object sender, EventArgs e) {
			(sender as System.Windows.Forms.Timer).Stop();
			resetEvent.Set();
			//RedrawWindow();
			//GC.WaitForPendingFinalizers();
			//GC.Collect();
			Shell32.SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
		}

		private void ShellView_MouseUp(object sender, MouseEventArgs e) {
			if (_IsDragSelect == LVIS.LVIS_SELECTED) {
				if (selectionTimer.Enabled) {
					selectionTimer.Stop();
					selectionTimer.Start();
				}
				else {
					selectionTimer.Start();
				}
			}
		}

		private void ShellView_GotFocus(object sender, EventArgs e) {
			this.Focus();
			User32.SetForegroundWindow(this.LVHandle);
		}

		private void ShellView_KeyDown(object sender, KeyEventArgs e) {
			if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
				switch (e.KeyCode) {
					case Keys.A:
						SelectAll();
						break;

					case Keys.Add:
						break;

					case Keys.Alt:
						break;

					case Keys.Apps:
						break;

					case Keys.Attn:
						break;

					case Keys.B:
						break;

					case Keys.Back:
						break;

					case Keys.BrowserBack:
						break;

					case Keys.BrowserFavorites:
						break;

					case Keys.BrowserForward:
						break;

					case Keys.BrowserHome:
						break;

					case Keys.BrowserRefresh:
						break;

					case Keys.BrowserSearch:
						break;

					case Keys.BrowserStop:
						break;

					case Keys.C:
						this.CopySelectedFiles();
						break;

					case Keys.Cancel:
						break;

					case Keys.Capital:
						break;

					case Keys.Clear:
						break;

					case Keys.Control:
						break;

					case Keys.ControlKey:
						break;

					case Keys.Crsel:
						break;

					case Keys.D:
						DeSelectAllItems();
						break;

					case Keys.D0:
						break;

					case Keys.D1:
						break;

					case Keys.D2:
						break;

					case Keys.D3:
						break;

					case Keys.D4:
						break;

					case Keys.D5:
						break;

					case Keys.D6:
						break;

					case Keys.D7:
						break;

					case Keys.D8:
						break;

					case Keys.D9:
						break;

					case Keys.Decimal:
						break;

					case Keys.Delete:
						break;

					case Keys.Divide:
						break;

					case Keys.Down:
						break;

					case Keys.E:
						break;

					case Keys.End:
						break;

					case Keys.Enter:
						break;

					case Keys.EraseEof:
						break;

					case Keys.Escape:
						break;

					case Keys.Execute:
						break;

					case Keys.Exsel:
						break;

					case Keys.F:
						break;

					case Keys.F1:
						break;

					case Keys.F10:
						break;

					case Keys.F11:
						break;

					case Keys.F12:
						break;

					case Keys.F13:
						break;

					case Keys.F14:
						break;

					case Keys.F15:
						break;

					case Keys.F16:
						break;

					case Keys.F17:
						break;

					case Keys.F18:
						break;

					case Keys.F19:
						break;

					case Keys.F2:
						break;

					case Keys.F20:
						break;

					case Keys.F21:
						break;

					case Keys.F22:
						break;

					case Keys.F23:
						break;

					case Keys.F24:
						break;

					case Keys.F3:
						break;

					case Keys.F4:
						break;

					case Keys.F5:
						break;

					case Keys.F6:
						break;

					case Keys.F7:
						break;

					case Keys.F8:
						break;

					case Keys.F9:
						break;

					case Keys.FinalMode:
						break;

					case Keys.G:
						break;

					case Keys.H:
						break;

					case Keys.HanguelMode:
						break;

					case Keys.HanjaMode:
						break;

					case Keys.Help:
						break;

					case Keys.Home:
						break;

					case Keys.I:
						InvertSelection();
						break;

					case Keys.IMEAccept:
						break;

					case Keys.IMEConvert:
						break;

					case Keys.IMEModeChange:
						break;

					case Keys.IMENonconvert:
						break;

					case Keys.Insert:
						break;

					case Keys.J:
						break;

					case Keys.JunjaMode:
						break;

					case Keys.K:
						break;

					case Keys.KeyCode:
						break;

					case Keys.L:
						break;

					case Keys.LButton:
						break;

					case Keys.LControlKey:
						break;

					case Keys.LMenu:
						break;

					case Keys.LShiftKey:
						break;

					case Keys.LWin:
						break;

					case Keys.LaunchApplication1:
						break;

					case Keys.LaunchApplication2:
						break;

					case Keys.LaunchMail:
						break;

					case Keys.Left:
						break;

					case Keys.LineFeed:
						break;

					case Keys.M:
						break;

					case Keys.MButton:
						break;

					case Keys.MediaNextTrack:
						break;

					case Keys.MediaPlayPause:
						break;

					case Keys.MediaPreviousTrack:
						break;

					case Keys.MediaStop:
						break;

					case Keys.Menu:
						break;

					case Keys.Modifiers:
						break;

					case Keys.Multiply:
						break;

					case Keys.N:
						break;

					case Keys.NoName:
						break;

					case Keys.None:
						break;

					case Keys.NumLock:
						break;

					case Keys.NumPad0:
						break;

					case Keys.NumPad1:
						break;

					case Keys.NumPad2:
						break;

					case Keys.NumPad3:
						break;

					case Keys.NumPad4:
						break;

					case Keys.NumPad5:
						break;

					case Keys.NumPad6:
						break;

					case Keys.NumPad7:
						break;

					case Keys.NumPad8:
						break;

					case Keys.NumPad9:
						break;

					case Keys.O:
						break;

					case Keys.Oem1:
						break;

					case Keys.Oem102:
						break;

					case Keys.Oem2:
						break;

					case Keys.Oem3:
						break;

					case Keys.Oem4:
						break;

					case Keys.Oem5:
						break;

					case Keys.Oem6:
						break;

					case Keys.Oem7:
						break;

					case Keys.Oem8:
						break;

					case Keys.OemClear:
						break;

					case Keys.OemMinus:
						break;

					case Keys.OemPeriod:
						break;

					case Keys.Oemcomma:
						break;

					case Keys.Oemplus:
						break;

					case Keys.P:
						break;

					case Keys.Pa1:
						break;

					case Keys.PageDown:
						break;

					case Keys.PageUp:
						break;

					case Keys.Pause:
						break;

					case Keys.Play:
						break;

					case Keys.Print:
						break;

					case Keys.PrintScreen:
						break;

					case Keys.ProcessKey:
						break;

					case Keys.Q:
						break;

					case Keys.R:
						break;

					case Keys.RButton:
						break;

					case Keys.RControlKey:
						break;

					case Keys.RMenu:
						break;

					case Keys.RShiftKey:
						break;

					case Keys.RWin:
						break;

					case Keys.Right:
						break;

					case Keys.S:
						break;

					case Keys.Scroll:
						break;

					case Keys.Select:
						break;

					case Keys.SelectMedia:
						break;

					case Keys.Separator:
						break;

					case Keys.Shift:
						break;

					case Keys.ShiftKey:
						break;

					case Keys.Space:
						break;

					case Keys.Subtract:
						break;

					case Keys.T:
						break;

					case Keys.Tab:
						break;

					case Keys.U:
						AsyncUnbuffCopy copy = new AsyncUnbuffCopy();
						copy.AsyncCopyFileUnbuffered(@"J:\Downloads\advinst.msi", @"J:\Downloads\advinst(2).msi", true, false, false, 4, false, 100000);
						break;

					case Keys.Up:
						break;

					case Keys.V:
						this.PasteAvailableFiles();
						break;

					case Keys.VolumeDown:
						break;

					case Keys.VolumeMute:
						break;

					case Keys.VolumeUp:
						break;

					case Keys.W:
						break;

					case Keys.X:
						this.CutSelectedFiles();
						break;

					case Keys.XButton1:
						break;

					case Keys.XButton2:
						break;

					case Keys.Y:
						break;

					case Keys.Z:
						break;

					case Keys.Zoom:
						break;

					default:
						break;
				}
			}
			if (e.KeyCode == Keys.Escape) {
				foreach (var index in this._CuttedIndexes) {
					LVITEM item = new LVITEM();
					item.mask = LVIF.LVIF_STATE;
					item.stateMask = LVIS.LVIS_CUT;
					item.state = 0;
					User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMSTATE, index, ref item);
				}
				this._CuttedIndexes.Clear();
				System.Windows.Forms.Clipboard.Clear();
			}
			if (e.KeyCode == Keys.Delete) {
				this.DeleteSelectedFiles((Control.ModifierKeys & Keys.Shift) != Keys.Shift);
			}
			if (e.KeyCode == Keys.F5) {
				this.RefreshContents();
			}
		}

		#endregion Events

		#region Overrides

		protected override void OnDragDrop(System.Windows.Forms.DragEventArgs e) {
			int row = -1;
			int collumn = -1;
			this.HitTest(PointToClient(new System.Drawing.Point(e.X, e.Y)), out row, out collumn);
			ShellItem destination = null;
			if (row != -1) {
				destination = this.Items[row];
			}
			else {
				destination = this.CurrentFolder;
			}
			switch (e.Effect) {
				case System.Windows.Forms.DragDropEffects.All:
					break;

				case System.Windows.Forms.DragDropEffects.Copy:
					this.DoCopy(e.Data, destination);
					break;

				case System.Windows.Forms.DragDropEffects.Link:
					System.Windows.MessageBox.Show("link");
					break;

				case System.Windows.Forms.DragDropEffects.Move:
					this.DoMove(e.Data, destination);
					break;

				case System.Windows.Forms.DragDropEffects.None:
					break;

				case System.Windows.Forms.DragDropEffects.Scroll:
					break;

				default:
					break;
			}

			IDropTargetHelper dropHelper = (IDropTargetHelper)new DragDropHelper();
			Win32Point wp = new Win32Point();
			wp.x = e.X;
			wp.y = e.Y;

			if (e.Data.GetDataPresent("DragImageBits"))
				dropHelper.Drop((System.Runtime.InteropServices.ComTypes.IDataObject)e.Data, ref wp, (int)e.Effect);

			if (_LastSelectedIndexByDragDrop != -1 & !DraggedItemIndexes.Contains(_LastSelectedIndexByDragDrop)) {
				this.DeselectItemByIndex(_LastSelectedIndexByDragDrop);
			}
		}

		protected override void OnDragLeave(EventArgs e) {
			IDropTargetHelper dropHelper = (IDropTargetHelper)new DragDropHelper();
			dropHelper.DragLeave();
		}

		protected override void OnDragOver(System.Windows.Forms.DragEventArgs e) {
			if ((e.KeyState & (8 + 32)) == (8 + 32) &&
			(e.AllowedEffect & System.Windows.Forms.DragDropEffects.Link) == System.Windows.Forms.DragDropEffects.Link) {
				// KeyState 8 + 32 = CTL + ALT

				// Link drag-and-drop effect.
				e.Effect = System.Windows.Forms.DragDropEffects.Link;
			}
			else if ((e.KeyState & 32) == 32 &&
				(e.AllowedEffect & System.Windows.Forms.DragDropEffects.Link) == System.Windows.Forms.DragDropEffects.Link) {
				// ALT KeyState for link.
				e.Effect = System.Windows.Forms.DragDropEffects.Link;
			}
			else if ((e.KeyState & 4) == 4 &&
				(e.AllowedEffect & System.Windows.Forms.DragDropEffects.Move) == System.Windows.Forms.DragDropEffects.Move) {
				// SHIFT KeyState for move.
				e.Effect = System.Windows.Forms.DragDropEffects.Move;
			}
			else if ((e.KeyState & 8) == 8 &&
				(e.AllowedEffect & System.Windows.Forms.DragDropEffects.Copy) == System.Windows.Forms.DragDropEffects.Copy) {
				// CTL KeyState for copy.
				e.Effect = System.Windows.Forms.DragDropEffects.Copy;
			}
			else if ((e.AllowedEffect & System.Windows.Forms.DragDropEffects.Move) == System.Windows.Forms.DragDropEffects.Move) {
				// By default, the drop action should be move, if allowed.
				e.Effect = System.Windows.Forms.DragDropEffects.Move;
			}
			else
				e.Effect = System.Windows.Forms.DragDropEffects.None;

			IDropTargetHelper dropHelper = (IDropTargetHelper)new DragDropHelper();
			Win32Point wp = new Win32Point();
			wp.x = e.X;
			wp.y = e.Y;

			int row = -1;
			int collumn = -1;
			this.HitTest(PointToClient(new System.Drawing.Point(e.X, e.Y)), out row, out collumn);

			if (_LastSelectedIndexByDragDrop != -1 && !DraggedItemIndexes.Contains(_LastSelectedIndexByDragDrop)) {
				this.DeselectItemByIndex(_LastSelectedIndexByDragDrop);
			}

			if (row != -1) {
				this.SelectItemByIndex(row);
			}
			else {
				if (_LastSelectedIndexByDragDrop != -1 & !DraggedItemIndexes.Contains(_LastSelectedIndexByDragDrop)) {
					this.DeselectItemByIndex(_LastSelectedIndexByDragDrop);
				}
			}
			_LastSelectedIndexByDragDrop = row;

			if (e.Data.GetDataPresent("DragImageBits"))
				dropHelper.DragOver(ref wp, (int)e.Effect);
		}

		protected override void OnDragEnter(System.Windows.Forms.DragEventArgs e) {
			if ((e.KeyState & (8 + 32)) == (8 + 32) &&
					(e.AllowedEffect & System.Windows.Forms.DragDropEffects.Link) == System.Windows.Forms.DragDropEffects.Link) {
				// KeyState 8 + 32 = CTL + ALT

				// Link drag-and-drop effect.
				e.Effect = System.Windows.Forms.DragDropEffects.Link;
			}
			else if ((e.KeyState & 32) == 32 &&
				(e.AllowedEffect & System.Windows.Forms.DragDropEffects.Link) == System.Windows.Forms.DragDropEffects.Link) {
				// ALT KeyState for link.
				e.Effect = System.Windows.Forms.DragDropEffects.Link;
			}
			else if ((e.KeyState & 4) == 4 &&
				(e.AllowedEffect & System.Windows.Forms.DragDropEffects.Move) == System.Windows.Forms.DragDropEffects.Move) {
				// SHIFT KeyState for move.
				e.Effect = System.Windows.Forms.DragDropEffects.Move;
			}
			else if ((e.KeyState & 8) == 8 &&
				(e.AllowedEffect & System.Windows.Forms.DragDropEffects.Copy) == System.Windows.Forms.DragDropEffects.Copy) {
				// CTL KeyState for copy.
				e.Effect = System.Windows.Forms.DragDropEffects.Copy;
			}
			else if ((e.AllowedEffect & System.Windows.Forms.DragDropEffects.Move) == System.Windows.Forms.DragDropEffects.Move) {
				// By default, the drop action should be move, if allowed.
				e.Effect = System.Windows.Forms.DragDropEffects.Move;
			}
			else
				e.Effect = System.Windows.Forms.DragDropEffects.None;

			IDropTargetHelper dropHelper = (IDropTargetHelper)new DragDropHelper();
			Win32Point wp = new Win32Point();
			wp.x = e.X;
			wp.y = e.Y;

			if (e.Data.GetDataPresent("DragImageBits"))
				dropHelper.DragEnter(this.Handle, (System.Runtime.InteropServices.ComTypes.IDataObject)e.Data, ref wp, (int)e.Effect);
		}

		protected override void OnQueryContinueDrag(System.Windows.Forms.QueryContinueDragEventArgs e) {
			base.OnQueryContinueDrag(e);
		}

		protected override void OnGiveFeedback(System.Windows.Forms.GiveFeedbackEventArgs e) {
			e.UseDefaultCursors = false;
			Cursor.Current = Cursors.Arrow;
			base.OnGiveFeedback(e);
			System.Windows.Forms.Application.DoEvents();
		}

		private void InsertNewItem(ShellItem obj)
		{
			if (!Items.Contains(obj) && !String.IsNullOrEmpty(obj.ParsingName))
			{
				Items.Add(obj);
				this.SetSortCollumn(this.LastSortedColumnIndex, this.LastSortOrder, false);
				if (this.IsGroupsEnabled)
				{
					SetGroupOrder(false);
				}
				
			}
			
		}
		private void UpdateItem(ShellItem obj1, ShellItem obj2)
		{
			if (this.CurrentRefreshedItemIndex != -1)
			{
				ShellItem tempItem = Items.SingleOrDefault(s => s.CachedParsingName == obj2.CachedParsingName);
				if (tempItem == null)
				{
					Items.Insert(this.CurrentRefreshedItemIndex == -1 ? 0 : CurrentRefreshedItemIndex, obj2);
					ItemsHashed.Add(obj2, this.CurrentRefreshedItemIndex == -1 ? 0 : CurrentRefreshedItemIndex);
					//this.SelectItemByIndex(this.CurrentRefreshedItemIndex == -1 ? 0 :CurrentRefreshedItemIndex);
					if (this.IsGroupsEnabled)
					{
						this.SetGroupOrder(false);
					}
					this.SetSortCollumn(this.LastSortedColumnIndex, this.LastSortOrder, false);

					if (this.ItemUpdated != null)
						this.ItemUpdated.Invoke(this, new ItemUpdatedEventArgs(ItemUpdateType.Created, obj2, null, this.CurrentRefreshedItemIndex == -1 ? 0 : CurrentRefreshedItemIndex));


				}
			}
			else
			{
				//if (this.CurrentRefreshedItemIndex == -1)
				//{
				ShellItem theItem = Items.SingleOrDefault(s => s.ParsingName == obj1.ParsingName);
				if (theItem != null)
				{
					int itemIndex = Items.IndexOf(theItem);
					Items[itemIndex] = obj2;
					ItemsHashed.Remove(theItem);
					ItemsHashed.Add(obj2, itemIndex);
					User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_UPDATE, itemIndex, 0);
					if (this.IsGroupsEnabled)
					{
						this.SetGroupOrder(false);
					}
					this.SetSortCollumn(this.LastSortedColumnIndex, this.LastSortOrder, false);
					RedrawWindow();
					if (this.ItemUpdated != null)
						this.ItemUpdated.Invoke(this, new ItemUpdatedEventArgs(ItemUpdateType.Renamed, obj2, obj1, itemIndex));
				}
			}
			//}
			this.CurrentRefreshedItemIndex = -1;
		}
		protected override void WndProc(ref Message m)
		{
			bool isSmallIcons = (View == ShellViewStyle.List || View == ShellViewStyle.SmallIcon || View == ShellViewStyle.Details);
			if (m.Msg == (int)WM.WM_PARENTNOTIFY) {
				if (User32.LOWORD((int)m.WParam) == (int)WM.WM_MBUTTONDOWN) {
					OnItemMiddleClick();
				}
			}

			if (m.Msg == ShellNotifications.WM_SHNOTIFY) {
				if (Notifications.NotificationReceipt(m.WParam, m.LParam)) {
					//var info = (NotifyInfos)Notifications.NotificationsReceived[Notifications.NotificationsReceived.Count - 1];
					foreach (NotifyInfos info in Notifications.NotificationsReceived.ToArray()) {
						if (info.Notification == ShellNotifications.SHCNE.SHCNE_CREATE || info.Notification == ShellNotifications.SHCNE.SHCNE_MKDIR) {
							var obj = new ShellItem(info.Item1);
							if (obj.Extension.ToLowerInvariant() != ".tmp")
							{
								if (obj.Parent.Equals(this.CurrentFolder))
								{
									InsertNewItem(obj);
									if (this.ItemUpdated != null)
										this.ItemUpdated.Invoke(this, new ItemUpdatedEventArgs(ItemUpdateType.Created, obj, null, this.Items.Count - 1));
								}
							}
							Notifications.NotificationsReceived.Remove(info);
						}
						if (info.Notification == ShellNotifications.SHCNE.SHCNE_DELETE || info.Notification == ShellNotifications.SHCNE.SHCNE_RMDIR) {
							var obj = new ShellItem(info.Item1);
							if (!String.IsNullOrEmpty(obj.ParsingName))
							{
								ShellItem theItem = Items.SingleOrDefault(s => s.ParsingName == obj.ParsingName);
								if (theItem != null) {
									Items.Remove(theItem);
									ItemsHashed.Remove(theItem);
									if (this.IsGroupsEnabled)
									{
										this.SetGroupOrder(false);
									}
									this.SetSortCollumn(this.LastSortedColumnIndex, this.LastSortOrder, false);

									if (this.ItemUpdated != null)
										this.ItemUpdated.Invoke(this, new ItemUpdatedEventArgs(ItemUpdateType.Deleted, obj, null, -1));
								}
								
							}
							Notifications.NotificationsReceived.Remove(info);
						}

						if (info.Notification == ShellNotifications.SHCNE.SHCNE_RENAMEFOLDER || info.Notification == ShellNotifications.SHCNE.SHCNE_RENAMEITEM) {
							var obj1 = new ShellItem(info.Item1);
							var obj2 = new ShellItem(info.Item2);
							if (!String.IsNullOrEmpty(obj1.ParsingName) && !String.IsNullOrEmpty(obj2.ParsingName)) {
								UpdateItem(obj1, obj2);
							}
							Notifications.NotificationsReceived.Remove(info);
						}

						if (info.Notification == ShellNotifications.SHCNE.SHCNE_DRIVEADD)
						{
							var obj = new ShellItem(info.Item1);

							if (this.CurrentFolder.Equals(KnownFolders.Computer))
							{
								this.InsertNewItem(obj);
							}

							Notifications.NotificationsReceived.Remove(info);

						}
						if (info.Notification == ShellNotifications.SHCNE.SHCNE_DRIVEREMOVED)
						{
							var obj = new ShellItem(info.Item1);

							if (this.CurrentFolder.Equals(KnownFolders.Computer))
							{
									Items.Remove(obj);
									ItemsHashed.Remove(obj);
									if (this.IsGroupsEnabled)
									{
										this.SetGroupOrder(false);
									}
									User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMCOUNT, this.Items.Count, 0);
							}

							Notifications.NotificationsReceived.Remove(info);

						}
					}
				}
			}
			base.WndProc(ref m);
			if (m.Msg == 78) {
				var nmhdrHeader = (NMHEADER)(m.GetLParam(typeof(NMHEADER)));
				if (nmhdrHeader.hdr.code == (int)HDN.HDN_DROPDOWN) {
					System.Windows.Forms.MessageBox.Show(nmhdrHeader.iItem.ToString());
				}
				if (nmhdrHeader.hdr.code == (int)HDN.HDN_BEGINTRACKW) {
					if (this.View != ShellViewStyle.Details)
						m.Result = (IntPtr)1;
				}
				if (nmhdrHeader.hdr.code == (int)HDN.HDN_BEGINTRACKW) {
					if (this.View != ShellViewStyle.Details)
						m.Result = (IntPtr)1;
				}

				NMHDR nmhdr = new NMHDR();
				nmhdr = (NMHDR)m.GetLParam(nmhdr.GetType());
				switch ((int)nmhdr.code) {
					case WNM.LVN_ENDLABELEDITW:
						var nmlvedit = (NMLVDISPINFO)m.GetLParam(typeof(NMLVDISPINFO));
						if (!String.IsNullOrEmpty(nmlvedit.item.pszText)) {
							RenameShellItem(this.Items[nmlvedit.item.iItem].m_ComInterface, nmlvedit.item.pszText);
							this.RedrawWindow();
						}
						break;

					case WNM.LVN_GETDISPINFOW:
						var nmlv = (NMLVDISPINFO)m.GetLParam(typeof(NMLVDISPINFO));
						//if ((nmlv.item.mask & LVIF.LVIF_COLUMNS) == LVIF.LVIF_COLUMNS)
						//{
						//	int[] varArray = {0,1,2,3};
						//	IntPtr ptr = Marshal.AllocHGlobal(varArray.Length*Marshal.SizeOf(varArray[0]));
						//	Marshal.Copy(varArray,0,ptr, varArray.Length);
						//	nmlv.item.cColumns = varArray.Length;
						//	nmlv.item.puColumns = (uint)ptr;
						//	Marshal.StructureToPtr(nmlv, m.LParam, false);
						//}
						var currentItem = Items[nmlv.item.iItem];
						if ((nmlv.item.mask & LVIF.LVIF_TEXT) == LVIF.LVIF_TEXT) {
							if (nmlv.item.iSubItem == 0) {
								nmlv.item.pszText = this.View == ShellViewStyle.Tile ? String.Empty : currentItem.DisplayName;
								Marshal.StructureToPtr(nmlv, m.LParam, false);
							}
							else {
								if (isSmallIcons) {
									try {
										var hash = currentItem.GetHashCode();
										Collumns currentCollumn = this.Collumns[nmlv.item.iSubItem];
										var valueCached = SubItemValues.ToArray().FirstOrDefault(s => s.Item1 == hash && s.Item2.fmtid == currentCollumn.pkey.fmtid && s.Item2.pid == currentCollumn.pkey.pid);
										if (valueCached != null && valueCached.Item3 != null) {
											String val = String.Empty;
											if (currentCollumn.CollumnType == typeof(DateTime)) {
												val = ((DateTime)valueCached.Item3).ToString(Thread.CurrentThread.CurrentCulture);
											}
											else if (currentCollumn.CollumnType == typeof(long)) {
												val = String.Format("{0} KB", (Math.Ceiling(Convert.ToDouble(valueCached.Item3.ToString()) / 1024).ToString("# ### ### ##0"))); //ShlWapi.StrFormatByteSize(Convert.ToInt64(pvar.Value.ToString()));
											}
											else if (currentCollumn.CollumnType == typeof(PerceivedType)) {
												val = ((PerceivedType)valueCached.Item3).ToString();
											}
											else {
												val = valueCached.Item3.ToString();
											}
											nmlv.item.pszText = val;
											Marshal.StructureToPtr(nmlv, m.LParam, false);
										}
										else {
											ShellItem temp = null;
											if (!(currentItem.IsNetDrive || currentItem.IsNetworkPath) && !currentItem.ParsingName.StartsWith("::")) {
												temp = new ShellItem(currentItem.ParsingName);
											}
											else {
												temp = currentItem;
											}
											IShellItem2 isi2 = (IShellItem2)temp.m_ComInterface;
											Guid guid = new Guid(InterfaceGuids.IPropertyStore);
											IPropertyStore propStore = null;
											isi2.GetPropertyStore(GetPropertyStoreOptions.FastPropertiesOnly, ref guid, out propStore);
											PROPERTYKEY pk = currentCollumn.pkey;
											PropVariant pvar = new PropVariant();
											if (propStore != null) {
												if (propStore.GetValue(ref pk, pvar) == HResult.S_OK) {
													String val = String.Empty;
													if (pvar.Value != null) {
														if (currentCollumn.CollumnType == typeof(DateTime)) {
															val = ((DateTime)pvar.Value).ToString(Thread.CurrentThread.CurrentCulture);
														}
														else if (currentCollumn.CollumnType == typeof(long)) {
															val = String.Format("{0} KB", (Math.Ceiling(Convert.ToDouble(pvar.Value.ToString()) / 1024).ToString("# ### ### ##0"))); //ShlWapi.StrFormatByteSize(Convert.ToInt64(pvar.Value.ToString()));
														}
														else if (currentCollumn.CollumnType == typeof(PerceivedType)) {
															val = ((PerceivedType)pvar.Value).ToString();
														}
														else {
															val = pvar.Value.ToString();
														}
														nmlv.item.pszText = val;
														Marshal.StructureToPtr(nmlv, m.LParam, false);
														pvar.Dispose();
													}
													else {
														ItemsForSubitemsUpdate.Enqueue(new Tuple<int, int, PROPERTYKEY>(nmlv.item.iItem, nmlv.item.iSubItem, pk));
													}
												}
											}
										}
									}
									catch {
									}
									//var currentItem = Items[nmlv.item.iItem];
									//var hash = currentItem.GetHashCode();
									//if (hash != null)
									//{
									//	ConcurrentDictionary<Collumns, object> dictionaryValues = null;
									//	if (SubItems.TryGetValue(hash, out dictionaryValues))
									//	{
									//		Collumns currentCollumn = this.Collumns[nmlv.item.iSubItem];
									//		object value = null;
									//		if (dictionaryValues.TryGetValue(currentCollumn, out value))
									//		{
									//			String val = String.Empty;
									//			if (value != null)
									//			{
									//				if (currentCollumn.CollumnType == typeof(DateTime))
									//				{
									//					val = ((DateTime)value).ToString(Thread.CurrentThread.CurrentCulture);
									//				}
									//				else if (currentCollumn.CollumnType == typeof(long))
									//				{
									//					val = String.Format("{0} KB", (Math.Ceiling(Convert.ToDouble(value.ToString()) / 1024).ToString("# ### ### ##0"))); //ShlWapi.StrFormatByteSize(Convert.ToInt64(pvar.Value.ToString()));
									//				}
									//				else
									//				{
									//					val = value.ToString();
									//				}
									//			}
									//			nmlv.item.pszText = val;
									//			Marshal.StructureToPtr(nmlv, m.LParam, false);
									//		}
									//		else
									//		{
									//			ItemsForSubitemsUpdate.Enqueue(nmlv.item.iItem);
									//		}
									//		dictionaryValues = null;
									//	}
									//	else
									//	{
									//		ItemsForSubitemsUpdate.Enqueue(nmlv.item.iItem);
									//	}
									//}
								}
							}
						}
						if (!currentItem.IsInitialised) {
							currentItem.IsInitialised = true;
							OnItemDisplayed(currentItem, nmlv.item.iItem);
						}
						break;

					case WNM.LVN_COLUMNCLICK:
						NMLISTVIEW nlcv = (NMLISTVIEW)m.GetLParam(typeof(NMLISTVIEW));
						if (this.IsGroupsEnabled)
						{
							if (this.LastGroupCollumn == this.Collumns[nlcv.iSubItem])
							{
								this.SetGroupOrder();
							}
							else
							{
								SetSortCollumn(nlcv.iSubItem, this.LastSortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending);
								this.SetGroupOrder(false);
							}
						}
						else
							SetSortCollumn(nlcv.iSubItem, this.LastSortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending);
						break;

					case WNM.LVN_GETINFOTIP:
						NMLVGETINFOTIP nmGetInfoTip = (NMLVGETINFOTIP)m.GetLParam(typeof(NMLVGETINFOTIP));
						var itemInfotip = this.Items[nmGetInfoTip.iItem];
						char[] charBuf = ("\0").ToCharArray();
						Marshal.Copy(charBuf, 0, nmGetInfoTip.pszText, Math.Min(charBuf.Length, nmGetInfoTip.cchTextMax));
						Marshal.StructureToPtr(nmGetInfoTip, m.LParam, false);		

						if (ToolTip.IsVisible)
							ToolTip.HideTooltip();
		
						ToolTip.CurrentItem = itemInfotip;
						ToolTip.ItemIndex = nmGetInfoTip.iItem;
						ToolTip.Type = nmGetInfoTip.dwFlags;
						ToolTip.Left = Cursor.Position.X;
						ToolTip.Top = Cursor.Position.Y;
						ToolTip.ShowTooltip();

						break;

					case WNM.LVN_ODFINDITEM:
						if (this.ToolTip != null && this.ToolTip.IsVisible)
							this.ToolTip.HideTooltip();
						var findItem = (NMLVFINDITEM)m.GetLParam(typeof(NMLVFINDITEM));
						_keyjumpstr = findItem.lvfi.psz;

						if (KeyJumpKeyDown != null) {
							KeyJumpKeyDown(this, new KeyEventArgs(Keys.A));
						}
						int startindex = findItem.iStart;
						int selind = GetFirstIndexOf(_keyjumpstr, startindex);
						if (selind != -1) {
							m.Result = (IntPtr)(selind);
						}
						else {
							int selindOver = GetFirstIndexOf(_keyjumpstr, 0);
							if (selindOver != -1) {
								m.Result = (IntPtr)(selindOver);
							}
						}

						break;

					case WNM.LVN_INCREMENTALSEARCH:
						var incrementalSearch = (NMLVFINDITEM)m.GetLParam(typeof(NMLVFINDITEM));
						break;

					case WNM.LVN_ITEMACTIVATE:
						if (this.ToolTip != null && this.ToolTip.IsVisible)
							this.ToolTip.HideTooltip();
						var iac = new NMITEMACTIVATE();
						iac = (NMITEMACTIVATE)m.GetLParam(iac.GetType());
						try {
							ShellItem selectedItem = Items[iac.iItem];
							if (selectedItem.IsFolder) {
								Navigate(selectedItem);
							}
							else {
								if (selectedItem.IsLink && selectedItem.ParsingName.EndsWith(".lnk")) {
									var shellLink = new ShellLink(selectedItem.ParsingName);
									var newSho = new ShellItem(shellLink.TargetPIDL);
									if (newSho.IsFolder) {
										Navigate(newSho);
									}
									else {
										StartProcessInCurrentDirectory(newSho);
									}
									shellLink.Dispose();
								}
								else {
									StartProcessInCurrentDirectory(selectedItem);
								}
							}
						}
						catch (Exception) {
						}
						break;

					case WNM.LVN_BEGINSCROLL:
						resetEvent.Reset();
						_ResetTimer.Stop();
						this.Cancel = true;
						ToolTip.HideTooltip();
						ThumbnailsForCacheLoad.Clear();
						overlayQueue.Clear();
						shieldQueue.Clear();
						//! to be revised this for performace
						try {
							if (MaintenanceThread != null && MaintenanceThread.IsAlive)
								MaintenanceThread.Abort();
							MaintenanceThread = new Thread(() => {
								while (ItemsForSubitemsUpdate.queue.Count > 0) {
									//Thread.Sleep(1);
									var item = ItemsForSubitemsUpdate.Dequeue();
									var itemBounds = new User32.RECT();
									LVITEMINDEX lvi = new LVITEMINDEX();
									lvi.iItem = item.Item1;
									lvi.iGroup = this.GetGroupIndex(item.Item1);
									User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMINDEXRECT, ref lvi, ref itemBounds);
									Rectangle r = new Rectangle(itemBounds.Left, itemBounds.Top, itemBounds.Right - itemBounds.Left, itemBounds.Bottom - itemBounds.Top);
									if (r.IntersectsWith(this.ClientRectangle)) {
										ItemsForSubitemsUpdate.Enqueue(item);
									}
								}

								while (waitingThumbnails.queue.Count > 0) {
									//Thread.Sleep(1);
									var iconIndex = waitingThumbnails.Dequeue();
									var itemBounds = new User32.RECT();
									LVITEMINDEX lvi = new LVITEMINDEX();
									lvi.iItem = iconIndex;
									lvi.iGroup = this.GetGroupIndex(iconIndex);
									User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMINDEXRECT, ref lvi, ref itemBounds);
									Rectangle r = new Rectangle(itemBounds.Left, itemBounds.Top, itemBounds.Right - itemBounds.Left, itemBounds.Bottom - itemBounds.Top);
									if (r.IntersectsWith(this.ClientRectangle)) {
										waitingThumbnails.Enqueue(iconIndex);
									}
								}
							});
							MaintenanceThread.Start();
						}
						catch (ThreadAbortException) {
						}
						GC.Collect();

						break;

					case WNM.LVN_ENDSCROLL:
						this.Cancel = false;
						_ResetTimer.Start();
						//Shell32.SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
						break;

					case -100:
						System.Windows.Forms.MessageBox.Show("AM");
						break;

					case WNM.LVN_ITEMCHANGED:
						//RedrawWindow();
						NMLISTVIEW nlv = (NMLISTVIEW)m.GetLParam(typeof(NMLISTVIEW));
						if ((nlv.uChanged & LVIF.LVIF_STATE) == LVIF.LVIF_STATE) {
							ToolTip.HideTooltip();
							selectionTimer.Interval = 100;
							selectionTimer.Tick += selectionTimer_Tick;
							this._IsDragSelect = nlv.uNewState;
							if (IsGroupsEnabled) {
								if (nlv.iItem != -1) {
									var itemBounds = new User32.RECT();
									LVITEMINDEX lvi = new LVITEMINDEX();
									lvi.iItem = nlv.iItem;
									lvi.iGroup = this.GetGroupIndex(nlv.iItem);
									User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMINDEXRECT, ref lvi, ref itemBounds);
									RedrawWindow(itemBounds);
								}
								else {
									RedrawWindow();
								}
							}
							//if ((nlv.uNewState & LVIS.LVIS_SELECTED) == 0)
							//{
							//RedrawWindow();
							//}
							if (!selectionTimer.Enabled) {
								selectionTimer.Start();
							}
						}

						break;

					case WNM.LVN_ODSTATECHANGED:
						//RedrawWindow();
						OnSelectionChanged();
						break;

					case WNM.LVN_KEYDOWN:

						NMLVKEYDOWN nkd = (NMLVKEYDOWN)m.GetLParam(typeof(NMLVKEYDOWN));
						Keys key = (Keys)((int)nkd.wVKey);
						if (KeyDown != null) {
							KeyDown(this, new KeyEventArgs(key));
						}

						switch (nkd.wVKey) {
							case (short)Keys.F2:
								RenameSelectedItem();
								break;

							case (short)Keys.Enter:
								if (SelectedItems[0].IsFolder)
									Navigate(SelectedItems[0]);
								else
									StartProcessInCurrentDirectory(SelectedItems[0]);
								break;
							//default:
							//	// initiate key jump code
							//	string skeyr = Enum.GetName(typeof(Keys), key);
							//	if (skeyr.StartsWith("NumPad"))
							//	{
							//		// allows the number pad to work (since the number pad keys give a different Key value)
							//		skeyr = skeyr.Substring(6);
							//	}
							//	if (skeyr.Length == 2 && skeyr.StartsWith("D"))
							//	{
							//		skeyr = skeyr.Substring(1);
							//	}

							// if (skeyr.Length == 1 || skeyr.ToUpperInvariant() == "SPACE" ||
							// skeyr.ToUpperInvariant() == "OEMPERIOD" || skeyr.ToUpperInvariant()
							// == "OEMMINUS") { if (SelectedItems.Count != 0) { _kpreselitem =
							// SelectedItems[0]; } else { _kpreselitem = null; }

							// if (_KeyJumpTimer.Enabled == false) { _KeyJumpTimer.Start();
							// _keyjumpstr = GetStringFromAcceptedKeyCodeString(skeyr); } else {
							// _KeyJumpTimer.Stop(); _KeyJumpTimer.Start(); _keyjumpstr +=
							// GetStringFromAcceptedKeyCodeString(skeyr); }

							// if (KeyJumpKeyDown != null) { KeyJumpKeyDown(this, new
							// KeyEventArgs(key)); } }
							//
							// break;
						}
						//m.Result = (IntPtr)1;
						this.Focus();
						break;

					case WNM.LVN_GROUPINFO:
						//RedrawWindow();
						break;

					case WNM.LVN_HOTTRACK:
						NMLISTVIEW nlvHotTrack = (NMLISTVIEW)m.GetLParam(typeof(NMLISTVIEW));
						if (nlvHotTrack.iItem != ToolTip.ItemIndex) {
							ToolTip.HideTooltip();
						}
						break;

					case WNM.LVN_BEGINDRAG:
						//uint CFSTR_SHELLIDLIST =
						//	User32.RegisterClipboardFormat("Shell IDList Array");
						//	System.Windows.Forms.DataObject dobj = new System.Windows.Forms.DataObject("")
						//Task.Run(() =>
						//{
						//	this.BeginInvoke(new MethodInvoker(() =>
						//	{
						this.DraggedItemIndexes.Clear();
						IntPtr dataObjPtr = IntPtr.Zero;
						System.Runtime.InteropServices.ComTypes.IDataObject dataObject = this.SelectedItems.ToArray().GetIDataObject(out dataObjPtr);

						uint ef = 0;
						Shell32.SHDoDragDrop(this.Handle, dataObject, null, unchecked((uint)System.Windows.Forms.DragDropEffects.All | (uint)System.Windows.Forms.DragDropEffects.Link), out ef);

						//	}));
						//});
						//Ole32.DoDragDrop(ddataObject, this, System.Windows.Forms.DragDropEffects.All, out effect);
						//DragSourceHelper.DoDragDrop(this, new System.Drawing.Point(0, 0), System.Windows.Forms.DragDropEffects.Copy, new KeyValuePair<string, object>("Shell IDList Array", new ShellItemArray(this.SelectedItems.Select(s => s.m_ComInterface).ToArray())));
						break;

					case WNM.NM_RCLICK:
						var nmhdrHdn = (NMHEADER)(m.GetLParam(typeof(NMHEADER)));
						if (nmhdrHdn.iItem != -1 && nmhdrHdn.hdr.hwndFrom == this.LVHandle) {
							var selitems = this.SelectedItems;
							NMITEMACTIVATE itemActivate = (NMITEMACTIVATE)m.GetLParam(typeof(NMITEMACTIVATE));
							ShellContextMenu cm = new ShellContextMenu(selitems.ToArray());
							cm.ShowContextMenu(this, itemActivate.ptAction, CMF.CANRENAME);
						}
						else if (nmhdrHdn.iItem == -1) {
						}
						else {
							//MessageBox.Show(MousePosition.X.ToString() + ", " + MousePosition.Y.ToString());
							if (ColumnHeaderRightClick != null) {
								ColumnHeaderRightClick(this, new MouseEventArgs(System.Windows.Forms.MouseButtons.Right, 1, MousePosition.X, MousePosition.Y, 0));
							}
							//MessageBox.Show("Header RClick!!!!!");
						}
						break;

					case WNM.NM_CLICK:
						break;

					case WNM.NM_SETFOCUS:
						if (IsGroupsEnabled)
							RedrawWindow();
						if (this.ToolTip != null && this.ToolTip.IsVisible)
							this.ToolTip.HideTooltip();
						OnGotFocus();
						break;

					case WNM.NM_KILLFOCUS:
						if (IsGroupsEnabled)
							RedrawWindow();
						if (this.ToolTip != null && this.ToolTip.IsVisible)
							this.ToolTip.HideTooltip();
						OnLostFocus();
						break;

					case CustomDraw.NM_CUSTOMDRAW: {
							if (nmhdr.hwndFrom == this.LVHandle) {
								User32.SendMessage(this.LVHandle, 296, User32.MAKELONG(1, 1), 0);
								var nmlvcd = (NMLVCUSTOMDRAW)m.GetLParam(typeof(NMLVCUSTOMDRAW));
								var index = (int)nmlvcd.nmcd.dwItemSpec;
								var hdc = nmlvcd.nmcd.hdc;
								if (nmlvcd.dwItemType == 1)
									return;
								ShellItem sho = null;

								if (Items.Count > index) {
									sho = Items[index];
								}

								//TODO: Consider [if (Items.Count > index) {] AND [if (sho != null)] Into 1 [If]
								System.Drawing.Color? textColor = null;
								if (sho != null) {
									if (this.LVItemsColorCodes != null && this.LVItemsColorCodes.Count > 0) {
										if (!String.IsNullOrEmpty(sho.Extension)) {
											var extItemsAvailable = this.LVItemsColorCodes.Where(c => c.ExtensionList.Contains(sho.Extension)).Count() > 0;
											if (extItemsAvailable) {
												var color = this.LVItemsColorCodes.Where(c => c.ExtensionList.ToLowerInvariant().Contains(sho.Extension)).Select(c => c.TextColor).SingleOrDefault();
												textColor = color;
											}
										}
									}
								}
								switch (nmlvcd.nmcd.dwDrawStage) {
									case CustomDraw.CDDS_PREPAINT:
										m.Result = (IntPtr)(CustomDraw.CDRF_NOTIFYITEMDRAW | CustomDraw.CDRF_NOTIFYPOSTPAINT | 0x40);
										break;

									case CustomDraw.CDDS_POSTPAINT:
										m.Result = (IntPtr)CustomDraw.CDRF_SKIPDEFAULT;
										break;

									case CustomDraw.CDDS_ITEMPREPAINT:
										if (textColor != null) {
											nmlvcd.clrText = ColorTranslator.ToWin32(textColor.Value);
											Marshal.StructureToPtr(nmlvcd, m.LParam, false);

											m.Result = (IntPtr)(CustomDraw.CDRF_NEWFONT | CustomDraw.CDRF_NOTIFYPOSTPAINT | CustomDraw.CDRF_NOTIFYSUBITEMDRAW | 0x40);
										}
										else {
											m.Result = (IntPtr)(CustomDraw.CDRF_NOTIFYPOSTPAINT | CustomDraw.CDRF_NOTIFYSUBITEMDRAW | 0x40);
										}
										break;

									case CustomDraw.CDDS_ITEMPREPAINT | CustomDraw.CDDS_SUBITEM:
										if (textColor != null) {
											nmlvcd.clrText = ColorTranslator.ToWin32(textColor.Value);
											Marshal.StructureToPtr(nmlvcd, m.LParam, false);
											m.Result = (IntPtr)CustomDraw.CDRF_NEWFONT;
										}
										else {
											m.Result = (IntPtr)CustomDraw.CDRF_DODEFAULT;
										}
										break;

									case CustomDraw.CDDS_ITEMPOSTPAINT:

										if (nmlvcd.clrTextBk != 0) {
											var itemBounds = nmlvcd.nmcd.rc;
											LVITEMINDEX lvi = new LVITEMINDEX();
											lvi.iItem = index;
											lvi.iGroup = this.GetGroupIndex(index);
											//User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMINDEXRECT, ref lvi, ref itemBounds);

											var iconBounds = new User32.RECT();

											iconBounds.Left = 1;

											User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMINDEXRECT, ref lvi, ref iconBounds);

											LVITEM lvItem = new LVITEM();
											lvItem.iItem = index;
											lvItem.iGroupId = lvi.iGroup;
											lvItem.iGroup = lvi.iGroup;
											lvItem.mask = LVIF.LVIF_STATE;
											lvItem.stateMask = LVIS.LVIS_SELECTED;

											LVITEM lvItemImageMask = new LVITEM();
											lvItemImageMask.iItem = index;
											lvItemImageMask.iGroupId = lvi.iGroup;
											lvItemImageMask.iGroup = lvi.iGroup;
											lvItemImageMask.mask = LVIF.LVIF_STATE;
											lvItemImageMask.stateMask = LVIS.LVIS_STATEIMAGEMASK;

											if (sho != null) {
												string shoExtension = sho.Extension;

												if (sho.OverlayIconIndex == -1) {
													overlayQueue.Enqueue(index);
												}
												if (sho.IsShielded == -1) {
													if (shoExtension == ".exe" || shoExtension == ".com" || shoExtension == ".bat")
														shieldQueue.Enqueue(index);
												}
												if (IconSize != 16) {
													var thumbnail = sho.GetShellThumbnail(IconSize, ShellThumbnailFormatOption.ThumbnailOnly, ShellThumbnailRetrievalOption.CacheOnly);
													if (thumbnail != null) {
														if (((thumbnail.Width > thumbnail.Height && thumbnail.Width != IconSize) || (thumbnail.Width < thumbnail.Height && thumbnail.Height != IconSize) || thumbnail.Width == thumbnail.Height && thumbnail.Width != IconSize))
														{
															ThumbnailsForCacheLoad.Enqueue(index);
														}
														else
														{
															sho.IsThumbnailLoaded = true;
														}
														using (Graphics g = Graphics.FromHdc(hdc)) {
															var cutFlag = User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMSTATE, index, LVIS.LVIS_CUT);
															if (sho.IsHidden || cutFlag != 0 || this._CuttedIndexes.Contains(index))
																thumbnail = Helpers.ChangeOpacity(thumbnail, 0.5f);
															g.DrawImageUnscaled(thumbnail, new Rectangle(iconBounds.Left + (iconBounds.Right - iconBounds.Left - thumbnail.Width) / 2, iconBounds.Top + (iconBounds.Bottom - iconBounds.Top - thumbnail.Height) / 2, thumbnail.Width, thumbnail.Height));

															if (this.ShowCheckboxes && View != ShellViewStyle.Details && View != ShellViewStyle.List) {
																var res = User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMW, 0, ref lvItemImageMask);

																if ((nmlvcd.nmcd.uItemState & CDIS.HOT) == CDIS.HOT || (uint)lvItemImageMask.state == (2 << 12)) {
																	res = User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMW, 0, ref lvItem);
																	var checkboxOffsetH = 14;
																	var checkboxOffsetV = 2;
																	if (View == ShellViewStyle.Tile || View == ShellViewStyle.SmallIcon)
																		checkboxOffsetH = 2;
																	if (View == ShellViewStyle.Tile)
																		checkboxOffsetV = 1;

																	if (lvItem.state != 0) {
																		CheckBoxRenderer.DrawCheckBox(g, new System.Drawing.Point(itemBounds.Left + checkboxOffsetH, itemBounds.Top + checkboxOffsetV), System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal);
																	}
																	else {
																		CheckBoxRenderer.DrawCheckBox(g, new System.Drawing.Point(itemBounds.Left + checkboxOffsetH, itemBounds.Top + checkboxOffsetV), System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
																	}
																}
															}
														}
														thumbnail.Dispose();
														thumbnail = null;
													}
													else {
														if (!sho.IsThumbnailLoaded)
															ThumbnailsForCacheLoad.Enqueue(index);
														if ((sho.IconType & IExtractIconPWFlags.GIL_PERCLASS) == IExtractIconPWFlags.GIL_PERCLASS) {
															var icon = sho.GetShellThumbnail(IconSize, ShellThumbnailFormatOption.IconOnly);
															if (icon != null) {
																sho.IsIconLoaded = true;
																using (Graphics g = Graphics.FromHdc(hdc)) {
																	var cutFlag = User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMSTATE, index, LVIS.LVIS_CUT);
																	if (sho.IsHidden || cutFlag != 0 || this._CuttedIndexes.Contains(index))
																		icon = Helpers.ChangeOpacity(icon, 0.5f);
																	g.DrawImageUnscaled(icon, new Rectangle(iconBounds.Left + (iconBounds.Right - iconBounds.Left - icon.Width) / 2, iconBounds.Top + (iconBounds.Bottom - iconBounds.Top - icon.Height) / 2, icon.Width, icon.Height));

																	if (this.ShowCheckboxes && View != ShellViewStyle.Details && View != ShellViewStyle.List) {
																		var res = User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMW, 0, ref lvItemImageMask);

																		if ((nmlvcd.nmcd.uItemState & CDIS.HOT) == CDIS.HOT || (uint)lvItemImageMask.state == (2 << 12)) {
																			var checkboxOffsetH = 14;
																			var checkboxOffsetV = 2;
																			if (View == ShellViewStyle.Tile || View == ShellViewStyle.SmallIcon)
																				checkboxOffsetH = 2;
																			if (View == ShellViewStyle.Tile)
																				checkboxOffsetV = 1;
																			res = User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMW, 0, ref lvItem);
																			if (lvItem.state != 0) {
																				CheckBoxRenderer.DrawCheckBox(g, new System.Drawing.Point(itemBounds.Left + checkboxOffsetH, itemBounds.Top + checkboxOffsetV), System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal);
																			}
																			else {
																				CheckBoxRenderer.DrawCheckBox(g, new System.Drawing.Point(itemBounds.Left + checkboxOffsetH, itemBounds.Top + checkboxOffsetV), System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
																			}
																		}
																	}
																}
																icon.Dispose();
															}
														}
														else if ((sho.IconType & IExtractIconPWFlags.GIL_PERINSTANCE) == IExtractIconPWFlags.GIL_PERINSTANCE) {
															if (!sho.IsIconLoaded) {
																waitingThumbnails.Enqueue(index);
																using (Graphics g = Graphics.FromHdc(hdc)) {
																	if (IconSize == 16) {
																		g.DrawImage(ExeFallBack16, new Rectangle(iconBounds.Left + (iconBounds.Right - iconBounds.Left - IconSize) / 2, iconBounds.Top + (iconBounds.Bottom - iconBounds.Top - IconSize) / 2, IconSize, IconSize));
																	}
																	else if (IconSize <= 48) {
																		g.DrawImage(ExeFallBack48, new Rectangle(iconBounds.Left + (iconBounds.Right - iconBounds.Left - IconSize) / 2, iconBounds.Top + (iconBounds.Bottom - iconBounds.Top - IconSize) / 2, IconSize, IconSize));
																	}
																	else if (IconSize <= 256) {
																		g.DrawImage(ExeFallBack256, new Rectangle(iconBounds.Left + (iconBounds.Right - iconBounds.Left - IconSize) / 2, iconBounds.Top + (iconBounds.Bottom - iconBounds.Top - IconSize) / 2, IconSize, IconSize));
																	}
																}
															}
															else {
																Bitmap icon = sho.GetShellThumbnail(IconSize, ShellThumbnailFormatOption.IconOnly);
																if (icon != null) {
																	sho.IsIconLoaded = true;
																	using (Graphics g = Graphics.FromHdc(hdc)) {
																		var cutFlag = User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMSTATE, index, LVIS.LVIS_CUT);
																		if (sho.IsHidden || cutFlag != 0 || this._CuttedIndexes.Contains(index))
																			icon = Helpers.ChangeOpacity(icon, 0.5f);
																		g.DrawImageUnscaled(icon, new Rectangle(iconBounds.Left + (iconBounds.Right - iconBounds.Left - icon.Width) / 2, iconBounds.Top + (iconBounds.Bottom - iconBounds.Top - icon.Height) / 2, icon.Width, icon.Height));

																		if (this.ShowCheckboxes && View != ShellViewStyle.Details && View != ShellViewStyle.List) {
																			var res = User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMW, 0, ref lvItemImageMask);

																			if ((nmlvcd.nmcd.uItemState & CDIS.HOT) == CDIS.HOT || (uint)lvItemImageMask.state == (2 << 12)) {
																				var checkboxOffsetH = 14;
																				var checkboxOffsetV = 2;
																				if (View == ShellViewStyle.Tile || View == ShellViewStyle.SmallIcon)
																					checkboxOffsetH = 2;
																				if (View == ShellViewStyle.Tile)
																					checkboxOffsetV = 1;

																				res = User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMW, 0, ref lvItem);
																				if (lvItem.state != 0) {
																					CheckBoxRenderer.DrawCheckBox(g, new System.Drawing.Point(itemBounds.Left + checkboxOffsetH, itemBounds.Top + checkboxOffsetV), System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal);
																				}
																				else {
																					CheckBoxRenderer.DrawCheckBox(g, new System.Drawing.Point(itemBounds.Left + checkboxOffsetH, itemBounds.Top + checkboxOffsetV), System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
																				}
																			}
																		}
																	}
																	icon.Dispose();
																}
															}
														}
													}
												}
												else {
													sho.IsThumbnailLoaded = true;
													if ((sho.IconType & IExtractIconPWFlags.GIL_PERCLASS) == IExtractIconPWFlags.GIL_PERCLASS) {
														var icon = sho.GetShellThumbnail(IconSize, ShellThumbnailFormatOption.IconOnly);
														if (icon != null) {
															sho.IsIconLoaded = true;
															using (Graphics g = Graphics.FromHdc(hdc)) {
																var cutFlag = User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMSTATE, index, LVIS.LVIS_CUT);
																if (sho.IsHidden || cutFlag != 0 || this._CuttedIndexes.Contains(index))
																	icon = Helpers.ChangeOpacity(icon, 0.5f);
																g.DrawImageUnscaled(icon, new Rectangle(iconBounds.Left + (iconBounds.Right - iconBounds.Left - icon.Width) / 2, iconBounds.Top + (iconBounds.Bottom - iconBounds.Top - icon.Height) / 2, icon.Width, icon.Height));
															}
															icon.Dispose();
														}
													}
													else if ((sho.IconType & IExtractIconPWFlags.GIL_PERINSTANCE) == IExtractIconPWFlags.GIL_PERINSTANCE) {
														if (!sho.IsIconLoaded) {
															waitingThumbnails.Enqueue(index);
															using (Graphics g = Graphics.FromHdc(hdc)) {
																g.DrawImage(ExeFallBack16, new Rectangle(iconBounds.Left + (iconBounds.Right - iconBounds.Left - IconSize) / 2, iconBounds.Top + (iconBounds.Bottom - iconBounds.Top - IconSize) / 2, IconSize, IconSize));
															}
														}
														else {
															Bitmap icon = sho.GetShellThumbnail(IconSize, ShellThumbnailFormatOption.IconOnly);
															if (icon != null) {
																sho.IsIconLoaded = true;
																using (Graphics g = Graphics.FromHdc(hdc)) {
																	var cutFlag = User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMSTATE, index, LVIS.LVIS_CUT);
																	if (sho.IsHidden || cutFlag != 0 || this._CuttedIndexes.Contains(index))
																		icon = Helpers.ChangeOpacity(icon, 0.5f);
																	g.DrawImageUnscaled(icon, new Rectangle(iconBounds.Left + (iconBounds.Right - iconBounds.Left - icon.Width) / 2, iconBounds.Top + (iconBounds.Bottom - iconBounds.Top - icon.Height) / 2, icon.Width, icon.Height));
																}
																icon.Dispose();
															}
														}
													}
												}

												if (sho.OverlayIconIndex > 0) {
													if (this.View == ShellViewStyle.Details || this.View == ShellViewStyle.List || this.View == ShellViewStyle.SmallIcon) {
														small.DrawOverlay(hdc, sho.OverlayIconIndex, new System.Drawing.Point(iconBounds.Left, iconBounds.Bottom - 16));
													}
													else {
														if (this.IconSize > 180) {
															jumbo.DrawOverlay(hdc, sho.OverlayIconIndex, new System.Drawing.Point(iconBounds.Left, iconBounds.Bottom - this.IconSize / 3), this.IconSize / 3);
														}
														else
															if (this.IconSize > 64) {
																extra.DrawOverlay(hdc, sho.OverlayIconIndex, new System.Drawing.Point(iconBounds.Left + 10, iconBounds.Bottom - 50));
															}
															else {
																large.DrawOverlay(hdc, sho.OverlayIconIndex, new System.Drawing.Point(iconBounds.Left + 10, iconBounds.Bottom - 32));
															}
													}
												}

												if (sho.IsShielded > 0) {
													if (this.View == ShellViewStyle.Details || this.View == ShellViewStyle.List || this.View == ShellViewStyle.SmallIcon) {
														small.DrawIcon(hdc, sho.IsShielded, new System.Drawing.Point(iconBounds.Right - 10, iconBounds.Bottom - 10), 8);
													}
													else {
														if (this.IconSize > 180) {
															jumbo.DrawIcon(hdc, sho.IsShielded, new System.Drawing.Point(iconBounds.Right - this.IconSize / 3, iconBounds.Bottom - this.IconSize / 3), this.IconSize / 3);
														}
														else
															if (this.IconSize > 64) {
																extra.DrawIcon(hdc, sho.IsShielded, new System.Drawing.Point(iconBounds.Right - 60, iconBounds.Bottom - 50));
															}
															else {
																large.DrawIcon(hdc, sho.IsShielded, new System.Drawing.Point(iconBounds.Right - 42, iconBounds.Bottom - 32));
															}
													}
												}

												if (View == ShellViewStyle.Tile) {
													var lableBounds = new User32.RECT();

													lableBounds.Left = 2;

													User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMINDEXRECT, ref lvi, ref lableBounds);

													using (Graphics g = Graphics.FromHdc(hdc)) {
														StringFormat fmt = new StringFormat();
														fmt.Trimming = StringTrimming.EllipsisCharacter;
														fmt.Alignment = StringAlignment.Center;
														fmt.Alignment = StringAlignment.Near;
														fmt.FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.FitBlackBox;
														fmt.LineAlignment = StringAlignment.Center;
														RectangleF lblrectTiles = new RectangleF(lableBounds.Left, itemBounds.Top + 4, lableBounds.Right - lableBounds.Left, 20);
														Font font = System.Drawing.SystemFonts.IconTitleFont;
														SolidBrush textBrush = new SolidBrush(textColor == null ? System.Drawing.SystemColors.ControlText : textColor.Value);
														g.DrawString(sho.DisplayName, font, textBrush, lblrectTiles, fmt);
														font.Dispose();
														textBrush.Dispose();
													}
												}
											}
										}
										m.Result = (IntPtr)CustomDraw.CDRF_SKIPDEFAULT;

										break;
								}
							}
						}
						break;
				}
			}
		}

		protected override void OnSizeChanged(EventArgs e) {
			base.OnSizeChanged(e);
			//this.Invalidate();
			//this.Refresh();
			User32.MoveWindow(this.LVHandle, 0, 0, this.ClientRectangle.Width, this.ClientRectangle.Height, true);
		}

		protected override void OnHandleCreated(EventArgs e) {
			base.OnHandleCreated(e);

			System.Windows.Forms.ImageList il = new System.Windows.Forms.ImageList();
			il.ImageSize = new System.Drawing.Size(48, 48);
			System.Windows.Forms.ImageList ils = new System.Windows.Forms.ImageList();
			ils.ImageSize = new System.Drawing.Size(16, 16);

			ComCtl32.INITCOMMONCONTROLSEX icc = new ComCtl32.INITCOMMONCONTROLSEX();
			icc.dwSize = Marshal.SizeOf(typeof(ComCtl32.INITCOMMONCONTROLSEX));
			icc.dwICC = 1;
			var res = ComCtl32.InitCommonControlsEx(ref icc);
			this.LVHandle = User32.CreateWindowEx(0, "SysListView32", "", User32.WindowStyles.WS_CHILD | User32.WindowStyles.WS_CLIPCHILDREN | User32.WindowStyles.WS_CLIPSIBLINGS |
						(User32.WindowStyles)User32.LVS_EDITLABELS | (User32.WindowStyles)User32.LVS_OWNERDATA | (User32.WindowStyles)User32.LVS_SHOWSELALWAYS | (User32.WindowStyles)User32.LVS_AUTOARRANGE,
								0, 0, this.ClientRectangle.Width, this.ClientRectangle.Height, this.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

			User32.ShowWindow(this.LVHandle, User32.ShowWindowCommands.Show);

			this.AddDefaultColumns();

			IntPtr headerhandle = User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETHEADER, 0, 0);

			for (int i = 0; i < this.Collumns.Count; i++) {
				this.Collumns[i].SetSplitButton(headerhandle, i);
			}

			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETIMAGELIST, 0, il.Handle);
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETIMAGELIST, 1, ils.Handle);
			//User32.SendMessage(this.LVHandle, 4170, 0, 0);
			//User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SET, 1, ils.Handle);
			UxTheme.SetWindowTheme(this.LVHandle, "Explorer", 0);

			this.View = ShellViewStyle.Medium;

			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SetExtendedStyle, (int)ListViewExtendedStyles.HeaderInAllViews, (int)ListViewExtendedStyles.HeaderInAllViews);
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SetExtendedStyle, (int)ListViewExtendedStyles.LVS_EX_AUTOAUTOARRANGE, (int)ListViewExtendedStyles.LVS_EX_AUTOAUTOARRANGE);
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SetExtendedStyle, (int)ListViewExtendedStyles.LVS_EX_DOUBLEBUFFER, (int)ListViewExtendedStyles.LVS_EX_DOUBLEBUFFER);
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SetExtendedStyle, (int)ListViewExtendedStyles.FullRowSelect, (int)ListViewExtendedStyles.FullRowSelect);
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SetExtendedStyle, (int)ListViewExtendedStyles.HeaderDragDrop, (int)ListViewExtendedStyles.HeaderDragDrop);
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SetExtendedStyle, (int)ListViewExtendedStyles.LabelTip, (int)ListViewExtendedStyles.LabelTip);
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SetExtendedStyle, (int)ListViewExtendedStyles.InfoTip, (int)ListViewExtendedStyles.InfoTip);
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SetExtendedStyle, (int)ListViewExtendedStyles.UnderlineHot, (int)ListViewExtendedStyles.UnderlineHot);
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SetExtendedStyle, (int)ListViewExtendedStyles.AutosizeColumns, (int)ListViewExtendedStyles.AutosizeColumns);
			//User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SetExtendedStyle, (int)ListViewExtendedStyles.colum, (int)ListViewExtendedStyles.AutosizeColumns);
			//User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SetExtendedStyle, (int)ListViewExtendedStyles.TrackSelect, (int)ListViewExtendedStyles.TrackSelect);
			//CurrentFolder = (ShellItem) KnownFolders.Desktop;
			this.Focus();
			User32.SetForegroundWindow(this.LVHandle);
		}

		protected override void OnHandleDestroyed(EventArgs e) {
			try {
				if (_IconLoadingThread.IsAlive)
					_IconLoadingThread.Abort();
				if (_IconCacheLoadingThread.IsAlive)
					_IconCacheLoadingThread.Abort();
				if (_OverlaysLoadingThread.IsAlive)
					_OverlaysLoadingThread.Abort();
				if (_ShieldLoadingThread.IsAlive)
					_ShieldLoadingThread.Abort();
				if (_UpdateSubitemValuesThread.IsAlive)
					_UpdateSubitemValuesThread.Abort();
				if (MaintenanceThread != null && MaintenanceThread.IsAlive)
					MaintenanceThread.Abort();
			}
			catch (ThreadAbortException) {
			}
			base.OnHandleDestroyed(e);
		}

		#endregion Overrides

		#region Public Methods

		[Obsolete("Does nothing or is not finished")]
		public void ShowFileProperties() {
			IntPtr doPtr = IntPtr.Zero;
			if (Shell32.SHMultiFileProperties(this.SelectedItems.ToArray().GetIDataObject(out doPtr), 0) != 0 /*S_OK*/) {
				throw new Win32Exception();
			}
		}

		public void ShowFileProperties(string Filename) {
			Shell32.SHELLEXECUTEINFO info = new Shell32.SHELLEXECUTEINFO();
			info.cbSize = Marshal.SizeOf(info);
			info.lpVerb = "properties";
			info.lpFile = Filename;
			info.nShow = SW_SHOW;
			info.fMask = SEE_MASK_INVOKEIDLIST;
			Shell32.ShellExecuteEx(ref info);
		}

		public void RedrawItem(int index) {
			//System.Windows.Forms.Application.DoEvents();
			var sho = Items[index];
			//System.Windows.Forms.Application.DoEvents();
			var itemBounds = new User32.RECT();
			itemBounds.Left = 1;
			LVITEMINDEX lvi = new LVITEMINDEX();
			lvi.iItem = index;
			lvi.iGroup = this.GetGroupIndex(index);
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMINDEXRECT, ref lvi, ref itemBounds);
			itemBounds.Left -= 2;
			itemBounds.Top -= 2;
			itemBounds.Bottom += 2;
			itemBounds.Right += 2;

			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_REDRAWITEMS, index, index);
			for (int i = 0; i < 1; i++) {
				if (IsGroupsEnabled) {
					RedrawWindow(itemBounds);
				}
			}
		}

		public void UpdateItem(int index) {
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_UPDATE, index, 0);
		}

		/// <summary> Navigates to the parent of the currently displayed folder. </summary>
		public void NavigateParent() {
			Navigate(m_CurrentFolder.Parent);
		}

		public void RefreshContents() {
			Navigate(this.CurrentFolder, true);
		}

		public void RefreshItem(int index, Boolean IsForceRedraw = false) {
			if (IsForceRedraw)
			{
				this.Items[index] = new ShellItem(this.Items[index].Pidl);
			}
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_REDRAWITEMS, index, index);
		}

		public void RenameItem(int index) {
			this.Focus();
			var res = User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_EDITLABELW, new IntPtr(index), IntPtr.Zero);
			this._IsInRenameMode = true;
		}

		public void RenameSelectedItem() {
			//User32.EnumChildWindows(this.LVHandle, RenameCallback, IntPtr.Zero);
			this.Focus();
			this.RenameItem(this.GetFirstSelectedItemIndex());
		}

		public void CutSelectedFiles() {
			foreach (var index in this.SelectedIndexes) {
				LVITEM item = new LVITEM();
				item.mask = LVIF.LVIF_STATE;
				item.stateMask = LVIS.LVIS_CUT;
				item.state = LVIS.LVIS_CUT;
				User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMSTATE, index, ref item);
			}
			this._CuttedIndexes.AddRange(this.SelectedIndexes.ToArray());
			IntPtr dataObjPtr = IntPtr.Zero;
			//System.Runtime.InteropServices.ComTypes.IDataObject dataObject = GetIDataObject(this.SelectedItems.ToArray(), out dataObjPtr);
			System.Windows.Forms.IDataObject ddataObject = new System.Windows.Forms.DataObject();
			// Copy or Cut operation (5 = copy; 2 = cut)
			ddataObject.SetData("Preferred DropEffect", true, new MemoryStream(new byte[] { 2, 0, 0, 0 }));
			ddataObject.SetData("Shell IDList Array", true, this.SelectedItems.ToArray().CreateShellIDList());
			System.Windows.Forms.Clipboard.SetDataObject(ddataObject, true);
		}

		public void CopySelectedFiles() {
			IntPtr dataObjPtr = IntPtr.Zero;
			//System.Runtime.InteropServices.ComTypes.IDataObject dataObject = GetIDataObject(this.SelectedItems.ToArray(), out dataObjPtr);
			System.Windows.Forms.DataObject ddataObject = new System.Windows.Forms.DataObject();
			ddataObject.SetData("Preferred DropEffect", true, new MemoryStream(new byte[] { 5, 0, 0, 0 }));
			ddataObject.SetData("Shell IDList Array", true, this.SelectedItems.ToArray().CreateShellIDList());
			System.Windows.Forms.Clipboard.SetDataObject(ddataObject, true);
		}

		public void PasteAvailableFiles() {
			var handle = this.Handle;
			var thread = new Thread(() => {
				var dataObject = System.Windows.Forms.Clipboard.GetDataObject();
				var dragDropEffect = System.Windows.DragDropEffects.Copy;
				var dropEffect = dataObject.ToDropEffect();
				var shellItemArray = dataObject.ToShellItemArray();
				var items = shellItemArray.ToArray();
				try {
					IIFileOperation fo = new IIFileOperation(handle);
					foreach (var item in items) {
						if (dropEffect == System.Windows.DragDropEffects.Copy) {
							fo.CopyItem(item, this.CurrentFolder.m_ComInterface, String.Empty);
						}
						else {
							fo.MoveItem(item, this.CurrentFolder.m_ComInterface, null);
						}
					}

					fo.PerformOperations();
				}
				catch (SecurityException) {
					throw;
				}
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		public void DoCopy(ShellItem destination) {
			var handle = this.Handle;
			var thread = new Thread(() => {
				IIFileOperation fo = new IIFileOperation(handle);
				foreach (var item in this.SelectedItems.Select(s => s.m_ComInterface).ToArray()) {
					fo.CopyItem(item, destination.m_ComInterface, null);
				}
				fo.PerformOperations();
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		public void DoCopy(System.Windows.Forms.IDataObject dataObject, ShellItem destination) {
			var handle = this.Handle;
			var thread = new Thread(() => {
				var shellItemArray = dataObject.ToShellItemArray();
				var items = shellItemArray.ToArray();
				try {
					IIFileOperation fo = new IIFileOperation(handle);
					foreach (var item in items) {
						fo.CopyItem(item, destination.m_ComInterface, String.Empty);
					}

					fo.PerformOperations();
				}
				catch (SecurityException) {
					throw;
				}
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		public void DoCopy(System.Windows.IDataObject dataObject, ShellItem destination) {
			var handle = this.Handle;
			var thread = new Thread(() => {
				var shellItemArray = dataObject.ToShellItemArray();
				var items = shellItemArray.ToArray();
				try {
					IIFileOperation fo = new IIFileOperation(handle);
					foreach (var item in items) {
						fo.CopyItem(item, destination.m_ComInterface, String.Empty);
					}

					fo.PerformOperations();
				}
				catch (SecurityException) {
					throw;
				}
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		public void DoMove(System.Windows.Forms.IDataObject dataObject, ShellItem destination) {
			var handle = this.Handle;
			var thread = new Thread(() => {
				var shellItemArray = dataObject.ToShellItemArray();
				var items = shellItemArray.ToArray();
				try {
					IIFileOperation fo = new IIFileOperation(handle);
					foreach (var item in items) {
						fo.MoveItem(item, destination.m_ComInterface, null);
					}

					fo.PerformOperations();
				}
				catch (SecurityException) {
					throw;
				}
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		public void DoMove(System.Windows.IDataObject dataObject, ShellItem destination) {
			var handle = this.Handle;
			var thread = new Thread(() => {
				var shellItemArray = dataObject.ToShellItemArray();
				var items = shellItemArray.ToArray();
				try {
					IIFileOperation fo = new IIFileOperation(handle);
					foreach (var item in items) {
						fo.MoveItem(item, destination.m_ComInterface, null);
					}

					fo.PerformOperations();
				}
				catch (SecurityException) {
					throw;
				}
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		public void DoMove(ShellItem destination) {
			var handle = this.Handle;
			var thread = new Thread(() => {
				IIFileOperation fo = new IIFileOperation(handle);
				foreach (var item in this.SelectedItems.Select(s => s.m_ComInterface).ToArray()) {
					fo.MoveItem(item, destination.m_ComInterface, null);
				}
				fo.PerformOperations();
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		public void DeleteSelectedFiles(Boolean isRecycling) {
			var handle = this.Handle;
			var thread = new Thread(() => {
				IIFileOperation fo = new IIFileOperation(handle, isRecycling);
				foreach (var item in this.SelectedItems.Select(s => s.m_ComInterface).ToArray()) {
					fo.DeleteItem(item);
				}
				fo.PerformOperations();
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		public void RenameShellItem(IShellItem item, String newName) {
			IIFileOperation fo = new IIFileOperation(true);
			fo.RenameItem(item, newName);
			fo.PerformOperations();
			//this.Navigate(this.CurrentFolder);
		}

		public void ResizeIcons(int value) {
			try {
				IconSize = value;
				this.Cancel = true;
				cache.Clear();
				cachedIndexes.Clear();
				ThumbnailsForCacheLoad.Clear();
				waitingThumbnails.Clear();
				foreach (var obj in this.Items) {
					obj.IsIconLoaded = false;
				}

				//Task.Run(() =>
				//{
				//	for (int i = 0; i < Items.Count(); i++)
				//	{
				//		if (Items[i].ThumbnailIcon != null)
				//		{
				//			Items[i].ThumbnailIcon.Dispose();
				//			Items[i].ThumbnailIcon = null;
				//			//GC.Collect();
				//		}
				//	}
				//	//GC.Collect();
				//});
				//GC.Collect();
				//this.cache.Clear();
				System.Windows.Forms.ImageList il = new System.Windows.Forms.ImageList();
				il.ImageSize = new System.Drawing.Size(value, value);
				System.Windows.Forms.ImageList ils = new System.Windows.Forms.ImageList();
				ils.ImageSize = new System.Drawing.Size(16, 16);
				User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETIMAGELIST, 0, il.Handle);
				User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETIMAGELIST, 1, ils.Handle);
				User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETICONSPACING, 0, (IntPtr)User32.MAKELONG(value + 28, value + 42));
				this.Cancel = false;
			}
			catch (Exception) {
			}
		}

		/// <summary> Runs an application as an administrator. </summary>
		/// <param name="ExePath"> The path of the application. </param>
		public void RunExeAsAdmin(string ExePath) {
			var psi = new ProcessStartInfo {
				FileName = ExePath,
				Verb = "runas",
				UseShellExecute = true,
				Arguments = String.Format("/env /user:Administrator \"{0}\"", ExePath),
			};
			Process.Start(psi);
		}

		public void SelectAll() {
			LVITEM item = new LVITEM();
			item.mask = LVIF.LVIF_STATE;
			item.stateMask = LVIS.LVIS_SELECTED;
			item.state = LVIS.LVIS_SELECTED;
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMSTATE, -1, ref item);
			this.Focus();
		}

		public void SelectItems(ShellItem[] ShellObjectArray) {
			foreach (ShellItem item in ShellObjectArray) {
				LVITEM lvi = new LVITEM();
				lvi.mask = LVIF.LVIF_STATE;
				lvi.stateMask = LVIS.LVIS_SELECTED;
				lvi.state = LVIS.LVIS_SELECTED;
				User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMSTATE, Items.IndexOf(item), ref lvi);
			}
			this.Focus();
		}

		public void SelectItemByIndex(int index, bool ensureVisisble = false) {
			LVITEM lvi = new LVITEM();
			lvi.mask = LVIF.LVIF_STATE;
			lvi.stateMask = LVIS.LVIS_SELECTED;
			lvi.state = LVIS.LVIS_SELECTED;
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMSTATE, index, ref lvi);
			if (ensureVisisble)
				User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_ENSUREVISISBLE, index, 0);
			this.Focus();
		}

		public void DropHighLightIndex(int index, bool ensureVisisble = false) {
			LVITEM lvi = new LVITEM();
			lvi.mask = LVIF.LVIF_STATE;
			lvi.stateMask = LVIS.LVIS_DROPHILITED | LVIS.LVIS_SELECTED;
			lvi.state = LVIS.LVIS_DROPHILITED | LVIS.LVIS_SELECTED;
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMSTATE, index, ref lvi);
			if (ensureVisisble)
				User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_ENSUREVISISBLE, index, 0);
			this.Focus();
		}

		public void SetColInView(Collumns col, bool Remove) {
			if (!Remove) {
				if (this.Collumns.Count(s => s.pkey.fmtid == col.pkey.fmtid && s.pkey.pid == col.pkey.pid) == 0) {
					this.Collumns.Add(col);
					var column = col.ToNativeColumn();
					User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_INSERTCOLUMN, this.Collumns.Count - 1, ref column);
				}
			}
			else {
				Collumns theColumn = this.Collumns.SingleOrDefault(s => s.pkey.fmtid == col.pkey.fmtid && s.pkey.pid == col.pkey.pid);
				if (theColumn != null) {
					int colIndex = this.Collumns.IndexOf(theColumn);
					this.Collumns.Remove(theColumn);
					User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_DELETECOLUMN, colIndex, 0);
				}
			}

			IntPtr headerhandle = User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETHEADER, 0, 0);

			for (int i = 0; i < this.Collumns.Count; i++) {
				this.Collumns[i].SetSplitButton(headerhandle, i);
			}
		}

		public void SetSortCollumn(int colIndex, SortOrder Order, Boolean reverseOrder = true) {
			if (colIndex == this.LastSortedColumnIndex && reverseOrder) {
				// Reverse the current sort direction for this column.
				if (this.LastSortOrder == SortOrder.Ascending) {
					this.LastSortOrder = SortOrder.Descending;
				}
				else {
					this.LastSortOrder = SortOrder.Ascending;
				}
			}
			else {
				// Set the column number that is to be sorted; default to ascending.
				this.LastSortedColumnIndex = colIndex;
				this.LastSortOrder = Order;
			}
			var i = 0;
			if (Order == SortOrder.Ascending) {
				this.Items = this.Items.OrderByDescending(o => o.IsFolder).ThenBy(o => o.GetPropertyValue(this.Collumns[colIndex].pkey, typeof(String)).Value).ToList();
			}
			else {
				this.Items = this.Items.OrderByDescending(o => o.IsFolder).ThenByDescending(o => o.GetPropertyValue(this.Collumns[colIndex].pkey, typeof(String)).Value).ToList();
			}

			this.ItemsHashed = this.Items.ToDictionary(k => k, el => i++);
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMCOUNT, this.Items.Count, 0);
			this.SetSortIcon(colIndex, Order);
		}

		/// <summary>
		/// Navigates the <see cref="ShellView"/> control forwards to the
		/// requested folder in the navigation history.
		/// </summary>
		///
		/// <remarks>
		/// The WebBrowser control maintains a history list of all the folders
		/// visited during a session. You can use the
		/// <see cref="NavigateForward"/> method to implement a drop-down menu
		/// on a <b>Forward</b> button similar to the one in Windows Explorer,
		/// which will allow your users to return to a folder in the 'forward'
		/// navigation history.
		/// </remarks>
		///
		/// <param name="folder">
		/// The folder to navigate to.
		/// </param>
		///
		/// <exception cref="Exception">
		/// The requested folder is not present in the
		/// <see cref="ShellView"/>'s 'forward' history.
		/// </exception>
		public void NavigateForward(ShellItem folder) {
			History.MoveForward(folder);
			m_CurrentFolder = folder;

			//OnNavigated(new NavigatedEventArgs(m_CurrentFolder));
		}

		/// <summary>
		/// Navigates the <see cref="ShellView"/> control to the next folder
		/// in the navigation history.
		/// </summary>
		///
		/// <remarks>
		/// <para>
		/// The WebBrowser control maintains a history list of all the folders
		/// visited during a session. You can use the <see cref="NavigateForward"/>
		/// method to implement a <b>Forward</b> button similar to the one
		/// in Windows Explorer, allowing your users to return to the next
		/// folder in the navigation history after navigating backward.
		/// </para>
		///
		/// <para>
		/// Use the <see cref="CanNavigateForward"/> property to determine
		/// whether the navigation history is available and contains a folder
		/// located after the current one.  This property is useful, for
		/// example, to change the enabled state of a <b>Forward</b> button
		/// when the ShellView control navigates to or leaves the end of the
		/// navigation history.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="InvalidOperationException">
		/// There is no history to navigate forwards through.
		/// </exception>
		public void NavigateForward() {
			m_CurrentFolder = History.MoveForward();
			//OnNavigated(new NavigatedEventArgs(m_CurrentFolder));
		}

		/// <summary>
		/// Navigates the <see cref="ShellView"/> control backwards to the
		/// requested folder in the navigation history.
		/// </summary>
		///
		/// <remarks>
		/// The WebBrowser control maintains a history list of all the folders
		/// visited during a session. You can use the <see cref="NavigateBack"/>
		/// method to implement a drop-down menu on a <b>Back</b> button similar
		/// to the one in Windows Explorer, which will allow your users to return
		/// to a previous folder in the navigation history.
		/// </remarks>
		///
		/// <param name="folder">
		/// The folder to navigate to.
		/// </param>
		///
		/// <exception cref="Exception">
		/// The requested folder is not present in the
		/// <see cref="ShellView"/>'s 'back' history.
		/// </exception>
		public void NavigateBack(ShellItem folder) {
			History.MoveBack(folder);
			m_CurrentFolder = folder;
			//RecreateShellView();
			//OnNavigated(new NavigatedEventArgs(m_CurrentFolder));
		}

		/// <summary>
		/// Navigates the <see cref="ShellView"/> control to the previous folder
		/// in the navigation history.
		/// </summary>
		///
		/// <remarks>
		/// <para>
		/// The WebBrowser control maintains a history list of all the folders
		/// visited during a session. You can use the <see cref="NavigateBack"/>
		/// method to implement a <b>Back</b> button similar to the one in
		/// Windows Explorer, which will allow your users to return to a
		/// previous folder in the navigation history.
		/// </para>
		///
		/// <para>
		/// Use the <see cref="CanNavigateBack"/> property to determine whether
		/// the navigation history is available and contains a previous page.
		/// This property is useful, for example, to change the enabled state
		/// of a Back button when the ShellView control navigates to or leaves
		/// the beginning of the navigation history.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="InvalidOperationException">
		/// There is no history to navigate backwards through.
		/// </exception>
		public void NavigateBack() {
			m_CurrentFolder = History.MoveBack();
			//RecreateShellView();
			//OnNavigated(new NavigatedEventArgs(m_CurrentFolder));
		}

		public void Navigate(ShellItem destination, Boolean isReload = false) {
			this.OnNavigating(new NavigatingEventArgs(destination));
			if (destination == null)
				return;

			if (ToolTip == null)
			{
				this.ToolTip = new ToolTip();
			}
			if (this.CurrentFolder != null) {
				if (destination.ParsingName == this.CurrentFolder.ParsingName && !isReload)
					return;
			}

			this.Notifications.UnregisterChangeNotify();
			overlays.Clear();
			shieldedIcons.Clear();
			cache.Clear();
			Items.Clear();
			cachedIndexes.Clear();
			ItemsForSubitemsUpdate.Clear();
			waitingThumbnails.Clear();
			overlayQueue.Clear();
			shieldQueue.Clear();
			this.Cancel = true;
			this.cache.Clear();
			this._CuttedIndexes.Clear();
			SubItems.Clear();
			Tuple<int, PROPERTYKEY, object> tmp = null;
			while (!SubItemValues.IsEmpty) {
				SubItemValues.TryTake(out tmp);
			}
			if (tmp != null)
				tmp = null;
			SubItems.Clear();

			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMCOUNT, 0, 0);

			var e = destination.GetEnumerator();
			var i = 0;
			while (e.MoveNext()) {
				System.Windows.Forms.Application.DoEvents();
				this.Items.Add(e.Current);
				System.Windows.Forms.Application.DoEvents();
				CurrentI++;
				if (CurrentI - LastI >= (destination.IsSearchFolder ? 5 : 2500) && destination.IsSearchFolder) {
					Thread.Sleep(10);
					System.Windows.Forms.Application.DoEvents();
					User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMCOUNT, this.Items.Count, 0);
					//Thread.Sleep(e.Item.Parent.IsSearchFolder? 5: 1);

					//Shell32.SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
					LastI = CurrentI;
					//GC.WaitForPendingFinalizers();
					//GC.Collect();
				}
			}

			//System.Windows.Forms.Application.DoEvents();
			if (destination.ParsingName.ToLowerInvariant() == KnownFolders.Computer.ParsingName.ToLowerInvariant()) {
				this.Items = this.Items.ToList();
			}
			else {
				this.Items = this.Items.Where(w => this.ShowHidden ? true : w.IsHidden == false).OrderByDescending(o => o.IsFolder).ThenBy(o => o.DisplayName).ToList();
			}
			this.ItemsHashed = this.Items.ToDictionary(k => k, el => i++);
			GC.WaitForPendingFinalizers();
			GC.Collect();
			Shell32.SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);

			this.Cancel = false;
			this.LastSortedColumnIndex = 0;
			this.LastSortOrder = SortOrder.Ascending;
			this.SetSortIcon(this.LastSortedColumnIndex, this.LastSortOrder);
			this.m_CurrentFolder = destination;
			Notifications.RegisterChangeNotify(this.Handle, destination, true);
			try {
				History.Add(destination);
			}
			catch { }

			
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMCOUNT, this.Items.Count, 0);
			if (IsGroupsEnabled) {
				this.Groups.Clear();
				User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_REMOVEALLGROUPS, 0, 0);
				GenerateGroupsFromColumn(this.Collumns.First());
			}
			//dest.Dispose();
			this.OnNavigated(new NavigatedEventArgs(destination));
			IsDoubleNavFinished = false;
			this.Focus();
		}

		public void DisableGroups() {
			this.Groups.Clear();
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_REMOVEALLGROUPS, 0, 0);
			const int LVM_ENABLEGROUPVIEW = 0x1000 + 157;
			var x = User32.SendMessage(this.LVHandle, LVM_ENABLEGROUPVIEW, 0, 0);
			//System.Diagnostics.Debug.WriteLine(x);

			const int LVM_SETOWNERDATACALLBACK = 0x10BB;
			x = User32.SendMessage(this.LVHandle, LVM_SETOWNERDATACALLBACK, 0, 0);
			IsGroupsEnabled = false;
			this.LastGroupCollumn = null;
			this.SetSortIcon(this.LastSortedColumnIndex, this.LastSortOrder);
		}

		public void EnableGroups() {
			VirtualGrouping g = new VirtualGrouping(this);

			const int LVM_SETOWNERDATACALLBACK = 0x10BB;
			IntPtr ptr = Marshal.GetComInterfaceForObject(g, typeof(IOwnerDataCallback));
			var x = User32.SendMessage(this.LVHandle, LVM_SETOWNERDATACALLBACK, ptr, IntPtr.Zero);
			//System.Diagnostics.Debug.WriteLine(x);
			Marshal.Release(ptr);

			const int LVM_ENABLEGROUPVIEW = 0x1000 + 157;

			x = (int)User32.SendMessage(this.LVHandle, LVM_ENABLEGROUPVIEW, 1, 0);
			IsGroupsEnabled = true;
		}

		public void GenerateGroupsFromColumn(Collumns col, Boolean reversed = false) {
			int LVM_INSERTGROUP = 0x1000 + 145;
			this.Groups.Clear();
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_REMOVEALLGROUPS, 0, 0);
			if (col.CollumnType == typeof(String)) {
				var i = reversed ? 3 : 0;
				ListViewGroupEx testgrn = new ListViewGroupEx();
				testgrn.Items = this.Items.Where(w => w.DisplayName.ToUpperInvariant().First() >= Char.Parse("0") && w.DisplayName.ToUpperInvariant().First() <= Char.Parse("9")).ToArray();
				testgrn.Header = String.Format("0 - 9 ({0})", testgrn.Items.Count());
				testgrn.Index = reversed ? i-- : i++;
				this.Groups.Add(testgrn);

				ListViewGroupEx testgr = new ListViewGroupEx();
				testgr.Items = this.Items.Where(w => w.DisplayName.ToUpperInvariant().First() >= Char.Parse("A") && w.DisplayName.ToUpperInvariant().First() <= Char.Parse("H")).ToArray();
				testgr.Header = String.Format("A - H ({0})", testgr.Items.Count());
				testgr.Index = reversed ? i-- : i++;
				this.Groups.Add(testgr);

				ListViewGroupEx testgr2 = new ListViewGroupEx();
				testgr2.Items = this.Items.Where(w => w.DisplayName.ToUpperInvariant().First() >= Char.Parse("I") && w.DisplayName.ToUpperInvariant().First() <= Char.Parse("P")).ToArray();
				testgr2.Header = String.Format("I - P ({0})", testgr2.Items.Count());
				testgr2.Index = reversed ? i-- : i++;
				this.Groups.Add(testgr2);

				ListViewGroupEx testgr3 = new ListViewGroupEx();
				testgr3.Items = this.Items.Where(w => w.DisplayName.ToUpperInvariant().First() >= Char.Parse("Q") && w.DisplayName.ToUpperInvariant().First() <= Char.Parse("Z")).ToArray();
				testgr3.Header = String.Format("Q - Z ({0})", testgr3.Items.Count());
				testgr3.Index = reversed ? i-- : i++;
				this.Groups.Add(testgr3);

				if (reversed)
					this.Groups.Reverse();

				foreach (var group in this.Groups)
				{
					var nativeGroup = group.ToNativeListViewGroup();
					User32.SendMessage(this.LVHandle, LVM_INSERTGROUP, -1, ref nativeGroup);
				}

			} else
			if (col.CollumnType == typeof(long)) {
				var j = reversed ? 7 : 0;
				ListViewGroupEx uspec = new ListViewGroupEx();
				uspec.Items = this.Items.Where(w => w.IsFolder).ToArray();
				uspec.Header = String.Format("Unspecified ({0})", uspec.Items.Count());
				uspec.Index = reversed ? j-- : j++;
				this.Groups.Add(uspec);

				ListViewGroupEx testgrn = new ListViewGroupEx();
				testgrn.Items = this.Items.Where(w => Convert.ToInt64(w.GetPropertyValue(col.pkey, typeof(long)).Value) == 0 && !w.IsFolder).ToArray();
				testgrn.Header = String.Format("Empty ({0})", testgrn.Items.Count());
				testgrn.Index = reversed ? j-- : j++;
				this.Groups.Add(testgrn);

				ListViewGroupEx testgr = new ListViewGroupEx();
				testgr.Items = this.Items.Where(w => Convert.ToInt64(w.GetPropertyValue(col.pkey, typeof(long)).Value) > 0 && Convert.ToInt64(w.GetPropertyValue(col.pkey, typeof(long)).Value) <= 10 * 1024).ToArray();
				testgr.Header = String.Format("Very Small ({0})", testgr.Items.Count());
				testgr.Index = reversed ? j-- : j++;
				this.Groups.Add(testgr);

				ListViewGroupEx testgr2 = new ListViewGroupEx();
				testgr2.Items = this.Items.Where(w => Convert.ToInt64(w.GetPropertyValue(col.pkey, typeof(long)).Value) > 10 * 1024 && Convert.ToInt64(w.GetPropertyValue(col.pkey, typeof(long)).Value) <= 100 * 1024).ToArray();
				testgr2.Header = String.Format("Small ({0})", testgr2.Items.Count());
				testgr2.Index = reversed ? j-- : j++;
				this.Groups.Add(testgr2);

				ListViewGroupEx testgr3 = new ListViewGroupEx();
				testgr3.Items = this.Items.Where(w => Convert.ToInt64(w.GetPropertyValue(col.pkey, typeof(long)).Value) > 100 * 1024 && Convert.ToInt64(w.GetPropertyValue(col.pkey, typeof(long)).Value) <= 1 * 1024 * 1024).ToArray();
				testgr3.Header = String.Format("Medium ({0})", testgr3.Items.Count());
				testgr3.Index = reversed ? j-- : j++;
				this.Groups.Add(testgr3);

				ListViewGroupEx testgr4 = new ListViewGroupEx();
				testgr4.Items = this.Items.Where(w => Convert.ToInt64(w.GetPropertyValue(col.pkey, typeof(long)).Value) > 1 * 1024 * 1024 && Convert.ToInt64(w.GetPropertyValue(col.pkey, typeof(long)).Value) <= 16 * 1024 * 1024).ToArray();
				testgr4.Header = String.Format("Big ({0})", testgr4.Items.Count());
				testgr4.Index = reversed ? j-- : j++;
				this.Groups.Add(testgr4);

				ListViewGroupEx testgr5 = new ListViewGroupEx();
				testgr5.Items = this.Items.Where(w => Convert.ToInt64(w.GetPropertyValue(col.pkey, typeof(long)).Value) > 16 * 1024 * 1024 && Convert.ToInt64(w.GetPropertyValue(col.pkey, typeof(long)).Value) <= 128 * 1024 * 1024).ToArray();
				testgr5.Header = String.Format("Huge ({0})", testgr5.Items.Count());
				testgr5.Index = reversed ? j-- : j++;
				this.Groups.Add(testgr5);

				ListViewGroupEx testgr6 = new ListViewGroupEx();
				testgr6.Items = this.Items.Where(w => Convert.ToInt64(w.GetPropertyValue(col.pkey, typeof(long)).Value) > 128 * 1024 * 1024).ToArray();
				testgr6.Header = String.Format("Gigantic ({0})", testgr6.Items.Count());
				testgr6.Index = reversed ? j-- : j++;
				this.Groups.Add(testgr6);

				if (reversed)
					this.Groups.Reverse();

				foreach (var group in this.Groups)
				{
					var nativeGroup = group.ToNativeListViewGroup();
					User32.SendMessage(this.LVHandle, LVM_INSERTGROUP, -1, ref nativeGroup);
				}
			}
			else
			{

			}


			this.LastGroupCollumn = col;
			this.LastGroupOrder = reversed ? SortOrder.Descending : SortOrder.Ascending;
			
			if (this.Collumns[this.LastSortedColumnIndex] == col || reversed)
				this.SetSortIcon(this.Collumns.IndexOf(col), this.LastGroupOrder);

			RefreshItemsCountInternal();
		}

		public void SetGroupOrder(Boolean reverse = true)
		{
			this.GenerateGroupsFromColumn(this.LastGroupCollumn, reverse ? this.LastGroupOrder == SortOrder.Ascending : false);
		}

		public ShellItem GetFirstSelectedItem() {
			LVITEMINDEX lvi = new LVITEMINDEX();
			lvi.iItem = -1;
			lvi.iGroup = 0;
			User32.SendMessage(this.LVHandle, LVM.GETNEXTITEMINDEX, ref lvi, LVNI.LVNI_SELECTED);
			if (lvi.iItem == -1) return null;
			return this.Items[lvi.iItem];
		}

		public int GetFirstSelectedItemIndex() {
			LVITEMINDEX lvi = new LVITEMINDEX();
			lvi.iItem = -1;
			lvi.iGroup = 0;
			User32.SendMessage(this.LVHandle, LVM.GETNEXTITEMINDEX, ref lvi, LVNI.LVNI_SELECTED);
			if (lvi.iItem == -1) return -1;
			return lvi.iItem;
		}

		public void _ShieldLoadingThreadRun() {
			while (true) {
				//Application.DoEvents();

				//while (shieldQueue.Count == 0)
				//{
				//	Thread.Sleep(5);
				//}
				resetEvent.WaitOne();
				Thread.Sleep(4);
				try {
					var index = shieldQueue.Dequeue();
					//Application.DoEvents();
					var shoTemp = Items[index];
					ShellItem sho = !(shoTemp.IsNetDrive || shoTemp.IsNetworkPath) && shoTemp.ParsingName.StartsWith("::") ? shoTemp : new ShellItem(shoTemp.ParsingName);

					var shieldOverlay = 0;
					if ((sho.GetShield() & IExtractIconPWFlags.GIL_SHIELD) != 0) {
						shieldOverlay = ShieldIconIndex;
					}

					shoTemp.IsShielded = shieldOverlay;
					if (shieldOverlay > 0) {
						this.RedrawItem(index);
					}

					//Application.DoEvents();
				}
				catch {
				}
			}
		}

		public void _OverlaysLoadingThreadRun() {
			while (true) {
				//Application.DoEvents();

				//while (overlayQueue.Count == 0)
				//{
				//	Thread.Sleep(5);
				//}
				Thread.Sleep(3);
				try {
					var index = overlayQueue.Dequeue();

					//if (this.Cancel)
					//	continue;
					//Application.DoEvents();
					var shoTemp = Items[index];
					ShellItem sho = !(shoTemp.IsNetDrive || shoTemp.IsNetworkPath) &&  shoTemp.ParsingName.StartsWith("::") ? shoTemp : new ShellItem(shoTemp.ParsingName);

					int overlayIndex = 0;
					small.GetIconIndexWithOverlay(sho.Pidl, out overlayIndex);
					shoTemp.OverlayIconIndex = overlayIndex;
					if (overlayIndex > 0)
						RedrawItem(index);
					resetEvent.WaitOne();
					//Application.DoEvents();
				}
				catch (Exception) {
				}
			}
		}

		public async void _IconsLoadingThreadRun() {
			while (true) {
				//resetEvent.WaitOne();
				Thread.Sleep(1);
				//Application.DoEvents();

				try {
					var index = waitingThumbnails.Dequeue();
					//if (User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_ISITEMVISIBLE, index, 0) == IntPtr.Zero)
					//	continue;
					//var itemBounds = new User32.RECT();
					//LVITEMINDEX lvi = new LVITEMINDEX();
					//lvi.iItem = index;
					//lvi.iGroup = this.GetGroupIndex(index);
					//User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMINDEXRECT, ref lvi, ref itemBounds);
					//Rectangle r = new Rectangle(itemBounds.Left, itemBounds.Top, itemBounds.Right - itemBounds.Left, itemBounds.Bottom - itemBounds.Top);
					//if (!r.IntersectsWith(this.ClientRectangle))
					//	continue;
					resetEvent.WaitOne();
					var sho = Items[index];
					ShellItem temp = !(sho.IsNetDrive || sho.IsNetworkPath) && sho.ParsingName.StartsWith("::") ? sho : new ShellItem(sho.ParsingName);

					var icon = temp.GetShellThumbnail(IconSize, ShellThumbnailFormatOption.IconOnly, ShellThumbnailRetrievalOption.Default);
					if (icon != null) {
						sho.IsIconLoaded = true;
						//if (!cache.ContainsKey(index)) {
						//	cache.TryAdd(index, new Bitmap(icon));
						//	this.RedrawItem(index);
						//	icon.Dispose();
						//	icon = null;
						//}
						//else {
						//	cache[index] = new Bitmap(icon);
						//	this.RedrawItem(index);
						//	icon.Dispose();
						//	icon = null;
						//}
						this.RedrawItem(index);
					}

					//Application.DoEvents();
				}
				catch {
				}
			}
		}

		public async void _IconCacheLoadingThreadRun() {
			while (true) {
				resetEvent.WaitOne();
				Thread.Sleep(1);
				try {
					//Application.DoEvents();

					var index = ThumbnailsForCacheLoad.Dequeue();

					if (index >= Items.Count) {
						continue;
					}

					var sho = Items[index];
					var thumb = sho.GetShellThumbnail(IconSize, ShellThumbnailFormatOption.ThumbnailOnly, ShellThumbnailRetrievalOption.Default);
					sho.IsThumbnailLoaded = true;
					if (thumb != null) {
						this.RedrawItem(index);
						thumb.Dispose();
						thumb = null;
					}

					//Application.DoEvents();
				}
				catch {
				}
			}
		}

		public void _UpdateSubitemValuesThreadRun() {
			while (true) {
				resetEvent.WaitOne();
				Thread.Sleep(1);
				var index = ItemsForSubitemsUpdate.Dequeue();
				//if (this.Cancel)
				//	continue;
				try {
					if (User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_ISITEMVISIBLE, index.Item1, 0) != IntPtr.Zero) {
						//	continue;
						//if (this.Cancel)
						//	continue;
						//Application.DoEvents();

						var currentItem = Items[index.Item1];
						ShellItem temp = null;
						if (!(currentItem.IsNetDrive || currentItem.IsNetworkPath) && !currentItem.ParsingName.StartsWith("::"))
						{
							temp = new ShellItem(currentItem.ParsingName);
						}
						else {
							temp = currentItem;
						}
						int hash = currentItem.GetHashCode();
						IShellItem2 isi2 = (IShellItem2)temp.m_ComInterface;
						var pvar = new PropVariant();
						var pk = index.Item3;
						Guid guid = new Guid(InterfaceGuids.IPropertyStore);
						IPropertyStore propStore = null;
						isi2.GetPropertyStore(GetPropertyStoreOptions.Default, ref guid, out propStore);
						if (propStore != null && propStore.GetValue(ref pk, pvar) == HResult.S_OK) {
							if (SubItemValues.ToArray().Count(c => c.Item1 == hash && c.Item2.fmtid == pk.fmtid && c.Item2.pid == pk.pid) == 0) {
								SubItemValues.Add(new Tuple<int, PROPERTYKEY, object>(hash, pk, pvar.Value));
								this.RedrawItem(index.Item1);
							}
							pvar.Dispose();
						}
					}
				}
				catch {
					//User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_UPDATE, index.Item1, 0);
				}
			}
		}

		/// <summary> Runs an application as an another user. </summary>
		/// <param name="ExePath">  The path of the application. </param>
		/// <param name="username"> The path of the username to use. </param>
		public static void RunExeAsAnotherUser(string ExePath, string username) {
			var psi = new ProcessStartInfo {
				FileName = ExePath,
				Verb = "runas",
				UseShellExecute = true,
				Arguments = String.Format("/env /user:{0} \"{1}\"", username, ExePath),
			};
			Process.Start(psi);
		}

		public static void StartCompartabilityWizzard() {
			Process.Start("msdt.exe", "-id PCWDiagnostic");
		}

		public void CleanupDrive() {
			string DriveLetter = "";
			if (SelectedItems.Count > 0) {
				if (Directory.GetLogicalDrives().Contains(SelectedItems[0].ParsingName)) {
					DriveLetter = SelectedItems[0].ParsingName;
				}
				else {
					DriveLetter = this.CurrentFolder.ParsingName;
				}
			}
			else {
				DriveLetter = this.CurrentFolder.ParsingName;
			}
			Process.Start("Cleanmgr.exe", "/d" + DriveLetter.Replace(":\\", ""));
		}

		public string CreateNewFolder() {
			string name = "New Folder";
			int suffix = 0;

			do {
				if (this.CurrentFolder.Parent != null) {
					if (this.CurrentFolder.Parent.ParsingName == KnownFolders.Libraries.ParsingName) {
						ShellLibrary lib = ShellLibrary.Load(this.CurrentFolder.DisplayName, true);
						name = String.Format("{0}\\New Folder ({1})", lib.DefaultSaveFolder, ++suffix);
						lib.Close();
					}
					else
						name = String.Format("{0}\\New Folder ({1})", this.CurrentFolder.ParsingName, ++suffix);
				}
				else
					name = String.Format("{0}\\New Folder ({1})",
													this.CurrentFolder.ParsingName, ++suffix);
			} while (Directory.Exists(name) || File.Exists(name));

			ERROR result = Shell32.SHCreateDirectory(IntPtr.Zero, name);

			switch (result) {
				case ERROR.FILE_EXISTS:
				case ERROR.ALREADY_EXISTS:
					throw new IOException("The directory already exists");
				case ERROR.BAD_PATHNAME:
					throw new IOException("Bad pathname");
				case ERROR.FILENAME_EXCED_RANGE:
					throw new IOException("The filename is too long");
			}
			return name;
		}

		public string CreateNewFolder(string name) {
			int suffix = 0;
			string endname = name;

			do {
				if (this.CurrentFolder.Parent != null) {
					if (this.CurrentFolder.Parent.ParsingName == KnownFolders.Libraries.ParsingName) {
						ShellLibrary lib =
														ShellLibrary.Load(this.CurrentFolder.DisplayName, true);
						endname = String.Format("{0}\\" + name + " ({1})", lib.DefaultSaveFolder, ++suffix);
						lib.Close();
					}
					else
						endname = String.Format("{0}\\" + name + " ({1})", this.CurrentFolder.ParsingName, ++suffix);
				}
				else
					endname = String.Format("{0}\\" + name + " ({1})", this.CurrentFolder.ParsingName, ++suffix);
			} while (Directory.Exists(endname) || File.Exists(endname));

			ERROR result = Shell32.SHCreateDirectory(IntPtr.Zero, endname);

			switch (result) {
				case ERROR.FILE_EXISTS:
				case ERROR.ALREADY_EXISTS:
					throw new IOException("The directory already exists");
				case ERROR.BAD_PATHNAME:
					throw new IOException("Bad pathname");
				case ERROR.FILENAME_EXCED_RANGE:
					throw new IOException("The filename is too long");
			}
			return endname;
		}

		public ShellLibrary CreateNewLibrary() {
			return CreateNewLibrary("New Library");
		}

		public ShellLibrary CreateNewLibrary(string name) {
			string endname = name;
			int suffix = 0;
			ShellLibrary lib = null;
			try {
				lib = ShellLibrary.Load(endname, true);
			}
			catch {
			}
			if (lib != null) {
				do {
					endname = String.Format(name + "({0})", ++suffix);
					try {
						lib = ShellLibrary.Load(endname, true);
					}
					catch {
						lib = null;
					}
				} while (lib != null);
			}

			return new ShellLibrary(endname, false);

			//return libcreate.GetDisplayName(DisplayNameType.Default);
		}

		public void SetLVBackgroundImage(Bitmap bitmap) {
			Helpers.SetListViewBackgroundImage(this.LVHandle, bitmap);
		}

		public HResult SetFolderIcon(string wszPath, string wszExpandedIconPath, int iIcon) {
			HResult hr;

			Shell32.LPSHFOLDERCUSTOMSETTINGS fcs = new Shell32.LPSHFOLDERCUSTOMSETTINGS();
			fcs.dwSize = (uint)Marshal.SizeOf(fcs);
			fcs.dwMask = Shell32.FCSM_ICONFILE;
			fcs.pszIconFile = wszExpandedIconPath.Replace(@"\\", @"\");
			fcs.cchIconFile = 0;
			fcs.iIconIndex = iIcon;

			// Set the folder icon
			hr = Shell32.SHGetSetFolderCustomSettings(ref fcs, wszPath.Replace(@"\\", @"\"), Shell32.FCS_FORCEWRITE);

			if (hr == HResult.S_OK) {
				// Update the icon cache
				SHFILEINFO sfi = new SHFILEINFO();
				var res = Shell32.SHGetFileInfo(Marshal.StringToHGlobalAuto(wszPath), 0, out sfi, (int)Marshal.SizeOf(sfi), SHGFI.ICONLOCATION);
				int iIconIndex = Shell32.Shell_GetCachedImageIndex(sfi.szDisplayName.Replace(@"\\", @"\"), sfi.iIcon, 0);
				Shell32.SHUpdateImage(sfi.szDisplayName.Replace(@"\\", @"\"), sfi.iIcon, 0, iIconIndex);
				//RefreshExplorer();
				Shell32.SHChangeNotify(Shell32.HChangeNotifyEventID.SHCNE_UPDATEIMAGE,
				Shell32.HChangeNotifyFlags.SHCNF_DWORD | Shell32.HChangeNotifyFlags.SHCNF_FLUSHNOWAIT, IntPtr.Zero, (IntPtr)sfi.iIcon);
			}

			Items[this.GetFirstSelectedItemIndex()] = new ShellItem(wszPath);
			//this.UpdateItem(this.SelectedIndexes[0]);
			this.RefreshItem(this.SelectedIndexes[0]);
			return hr;
		}

		public HResult ClearFolderIcon(string wszPath) {
			HResult hr;

			Shell32.LPSHFOLDERCUSTOMSETTINGS fcs = new Shell32.LPSHFOLDERCUSTOMSETTINGS();
			fcs.dwSize = (uint)Marshal.SizeOf(fcs);
			fcs.dwMask = Shell32.FCSM_ICONFILE;
			hr = Shell32.SHGetSetFolderCustomSettings(ref fcs, wszPath, Shell32.FCS_FORCEWRITE);
			if (hr == HResult.S_OK) {
				// Update the icon cache
				SHFILEINFO sfi = new SHFILEINFO();
				Shell32.SHGetFileInfo(Marshal.StringToHGlobalAuto(wszPath.Replace(@"\\", @"\")), 0, out sfi, (int)Marshal.SizeOf(sfi), SHGFI.ICONLOCATION);
				int iIconIndex = Shell32.Shell_GetCachedImageIndex(sfi.szDisplayName.Replace(@"\\", @"\"), sfi.iIcon, 0);
				Shell32.SHUpdateImage(sfi.szDisplayName.Replace(@"\\", @"\"), sfi.iIcon, 0, iIconIndex);
				Shell32.SHChangeNotify(Shell32.HChangeNotifyEventID.SHCNE_UPDATEIMAGE,
				Shell32.HChangeNotifyFlags.SHCNF_DWORD | Shell32.HChangeNotifyFlags.SHCNF_FLUSHNOWAIT, IntPtr.Zero, (IntPtr)sfi.iIcon);
			}
			Items[this.SelectedIndexes[0]] = new ShellItem(wszPath);
			//this.UpdateItem(this.SelectedIndexes[0]);
			this.RefreshItem(this.SelectedIndexes[0]);
			return hr;
		}

		public void DefragDrive() {
			string DriveLetter = "";
			if (SelectedItems.Count > 0) {
				if (Directory.GetLogicalDrives().Contains(SelectedItems[0].ParsingName)) {
					DriveLetter = SelectedItems[0].ParsingName;
				}
				else {
					DriveLetter = this.CurrentFolder.ParsingName;
				}
			}
			else {
				DriveLetter = this.CurrentFolder.ParsingName;
			}
			Process.Start(Path.Combine(Environment.SystemDirectory, "dfrgui.exe"), "/u /v " + DriveLetter.Replace("\\", ""));
		}

		public void DeSelectAllItems() {
			LVITEM item = new LVITEM();
			item.mask = LVIF.LVIF_STATE;
			item.stateMask = LVIS.LVIS_SELECTED;
			item.state = 0;
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMSTATE, -1, ref item);
			this.Focus();
		}

		public void DeselectItemByIndex(int index) {
			LVITEM item = new LVITEM();
			item.mask = LVIF.LVIF_STATE;
			item.stateMask = LVIS.LVIS_SELECTED;
			item.state = 0;
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMSTATE, index, ref item);
		}

		public void RemoveDropHighLightItemByIndex(int index) {
			LVITEM item = new LVITEM();
			item.mask = LVIF.LVIF_STATE;
			item.stateMask = LVIS.LVIS_SELECTED | LVIS.LVIS_DROPHILITED;
			item.state = 0;
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMSTATE, index, ref item);
		}

		/// <summary> Gives the ShellListView focus </summary>
		public void Focus() {
			if (!this._IsInRenameMode)
				User32.SetFocus(this.LVHandle);
		}

		public void FormatDrive(IntPtr handle) {
			string DriveLetter = "";
			if (SelectedItems.Count > 0) {
				if (Directory.GetLogicalDrives().Contains(SelectedItems[0].ParsingName)) {
					DriveLetter = SelectedItems[0].ParsingName;
				}
				else {
					DriveLetter = this.CurrentFolder.ParsingName;
				}
			}
			else {
				DriveLetter = this.CurrentFolder.ParsingName;
			}
			Shell32.FormatDrive(handle, DriveLetter);
		}

		public int GetItemsCount() {
			return this.Items.Count;
		}

		public int GetSelectedCount() {
			return (int)User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETSELECTEDCOUNT, 0, 0);
		}

		public void InvertSelection() {
			int itemCount = (int)User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMCOUNT, 0, 0);

			for (int n = 0; n < itemCount; ++n) {
				var state = User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_GETITEMSTATE, n, LVIS.LVIS_SELECTED);

				LVITEM item_new = new LVITEM();
				item_new.mask = LVIF.LVIF_STATE;
				item_new.stateMask = LVIS.LVIS_SELECTED;
				item_new.state = (state & LVIS.LVIS_SELECTED) == LVIS.LVIS_SELECTED ? 0 : LVIS.LVIS_SELECTED;
				User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMSTATE, n, ref item_new);
			}
			this.Focus();
		}

		#endregion Public Methods

		#region Private Methods

		private int GetGroupIndex(int itemIndex) {
			if (itemIndex == -1) return 0;
			var item = this.Items[itemIndex];
			foreach (var group in this.Groups) {
				if (group.Items.Count(c => c == item) > 0)
					return group.Index;
			}
			return 0;
		}

		private static BitmapFrame CreateResizedImage(IntPtr hBitmap, int width, int height, int margin) {
			var source = Imaging.CreateBitmapSourceFromHBitmap(
															hBitmap,
															IntPtr.Zero,
															System.Windows.Int32Rect.Empty,
															BitmapSizeOptions.FromEmptyOptions()).Clone();
			Gdi32.DeleteObject(hBitmap);

			var group = new DrawingGroup();
			RenderOptions.SetBitmapScalingMode(
											group, BitmapScalingMode.Fant);
			group.Children.Add(
											new ImageDrawing(source,
																			new Rect(0, 0, width, height)));
			var targetVisual = new DrawingVisual();
			var targetContext = targetVisual.RenderOpen();
			targetContext.DrawDrawing(group);
			var target = new RenderTargetBitmap(
											width, height, 96, 96, PixelFormats.Default);
			targetContext.Close();
			target.Render(targetVisual);
			return BitmapFrame.Create(target);
		}

		/// <summary>
		/// Returns the index of the first item whose display name starts with the search string.
		/// </summary>
		/// <param name="search">     The string for which to search for. </param>
		/// <param name="startindex">
		/// The index from which to start searching. Enter '0' to search all items.
		/// </param>
		/// <returns> The index of an item within the list view. </returns>
		private int GetFirstIndexOf(string search, int startindex) {
			bool found = false;
			int i = startindex;

			while (found == false) {
				if (i < Items.Count) {
					if (Items[i].GetDisplayName(SIGDN.NORMALDISPLAY).ToUpperInvariant().StartsWith(search.ToUpperInvariant())) {
						found = true;
					}
					else {
						i++;
					}
				}
				else {
					found = true;
					i = -1;
				}
			}

			return i;
		}

		private string GetStringFromAcceptedKeyCodeString(string str) {
			str = str.ToUpperInvariant();
			if (str.Length == 1) {
				return str;
			}
			else if (str == "SPACE") {
				return " ";
			}
			else if (str == "OEMPERIOD") {
				return ".";
			}
			else if (str == "OEMMINUS") {
				return "-";
			}
			else {
				return "";
			}
		}

		private void StartProcessInCurrentDirectory(ShellItem item) {
			var psi = new ProcessStartInfo();
			psi.FileName = item.ParsingName;
			psi.WorkingDirectory = this.CurrentFolder.ParsingName;
			Process.Start(psi);
		}

		private void RedrawWindow() {
			//User32.RedrawWindow(this.LVHandle, IntPtr.Zero, IntPtr.Zero,
			//										 0x0001/*RDW_INVALIDATE*/);
			User32.InvalidateRect(this.LVHandle, IntPtr.Zero, false);
		}

		private void RedrawWindow(User32.RECT rect) {
			//User32.InvalidateRect(this.LVHandle, ref rect, false);
			User32.RedrawWindow(this.LVHandle, ref rect, IntPtr.Zero,
													 0x0001/*RDW_INVALIDATE*/| 0x100);
			//User32.UpdateWindow(this.LVHandle);
		}

		internal void OnGotFocus() {
			if (GotFocus != null) {
				GotFocus(this, EventArgs.Empty);
			}
		}

		internal void OnLostFocus() {
			if (LostFocus != null) {
				LostFocus(this, EventArgs.Empty);
			}
		}

		internal void OnSelectionChanged() {
			if (SelectionChanged != null) {
				SelectionChanged(this, EventArgs.Empty);
			}
		}

		internal void OnItemMiddleClick() {
			if (ItemMiddleClick != null) {
				var row = -1;
				var column = -1;
				this.HitTest(this.PointToClient(Cursor.Position), out row, out column);
				if (row != -1 && this.Items[row].IsFolder) {
					ItemMiddleClick.Invoke(this, new NavigatedEventArgs(this.Items[row]));
				}
			}
		}

		private void ResumeLayout()
		{
			User32.SendMessage(this.LVHandle, (int)WM.WM_SETREDRAW, 1, 0);
		}
		private void SuspendLayout()
		{
			User32.SendMessage(this.LVHandle, (int)WM.WM_SETREDRAW, 0, 0);
		}
		private void RefreshItemsCountInternal()
		{
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMCOUNT, 0, 0);
			User32.SendMessage(this.LVHandle, BExplorer.Shell.Interop.MSG.LVM_SETITEMCOUNT, this.Items.Count, 0);
		}
		#endregion Private Methods

		#region Unmanaged

		[UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
		public delegate int funcInvoke(IntPtr refer, [In, MarshalAs(UnmanagedType.Interface)] System.Runtime.InteropServices.ComTypes.IDataObject pdo);

		#endregion Unmanaged

		public void AutosizeAllColumns(int autosizeParam)
		{
			this.SuspendLayout();
			for (int i = 0; i < this.Collumns.Count; i++) {
				User32.SendMessage(this.LVHandle, LVM.SETCOLUMNWIDTH, i, autosizeParam);
			}
			this.ResumeLayout();
		}
	}
}