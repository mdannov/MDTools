using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Specialized;
using System.Threading;
using MDTools.Data;
using System.IO.Compression;
using MDTools.IO;

namespace MDTools.Web {

	/// <summary>
	/// ClientCache applies to Request and Response Communication in the headers between Client & Server
	/// Helps detect if Client already has a valid cached copy so content doesn't get retransferred, and 
	/// helps set up Response so Client can cache locally for next time.
	/// </summary>
	public static class CacheExt {

		#region Last-Modified/If-Modified-Since Setting and Testing

		/// <summary>
		/// Check if If-Modified-Since Date has a valid Client's Cached Version based on time of physical file
		///   Sends 304 if Client's cache is still valid based on date, sets last modified date if not
		/// </summary>
		/// <param name="path">File to lookup time and compare against</param>
		/// <param name="isVirtPath">File referenced is virtualPath or fullFilePath</param>
		/// <param name="ResolutionSecs">Forgive this many seconds</param>
		/// <param name="ResponseEnd">Sends Response End if Cache condition is true</param>
		public static bool Send304IfClientCacheValid( this HttpContext Context, bool isVirtPath, string path, int ResolutionSecs = 1, bool ResponseEnd = true ) {
			var currentModifiedTime = File.GetLastWriteTime( isVirtPath ? Context.Server.MapPath( path ) : path );
			return Send304IfClientCacheValid( Context, currentModifiedTime, ResolutionSecs, ResponseEnd );
		}

		/// <summary>
		/// Check if If-Modified-Since Date has a valid Client's Cached Version based on time of physical file
		///   Sends 304 if Client's cache is still valid based on date, sets last modified date if not
		/// </summary>
		/// <param name="currentModifiedTime">Time To compare Client Cached Date against</param>
		/// <param name="ResponseEnd">Sends Response End if Cache condition is true</param>
		public static bool Send304IfClientCacheValid( this HttpContext Context, DateTime currentModifiedTime, int ResolutionSecs = 1, bool ResponseEnd = true ) {
			// Get Client's Cache with page's Last Modified date
			var ClientLastModified = GetLastModifiedFromHeader( Context.Request );
			// Check if the cache is still valid
			var modifyDiff = currentModifiedTime - ClientLastModified;
			if( modifyDiff < TimeSpan.FromSeconds( ResolutionSecs ) ) {
				Send304ClientCacheValid( Context.Response, ResponseEnd );
				return true;
			}
			// Set current page's last modified so Client can cache it for next send
			SetLastModifiedDate( Context.Response, currentModifiedTime );
			return false;
		}

		public static DateTime GetLastModifiedFromHeader( this HttpRequest Request ) {
			var ifModifiedSince = Request.Headers["If-Modified-Since"];
			if( string.IsNullOrEmpty( ifModifiedSince ) )
				return default( DateTime );
			return System.DateTime.ParseExact( ifModifiedSince, "r", CultureInfo.InvariantCulture ).ToLocalTime();
#if checkwhichisfaster //!!
				   ifModifiedSinceTime = DateTime.Parse(ifModifiedSinceHeaderText);
					//DateTime.Parse will return localized time but we want UTC
				   ifModifiedSinceTime = ifModifiedSinceTime .Value.ToUniversalTime();
#endif
		}

		/// <summary>
		/// Tell client browser this Page's Last Modified Date in Response (should be in the past).
		/// Client browser will send this date back as part of Request's If-Modified-Since header
		/// if still available in its cache.
		/// </summary>
		public static void SetLastModifiedDate( this HttpResponse Response, DateTime currentModifiedTime ) {
			// Set last modified Time
			//Context.Response.Cache.SetAllowResponseInBrowserHistory( true );
			Response.Cache.SetCacheability( HttpCacheability.Public );//** this default can be overridden after call
			if( currentModifiedTime <= DateTime.Now )
				Response.Cache.SetLastModified( currentModifiedTime );
			//Response.Headers["Last-Modified"] = currentModifiedTime.to?;
		}
		/// <summary>
		/// Tell client browser this Page's Last Modified Date in Response (based on File's last modified date).
		/// Client browser will send this date back as part of Request's If-Modified-Since header
		/// if still available in its cache.
		/// </summary>
		public static void SetLastModifiedDate( this HttpContext Context, string virtPath ) {
			// Set last modified Time
			var currentModifiedTime = File.GetLastWriteTime( Context.Server.MapPath( virtPath ) );
			SetLastModifiedDate( Context.Response, currentModifiedTime );
		}
		public static void SetLastModifiedDate( this HttpResponse Response, string fullFilePath ) {
			// Set last modified Time
			var currentModifiedTime = File.GetLastWriteTime( fullFilePath );
			SetLastModifiedDate( Response, currentModifiedTime );
		}

		#endregion

		#region ETag and If-None-Match Setting and Testing

		/// Check if If-None-Match matches Client's Cached Version based on ETag
		///   Sends 304 if Client's cache is valid based on ETag; sets ETag if not
		/// </summary>
		/// <param name="ETag">ETag to compare against</param>
		/// <param name="ResponseEnd">Sends Response End if Cache condition is true</param>
		public static bool Send304IfClientCacheValid( this HttpContext Context, string ETag, bool ResponseEnd = true ) {
			if( ETag == GetETagFromHeader( Context.Request ) ) {
				Send304ClientCacheValid( Context.Response, ResponseEnd );
				return true;
			}
			SetETag( Context.Response, ETag );
			return false;
		}
		public static bool Send304IfClientCacheValid( this HttpContext Context, ETagScope ETag, bool ResponseEnd = true ) {
			return Send304IfClientCacheValid( Context, ETag.Tag, ResponseEnd );
		}
		public static string GetETagFromHeader( this HttpRequest Request ) {
			// Check the If-None-Match header, if it exists. This header is used by FireFox to validate entities based on the ETag response header 
			return Request.Headers["If-None-Match"];
		}
		public static void SetETag( this HttpResponse Response, string ETag ) {
			//Response.Headers["ETag"] = ETag;
			Response.Cache.SetETag( ETag );
		}
		public static void SetETag( this HttpResponse Response, ETagScope ETag ) {
			//Response.Headers["ETag"] = ETag.Tag;
			Response.Cache.SetETag( ETag.Tag );
		}

