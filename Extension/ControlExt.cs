using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

namespace MDTools.Extension {

#if WebForms
	public static class ControlExt {

		public static TYPE FindControl<TYPE>( this Control FromCtl, int occurence = 0 ) where TYPE : Control {
			for( int i = 0, len = FromCtl.Controls.Count; i < len; i++ ) {
				// Find all top-level button controls
				var ctl = FromCtl.Controls[i];
				if( ctl is TYPE ) {
					if( occurence-- <= 0 )
						return (TYPE)ctl;
				}
				if( ctl.Controls.Count > 0 ) {
					var fnd = ctl.FindControl<TYPE>( occurence );
					if( fnd != null )
						return fnd;
				}
			}
			return null;
		}
		public static IEnumerable<TYPE> FindControls<TYPE>( this Control FromCtl ) {
			return FromCtl.Controls.OfType<TYPE>();
		}

	}
#endif

}
