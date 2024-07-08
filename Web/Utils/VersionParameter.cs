using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.UI.HtmlControls;

namespace MDTools.Web {

	public static class VersionParameter {

		public readonly static bool AS_VersionActive = ConfigurationManager.AppSettings["VersionParameters"] == "1";

		public static string ProcessScript( string script ) {
			int srcpos = script.IndexOf( "src=\"" );
			if( srcpos < 0 )
				return script;
			int jspos = script.IndexOf( ".js\"" );
			if( jspos < 0 )
				return script;
			srcpos += 5;
			jspos += 3;
			string file = script.Substring( srcpos, jspos - srcpos );
			var path = HostingEnvironment.MapPath( file );
			if( !File.Exists( path ) )
				return script;
			var link = string.Concat( file.IndexOf( '?' ) < 0 ? "?" : "&", "ver=", File.GetLastWriteTime( path ).Ticks );
			return script.Insert( jspos, link );
		}

		public static void ProcessHtmlLink( HttpServerUtility Server, HtmlLink link ) {
			link.Href += "?ver=" + File.GetLastWriteTime( Server.MapPath( link.Href ) ).Ticks;
		}
		public static void ProcessHtmlLink( HttpServerUtility Server, HtmlGenericControl ctl ) {
			if( string.Compare( ctl.TagName, "link", true ) == 0 ) {
				var link = ctl.Attributes["href"];
				link += string.Concat( link.IndexOf( '?' ) < 0 ? "?" : "&", "ver=", File.GetLastWriteTime( Server.MapPath( link ) ).Ticks );
				ctl.Attributes["href"] = link;
			}
		}
	}

}
