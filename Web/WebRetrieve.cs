using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MDTools.Web;
using System.Net;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using MDTools.IO;
using System.Security.Cryptography;


namespace MDTools.Web {

	/// <summary>
	/// Summary description for HttpRequest
	/// </summary>
	public class WebRetrieve {
		private string UserAgent;

		public WebRetrieve( string useragent ) {
			this.UserAgent = useragent;
		}

		#region String Response Requests

		public string Get( string UrlPath ) {
			return Request( "GET", UrlPath );
		}
		public string Post( string UrlPath, NameValueCollection Postargs ) {
			return Request( "POST", UrlPath, Postargs );
		}
		public string Post( string UrlPath, string ContentType, string Content ) {
			return Request( "POST", UrlPath, ContentType, Content );
		}
		public string Post( string UrlPath, string ContentType, byte[] Content, int ContentLen ) {
			return Request( "POST", UrlPath, ContentType, Content, ContentLen );
		}
		public string Delete( string UrlPath ) {
			return Request( "DELETE", UrlPath );
		}
		public string Put( string UrlPath, NameValueCollection Postargs ) {
			return Request( "PUT", UrlPath, Postargs );
		}
		public string Put( string UrlPath, string ContentType, string Content ) {
			return Request( "PUT", UrlPath, ContentType, Content );
		}
		public string Put( string UrlPath, string ContentType, byte[] Content, int ContentLen ) {
			return Request( "PUT", UrlPath, ContentType, Content, ContentLen );
		}

		/// <summary>
		/// Make a HTTP request with the given query args
		/// </summary>
		public string Request( string Method, string UrlPath, NameValueCollection Args, string AcceptType = null, NameValueCollection Header = null ) {
			if( Args != null ) {
				// Convert Payload from name-value pair query to bytes
				byte[] postDataBytes = Encoding.UTF8.GetBytes( Args.ToQueryString() );
				return Request(
					Method, UrlPath,
					"application/x-www-form-urlencoded", postDataBytes, postDataBytes.Length,
					AcceptType,
					Header );
			}
			// No Payload
			return Request(
				Method, UrlPath,
				"application/x-www-form-urlencoded", null, 0,
				AcceptType,
				Header );
		}

		/// <summary>
		/// Make a HTTP request with the given string payload
		/// </summary>
		public string Request( string Method, string UrlPath, string ContentType, string Content, string AcceptType = null, NameValueCollection Header = null ) {
			if( !string.IsNullOrEmpty( Content ) ) {
				// Convert Payload from string to bytes
				byte[] postDataBytes = Encoding.UTF8.GetBytes( Content );
				return Request(
					Method, UrlPath,
					ContentType, postDataBytes, postDataBytes.Length,
					AcceptType,
					Header );
			}
			// No Payload
			return Request(
				Method, UrlPath,
				ContentType, null, 0,
				AcceptType,
				Header );
		}

		/// <summary>
		/// Make a HTTP request with the given binary payload
		/// </summary>
		public string Request( string Method, string UrlPath, string ContentType = null, byte[] Content = null, int ContentLen = 0, string AcceptType = null, NameValueCollection Header = null ) {
			try {
				var request = BuildRequest( Method, UrlPath, ContentType, Content, ContentLen, AcceptType, Header );
				string res = null;
				using( HttpWebResponse response = request.GetResponse() as HttpWebResponse ) {
					using( StreamReader reader = new StreamReader( response.GetResponseStream() ) ) {
						res = reader.ReadToEnd();
					}
				}
				return res;
			}
			catch {
				var Context = HttpContext.Current;
				TextLog.Log( 
					Context, 
					new UniqueLifeCookie( Context ).Guid,
					"Request - Url:'{0}', Method:'{1}', ContentType:'{2}', AcceptType:'{3}', Content:'{4}', Headers:'{5}'\n", 
					UrlPath, Method, ContentType, AcceptType, Content == null ? "null" : Encoding.UTF8.GetString( Content ), Header==null ? "null" : Header.ToString()
				);
				throw;
			}
		}

		public static bool TestAvailability( string UrlPath, int TimeOut=300 ) {
			try {
				var req = (HttpWebRequest)WebRequest.Create( UrlPath );
				req.Timeout = TimeOut;
				req.Method = "HEAD";
				using( var resp = (HttpWebResponse)req.GetResponse() ) {
					return ( resp.StatusCode == HttpStatusCode.OK );
				}
			}
			catch {
				return false;
			}
		}

		#endregion

		#region Binary Response Requests

