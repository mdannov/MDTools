#define Yui
using System;
using System.Configuration;
using System.Web;
using System.IO;
using MDTools.Web;
using MDTools.Web.Utils;
using MDTools.IO;

#if Yui
using Yahoo.Yui.Compressor;
using MDTools.IO;
#endif

namespace MDTools.Web.Handler {

	public class MinifyHandler : IHttpHandler {

		public static readonly int AS_MaxCacheFileSize = Convert.ToInt32( ConfigurationManager.AppSettings["MaxCacheFileSize"] );

		protected HttpContext Context;

		/// <summary>
		/// Minify handler with parameters
		/// 
		/// l=length of time to state image is in the response cache
		/// !! Make some way to use server side settings instead of params. Params are unsafe for attack
		/// </summary>
		public void ProcessRequest( HttpContext Context ) {
			this.Context = Context;
			var Request = Context.Request;
			var Response = Context.Response;
			var Server = Context.Server;

			// Explicit File referenced is the file to transmit
			var url = new Url( Context.Request );
			var virtpath = url.Path;

			// File must still exist
			Context.Send404IfFileDoesntExist( virtpath );

			// Change long-life cache based on l param
			HandlerHelper.SetExpiresLParameter( Context );

			// Check if Cache on Client is Valid against Physical File
			if( Context.Send304IfClientCacheValid( true, virtpath ) )
				return;

			var contenttype = ContentTypes.FromExtension( url.Extension );
			if( !string.IsNullOrEmpty( contenttype ) )
				Context.Response.ContentType = contenttype;

			// Minify based on contenttype
			switch( contenttype ) {
			case "text/javascript":
			case "application/javascript":
			case "application/x-javascript":
				SendContentByType(
					virtpath,
					() => {
						ServerCache.SendCachedOrPersistedText(
							Context, virtpath, MinifySettings.AS_GenStaticDirectory, delegate( string fullFilePath ) {
#if Yui
								return new JavaScriptCompressor().Compress( IOHelper.TextFromFile( fullFilePath ) );
#else
								return new JsminCs().Minify( IOHelper.TextFromFile( fullFilePath ), true, false );
#endif
						}, 
						true, null, AS_MaxCacheFileSize
						);
					}
				);
				break;
			case "text/css":
				SendContentByType(
					virtpath,
					() => {
						ServerCache.SendCachedOrPersistedText(
							Context, virtpath, MinifySettings.AS_GenStaticDirectory, delegate( string fullFilePath ) {
#if Yui
								return new CssCompressor().Compress( IOHelper.TextFromFile( fullFilePath ) );
#else
								return new JsminCs().Minify( IOHelper.TextFromFile( fullFilePath ), false, true );
#endif
						}, 
						true, null, AS_MaxCacheFileSize
						);
					}
				);
				break;
			case "text/html":
				SendContentByType(
					virtpath,
					() => {
						ServerCache.SendCachedOrPersistedText(
							Context, virtpath, MinifySettings.AS_GenStaticDirectory, 
							delegate( string fullFilePath ) {
								return MinifyHtml.CleanHtmlAndInlines( IOHelper.TextFromFile( fullFilePath ) );
							},
							true, null, AS_MaxCacheFileSize
						);
					}
				);
				break;
			case "text/xml":
			default:
				// Probably should handle a bunch of other static types that require ContentType 
				SendContentByType(
					virtpath,
					() => {
						ServerCache.SendCachedOrPersistedText(
							Context, virtpath, MinifySettings.AS_GenStaticDirectory, 
							delegate( string fullFilePath ) {
								return MinifyHtml.CleanHtml( IOHelper.TextFromFile( fullFilePath ) );
							},
							true, null, AS_MaxCacheFileSize
						);
					}
				);
				break;
			}

			Response.End();
		}

		/// <summary>
		/// Add ContentType and test if minify testing
		/// </summary>
		private void SendContentByType( string filepath, Action actionFn ) {
			if( !MinifySettings.AS_MinifyActive ) {
				ServerCache.SendTextFileOrCachePersistCompress( Context, filepath, MinifySettings.AS_GenStaticDirectory );
				return;
			}
			actionFn();
		}

		public bool IsReusable {
			get {
				return false;
			}
		}

	}

}
