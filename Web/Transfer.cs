using System;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using MDTools.Data;

namespace MDTools.Web {

	/// <summary>
	/// Summary description for Page
	/// </summary>
	public static class Server {
	
		public const string ORIGINAL_PATH = "$orig_path";

		public static void RewriteTransfer( this HttpServerUtility Server, HttpContext Context, string to ) {
//			Context.Items[ORIGINAL_PATH] = Context.Request.Path;
//			Context.RewritePath( to );
			Server.Execute( to );
		}

		public static void RewriteRestore( this HttpServerUtility Server, HttpContext Context) {
//			string path = Context.Items[ORIGINAL_PATH] as string;
//			if(!string.IsNullOrEmpty(path))
//				Context.RewritePath( path );
		}




	}

}
