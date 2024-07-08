using System;
using System.Collections.Generic;
using System.Web;
using System.IO;
using MDTools.Web;
using MDTools.Web.Utils;


namespace MDTools.Web.Handler {

	public static class HandlerHelper {

		/// <summary>
		/// Parameter:
		///   L=(negative number for seconds or positive number for minutes) 
		/// </summary>
		public static bool SetExpiresLParameter( HttpContext Context ) {
			Context.Response.Cache.SetCacheability( HttpCacheability.Public );
			// Change long-life cache based on l param
			var l = Context.Request["l"];
			if( !string.IsNullOrEmpty( l ) ) {
				int lim = 0;
				int.TryParse( l, out lim );// negative value is seconds, positive is minutes
				Context.Response.SetExpiresFromNow( lim < 0 ? -lim : lim * 60 );
				return true;
			}
			return false;
		}

		/// <summary>
		/// Parameter:
		///   file=(optional path to file to use) 
		///   If file is not passed, the request url is used
		/// </summary>
		public static Url FileParmeterOrRequest( HttpRequest Request ) {
			string file = Request.QueryString["file"];
			return string.IsNullOrEmpty( file ) ? new Url( Request.Url ) : new Url( file );
		}

		/// <summary>
		/// Verify the file and send 404 if not present
		/// </summary>
		public static void Send404IfFileDoesntExist( this HttpContext Context, string virtPath ) {
			Send404IfFileDoesntExist( Context.Response, Context.Server.MapPath( virtPath ) );
		}
		/// <summary>
		/// Verify the file and send 404 if not present
		/// </summary>
		public static void Send404IfFileDoesntExist( this HttpResponse Response, string fullFilePath ) {
			if( !File.Exists( fullFilePath ) ) {
				Response.StatusCode = (int)System.Net.HttpStatusCode.NotFound;//404
				Response.StatusDescription = "File Not Found";
				Response.AddHeader( "Content-Length", "0" );
				Response.SuppressContent = true;
				Response.End();
			}
		}

	}

}
