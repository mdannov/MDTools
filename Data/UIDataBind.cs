using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;

namespace SceneCalendar.Web {

#if WebForms
	public static class UIDataLayer {
		/// <summary>
		/// Bind Data to a control for later lookup by that control's name
		/// </summary>
		public static void BindDataToControl<TYPE>( this HttpContext Context, System.Web.UI.Control ctl, TYPE data ) {
			Context.Items[ctl.ClientID + "-ctl"] = data;
		}
		/// <summary>
		/// Bind Data to a control for later lookup by that control's name and key
		/// </summary>
		public static void BindDataToControl<TYPE>( this HttpContext Context, System.Web.UI.Control ctl, TYPE data, string key ) {
			Context.Items[string.Concat(ctl.ClientID, "-ctl", key)] = data;
		}
		/// <summary>
		/// Bind Data to a control for later lookup by that control's name
		/// </summary>
		public static void BindDataToControl<TYPE>( this HttpContext Context, string ctlid, TYPE data ) {
			Context.Items[ctlid + "-ctl"] = data;
		}
		/// <summary>
		/// Bind Data to a control for later lookup by that control's name and key
		/// </summary>
		public static void BindDataToControl<TYPE>( this HttpContext Context, string ctlid, TYPE data, string key ) {
			Context.Items[string.Concat( ctlid, "-ctl", key )] = data;
		}
		/// <summary>
		/// Get data that was previously bound to the control by the control's name
		/// </summary>
		public static TYPE GetBoundData<TYPE>( this HttpContext Context, System.Web.UI.Control ctl ) {
			return (TYPE)Context.Items[ctl.ClientID + "-ctl"];
		}
		/// <summary>
		/// Get data that was previously bound to the control by the control's name and key
		/// </summary>
		public static TYPE GetBoundData<TYPE>( this HttpContext Context, System.Web.UI.Control ctl, string key ) {
			return (TYPE)Context.Items[string.Concat( ctl.ClientID, "-ctl", key )];
		}

		/// <summary>
		/// Bind List of Data to a new list of instantiated controls as children of a parent control for later lookup by each control's name
		/// Add EmptyMsg if you wish to bind a default label to the parent if empty
		/// Returns true if controls were made
		/// </summary>
		public static bool BindListToInstatiatedControls<ITEM>( this HttpContext Context, System.Web.UI.Control parent, IList<ITEM> data, string NewCtrl, string emptyMsg = null, string key=null ) {
			if( data == null )
				return false;
			int cnt = data.Count;
			if( cnt <= 0 && !string.IsNullOrEmpty( emptyMsg ) ) {
				var lbl = new System.Web.UI.WebControls.Label();
				lbl.Text = emptyMsg;
				parent.Controls.Add( lbl );
				return false;
			}
			// Save dates to page
			if( key == null ) {
				for( int i = 0; i < cnt; i++ )
					if( BindDataToInstatiatedControl<ITEM>( Context, parent, data[i], NewCtrl ) == null )
						return false;
			} else {
				for( int i = 0; i < cnt; i++ )
					if( BindDataToInstatiatedControl<ITEM>( Context, parent, data[i], NewCtrl, key ) == null )
						return false;
			}
			return true;
		}
		/// <summary>
		/// Bind Data to a new instantiated control as child of a parent for later lookup by the control's name
		/// Returns instantiated control
		/// </summary>
		public static System.Web.UI.Control BindDataToInstatiatedControl<ITEM>( this HttpContext Context, System.Web.UI.Control parent, ITEM data, string NewCtrl, string key = null ) {
			if( data == null )
				return null;
			// Load the control
			var newctl = parent.Page.LoadControl( NewCtrl );
			parent.Controls.Add( newctl );
			// Attach date data to the page by clientid name so when control renders, it can look it up by its own name
			if(key==null)
				BindDataToControl<ITEM>( Context, newctl, data );
			else
				BindDataToControl<ITEM>( Context, newctl, data, key);
			return newctl;
		}

	}
#endif
}
