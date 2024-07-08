using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web;
using MDTools.Extension;
using MDTools.Images;
using MDTools.IO;
using MDTools.Web;

namespace MDTools.Web.Handler {

	public class ImageHandler : IHttpHandler {

		public static readonly string AS_ImageViews = ConfigurationManager.AppSettings["ImageViews"];
		protected static Dictionary<string, Size> KeyedConstraints = new Dictionary<string, Size>() {
			// Defaults
			{ "t", new Size( 150, 225 ) },
			{ "m", new Size( 250, 375 ) },
			{ "l", new Size( 800, 800 ) }
		};// Default

		static ImageHandler() {
			// AS_ImageViews is passed as a coma-separated list with key and constraints in the value as follows (eg: t=150x150,l=300)
			if( string.IsNullOrEmpty( AS_ImageViews ) )
				return;
			var views = AS_ImageViews.Split(',');
			if( views == null || views.Length == 0 )
				return;
			for( int i = 0, len = views.Length; i < len; i++ ) {
				var kv = views[i].Split( '=' );
				switch( kv.Length ) {
				case 1:
					// Set no value allows programmer to delete default keys
					KeyedConstraints.Remove( kv[0] );
					continue;
				case 2:
					var key = kv[0];
					var nv = kv[1];
					var val = nv.Split( 'x' );
					var sz = new Size();
					switch( val.Length ) {
					case 1:
						sz.Width = sz.Height = Convert.ToInt32( val[0] );
						if( sz.Width <= 0 )
							continue;// invalid size
						break;
					case 2:
						sz.Width = Convert.ToInt32( val[0] );
						sz.Height = Convert.ToInt32( val[1] );
						if( sz.Width <= 0 || sz.Height <= 0 )
							continue;// invalid size
						break;
					case 0:
						// Set no value allows programmer to delete default keys
						KeyedConstraints.Remove( key );
						continue;
					default:
						continue;// invalid constraint construction
					}
					// Override existing or add
					KeyedConstraints[key] = sz;
					break;
				default: // invalid
					continue;
				}
			}

		}

		/// <summary>
		/// Image handler with parameters
		/// 
		/// Parameters: 
		///   file=file to send (otherwise request file is used)
		///   l=length of time to state image is in the response cache
		///   v=resize view to preset sizes by names 
		///   c=cache, save the item in cache, and retrieve from cache !!not-implemented
		/// !! Make some way to use server side settings instead of params. Params are unsafe for attack
		/// </summary>
		public void ProcessRequest( System.Web.HttpContext Context ) {
			var Request = Context.Request;
			var Response = Context.Response;
			var Server = Context.Server;

			// Verify content type is image for security reasons
			var contenttype = ContentTypes.FromExtension( Request.Url.Extension() );
			if( !contenttype.StartsWith( "image/" ) )
				throw new WebException( Request, (int)System.Net.HttpStatusCode.InternalServerError, "File must be image type" );

			var imgVirtPath = Request.Url.Path();
			var origFullPath = Server.MapPath( imgVirtPath );

			// Original file must still exist
			Response.Send404IfFileDoesntExist( origFullPath );

			// Change long-life cache based on l param
			HandlerHelper.SetExpiresLParameter( Context );

			string queryfix = null;
			string procFullPath = origFullPath;
			Size sz = default( Size );

			// Construct the path to the processed file for validation - Only V causes processing
			if(Request.QueryString.Count > 0) {
				queryfix = GetVParameter( Request, ref sz );
				if( !string.IsNullOrEmpty( queryfix ) )
					procFullPath = string.Concat( Path.GetDirectoryName( origFullPath ), @"\", ServerCache.GetPersistedName( origFullPath, true, false, queryfix ) );
			}

			// Do not send if already cached
			if( Context.Send304IfClientCacheValid( false, procFullPath ) )
				return;
	
			Response.ContentType = contenttype;

			try {
				// View
				if( sz.Width > 0 && sz.Height > 0 ) {
					// Thumbnail 180x180
#if !nonpersisted
					ServerCache.SendPersistedBinary(
						Context, imgVirtPath, Request.Url.PathOnly(),
						delegate( string fullFilePath ) {
							return ImageUtilities.ResizeImage( fullFilePath, sz.Width, sz.Height );
						}, 
						false,
						queryfix
					);
#else
						SendResizedImage( imgPath, sz.Width, sz.Height, Response.OutputStream );
#endif
					return;

				} else {

					// Now write the byte buffer to a response stream 
					Response.BinaryWrite( File.ReadAllBytes( origFullPath ) );
#if drawingvercanGDIerror
					// Default - Just send the image native size and format
					using( System.Drawing.Image Img = System.Drawing.Image.FromFile( imgPath ) ) {
						Img.Save( Response.OutputStream, Img.RawFormat );
					}
#endif
				}
			}
			catch( Exception ex ) {
				throw new HttpException( 404, imgVirtPath + '\n' + ex.Message );
			}

			Response.End();
		}

		protected string GetVParameter( HttpRequest Request, ref Size sz ) {
			var vKey = Request["v"];
			if( string.IsNullOrEmpty( vKey ) )
				return null;
			KeyedConstraints.TryGetValue( vKey, out sz );
			return string.Concat( sz.Width, 'x', sz.Height, '-' );
		}

		public static Size? GetVParameterSize( string parameter ) {
			if( string.IsNullOrEmpty( parameter ) )
				return null;
			// Attempt to process the parameter as just the key
			Size sz;
			if( KeyedConstraints.TryGetValue( parameter, out sz ) )
				return sz;
			// Attempt to process the parameter a querry string
			var qs = HttpUtility.ParseQueryString( parameter );
			if( qs != null && qs.Count > 0 )
				if( KeyedConstraints.TryGetValue( qs["v"], out sz ) )
					return sz;
			return null;
		}

		public bool IsReusable {
			get {
				return false;
			}
		}

#if oldway
		protected void SendResizedImage( String fullFilePath, int width, int height, Stream ResponseStream ) {
			using( System.Drawing.Image Img = System.Drawing.Image.FromFile( fullFilePath ) ) {
				var revsize = Img.Size.ResizeProportionally( width, height );
				using( Bitmap bitmap = new Bitmap( Img, revsize.Width, revsize.Height ) ) {
					bitmap.Save( ResponseStream, Img.RawFormat );
				}
			}
			/*			Bitmap imgIn = new Bitmap( path );
						double y = imgIn.Height;
						double x = imgIn.Width;
						double factor = 1;
						if( width > 0 ) {
							factor = width / x;
						} else if( height > 0 ) {
							factor = height / y;
						}
						System.IO.MemoryStream outStream = new System.IO.MemoryStream();
						Bitmap imgOut = new Bitmap( (int)( x * factor ), (int)( y * factor ) );
						Graphics g = Graphics.FromImage( imgOut );
						g.Clear( Color.White );
						g.DrawImage( imgIn, new Rectangle( 0, 0, (int)( factor * x ), (int)( factor * y ) ), new Rectangle( 0, 0, (int)x, (int)y ), GraphicsUnit.Pixel );
						imgOut.Save( outStream, getImageFormat( path ) );
						return outStream.ToArray();*/
		}

#endif
	}
}
