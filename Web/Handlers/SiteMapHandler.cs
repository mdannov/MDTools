using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Xml;
using System.Text;
using System.Web;
using MDTools.Web;
using MDTools.Data;
using System.Threading;

namespace MDTools.Web.Handler {



	public abstract class GoogleSiteIndexHandler : SiteMapBaseHandler, IHttpHandler {
		protected override ISiteMap Init( HttpContext Context, string outputpath ) {
			return new GoogleSiteIndex( Context, outputpath );
		}
	}

	public abstract class GoogleSiteMapHandler : SiteMapBaseHandler, IHttpHandler {
		protected override ISiteMap Init(HttpContext Context, string outputpath) {
			return new GoogleSiteMap( Context, outputpath, GoogleSiteMap.SitemapStreamType.XmlTextFile );
		}
	}

	public abstract class SiteMapBaseHandler : IHttpHandler {

		/// <summary>
		/// This is the function the main application must implement to construct Google 
		///		map.AddPage(
		///			new GooglePage() {
		///				Url = value.url,// example: "www.mydomain.com/page",
		///				LastModifiedDateTime = value.FileDate,
		///				ChangeFreq=value.Frequency,
		///				Priority = value.Priority / 10m
		///			}
		///		);
		/// </summary>
		public abstract void AddPages( ISiteMap map, Uri uri );

		public HttpContext Context;
		protected static readonly Int16 AS_SiteMapExpireHours=20;//default
		protected static readonly string AS_SiteMapPath;

		static SiteMapBaseHandler() {
			var sitemapexpires = ConfigurationManager.AppSettings["SiteMapExpireHours"];
			if(!string.IsNullOrEmpty(sitemapexpires))
				Int16.TryParse( sitemapexpires, out AS_SiteMapExpireHours );
			AS_SiteMapPath = ConfigurationManager.AppSettings["SiteMapPath"];
		}

		public void ProcessRequest( HttpContext Context ) {

			this.Context = Context;
			var Response = Context.Response;

			//!! Check if file is cached and still valid
			var outputPath = AS_SiteMapPath;
			if( string.IsNullOrEmpty( outputPath ) ) 
				outputPath = "~/";

			// Create a memory stream xml file
			using( ISiteMap map = Init( Context, outputPath ) ) {

				Response.ContentType = map.ResponseType;

				bool StreamFile = false;

				// Check if File is already cached and valid
				DateTime FileDate = map.FileDate;
				DateTime FileDateExpires;
				if( FileDate > new DateTime( 2000, 1, 1 ) ) {

					FileDateExpires = FileDate.AddHours( AS_SiteMapExpireHours );

					// Check if File has expired - after 24hrs or whatever
					if( !CacheExt.IsExpiredNow( FileDateExpires ) ) {

						// Check if Client has item already
						if( Context.Send304IfClientCacheValid( FileDate ) )
							return;

						// Client doesn't have copy, so stream existing file
						StreamFile = true;

					}

				} else {
					StreamFile = true;
					FileDate = DateTime.Now;
					FileDateExpires = FileDate.AddHours( AS_SiteMapExpireHours );
				}

				if( StreamFile ) {

					var uri = Context.Request.Url;

					lock( fsLock ) {//!! this is dangerous but solves a multitasking issue when multiple crawlers come in
						// If another enters after lock, send them the cached
						if( Context.Send304IfClientCacheValid( FileDate ) )
							return;
						// Create the Site Index File Now
						map.Create();

						AddPages( map, uri );
		
						map.Close();
					}//lock

				}
				map.Close();//jic

				//Context.Response.SetExpires( FileDateExpires );//** Don't expire because you may want to force a rebuild
				Context.Response.SetLastModifiedDate( FileDate );

				// Stream site index xml
				ServerCache.SendTextFileOrPersistCompress( Context, '~' + map.Filepath, AS_SiteMapPath );
			}

		}
		private static readonly Object fsLock = new Object();

		protected abstract ISiteMap Init( HttpContext Context, string outputpath );

		public bool IsReusable {
			get {
				return false;
			}
		}

	}

}

