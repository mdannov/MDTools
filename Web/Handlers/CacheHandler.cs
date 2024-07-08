using System;
using System.Configuration;
using System.Web;
using System.IO;
using MDTools.Web;
using MDTools.Web.Utils;


namespace MDTools.Web.Handler {

	public class CacheHandler : IHttpHandler {

		public static readonly int AS_MaxCacheFileSize = Convert.ToInt32( ConfigurationManager.AppSettings["MaxCacheFileSize"] );

		protected HttpContext Context;

		/// <summary>
		/// Cache handler with parameters - Send items to this handler if you wish to cache the items to memory for faster response
		/// 
		/// l=length of time to state image is in the response cache
		/// !! Make some way to use server side settings instead of params. Params are unsafe for attack
		/// </summary>
		public void ProcessRequest( HttpContext Context ) {

			// Explicit File referenced is the file to transmit
			var Request = Context.Request;
			var virtpath = Request.Url.Path();
			var fullpath = Context.Server.MapPath( virtpath );

			// File must still exist
			Context.Send404IfFileDoesntExist( fullpath );

			// Change long-life cache based on l param
			HandlerHelper.SetExpiresLParameter( Context );

			// Do not send if already cached
			if( Context.Send304IfClientCacheValid( false, fullpath ) )
				return;

			// Set ContentType
			var contenttype = ContentTypes.FromExtension( Request.Url.Extension() );
			Context.Response.ContentType = contenttype;

			// Send the file
			ServerCache.SendCachedBinary( Context, virtpath, null, ContentTypes.CanCompress(contenttype), null, AS_MaxCacheFileSize, true );

			Context.Response.End();
		}

		public bool IsReusable {
			get {
				return false;
			}
		}

	}

}
