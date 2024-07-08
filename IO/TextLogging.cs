using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Web;
using System.Text;
using System.Net;
using MDTools.Math;
using MDTools.Extension;
using MDTools.Web;

namespace MDTools.IO {

	public class TextLog : IDisposable {

		/// <summary>
		/// Logging set to "1" or "0" if logging is on or off
		/// </summary>
		public static readonly string AS_Logging = ConfigurationManager.AppSettings["Logging"];
		/// <summary>
		/// Physical directory offset from base directory for log files
		/// </summary>
		public static readonly string AS_LogDirectory = ConfigurationManager.AppSettings["LogDirectory"];

		public enum LogTypes { Info, Important, Warn, Error, Exception };
		private bool on = false;
		public bool On { get { return on; } set { on = ( value == true && file != null ); } }
		public bool Off { get { return !on; } set { On = !value; } }
		protected Guid Key;

		protected StreamWriter file = null;
		public bool AutoFlush {
			get { return ( file != null ? file.AutoFlush : false ); }
			set { if( file != null ) file.AutoFlush = value; }
		}
		protected TextLog( Guid key ) { this.Key = key; }

		public static bool IsLoggingOn() {
			string res = AS_Logging;
			return ( !string.IsNullOrEmpty( res ) && res.Equals( "1" ) );
		}

#if producestoomanylogs
		public static TextLog StartLog( HttpServerUtility Server ) {
			return StartLog( Server, Guid.NewGuid() );
		}
#endif
		private HttpContext Context = null;
		public static TextLog StartLog( HttpContext Context, Guid key ) {
			var Log = new TextLog( key );
			Log.Context = Context;

			// Check if logging is implicitly turned on/off
			Log.on = IsLoggingOn();
			if( !Log.on )
				return Log;
			// File LogDirectory not set, turn logging off
			string Filename = Log.GetFilename( Context.Server, string.Empty );
			if( string.IsNullOrEmpty( Filename ) ) {
				Log.on = false;
				return Log;
			}

			// Verify file can be created
			try {
				if( !IOHelper.VerifyDirectoryInPath( Filename, true ) ) {// create path 
					Log.on = false;
					return Log;
				}
				Log.file = new StreamWriter( Filename, true );			// append mode
			}
			catch {
				Log.on = false;
				return Log;
			}
			// Log initialization start
			//Log.Log( "Log Started " );
			return Log;
		}

		protected string GetFilename( HttpServerUtility Server, string extend ) {
			var time = DateTime.Now;//!! Use FastDateTime
			// File LogDirectory not set, turn logging off
			string filename = AS_LogDirectory;
			if( string.IsNullOrEmpty( filename ) )
				return null;
			if( extend == null )
				extend = string.Empty;
			// Verify Log exists
			filename = Server.MapPath( filename );
			return string.Format( "{0}/{1:yyMMdd}/{2}{3}.log", filename, time, Key.ToString(), extend );
		}

		public static void Log( HttpContext Context, Guid key, LogTypes logType, string format, params object[] data ) {
			if( IsLoggingOn() )
				using( var log = StartLog( Context, key ) ) {
					log.Log( logType, string.Format( format, data ) );
				}
		}
		public static void Log( HttpContext Context, Guid key, string message ) {
			if( IsLoggingOn() )
				using( var log = StartLog( Context, key ) ) {
					log.Log( LogTypes.Info, message );
				}
		}
		public static void Log( HttpContext Context, Guid key, string format, params object[] data ) {
			if( IsLoggingOn() )
				using( var log = StartLog( Context, key ) ) {
					log.Log( LogTypes.Info, format, data );
				}
		}
		public static void Log( HttpContext Context, Guid key, System.Exception ex, string message ) {
			if( IsLoggingOn() )
				using( var log = StartLog( Context, key ) ) {
					log.Log( ex, message );
				}
		}
		public static void Log( HttpContext Context, Guid key, string message, System.Exception ex, string format, params object[] data ) {
			if( IsLoggingOn() )
				using( var log = StartLog( Context, key ) ) {
					log.Log( message, ex, format, data );
				}
		}
		public static void Log( HttpContext Context, Guid key, System.Exception ex ) {
			if( IsLoggingOn() )
				using( var log = StartLog( Context, key ) ) {
					log.Log( ex );
				}
		}
		