		#endregion

		#region Both Last-Modified/If-Modified-Since and ETag/If-None-Match Setting and Testing

		/// <summary>
		/// Check if If-Modified-Since Date has a valid Client's Cached Version based on time of physical file or If-None-Match matches ETag 
		///   Sends 304 if Client's cache is still valid based on date or ETag, sets last modified date and ETag
		/// </summary>
		/// <param name="path">File to lookup time and compare against</param>
		/// <param name="isVirtPath">File referenced is virtualPath or fullFilePath</param>
		/// <param name="ETag">ETag to compare against</param>
		/// <param name="ResolutionSecs">Forgive this many seconds</param>
		/// <param name="ResponseEnd">Sends Response End if Cache condition is true</param>
		public static bool Send304IfClientCacheValid( this HttpContext Context, bool isVirtPath, string path, string ETag, int ResolutionSecs = 1, bool ResponseEnd = true ) {
			if( !string.IsNullOrEmpty( ETag ) )
				SetETag( Context.Response, ETag );	// Need to set ETag even if ClientCache is valid
			if( Send304IfClientCacheValid( Context, isVirtPath, path, ResolutionSecs, ResponseEnd ) ) // This will set LastModified
				return true;
			return Send304IfClientCacheValid( Context, ETag, ResponseEnd );
		}
		public static bool Send304IfClientCacheValid( this HttpContext Context, bool isVirtPath, string path, ETagScope ETag, int ResolutionSecs = 1, bool ResponseEnd = true ) {
			return Send304IfClientCacheValid( Context, isVirtPath, path, ETag.Tag, ResolutionSecs, ResponseEnd );
		}

		/// <summary>
		/// Check if If-Modified-Since Date has a valid Client's Cached Version based on time of physical file
		///   Sends 304 if Client's cache is still valid based on date, sets last modified date if not
		/// </summary>
		/// <param name="currentModifiedTime">Time To compare Client Cached Date against</param>
		/// <param name="ETag">ETag to compare against</param>
		/// <param name="ResponseEnd">Sends Response End if Cache condition is true</param>
		public static bool Send304IfClientCacheValid( this HttpContext Context, DateTime currentModifiedTime, string ETag, int ResolutionSecs = 1, bool ResponseEnd = true ) {
			SetETag( Context.Response, ETag );	// Need to set ETag even if ClientCache is valid
			if( Send304IfClientCacheValid( Context, currentModifiedTime, ResolutionSecs, ResponseEnd ) ) // This will set LastModified
				return true;
			return Send304IfClientCacheValid( Context, ETag, ResponseEnd );
		}
		public static bool Send304IfClientCacheValid( this HttpContext Context, DateTime currentModifiedTime, ETagScope ETag, int ResolutionSecs = 1, bool ResponseEnd = true ) {
			return Send304IfClientCacheValid( Context, currentModifiedTime, ETag.Tag, ResolutionSecs, ResponseEnd );
		}

		#endregion

		#region Send 304 NotModified If Client Cache Exists at all - no testing

		public static bool Send304IfClientCacheExists( this HttpContext Context, bool isVirtPath, string path, string ETag, bool ResponseEnd = true ) {
			bool avail = CheckClientCacheAvailable( Context.Request );
			if( avail )
				Send304ClientCacheValid( Context.Response, ResponseEnd );
			if( !string.IsNullOrEmpty( path ) ) {
				var currentModifiedTime = File.GetLastWriteTime( isVirtPath ? Context.Server.MapPath( path ) : path );
				SetLastModifiedDate( Context.Response, currentModifiedTime );
			}
			if( !string.IsNullOrEmpty( ETag ) )
				SetETag( Context.Response, ETag );
			return avail;
		}
		public static bool Send304IfClientCacheExists( this HttpContext Context, bool isVirtPath, string path, ETagScope ETag, bool ResponseEnd = true ) {
			return Send304IfClientCacheExists( Context, isVirtPath, path, ETag != null ? ETag.Tag : null, ResponseEnd );
		}

		public static bool Send304IfClientCacheExists( this HttpContext Context, DateTime currentModifiedTime, string ETag, bool ResponseEnd = true ) {
			bool avail = CheckClientCacheAvailable( Context.Request );
			if( avail )
				Send304ClientCacheValid( Context.Response, ResponseEnd );
			if( currentModifiedTime != DateTime.MinValue )
				SetLastModifiedDate( Context.Response, currentModifiedTime );
			if( !string.IsNullOrEmpty( ETag ) )
				SetETag( Context.Response, ETag );
			return avail;
		}
		public static bool Send304IfClientCacheExists( this HttpContext Context, DateTime currentModifiedTime, ETagScope ETag, bool ResponseEnd = true ) {
			return Send304IfClientCacheExists( Context, currentModifiedTime, ETag != null ? ETag.Tag : null, ResponseEnd );
		}

		public static bool CheckClientCacheAvailable( this HttpRequest Request ) {
			// Check If-Modified-Since has been provided to indicate a cached version is available
			return !string.IsNullOrEmpty( Request.Headers["If-Modified-Since"] ) || !string.IsNullOrEmpty( Request.Headers["If-None-Match"] );
		}

		#endregion


		#region Server Response Headers

