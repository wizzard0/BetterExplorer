﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleControl {
	/// <summary>
	/// The ConsoleEventArgs are arguments for a console event.
	/// </summary>
	public class ConsoleEventArgs : EventArgs {

		/// <summary>
		/// Gets the content.
		/// </summary>
		public string Content { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConsoleEventArgs"/> class.
		/// </summary>
		/// <param name="content">The content.</param>
		public ConsoleEventArgs(string content = "") {
			//  Set the content.
			Content = content;
		}
	}
}
