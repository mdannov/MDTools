using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.Adapters;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.Adapters;
using MDTools.Web.Utils;

/// <summary>
/// To use these controls, must create App_Browsers and place a file inside that lists:
/// <browsers>
///	<browser refID="Default">
///		<controlAdapters>
///			<adapter controlType="System.Web.UI.HtmlControls.HtmlGenericControl"
///			   adapterType="MDTools.VersionAdapter" />
///			<adapter controlType="System.Web.UI.HtmlControls.HtmlLink"
///			   adapterType="MDTools.LinkAdapter" />
///		</controlAdapters>
///	</browser>
///</browsers>
/// </summary>

namespace MDTools.Web.Controls {

	public class LinkAdapter : ControlAdapter {
		protected override void Render( HtmlTextWriter writer ) {
			if(VersionParameter.AS_VersionActive)
				VersionParameter.ProcessHtmlLink( Page.Server, (HtmlLink)base.Control );
			base.Render( writer );
		}
	}

	public class VersionAdapter : ControlAdapter {
		protected override void Render( HtmlTextWriter writer ) {
			if( VersionParameter.AS_VersionActive )
				VersionParameter.ProcessHtmlLink( Page.Server, (HtmlGenericControl)base.Control );
			base.Render( writer );
		}
	}

}