		/// <summary>
		/// Tell client browser when Page is allowed to be expired by browser.
		/// Be wary; some clients won't even ask the server if it can use the cache before expired time.
		/// </summary>
		public static void SetExpiresFromNow( this HttpResponse Response, int ExpiresSeconds = 0 ) {
			Response.Cache.SetValidUntilExpires( true );
			Response.Cache.SetExpires( ExpiresSeconds == 0 ? DateTime.Now : DateTime.Now.AddSeconds( ExpiresSeconds ) );
			//Response.Headers["Expires"] = ( ExpiresSeconds == 0 ? DateTime.Now : DateTime.Now.AddSeconds( ExpiresSeconds ) ).To
		}
		/// <summary>
		/// Tell client browser when Page is allowed to be expired by browser.
		/// Be wary; some clients won't even ask the server if it can use the cache before expired time.
		/// </summary>
		public static void SetExpires( this HttpResponse Response, DateTime time ) {
			Response.Cache.SetValidUntilExpires( true );
			Response.Cache.SetExpires( time );
			//Response.Headers["Expires"] = time.To
		}
		/// <summary>
		/// Set Response to use Client Cached 
		/// </summary>
		public static void Send304ClientCacheValid( this HttpResponse Response, bool ResponseEnd = true ) {
			Response.StatusCode = (int)System.Net.HttpStatusCode.NotModified;
			Response.StatusDescription = "Not Modified";
			Response.AddHeader( "Content-Length", "0" );
			if( ResponseEnd ) {
				Response.SuppressContent = true;
				Response.End();
			}
		}

		#endregion

		#region Flexibile Date Helpers

		/// <summary>
		/// Check if Date is Expired based on time resolution specified 
		/// Use to determine whether to stream cached content or not
		/// </summary>
		public static bool IsExpiredNow( DateTime ExpiresTime, int ResolutionSecs = 1 ) {
			var modifyDiff = ExpiresTime - DateTime.Now;
			return ResolutionSecs == 0 ? ( modifyDiff.Ticks < 0 ) : ( modifyDiff < TimeSpan.FromSeconds( ResolutionSecs ) );
		}
		/// <summary>
		/// Check if persisted file is Expired based on time resolution specified 
		/// Use to determine whether to stream cached content or not
		/// </summary>
		/// <param name="fullFilePath">File to lookup time and compare against</param>
		/// <param name="ResolutionSecs">Forgive this many seconds</param>
		public static bool IsExpiredNow( int MinutesAfterExpires, string fullFilePath, int ResolutionSecs = 1 ) {
			return IsExpiredNow( File.GetLastWriteTime( fullFilePath ).AddMinutes( MinutesAfterExpires ), ResolutionSecs );
		}
		public static bool IsExpiredNow( this HttpServerUtility Server, int MinutesAfterExpires, string virtPath, int ResolutionSecs = 1 ) {
			return IsExpiredNow( File.GetLastWriteTime( Server.MapPath( virtPath ) ).AddMinutes( MinutesAfterExpires ), ResolutionSecs );
		}

		#endregion
	}


	/// <summary>
	/// After the ClientCache is not found valid, the Server may have one persisted to send back.
	/// This code acts as a helper to validate if the cache is still valid and sends persisted 
	/// content from memory, disk, etc.
	/// </summary>
	public static class ServerCache {

		/// <summary>
		/// Send Text File (virtual path)
		/// </summary>
		public static void SendTextFile( this HttpContext Context, string originalVirtPath ) {
			Context.Response.WriteFile( Context.Server.MapPath( originalVirtPath ) );
		}
		/// <summary>
		/// Send Text File (absolutepath)
		/// </summary>
		public static void SendTextFile( this HttpResponse Response, string fullFilePath ) {
			Response.WriteFile( fullFilePath );
		}
		/// <summary>
		/// Send Text File; if Compressable, Convert 
		/// </summary>
		public static void SendTextFileOrCompress( this HttpContext Context, string originalVirtPath ) {
			if( ClientSupportsCompression( Context.Request ) ) {
				SendCompressed( Context.Response, CompressGZip( TextFromFile( Context.Server, originalVirtPath ) ), false );
				return;
			}
			Context.Response.WriteFile( Context.Server.MapPath( originalVirtPath ) );
		}
		/// <summary>
		/// Send Text File; if Compressable, Persist-only
		/// </summary>
		public static void SendTextFileOrPersistCompress( this HttpContext Context, string originalVirtPath, string persistVirtPath ) {
			if( ClientSupportsCompression( Context.Request ) ) {
				_ProcessText( false, true, true, false, Context, originalVirtPath, persistVirtPath, null, true, null, 0 );
				return;
			}
			Context.Response.WriteFile( Context.Server.MapPath( originalVirtPath ) );
		}
		/// <summary>
		/// Send Text File; if Compressable, Cache-only
		/// </summary>
		public static void SendTextFileOrCacheCompress( this HttpContext Context, string originalVirtPath, int MaxSize=0 ) {
			if( ClientSupportsCompression( Context.Request ) ) {
				_ProcessText( true, false, true, false, Context, originalVirtPath, null, null, true, null, MaxSize );
				return;
			}
			Context.Response.WriteFile( Context.Server.MapPath( originalVirtPath ) );
		}
		/// <summary>
		/// Send Text File; if Compressable, Cache or Persist
		/// </summary>
		public static void SendTextFileOrCachePersistCompress( this HttpContext Context, string originalVirtPath, string persistVirtPath, int MaxSize = 0) {
			if( ClientSupportsCompression( Context.Request ) ) {
				_ProcessText( true, true, true, false, Context, originalVirtPath, persistVirtPath, null, true, null, MaxSize );
				return;
			}
			Context.Response.WriteFile( Context.Server.MapPath( originalVirtPath ) );
		}