		public MemoryStream BinaryGet( string UrlPath ) {
			return BinaryRequest( "GET", UrlPath );
		}
		public MemoryStream BinaryPost( string UrlPath, NameValueCollection Postargs ) {
			return BinaryRequest( "POST", UrlPath, Postargs );
		}
		public MemoryStream BinaryPost( string UrlPath, string ContentType, string Content ) {
			return BinaryRequest( "POST", UrlPath, ContentType, Content );
		}
		public MemoryStream BinaryPost( string UrlPath, string ContentType, byte[] Content, int ContentLen ) {
			return BinaryRequest( "POST", UrlPath, ContentType, Content, ContentLen );
		}
		public MemoryStream BinaryDelete( string UrlPath ) {
			return BinaryRequest( "DELETE", UrlPath );
		}
		public MemoryStream BinaryPut( string UrlPath, NameValueCollection Postargs ) {
			return BinaryRequest( "PUT", UrlPath, Postargs );
		}
		public MemoryStream BinaryPut( string UrlPath, string ContentType, string Content ) {
			return BinaryRequest( "PUT", UrlPath, ContentType, Content );
		}
		public MemoryStream BinaryPut( string UrlPath, string ContentType, byte[] Content, int ContentLen ) {
			return BinaryRequest( "PUT", UrlPath, ContentType, Content, ContentLen );
		}

		/// <summary>
		/// Make a HTTP request with the given query args
		/// </summary>
		public MemoryStream BinaryRequest( string Method, string UrlPath, NameValueCollection Args, string AcceptType = null, NameValueCollection Header = null ) {
			if( Args != null ) {
				// Convert Payload from name-value pair query to bytes
				byte[] postDataBytes = Encoding.UTF8.GetBytes( Args.ToQueryString() );
				return BinaryRequest(
					Method, UrlPath,
					"application/x-www-form-urlencoded", postDataBytes, postDataBytes.Length,
					AcceptType,
					Header );
			}
			// No Payload
			return BinaryRequest(
				Method, UrlPath,
				"application/x-www-form-urlencoded", null, 0,
				AcceptType,
				Header );
		}

		/// <summary>
		/// Make a HTTP request with the given string payload
		/// </summary>
		public MemoryStream BinaryRequest( string Method, string UrlPath, string ContentType, string Content, string AcceptType = null, NameValueCollection Header = null ) {
			if( !string.IsNullOrEmpty( Content ) ) {
				// Convert Payload from string to bytes
				byte[] postDataBytes = Encoding.UTF8.GetBytes( Content );
				return BinaryRequest(
					Method, UrlPath,
					ContentType, postDataBytes, postDataBytes.Length,
					AcceptType,
					Header );
			}
			// No Payload
			return BinaryRequest(
				Method, UrlPath,
				ContentType, null, 0,
				AcceptType,
				Header );
		}

		/// <summary>
		/// Make a HTTP request with the given binary payload
		/// </summary>
		public MemoryStream BinaryRequest( string Method, string UrlPath, string ContentType = null, byte[] Content = null, int ContentLen = 0, string AcceptType = null, NameValueCollection Header = null ) {
			try {
				var request = BuildRequest( Method, UrlPath, ContentType, Content, ContentLen, AcceptType, Header );
				MemoryStream ms = null;
				using( HttpWebResponse response = request.GetResponse() as HttpWebResponse ) {
					using( BinaryReader reader = new BinaryReader( response.GetResponseStream() ) ) {
						const int bufferSize = 16384;
						ms = new MemoryStream();
						byte[] buffer = new byte[bufferSize];
						int count;
						while( ( count = reader.Read( buffer, 0, buffer.Length ) ) != 0 )
							ms.Write( buffer, 0, count );
					}
				}
				return ms;
			}
			catch {
				var Context = HttpContext.Current;
				TextLog.Log(
					Context,
					new UniqueLifeCookie( Context ).Guid,
					"BinaryRequest - Url:'{0}', Method:'{1}', ContentType:'{2}', AcceptType:'{3}', Content:'{4}', Headers:'{5}'\n",
					UrlPath, Method, ContentType, AcceptType, Content==null ? "null" : Encoding.UTF8.GetString( Content ), Header == null ? "null" : Header.ToString()
				);
				throw;
			}
		}

		public static byte[] ToBytes( ref MemoryStream ms, bool dispose=true ) {
			var data = ms.ToArray();
			if( dispose ) {
				ms.Dispose();
				ms = null;
			}
			return data;
		}

		#endregion

