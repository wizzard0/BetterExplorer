﻿//Copyright (c) Microsoft Corporation.  All rights reserved.

using BExplorer.Shell.Interop;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BExplorer.Shell {
	/// <summary>
	/// Represents a registered file system Known Folder
	/// </summary>
	public class FileSystemKnownFolder : ShellItem, IKnownFolder {
		#region Private Fields

		private IKnownFolderNative knownFolderNative;
		private KnownFolderSettings knownFolderSettings;

		#endregion

		#region Internal Constructors

		internal FileSystemKnownFolder(IShellItem shellItem) : base(shellItem) { }

		IShellItem nativeShellItem;
		internal FileSystemKnownFolder(IKnownFolderNative kf) {		
			Debug.Assert(kf != null);
			knownFolderNative = kf;

			// Set the native shell item
			// and set it on the base class (ShellObject)

			Guid guid = new Guid(InterfaceGuids.IShellItem);
			knownFolderNative.GetShellItem(0, ref guid, out nativeShellItem);
			base.m_ComInterface = nativeShellItem;
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="FileSystemKnownFolder"/> class.
		/// </summary>
		/// <remarks>
		/// Takes a <see cref="Uri"/> containing the location of the FileSystemKnownFolder. 
		/// This constructor accepts URIs using two schemes:
		///
		/// - file: A file or folder in the computer's filesystem, e.g.
		/// file:///D:/Folder
		/// - shell: A virtual folder, or a file or folder referenced from 
		/// a virtual folder, e.g. shell:///Personal/file.txt
		/// </remarks>
		/// <param name="uri">
		/// A <see cref="Uri"/> containing the location of the FileSystemKnownFolder.
		/// </param>
		public FileSystemKnownFolder(Uri uri)
			: base(uri) {

		}
		public FileSystemKnownFolder() {

		}
		/// <summary>
		/// Initializes a new instance of the <see cref="FileSystemKnownFolder"/> class.
		/// </summary>
		/// <remarks>
		/// Takes a <see cref="string"/> containing the location of the FileSystemKnownFolder. 
		/// This constructor accepts URIs using two schemes:
		///
		/// - file: A file or folder in the computer's filesystem, e.g.
		/// file:///D:/Folder
		/// - shell: A virtual folder, or a file or folder referenced from 
		/// a virtual folder, e.g. shell:///Personal/file.txt
		/// </remarks>
		/// <param name="path">
		/// A string containing a Uri with the location of the FileSystemKnownFolder.
		/// </param>
		public FileSystemKnownFolder(string path)
			: base(path) {

		}
		/// <summary>
		/// Initializes a new instance of the <see cref="FileSystemKnownFolder"/> class.
		/// </summary>
		/// <remarks>
		/// Takes an <see cref="Environment.SpecialFolder"/> containing the 
		/// location of the folder.
		/// </remarks>
		/// <param name="folder">
		/// An <see cref="Environment.SpecialFolder"/> containing the 
		/// location of the folder.
		/// </param>
		public FileSystemKnownFolder(Environment.SpecialFolder folder)
			: base(folder) {

		}
		/// <summary>
		/// Initializes a new instance of the <see cref="FileSystemKnownFolder"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a FileSystemKnownFolder which is a named child of <paramref name="parent"/>.
		/// </remarks>
		/// <param name="parent">
		/// The parent folder of the item.
		/// </param>
		/// <param name="name">
		/// The name of the child item.
		/// </param>
		public FileSystemKnownFolder(ShellItem parent, string name)
			: base(parent, name) {

		}
		internal FileSystemKnownFolder(IntPtr pidl)
			: base(pidl) {

		}
		internal FileSystemKnownFolder(ShellItem parent, IntPtr pidl)
			: base(parent, pidl) {

		}

		#endregion

		#region Private Members

		private KnownFolderSettings KnownFolderSettings {
			get {
				if (knownFolderNative == null) {
					// We need to get the PIDL either from the NativeShellItem,
					// or from base class's property (if someone already set it on us).
					// Need to use the PIDL to get the native IKnownFolder interface.

					// Get the PIDL for the ShellItem
					if (nativeShellItem != null && base.Pidl == IntPtr.Zero) {
						base.m_ComInterface = nativeShellItem;
					}

					// If we have a valid PIDL, get the native IKnownFolder
					if (base.Pidl != IntPtr.Zero) {
						knownFolderNative = KnownFolderHelper.FromPIDL(base.Pidl);
					}

					Debug.Assert(knownFolderNative != null);
				}

				// If this is the first time this property is being called,
				// get the native Folder Defination (KnownFolder properties)
				if (knownFolderSettings == null) {
					knownFolderSettings = new KnownFolderSettings(knownFolderNative);
				}

				return knownFolderSettings;
			}
		}

		#endregion

		#region IKnownFolder Members

		/// <summary>
		/// Gets the path for this known folder.
		/// </summary>
		/// <value>A <see cref="System.String"/> object.</value>
		//public override string Path
		//{
		//    get { return KnownFolderSettings.Path; }
		//}

		/// <summary>
		/// Gets the category designation for this known folder.
		/// </summary>
		/// <value>A <see cref="FolderCategory"/> value.</value>
		public FolderCategory Category {
			get { return KnownFolderSettings.Category; }
		}

		/// <summary>
		/// Gets this known folder's canonical name.
		/// </summary>
		/// <value>A <see cref="System.String"/> object.</value>
		public string CanonicalName {
			get { return KnownFolderSettings.CanonicalName; }
		}

		/// <summary>
		/// Gets this known folder's description.
		/// </summary>
		/// <value>A <see cref="System.String"/> object.</value>
		public string Description {
			get { return KnownFolderSettings.Description; }
		}

		/// <summary>
		/// Gets the unique identifier for this known folder's parent folder.
		/// </summary>
		/// <value>A <see cref="System.Guid"/> value.</value>
		public Guid ParentId {
			get { return KnownFolderSettings.ParentId; }
		}

		/// <summary>
		/// Gets this known folder's relative path.
		/// </summary>
		/// <value>A <see cref="System.String"/> object.</value>
		public string RelativePath {
			get { return KnownFolderSettings.RelativePath; }
		}


		/// <summary>
		/// Gets this known folder's tool tip text.
		/// </summary>
		/// <value>A <see cref="System.String"/> object.</value>
		public string Tooltip {
			get { return KnownFolderSettings.Tooltip; }
		}
		/// <summary>
		/// Gets the resource identifier for this 
		/// known folder's tool tip text.
		/// </summary>
		/// <value>A <see cref="System.String"/> object.</value>
		public string TooltipResourceId {
			get { return KnownFolderSettings.TooltipResourceId; }
		}

		/// <summary>
		/// Gets this known folder's localized name.
		/// </summary>
		/// <value>A <see cref="System.String"/> object.</value>
		public string LocalizedName {
			get { return KnownFolderSettings.LocalizedName; }
		}
		/// <summary>
		/// Gets the resource identifier for this 
		/// known folder's localized name.
		/// </summary>
		/// <value>A <see cref="System.String"/> object.</value>
		public string LocalizedNameResourceId {
			get { return KnownFolderSettings.LocalizedNameResourceId; }
		}

		/// <summary>
		/// Gets this known folder's security attributes.
		/// </summary>
		/// <value>A <see cref="System.String"/> object.</value>
		public string Security {
			get { return KnownFolderSettings.Security; }
		}

		/// <summary>
		/// Gets this known folder's file attributes, 
		/// such as "read-only".
		/// </summary>
		/// <value>A <see cref="System.IO.FileAttributes"/> value.</value>
		public System.IO.FileAttributes FileAttributes {
			get { return KnownFolderSettings.FileAttributes; }
		}

		/// <summary>
		/// Gets an value that describes this known folder's behaviors.
		/// </summary>
		/// <value>A <see cref="DefinitionOptions"/> value.</value>
		public DefinitionOptions DefinitionOptions {
			get { return KnownFolderSettings.DefinitionOptions; }
		}

		/// <summary>
		/// Gets the unique identifier for this known folder's type.
		/// </summary>
		/// <value>A <see cref="System.Guid"/> value.</value>
		public Guid FolderTypeId {
			get { return KnownFolderSettings.FolderTypeId; }
		}

		/// <summary>
		/// Gets a string representation of this known folder's type.
		/// </summary>
		/// <value>A <see cref="System.String"/> object.</value>
		public string FolderType {
			get { return KnownFolderSettings.FolderType; }
		}
		/// <summary>
		/// Gets the unique identifier for this known folder.
		/// </summary>
		/// <value>A <see cref="System.Guid"/> value.</value>
		public Guid FolderId {
			get { return KnownFolderSettings.FolderId; }
		}

		/// <summary>
		/// Gets a value that indicates whether this known folder's path exists on the computer. 
		/// </summary>
		/// <value>A bool<see cref="System.Boolean"/> value.</value>
		/// <remarks>If this property value is <b>false</b>, 
		/// the folder might be a virtual folder (<see cref="Category"/> property will
		/// be <see cref="FolderCategory.Virtual"/> for virtual folders)</remarks>
		public bool PathExists {
			get { return KnownFolderSettings.PathExists; }
		}

		/// <summary>
		/// Gets a value that states whether this known folder 
		/// can have its path set to a new value, 
		/// including any restrictions on the redirection.
		/// </summary>
		/// <value>A <see cref="RedirectionCapability"/> value.</value>
		public RedirectionCapability Redirection {
			get { return KnownFolderSettings.Redirection; }
		}

		#endregion




		FileAttributes IKnownFolder.FileAttributes {
			get { throw new NotImplementedException(); }
		}

		public new System.Collections.IEnumerator GetEnumerator() {
			throw new NotImplementedException();
		}

		public string Path {
			get { return KnownFolderSettings.Path; }
		}
	}
}