		/// <summary>
		/// Send Text File (virtual path)
		/// </summary>
		public static void SendBinaryFile( this HttpContext Context, string virtPath, bool isCompressed, bool flush = true ) {
			SendBinaryData( Context.Response, BinaryFromFile( Context.Server, virtPath ), isCompressed, flush );
		}
		/// <summary>
		/// Send Text File (absolute path)
		/// </summary>
		public static void SendBinaryFile( this HttpResponse Response, string fullFilePath, bool isCompressed, bool flush = true ) {
			SendBinaryData( Response, IOHelper.BinaryFromFile( fullFilePath ), isCompressed, flush );
		}
		/// <summary>
		/// Send BinaryFile; if Compressable, Convert 
		/// </summary>
		public static void SendBinaryFileOrCompress( this HttpContext Context, string originalVirtPath, bool flush = true ) {
			if( ClientSupportsCompression( Context.Request ) ) {
				SendCompressed( Context.Response, CompressGZip( BinaryFromFile( Context.Server, originalVirtPath ) ), flush );
				return;
			}
			SendBinaryData( Context.Response, BinaryFromFile( Context.Server, originalVirtPath ), false, flush );
		}
		/// <summary>
		/// Send Text File; if Compressable, Persist-only
		/// </summary>
		public static void SendBinaryFileOrPersistCompress( this HttpContext Context, string originalVirtPath, string persistVirtPath, bool flush=true ) {
			if( ClientSupportsCompression( Context.Request ) ) {
				_ProcessBinary( false, true, true, false, Context, originalVirtPath, persistVirtPath, null, true, null, 0, flush );
				return;
			}
			SendBinaryData( Context.Response, BinaryFromFile( Context.Server, originalVirtPath ), false, flush );
		}
		/// <summary>
		/// Send Binary File; if Compressable, Cache or Persist
		/// </summary>
		public static void SendBinaryFileOrCachePersistCompress( this HttpContext Context, string originalVirtPath, string persistVirtPath, int MaxSize = 0, bool flush=true ) {
			if( ClientSupportsCompression( Context.Request ) ) {
				_ProcessBinary( true, true, true, false, Context, originalVirtPath, persistVirtPath, null, true, null, MaxSize, flush );
				return;
			}
			Context.Response.WriteFile( Context.Server.MapPath( originalVirtPath ) );
		}

		public static string TextFromFile( this HttpServerUtility Server, string virtPath ) {
			return IOHelper.TextFromFile( Server.MapPath( virtPath ) );
		}

		public static byte[] BinaryFromFile( this HttpServerUtility Server, string filePath ) {
			return IOHelper.BinaryFromFile( Server.MapPath( filePath ) );
		}

		public static void SendBinaryData( this HttpResponse Response, byte[] data, bool isCompressed, bool flush = true ) {
			if( isCompressed )
				TurnOnGZipEncoding( Response );
			Response.Buffer = true;
			Response.AddHeader( "Content-Length", data.Length.ToString() );
			Response.BinaryWrite( data );
			// If you flush, make sure all header values were set before calling
			if( flush )
				Response.Flush();
		}

		public static void SendCompressed( this HttpResponse Response, byte[] data, bool flush = true ) {
			TurnOnGZipEncoding( Response );
			Response.Buffer = true;
			Response.AddHeader( "Content-Length", data.Length.ToString() );
			Response.BinaryWrite( CompressGZip( data ) );
			// If you flush, make sure all header values were set before calling
			if( flush )
				Response.Flush();
		}
		public static void SendCompressed( this HttpResponse Response, string content, bool flush = true ) {
			SendBinaryData( Response, CompressGZip( content ), true, flush );
		}


		private static ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();


		public static string GetPersistedName( HttpServerUtility Server, string originalVirtPath, bool isProcessed, bool isCompressed, string extendName = null ) {
			// Construct name from original File and append the date to the name for matching lookup
			return GetPersistedName( Server.MapPath( originalVirtPath ), isProcessed, isCompressed, extendName );
		}
		public static string GetPersistedName( string originalFullFilePath, bool isProcessed, bool isCompressed, string extendName = null ) {
			// Construct name from original File and append the date to the name for matching lookup
			var lastmod = File.GetLastWriteTime( originalFullFilePath );
			var filename = Path.GetFileName( originalFullFilePath );
			string fullFile = string.IsNullOrEmpty( extendName ) ?
				string.Concat( lastmod.ToString( "yyyyMMddHHmmss" ), "-", isProcessed ? "p-" : string.Empty, filename ) :
				string.Concat( lastmod.ToString( "yyyyMMddHHmmss" ), "-", isProcessed ? "p-" : string.Empty, extendName, filename );
			return isCompressed ? fullFile + ".gz" : fullFile;
		}