		protected HttpWebRequest BuildRequest( string Method, string UrlPath, string ContentType = null, byte[] Content = null, int ContentLen = 0, string AcceptType = null, NameValueCollection Header = null ) {
			// Construct request to url
			HttpWebRequest request = HttpWebRequest.Create( UrlPath ) as HttpWebRequest;
			request.Method = Method;
			if( AcceptType != null )
				request.Accept = AcceptType;
			if( Header != null )
				request.Headers.Add( Header );
			if( !string.IsNullOrEmpty( this.UserAgent ) )
				request.UserAgent = this.UserAgent;
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

			//!! are these required for uploading file
			//request.ServicePoint.Expect100Continue = false;
			//request.KeepAlive = false;
			//request.AllowWriteStreamBuffering = false;

			if( Content != null ) {
				request.ContentType = ContentType;
				request.ContentLength = ContentLen;
				using( Stream requestStream = request.GetRequestStream() ) {
					requestStream.Write( Content, 0, Content.Length );
					//requestStream.Close();
				}
			}
			return request;
		}

		public string MultipartUploadFile( string UrlPath, string FileContentRef, string FilePath, NameValueCollection Postargs, string AcceptType = null, NameValueCollection Header = null ) {
			string boundary = "---------------------------" + DateTime.Now.Ticks.ToString( "x" );
			// New String Builder
			StringBuilder sb = new StringBuilder();

			// Add Form Data
			if(Postargs!=null)
			for( int i = 0, lenx = Postargs.Count; i < lenx; i++ ) {
				// Access Token
				sb.AppendFormat( 
					"--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}\r\n",
					boundary, Postargs.GetKey( i ), Postargs.Get( i ) );
			}

			// Header for File
			sb.AppendFormat( 
				"--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
				boundary, FileContentRef, FilePath, @"application/octet-stream" );

			byte[] image = File.ReadAllBytes( HttpContext.Current.Server.MapPath( FilePath ) );
			if(image.LongLength>int.MaxValue)
				throw new Exception("Image File too big.");

			// Populate MemoryStream
			int bytelen = sb.Length + boundary.Length + 8 + image.Length;	// estimate
			using( var mem = new MemoryStream( bytelen ) ) {
				// Store Header
				var b = Encoding.ASCII.GetBytes( sb.ToString() );
				int pos = 0;
				int len = b.Length;
				mem.Write( b, 0, len );
				// Store Image
				b = image;
				pos += len;
				len = b.Length;
				mem.Write( b, 0, len );
				// Store Footer
				b = Encoding.ASCII.GetBytes( "\r\n--" + boundary + "--\r\n" );
				pos += len;
				len = b.Length;
				mem.Write( b, 0, len );

				return Request(
					"POST", UrlPath,
					"multipart/form-data; boundary=" + boundary, mem.GetBuffer(), (int)mem.Length,
					AcceptType,
					Header );
			}
		}

#if originalway
		/// <summary>
		/// http://stackoverflow.com/questions/4898950/posting-image-from-net-to-facebook-wall-using-the-graph-api
		/// </summary>
		public string UploadPhoto( string album_id, string message, string filename, Byte[] bytes, string Token ) {
			// Create Boundary
			string boundary = "---------------------------" + DateTime.Now.Ticks.ToString( "x" );

			// Create Path
			string Path = @"https://graph.facebook.com/";
			if( !String.IsNullOrEmpty( album_id ) ) {
				Path += album_id + "/";
			}
			Path += "photos";

			// Create HttpWebRequest
			HttpWebRequest uploadRequest;
			uploadRequest = (HttpWebRequest)HttpWebRequest.Create( Path );
			uploadRequest.ServicePoint.Expect100Continue = false;
			uploadRequest.Method = "POST";
			uploadRequest.UserAgent = this.UserAgent;
			uploadRequest.ContentType = "multipart/form-data; boundary=" + boundary;
			uploadRequest.KeepAlive = false;

			// New String Builder
			StringBuilder sb = new StringBuilder();

			// Add Form Data
			string formdataTemplate = "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}\r\n";

			// Access Token
			sb.AppendFormat( formdataTemplate, boundary, "access_token", HttpUtility.UrlEncode( Token ) );

			// Message
			sb.AppendFormat( formdataTemplate, boundary, "message", message );

			// Header
			string headerTemplate = "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n";
			sb.AppendFormat( headerTemplate, boundary, "source", filename, @"application/octet-stream" );

			// File
			string formString = sb.ToString();
			byte[] formBytes = Encoding.UTF8.GetBytes( formString );
			byte[] trailingBytes = Encoding.UTF8.GetBytes( "\r\n--" + boundary + "--\r\n" );
			byte[] image;
			if( bytes == null ) {
				image = File.ReadAllBytes( HttpContext.Current.Server.MapPath( filename ) );
			} else {
				image = bytes;
			}

			// Memory Stream
			MemoryStream imageMemoryStream = new MemoryStream();
			imageMemoryStream.Write( image, 0, image.Length );

			// Set Content Length
			long imageLength = imageMemoryStream.Length;
			long contentLength = formBytes.Length + imageLength + trailingBytes.Length;
			uploadRequest.ContentLength = contentLength;

			// Get Request Stream
			uploadRequest.AllowWriteStreamBuffering = false;
			Stream strm_out = uploadRequest.GetRequestStream();

			// Write to Stream
			strm_out.Write( formBytes, 0, formBytes.Length );
			byte[] buffer = new Byte[checked( (uint)Math.Min( 4096, (int)imageLength ) )];
			int bytesRead = 0;
			int bytesTotal = 0;
			imageMemoryStream.Seek( 0, SeekOrigin.Begin );
			while( ( bytesRead = imageMemoryStream.Read( buffer, 0, buffer.Length ) ) != 0 ) {
				strm_out.Write( buffer, 0, bytesRead );
				bytesTotal += bytesRead;
			}
			strm_out.Write( trailingBytes, 0, trailingBytes.Length );

			// Close Stream
			strm_out.Close();

			// Get Web Response
			HttpWebResponse response = uploadRequest.GetResponse() as HttpWebResponse;

			// Create Stream Reader
			StreamReader reader = new StreamReader( response.GetResponseStream() );

			// Return
			return reader.ReadToEnd();
		}
#endif

#if anotherway
		public string UploadFilesToRemoteUrl( string url, string[] files, string logpath, NameValueCollection nvc ) {

			long length = 0;
			string boundary = "----------------------------" +
			DateTime.Now.Ticks.ToString( "x" );

			HttpWebRequest httpWebRequest2 = (HttpWebRequest)WebRequest.Create( url );
			httpWebRequest2.ContentType = "multipart/form-data; boundary=" + boundary;
			httpWebRequest2.Method = "POST";
			httpWebRequest2.KeepAlive = true;
			httpWebRequest2.Credentials =
			System.Net.CredentialCache.DefaultCredentials;

			Stream memStream = new System.IO.MemoryStream();
			byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes( "\r\n--" + boundary + "\r\n" );

			string formdataTemplate = "\r\n--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}";

			foreach( string key in nvc.Keys ) {
				string formitem = string.Format( formdataTemplate, key, nvc[key] );
				byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes( formitem );
				memStream.Write( formitembytes, 0, formitembytes.Length );
			}

			memStream.Write( boundarybytes, 0, boundarybytes.Length );
			string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n Content-Type: application/octet-stream\r\n\r\n";

			for( int i = 0; i < files.Length; i++ ) {
				//string header = string.Format(headerTemplate, "file" + i, files[i]);
				string header = string.Format( headerTemplate, "uplTheFile", files[i] );
				byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes( header );
				memStream.Write( headerbytes, 0, headerbytes.Length );

				FileStream fileStream = new FileStream( files[i], FileMode.Open,
				FileAccess.Read );
				byte[] buffer = new byte[1024];
				int bytesRead = 0;
				while( ( bytesRead = fileStream.Read( buffer, 0, buffer.Length ) ) != 0 ) {
					memStream.Write( buffer, 0, bytesRead );
				}

				memStream.Write( boundarybytes, 0, boundarybytes.Length );
				fileStream.Close();
			}

			httpWebRequest2.ContentLength = memStream.Length;
			Stream requestStream = httpWebRequest2.GetRequestStream();

			memStream.Position = 0;
			byte[] tempBuffer = new byte[memStream.Length];
			memStream.Read( tempBuffer, 0, tempBuffer.Length );
			memStream.Close();
			requestStream.Write( tempBuffer, 0, tempBuffer.Length );
			requestStream.Close();

			WebResponse webResponse2 = httpWebRequest2.GetResponse();
			Stream stream2 = webResponse2.GetResponseStream();
			StreamReader reader2 = new StreamReader( stream2 );

			var res = reader2.ReadToEnd();

			webResponse2.Close();
			httpWebRequest2 = null;
			webResponse2 = null;
			return res;
		}
#endif

