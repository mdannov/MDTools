using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.Adapters;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.Adapters;
using MDTools.Web.Utils;

/// <summary>
/// To use these controls, must create App_Browsers and place a file inside that lists:
/// <browsers>
///	<browser refID="Default">
///		<controlAdapters>
///			<adapter controlType="System.Web.UI.LiteralControl"
///				   adapterType="MDTools.Web.Controls.LiteralAdapter" />
///		</controlAdapters>
///	</browser>
///</browsers>
/// </summary>

namespace MDTools.Web.Controls {


	// <script runat="server"> HtmlGenericControl 
	// <style runat="server"> HtmlGenericControl 
	// <link runat="server"> HtmlLink


	public class LiteralAdapter : ControlAdapter {
		protected override void Render( HtmlTextWriter writer ) {
			if( !MinifySettings.AS_MinifyActive && !VersionParameter.AS_VersionActive) {
				base.Render( writer );
				return;
			}
			var text = ( (LiteralControl)base.Control ).Text;
			if( string.IsNullOrEmpty( text ) )
				return;
			writer.Write( MinifyHtml.CleanHtmlAndInlines( text ) );
		}
	}

	/// <summary>
	/// General MinifyAdapter can be used for any webcontrol as it processes the base control's Render
	/// </summary>
	public class MinifyAdapter : ControlAdapter {
		protected override void Render( HtmlTextWriter writer ) {
			if( !MinifySettings.AS_MinifyActive ) {
				base.Render( writer );
				return;
			}
			StringBuilder sb = new StringBuilder();
			base.Render( new HtmlTextWriter( new StringWriter( sb ) ) );
			if( sb.Length == 0 )
				return;
			writer.Write( MinifyHtml.CleanHtmlAndInlines( sb.ToString() ) );
		}
	}

	public delegate void Renderer( HtmlTextWriter writer );

}