		public static void PersistText( string originalFullPath, string persistFullPath, string content ) {
			PersistText( string.Concat( persistFullPath, @"\", Path.GetFileName( originalFullPath ) ), content );
		}
		public static void PersistText( HttpServerUtility Server, string originalVirtPath, string persistVirtPath, string content, bool isProcessed, string extendName = null ) {
			var filename = GetPersistedName( Server, originalVirtPath, isProcessed, false, extendName );
			// If output path is empty, use root
			var persistFullPath = string.Concat(
				Server.MapPath( string.IsNullOrEmpty( persistVirtPath ) ? "~/" : persistVirtPath ),
				filename );
			PersistText( persistFullPath, content );
		}
		public static void PersistText( string fullFilePath, string content ) {
			// Persist the content
			_lock.EnterWriteLock();
			try {
				// If we've already saved this file in another thread, we don't need to save it again
				if( File.Exists( fullFilePath ) )
					return;
				File.WriteAllText( fullFilePath, content );
			}
			finally {
				_lock.ExitWriteLock();
			}
		}
		public static void PersistBinary( string originalFullFilePath, string persistFullPath, byte[] data ) {
			PersistBinary( string.Concat( originalFullFilePath, @"\", Path.GetFileName( originalFullFilePath ) ), data );
		}
		public static void PersistBinary( HttpServerUtility Server, string originalVirtPath, string persistVirtPath, byte[] data, bool isProcessed, bool isCompressed, string extendName = null ) {
			var filename = GetPersistedName( Server, originalVirtPath, isProcessed, isCompressed, extendName );
			// If output path is empty, use root
			var persistFullPath = string.Concat(
				Server.MapPath( string.IsNullOrEmpty( persistVirtPath ) ? "~/" : persistVirtPath ),
				filename );
			PersistBinary( persistFullPath, data );
		}
		public static void PersistBinary( string fullFilePath, byte[] data ) {
			// Persist the content
			_lock.EnterWriteLock();
			try {
				// If we've already saved this file in another thread, we don't need to save it again
				if( File.Exists( fullFilePath ) )
					return;
				File.WriteAllBytes( fullFilePath, data );
			}
			finally {
				_lock.ExitWriteLock();
			}
		}

		public static void CacheText( HttpContext Context, string originalVirtPath, string content, bool isProcessed, bool isCompressed, string extendName = null, int MaxSize = 0 ) {
			var filename = GetPersistedName( Context.Server, originalVirtPath, isProcessed, isCompressed, extendName );
			// If item is already found in the cache, we don't need to again
			_Cache( Context, filename, content, MaxSize );
		}
		public static void CacheBinary( HttpContext Context, string originalVirtPath, byte[] data, bool isProcessed, bool isCompressed, string extendName = null, int MaxSize = 0 ) {
			var filename = GetPersistedName( Context.Server, originalVirtPath, isProcessed, isCompressed, extendName );
			// If item is already found in the cache, we don't need to again
			_Cache( Context, filename, data, MaxSize );
		}

#if oldcache
		private static string[] _Keys = { "Content_", "Content_p_", "Content_gz", "Content_p_gz_" };
		private static bool _Cache( HttpContext Context, string Key, string content, bool isProcessed, bool isCompressed, int MaxSize = 0 ) {
			if( MaxSize <= 0 || content.Length <= MaxSize ) {
				Context.Cache[ _Keys[ (!isProcessed ? 0: 1) + (!isCompressed ? 0 : 2)] + Key ] = content;
				return true;
			} else
				return false;
		}
		private static bool _Cache( HttpContext Context, string Key, byte[] data, bool isProcessed, bool isCompressed, int MaxSize = 0 ) {
			if( MaxSize <= 0 || data.Length <= MaxSize ) {
				Context.Cache[_Keys[( !isProcessed ? 0 : 1 ) + ( !isCompressed ? 0 : 2 )] + Key] = data;
				return true;
			} else
				return false;
		}
		private static object _GetCache( HttpContext Context, string Key, bool isProcessed, bool isCompressed ) {
			return Context.Cache[_Keys[( !isProcessed ? 0 : 1 ) + ( !isCompressed ? 0 : 2 )] + Key];
		}
#else
		private static bool _Cache( HttpContext Context, string PersistedName, string content, int MaxSize = 0 ) {
			if( MaxSize <= 0 || content.Length <= MaxSize ) {
				Context.Cache[PersistedName] = content;
				return true;
			} else
				return false;
		}
		private static bool _Cache( HttpContext Context, string PersistedName, byte[] data, int MaxSize = 0 ) {
			if( MaxSize <= 0 || data.Length <= MaxSize ) {
				Context.Cache[PersistedName] = data;
				return true;
			} else
				return false;
		}
		private static object _GetCache( HttpContext Context, string PersistedName ) {
			return Context.Cache[PersistedName];
		}
#endif
		public static bool IsPersisted( HttpServerUtility Server, string originalVirtPath, string persistVirtPath, bool isProcessed, bool isCompressed, string extendName = null ) {
			var filename = GetPersistedName( Server, originalVirtPath, isProcessed, isCompressed, extendName );
			// If output path is empty, use root
			var persistFullPath = string.Concat(
				Server.MapPath( string.IsNullOrEmpty( persistVirtPath ) ? "~/" : persistVirtPath ),
				filename );
			return ( File.Exists( persistFullPath ) );
		}
		public static bool IsPersisted( string filePath ) {
			return ( File.Exists( filePath ) );
		}

		public static void CacheAndPersistText( HttpContext Context, string originalVirtPath, string persistVirtPath, string content, bool useCompression, string extendName = null, int MaxSize = 0 ) {
			_ProcessText( true, true, false, true, Context, originalVirtPath, persistVirtPath, null, useCompression, extendName, MaxSize );
		}
		public static void CacheAndPersistBinary( HttpContext Context, string originalVirtPath, string persistVirtPath, byte[] content, bool useCompression, string extendName = null, int MaxSize = 0 ) {
			_ProcessBinary( true, true, false, true, Context, originalVirtPath, persistVirtPath, null, useCompression, extendName, MaxSize, false );
		}

		public static void SendPeristedText( HttpContext Context, string originalVirtPath, string persistVirtPath, bool useCompression, string extendName = null ) {
			_ProcessText( false, true, true, true, Context, originalVirtPath, persistVirtPath, null, useCompression, extendName, 0 );
		}
		public static void SendPeristedBinary( HttpContext Context, string originalVirtPath, string persistVirtPath, bool useCompression, string extendName = null, bool flush = true ) {
			_ProcessBinary( false, true, true, true, Context, originalVirtPath, persistVirtPath, null, useCompression, extendName, 0, flush );
		}

		public delegate string ProcessFileToTextFn( string fullFilePath );
		public delegate byte[] ProcessFileToBinaryFn( string fullFilePath );

		public static void SendPersistedText( HttpContext Context, string originalVirtPath, string persistVirtPath, ProcessFileToTextFn processFn, bool useCompression, string extendName = null ) {
			_ProcessText( false, true, true, true, Context, originalVirtPath, persistVirtPath, processFn, useCompression, extendName );
		}
		public static void SendPersistedBinary( HttpContext Context, string originalVirtPath, string persistVirtPath, ProcessFileToBinaryFn processFn, bool useCompression, string extendName = null, bool flush = true ) {
			_ProcessBinary( false, true, true, true, Context, originalVirtPath, persistVirtPath, processFn, useCompression, extendName, 0, flush );
		}

		public static void SendCachedText( HttpContext Context, string originalVirtPath, ProcessFileToTextFn processFn, bool useCompression, string extendName = null, int MaxSize = 0 ) {
			_ProcessText( true, false, true, true, Context, originalVirtPath, null, processFn, useCompression, extendName, MaxSize );
		}
		public static void SendCachedBinary( HttpContext Context, string originalVirtPath, ProcessFileToBinaryFn processFn, bool useCompression, string extendName = null, int MaxSize = 0, bool flush = true ) {
			_ProcessBinary( true, false, true, true, Context, originalVirtPath, null, processFn, useCompression, extendName, MaxSize, flush );
		}

		public static void SendCachedOrPersistedText( HttpContext Context, string originalVirtPath, string persistVirtPath, ProcessFileToTextFn processFn, bool useCompression, string extendName = null, int MaxSize = 0 ) {
			_ProcessText( true, true, true, true, Context, originalVirtPath, persistVirtPath, processFn, useCompression, extendName, MaxSize );
		}
		public static void SendCacheOrPersistedBinary( HttpContext Context, string originalVirtPath, string persistVirtPath, ProcessFileToBinaryFn processFn, bool useCompression, string extendName = null, int MaxSize = 0, bool flush = true ) {
			_ProcessBinary( true, true, true, true, Context, originalVirtPath, persistVirtPath, processFn, useCompression, extendName, MaxSize, flush );
		}

		private static void _ProcessText( bool useCache, bool persist, bool send, bool test, HttpContext Context, string originalVirtPath, string persistVirtPath, ProcessFileToTextFn processFn, bool useCompression, string extendName = null, int MaxSize = 0 ) {
#if DEBUG
			if( !useCache && !persist && !send )
				throw new ArgumentException( "Need to at least do something" );
#endif
			var originalFilePath = Context.Server.MapPath( originalVirtPath );
			bool isCompressed = test ? (useCompression ? ClientSupportsCompression( Context.Request ) : false ) : useCompression;
			var filename = GetPersistedName( originalFilePath, processFn != null, isCompressed, extendName );
			string content = null;
			byte[] data = null;
			// Check if item is already Cached and Send
			object c;
			if( useCache && send ) {
				c = _GetCache( Context, filename );
				if( c != null ) {
					if( !isCompressed ) {
						content = c as string;
						Context.Response.Write( content );
						return;
					}
					data = c as byte[];
					SendBinaryData( Context.Response, data, isCompressed, false );
					return;
				}
			}
			// Check if compressed or processed file already exists to send
			string persistFullPath = null;
			if( persist ) {
				// Only check if compressed or processed file already exists
				if( isCompressed || processFn != null ) {
					persistFullPath = string.Concat(
						Context.Server.MapPath( string.IsNullOrEmpty( persistVirtPath ) ? "~/" : persistVirtPath ),
						"\\", filename );
					if( File.Exists( persistFullPath ) ) {
						if( !isCompressed ) {
							// Send the data from the existing processed or unprocessed text file and recache it
							content = IOHelper.TextFromFile( persistFullPath );
							if( useCache )
								_Cache( Context, filename, content, MaxSize );
							if( send )
								Context.Response.Write( content );
							return;
						}
						// Send the data from existing compressed file and recache it
						data = IOHelper.BinaryFromFile( persistFullPath );
						if( useCache )
							_Cache( Context, filename, data, MaxSize );
						if( send )
							SendBinaryData( Context.Response, data, isCompressed, false );
						return;
					}
					// Existing compressed or processed file wasn't found, but if not processing, we can just compress the original file
					if( processFn == null ) {//** isCompressed==true
						if( File.Exists( originalFilePath ) ) {
							// Convert uncompressed to compressed
							content = IOHelper.TextFromFile( originalFilePath );
							data = CompressGZip( content );
							if( useCache )
								_Cache( Context, filename, data, MaxSize );
							PersistBinary( persistFullPath, data );
							if( send )
								SendBinaryData( Context.Response, data, isCompressed, false );
							return;
						} else
							// Original file not found
							throw new WebException( Context.Request, 404, "File not found" );
					}
				}
			} 
			// As the processed file has not been found to exist already, we need to process it
			if( processFn != null ) {
				lock( KeyLockObject.Get( isCompressed ? filename + "gz" : filename ) ) {
					if( send ) {
						// This is the redudant post wait lock check to see if another thread already processed it
						if( useCache ) {
							c = _GetCache( Context, filename );
							if( c != null ) {
								if( !isCompressed ) {
									content = c as string;
									Context.Response.Write( content );
									return;
								}
								data = c as byte[];
								SendBinaryData( Context.Response, data, isCompressed, false );
								return;
							}
						} else if( persist ) {
							if( persistFullPath == null )
								persistFullPath = string.Concat(
									Context.Server.MapPath( string.IsNullOrEmpty( persistVirtPath ) ? "~/" : persistVirtPath ),
									"\\", filename );
							if( File.Exists( persistFullPath ) ) {
								if( !isCompressed ) {
									SendTextFile( Context.Response, persistFullPath );
									return;
								}
								SendBinaryFile( Context.Response, persistFullPath, isCompressed );
								return;
							}
						}
					}
					content = processFn( originalFilePath );
					// Now persist the data
					if( persistFullPath == null && persist )
						persistFullPath = string.Concat( 
							Context.Server.MapPath( string.IsNullOrEmpty( persistVirtPath ) ? "~/" : persistVirtPath ), 
							"\\", filename );
					if( isCompressed ) {
						data = CompressGZip( content );
						if( persist )
							PersistBinary( persistFullPath, data );
						if( useCache )
							_Cache( Context, filename, data, MaxSize );
					} else {
						if( persist )
							PersistText( persistFullPath, content );
						if( useCache )
							_Cache( Context, filename, content, MaxSize );
					}
				}
			}
			// Now send the content
			if( isCompressed )
				SendBinaryData( Context.Response, data, isCompressed, false );
			if( send )
				Context.Response.Write( content );
		}

		private static void _ProcessBinary( bool useCache, bool persist, bool send, bool test, HttpContext Context, string originalVirtPath, string persistVirtPath, ProcessFileToBinaryFn processFn, bool useCompression, string extendName = null, int MaxSize = 0, bool flush = true ) {
#if DEBUG
			if(!useCache && !persist && !send )
				throw new ArgumentException("Need to at least do something" );
#endif
			var originalFilePath = Context.Server.MapPath( originalVirtPath );
			bool isCompressed = test ? ( useCompression ? ClientSupportsCompression( Context.Request ) : false ) : useCompression;
			var filename = GetPersistedName( originalFilePath, processFn != null, isCompressed, extendName );
			byte[] data = null;
			// Check if item is already Cached and Send
			object c;
			if( useCache && send ) {
				c = _GetCache( Context, filename );
				if( c != null ) {
					data = c as byte[];
					SendBinaryData( Context.Response, data, isCompressed, flush );
					return;
				}
			}
			string persistFullPath = null;
			if( persist ) {
				// Only check if compressed or processed file already exists
				if( isCompressed || processFn != null ) {
					persistFullPath = string.Concat(
						Context.Server.MapPath( string.IsNullOrEmpty( persistVirtPath ) ? "~/" : persistVirtPath ),
						"\\", filename );
					if( File.Exists( persistFullPath ) ) {
						// Load the data from file and recache it
						data = IOHelper.BinaryFromFile( persistFullPath );
						if( useCache )
							_Cache( Context, filename, data, MaxSize );
						if( send )
							SendBinaryData( Context.Response, data, isCompressed, flush );
						return;
					}
					// Existing compressed or processed file wasn't found, but if not processing, we can just compress the original file
					if( processFn == null ) {//** isCompressed==true
						if( File.Exists( originalFilePath ) ) {
							// Load the data from file and recache it
							data = IOHelper.BinaryFromFile( originalFilePath );
							data = isCompressed ? CompressGZip( data ) : DecompressToByte( data );
							if( useCache )
								_Cache( Context, filename, data, MaxSize );
							PersistBinary( persistFullPath, data );
							if( send )
								SendBinaryData( Context.Response, data, isCompressed, flush );
							return;
						} else
							// Original file not found
							throw new WebException( Context.Request, 404, "File not found" );
					} 
				}
			}
			// As the processed file has not been found to exist already, we need to process it
			if( processFn != null ) {
				lock( KeyLockObject.Get( isCompressed ? filename + "gz" : filename ) ) {
					if( send ) {
						// This is the redudant post wait lock check to see if another thread already processed it
						if( useCache ) {
							c = _GetCache( Context, filename );
							if( c != null ) {
								data = c as byte[];
								SendBinaryData( Context.Response, data, isCompressed, flush );
								return;
							}
						} else if( persist ) {
							if( persistFullPath == null )
								persistFullPath = string.Concat(
									Context.Server.MapPath( string.IsNullOrEmpty( persistVirtPath ) ? "~/" : persistVirtPath ),
									"\\", filename );
							if( File.Exists( persistFullPath ) ) {
								SendBinaryFile( Context.Response, persistFullPath, isCompressed );
								return;
							}
						}
					}
					data = processFn( originalFilePath );
					if( isCompressed )
						data = CompressGZip( data );
					// Now persist the data
					if( persist ) {
						if( persistFullPath == null )
							persistFullPath = string.Concat( 
								Context.Server.MapPath( string.IsNullOrEmpty( persistVirtPath ) ? "~/" : persistVirtPath ), 
								"\\", filename );
						PersistBinary( persistFullPath, data );
					}
					if( useCache )
						_Cache( Context, filename, data, MaxSize );
				}
			}
			// Now send the content
			if( send )
				SendBinaryData( Context.Response, data, isCompressed, flush );
		}

		#region Compression

		public static void MakePageCompress( this HttpContext Context ) {
			if( !Context.Response.IsRequestBeingRedirected )
				return;	
			if( !ClientSupportsCompression( Context.Request ) )
				return;
			TurnOnGZipEncoding( Context.Response );
			Context.Response.Filter = new GZipStream( Context.Response.Filter, CompressionMode.Compress );
		}

		public static bool ClientSupportsCompression( this HttpRequest Request ) {
			// Unnecessary to test for IE6 or lower since it won't offer accept-encoding anyway
			var avail = Request.Headers["Accept-Encoding"];
			if( string.IsNullOrEmpty( avail ) )
				return false;
			avail = avail.ToLower();
			if( avail.Contains( "gzip" ) )
				return true;
			return avail.Contains( "deflate" );
		}

		public enum CompressionEncoding { None, Deflate, GZip };

		public static CompressionEncoding ClientEncodingSupported( this HttpRequest Request ) {
			// Unnecessary to test for IE6 or lower since it won't offer accept-encoding anyway
			var avail = Request.Headers["Accept-Encoding"];
			if( string.IsNullOrEmpty( avail ) )
				return CompressionEncoding.None;
			avail = avail.ToLower();
			if( avail.Contains( "deflate" ) )
				return CompressionEncoding.Deflate;
			if( avail.Contains( "gzip" ) )
				return CompressionEncoding.GZip;
			return CompressionEncoding.None;
		}

		public static void TurnOnGZipEncoding( this HttpResponse Response ) {
			Response.AppendHeader( "Content-Encoding", "gzip" );
			Response.Charset = "utf-8";
		}

		public static byte[] CompressGZip( string content ) {
			byte[] array = Encoding.ASCII.GetBytes( content );
			using( var stream = new MemoryStream() ) {
				using( var zipstream = new GZipStream( stream, CompressionMode.Compress ) ) {
					zipstream.Write( array, 0, array.Length );
				}
				return stream.ToArray();
			}
		}
		public static byte[] CompressGZip( byte[] data ) {
			using( var stream = new MemoryStream() ) {
				using( var zipstream = new GZipStream( stream, CompressionMode.Compress ) ) {
					zipstream.Write( data, 0, data.Length );
				}
				return stream.ToArray();
			}
		}

		public static string DecompressGZipToText( byte[] data ) {
			using( var ms = new MemoryStream( data ) ) {
				using( var zip = new GZipStream( ms, CompressionMode.Decompress ) ) {
					using( var sr = new StreamReader( zip, Encoding.ASCII ) ) {
						return sr.ReadToEnd();
					}
				}
			}
		}
		public static byte[] DecompressToByte( byte[] data ) {
			int size = 16384;
			using( var ms = new MemoryStream( data ) ) {
				using( var zip = new GZipStream( ms, CompressionMode.Decompress ) ) {
					byte[] buffer = new byte[size];
					using( var memory = new MemoryStream() ) {
						int count = 0;
						do {
							count = zip.Read( buffer, 0, size );
							if( count > 0 )
								memory.Write( buffer, 0, count );
						} while( count > 0 );
						return memory.ToArray();
					}
				}
			}
		}

		public static void TurnOnDeflateEncoding( this HttpResponse Response ) {
			Response.AppendHeader( "Content-Encoding", "deflate" );
			Response.Charset = "utf-8";
		}

		public static byte[] CompressDeflate( string content ) {
			byte[] array = Encoding.ASCII.GetBytes( content );
			using( var stream = new MemoryStream() ) {
				using( var zipstream = new DeflateStream( stream, CompressionMode.Compress ) ) {
					zipstream.Write( array, 0, array.Length );
				}
				return stream.ToArray();
			}
		}
		public static byte[] CompressDeflate( byte[] data ) {
			using( var stream = new MemoryStream() ) {
				using( var zipstream = new DeflateStream( stream, CompressionMode.Compress ) ) {
					zipstream.Write( data, 0, data.Length );
				}
				return stream.ToArray();
			}
		}

		public static string DecompressDeflateToText( byte[] data ) {
			using( var ms = new MemoryStream( data ) ) {
				using( var zip = new DeflateStream( ms, CompressionMode.Decompress ) ) {
					using( var sr = new StreamReader( zip, Encoding.ASCII ) ) {
						return sr.ReadToEnd();
					}
				}
			}
		}
		public static byte[] DecompressDeflateToByte( byte[] data ) {
			int size = 16384;
			using( var ms = new MemoryStream( data ) ) {
				using( var zip = new DeflateStream( ms, CompressionMode.Decompress ) ) {
					byte[] buffer = new byte[size];
					using( var memory = new MemoryStream() ) {
						int count = 0;
						do {
							count = zip.Read( buffer, 0, size );
							if( count > 0 )
								memory.Write( buffer, 0, count );
						} while( count > 0 );
						return memory.ToArray();
					}
				}
			}
		}

		public static void TurnOffContentEncoding( this HttpResponse Response ) {
			Response.Headers.Remove("Content-Encoding" );
		}

		#endregion

	}

#if thismaynotbenecessary
	#region Cache File & Response Headers

	public class StaticResponse {
		public string ContentType;
		public Encoding ContentEncoding;
		public NameValueCollection Headers;
		public Encoding HeaderEncoding;
		public string Charset;
		public HttpCachePolicy Cache;

		public StaticResponse( HttpResponse Response ) {
			this.ContentType = Response.ContentType;
			this.ContentEncoding = Response.ContentEncoding;
			this.Headers = Response.Headers;
			this.HeaderEncoding = Response.HeaderEncoding;
			this.Charset = Response.Charset;
			this.Cache = Response.Cache;
		}

		public void SetStaticResponse( HttpResponse Response ) {
			Response.ContentType = this.ContentType;
			Response.ContentEncoding = this.ContentEncoding;
			Response.Headers.Add( this.Headers );
			Response.HeaderEncoding = this.HeaderEncoding;
			Response.Charset = this.Charset;
			Response.Cache = this.Cache;
		}


	}
	public class TextResponseContent : StaticResponse {
		public string Content;
	}
	public class BinaryResponseContent : StaticResponse {
		public byte[] Content;
	}
	#endregion
#endif


	#region ETags by Scope

	public abstract class ETagScope {
		public string Tag { get; protected set; }
	}

	/// <summary>
	/// ETag Scoped to name
	/// </summary>
	public class ETagName : ETagScope {
		public ETagName( string tag ) {
			Tag = tag;
		}
	}

	/// <summary>
	/// ETag Scoped to Life of Session & Page
	/// </summary>
	public class ETagSession : ETagScope {
		public ETagSession( HttpContext Context ) {
			Tag = Context.Request.RawUrl + Context.Session.SessionID;
		}
	}

	/// <summary>
	/// ETag Scoped to Page
	/// </summary>
	public class ETagPage : ETagScope {
		public ETagPage( HttpContext Context ) {
			Tag = Context.Request.RawUrl;
		}
	}

	/// <summary>
	/// ETag Scoped to Page & Date
	/// </summary>
	public class ETagDate : ETagScope {
		public ETagDate( HttpContext Context, DateTime date ) {
			Tag = Context.Request.RawUrl + date.ToString();
		}
		public ETagDate( HttpContext Context, string filePath ) :
			this( Context, File.GetLastWriteTime( filePath ) ) { }
	}

	#endregion

}