		/// <summary>
		/// Combine name and value into path
		/// </summary>
		public static string AppendQuery( string path, string name, string value ) {
			int pos = path.IndexOf( '?' );
			return string.Concat( path, pos >= 0 ? "&" : "?", name, "=", value );
		}
		/// <summary>
		/// Combine name-value pairs into path
		/// </summary>
		public static string AppendQuery( string path, NameValueCollection qs ) {
			if( qs == null || qs.Count == 0 )
				return path;
			int pos = path.IndexOf( '?' );
			if( pos >= 0 ) {
				var res = HttpUtility.ParseQueryString( path.Substring( pos + 1 ) );
				res.Add( qs );
				return path.Substring( 0, pos + 1 ) + res.ToString();// .ToQueryString(); // should produce equivalent to static ToQueryString()
			}
			return string.Concat( path, "?", qs.ToQueryString() );
		}

	}

	public class JSONRetrieve {

		/// <summary>
		/// Tests then returns same JSON object if valid
		/// </summary>
		public delegate JSONObject CheckJSON( JSONObject json );

		protected CheckJSON CheckJSONFn = null;

		protected WebRetrieve Req = null;
		protected const string AcceptType = "application/json";
		public JSONRetrieve( string useragent, CheckJSON fn = null) {
			Req = new WebRetrieve( useragent );
			CheckJSONFn = fn;
		}

