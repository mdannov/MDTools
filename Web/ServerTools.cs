using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MDTools.Web {

	public static class ServerExtTools {

		/// <summary>
		/// Turns off all output page caching for all Non-GET Methods like POST, DEL, PUT, etc.
		/// Ensures Ajax and Postback works without caching
		/// Call once, usually best in IModule's Init
		/// </summary>
		public static void DisableAllNonGetOutputCaching( this HttpApplication App ) {
			App.PostMapRequestHandler -= OnDisableNonGetCaching;// remove if existing
			App.PostMapRequestHandler += OnDisableNonGetCaching;
		}

		/// <summary>
		/// Limit Output Caching to GET Method only
		/// </summary>
		public static void OnDisableNonGetCaching( object sender, EventArgs e ) {
			var App = (HttpApplication)sender;
			if( !String.Equals( App.Context.Request.HttpMethod, "GET", StringComparison.InvariantCultureIgnoreCase ) )
				App.Context.Response.Cache.SetNoServerCaching();
		}

		//!! Move this to Event static Class
		public static bool IsEventHandlerRegistered( this EventHandler EventHandler, Delegate prospectiveHandler ) {
			if( EventHandler != null ) {
				foreach( Delegate existingHandler in EventHandler.GetInvocationList() ) {
					if( existingHandler == prospectiveHandler ) {
						return true;
					}
				}
			}
			return false;
		}

	}
}