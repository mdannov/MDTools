#if WinForms
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MDTools.Forms {
	public class ImmediateCallTimer {

		private bool Enabled { get; set; }
		private int Interval { get; set; }
		private object Tag { get; set; }
		private EventHandler Tick;

		public static void AddImmediate( Timer timer, EventHandler onEvent ) {
			AddImmediate( timer, onEvent, timer.Enabled );
		}
		public static void AddImmediate(Timer timer, EventHandler onEvent, bool enabled) {
			// Turn off all thread activity for the moment
			timer.Enabled = false;
			// Remove existing Tick event if already present
			timer.Tick -= onEvent;
			// Save the existing timer into store to be restored by FirstEvent
			var iTimer = new ImmediateCallTimer();
			iTimer.Enabled = enabled;
			iTimer.Interval = timer.Interval;
			iTimer.Tag = timer.Tag;
			iTimer.Tick = onEvent;
			// Switch requested Timer to our first time Carryover Event and invoke immediately
			timer.Tick += FirstEvent;
			timer.Interval = 1;// 1ms virtually immediately
			timer.Tag = iTimer;
			// Invoke immediately
			timer.Enabled = true;
		}

		private static void FirstEvent( object sender, EventArgs e ) {
			// Disable the timer for reset
			var timer = (Timer)sender;
			timer.Enabled = false;
			// Remove First Event
			timer.Tick -= FirstEvent;
			// Call Tick Event
			var iTimer = (ImmediateCallTimer)timer.Tag;
			try {
				iTimer.Tick( sender, e );
			}
			catch( Exception ex ) {
				// rethrow exception
				throw ( ex );
			}
			// Make sure the timer is restored to original state regardless of exception
			finally {
				// Restore Timer back to original settings/state
				timer.Interval = iTimer.Interval;
				timer.Tag = iTimer.Tag;
				timer.Tick += iTimer.Tick;
				// Enable Timer based on specified state
				timer.Enabled = iTimer.Enabled;
			}
		}

	}

}
#endif