		public JSONObject Get( string UrlPath ) {
			return JSONResponse( Req.Request( "GET", UrlPath, default(NameValueCollection), AcceptType ) );
		}
		public JSONObject Post( string UrlPath, NameValueCollection postargs ) {
			return JSONResponse( Req.Request( "POST", UrlPath, postargs, AcceptType ));
		}
		public JSONObject Post( string UrlPath, string ContentType, string Content ) {
			return JSONResponse( Req.Request( "POST", UrlPath, ContentType, Content, AcceptType ));
		}
		public JSONObject Post( string UrlPath, string ContentType, byte[] Content, int ContentLen ) {
			return JSONResponse( Req.Request( "POST", UrlPath, ContentType, Content, ContentLen, AcceptType ) );
		}
		public JSONObject Delete( string UrlPath, NameValueCollection postargs ) {
			return JSONResponse( Req.Request( "DELETE", UrlPath, postargs, AcceptType ) );
		}
		public JSONObject Put( string UrlPath, NameValueCollection postargs ) {
			return JSONResponse( Req.Request( "PUT", UrlPath, postargs, AcceptType ));
		}
		public JSONObject Put( string UrlPath, string ContentType, string Content ) {
			return JSONResponse( Req.Request( "PUT", UrlPath, ContentType, Content, AcceptType ) );
		}
		public JSONObject Put( string UrlPath, string ContentType, byte[] Content, int ContentLen ) {
			return JSONResponse( Req.Request( "PUT", UrlPath, ContentType, Content, ContentLen, AcceptType ) );
		}

		/// <summary>
		/// Make a HTTP request with the given query args
		/// </summary>
		public JSONObject Request( string Method, string UrlPath, NameValueCollection Args, NameValueCollection Header = null ) {
			return JSONResponse( Req.Request( Method, UrlPath, Args, AcceptType, Header ) );
		}
		/// <summary>
		/// Make a HTTP request with the given string payload
		/// </summary>
		public JSONObject Request( string Method, string UrlPath, string ContentType, string Content, NameValueCollection Header = null ) {
			return JSONResponse( Req.Request( Method, UrlPath, ContentType, Content, AcceptType, Header ) );
		}
		/// <summary>
		/// Make a HTTP request with the given binary payload
		/// </summary>
		public JSONObject Request( string Method, string UrlPath, string ContentType = null, byte[] Content = null, int ContentLen = 0, NameValueCollection Header = null ) {
			return JSONResponse( Req.Request( Method, UrlPath, ContentType, Content, ContentLen, AcceptType, Header ) );
		}
		/// <summary>
		/// Upload a file as as a multipart with a binary file payload
		/// </summary>
		public JSONObject MultipartUploadFile( string UrlPath, string FileContentRef, string FilePath, NameValueCollection Postargs, NameValueCollection Header = null ) {
			return JSONResponse( Req.MultipartUploadFile( UrlPath, FileContentRef, FilePath, Postargs, AcceptType, Header ) );
		}

		protected JSONObject JSONResponse( string res ) {
			if( string.IsNullOrEmpty( res ) )
				return null;
			JSONObject json = null;
			try {
				json = JSONObject.Create( res );
			}
			catch {
				json = JSONObject.SetString( res );
			}
			return ( CheckJSONFn == null ) ? json : CheckJSONFn( json );
		}

	}

	public static class KeySecret {
		public static string EncodeParamsToSecret( string key, string pkg = null ) {
			var hmacsha512 = new HMACSHA512( Convert.FromBase64String( key ) );
			var byteArray = hmacsha512.ComputeHash( Encoding.UTF8.GetBytes( pkg ) );
			return Convert.ToBase64String( byteArray );
		}

	}

}