		public void Log( LogTypes logType, string format, params object[] data ) { Log( logType, string.Format( format, data ) ); }
		public void Log( string data ) { Log( LogTypes.Info, data ); }
		public void Log( string format, params object[] data ) { Log( LogTypes.Info, format, data ); }

		public void Log( LogTypes logType, string data ) {
			if( !on || file == null )
				return;
			try {
				file.WriteLine( string.Concat(
					DateTime.Now.ToLongTimeString(),
					",\"",
					UserHelper.GetClientIP( Context.Request ),
					"\",\"",
					Context.Request.RawUrl.Replace( '"', '\'' ),
					"\",\"",
					logType.ToString(),
					"\",\"",
					data.Replace( '"', '\'' ),
					"\"" )
				);
			}
			catch( System.IO.IOException ) {
				// If there's an error writing, turn off logging 
				on = false;
			}
		}
		public void LogLn() {
			if( !on || file == null )
				return;
			try {
				file.WriteLine();
			}
			catch( System.IO.IOException ) {
				// If there's an error writing, turn off logging
				on = false;
			}
		}
		public void Log( System.Exception ex, string format, params object[] data ) { Log( ex, string.Format( format, data ) ); }
		public void Log( System.Exception ex, string message = null ) {
			if( !on || file == null )
				return;
			StringBuilder sb = new StringBuilder( 500 );
			if( !string.IsNullOrEmpty( message ) ) {
				sb.Append( message );
				sb.Append( "\r\n" );
			}
			// Insert exception message
			sb.Append( "Exception: " );
			sb.Append( ex.Message );
			sb.Append( "\r\n" );
			sb.Append( ex.Source );
			sb.Append( "\r\n" );
			sb.Append( ex.StackTrace );
			sb.Append( "\r\n" );
			sb.Append( ex.Data.ToString() );
			sb.Append( "\r\n" );
			// Insert inner exception message (if present)
			if( ex.InnerException != null ) {
				sb.Append( "Inner Exception: \r\n" );
				sb.Append( ex.InnerException.Message );
				sb.Append( "\r\n" );
				sb.Append( ex.InnerException.Source );
				sb.Append( "\r\n" );
				sb.Append( ex.InnerException.StackTrace );
				sb.Append( "\r\n" );
				sb.Append( ex.InnerException.Data.ToString() );
				sb.Append( "\r\n" );
			}
			Log( LogTypes.Exception, sb.ToString() );
		}

		/// <summary>
		/// For manually externalizing reference to another log by filename
		/// </summary>
		public void ExternalLog( string Filename ) {
			if( !on || file == null )
				return;
			file.WriteLine( "Detail externalized to: " + Filename );
		}
		/// <summary>
		/// For externalizing to another already open log
		/// </summary>
		public void ExternalLog( HttpServerUtility Server, TextLog Log ) {
			if( !on || file == null )
				return;
			var Filename = Log.GetFilename( Server, string.Empty );
			ExternalLog( Filename );
		}
		/// <summary>
		/// For externalizing a single large text string to a newly created log
		/// </summary>
		public void ExternalLog( HttpServerUtility Server, string outputstr ) {
			if( !on || file == null )
				return;
			var Filename = GetFilename( Server, Guid.NewGuid().ToBase64() );
			using( StreamWriter outfile = new StreamWriter( Filename, true ) ) {
				outfile.Write( outputstr );
				outfile.Flush();
				//outfile.Close();
			}
			ExternalLog( Filename );
		}

		#region IDisposable Members

		public void Dispose() {
			Dispose( true );
			GC.SuppressFinalize( this );
		}
		protected virtual void Dispose( bool disposing ) {
			if( disposing ) {
				if( file != null ) {
					file.Flush();
					file.Close();
					file.Dispose();
					file = null;
				}
			}
		}

		#endregion
	}

}
