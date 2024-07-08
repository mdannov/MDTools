using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Xml;
using MDTools.Web;

/*	Code Copyright Michael Dannov 2011-2012
 * 
 *	The Classes in this file are created, owned and copyrighted by Michael Dannov. 
 *	If you possess this file or it is part of your software library resources, you may
 *	need to verify with the author if you have been granted authorization and license to use. 
 */

///	Version 1.2 - TBD     - (Add method to detect if existing sitemap is valid and cached/persisted)
///	Version 1.1 - 6/18/11 - GoogleSiteIndex built Google SiteIndex using same SiteMap interfaces
/// Version 1.0 - 5/10/11 - GoogleSiteMap class to build Google SiteMap file


namespace MDTools.Web {

	public class GooglePage {
		public enum SiteMapFreq : byte { none = 0, always, hourly, daily, weekly, monthly, yearly, never };

		public string Url { get; set; }
		public DateTime LastModifiedDateTime { get; set; }
		public string LastModifiedString { 
			get { 
				return LastModifiedDateTime.Equals(DateTime.MinValue) ? null : LastModifiedDateTime.ToString( @"yyyy-MM-dd" ); 
			}
		}
		public SiteMapFreq ChangeFreq { get; set; }
		public decimal Priority { get; set; }
	}
	public static class SiteMapFreqHelper {

		public static void Set( this GooglePage.SiteMapFreq smf, string freq ) { smf = (GooglePage.SiteMapFreq)Enum.Parse( typeof( GooglePage.SiteMapFreq ), freq, true ); }
		public static void Set( this GooglePage.SiteMapFreq smf, char freq ) {
			switch( freq ) {
			case 'a':
				smf = GooglePage.SiteMapFreq.always;
				break;
			case 'h':
				smf = GooglePage.SiteMapFreq.hourly;
				break;
			case 'd':
				smf = GooglePage.SiteMapFreq.daily;
				break;
			case 'w':
				smf = GooglePage.SiteMapFreq.weekly;
				break;
			case 'm':
				smf = GooglePage.SiteMapFreq.monthly;
				break;
			case 'y':
				smf = GooglePage.SiteMapFreq.yearly;
				break;
			case 'n':
				smf = GooglePage.SiteMapFreq.never;
				break;
			default:
				smf = GooglePage.SiteMapFreq.none;
				break;
			}
		}
	}


	public interface ISiteMap : IDisposable {
		void Create();
		void AddPage( GooglePage page );
		void Close();
		string Filename { get; }
		string Filepath { get; }
		DateTime FileDate { get; }
		string ResponseType { get; }
	}

	public class GoogleSiteMap : ISiteMap {

		public event EventHandler OnOverflow = null;
		private HttpContext Context;

		private string filename = @"sitemap.xml";
		private string filepath = @"/";

		public string Filename {
			get { return filename; }
			set {
				filename = value;
				if(type == SitemapStreamType.XmlGZipFile )
					if( !filename.EndsWith( ".gz" ) )
						filename = filename + ".gz";
			}
		}
		public string Filepath {
			get {
				int len = filepath.Length;
				return ( len == 0 ) ? filename : ( filepath[len - 1] == '/' ) ? filepath + filename : filepath + '/' + filename;
			}
			set { filepath = value; }
		}

		public DateTime FileDate {
			get {
				if( type == SitemapStreamType.XmlMemoryStream)
					return default( DateTime );
				var dt = System.IO.File.GetLastWriteTime( Context.Server.MapPath( '~' + Filepath ) );
				return ( dt > new DateTime( 1602, 1, 1 ) ) ? dt : default( DateTime );
			}
		}

		private int cnt = 0;
		public int Count { get { return cnt; } }
		public long Length { get { return ( xml != null ) ? xml.BaseStream.Length : 0L; } }

		public DateTime lastModifiedDateTime;
		public string LastModifiedString { get { return lastModifiedDateTime.ToString( @"yyyy-MM-dd" ); } }

		private SitemapStreamType type;

		private XmlTextWriter xml = null;
		private MemoryStream mem = null;

		public XmlTextWriter Stream { get { return xml; } }

		public enum SitemapStreamType { XmlMemoryStream, XmlTextFile, XmlGZipFile };

		public string ResponseType {
			get {
				return ( type == SitemapStreamType.XmlGZipFile ) ? "application/x-gzip" : "text/xml";
			}
		}

		private const string xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";


		public GoogleSiteMap( HttpContext context, string path, SitemapStreamType type = SitemapStreamType.XmlGZipFile ) {
			this.Context = context;
			this.type = type;
			this.filepath = path;
			OnOverflow = Stop;
		}

		/// <summary>
		/// Create a new Sitemap Stream
		/// </summary>
		public void Create() {
			Create( null );
		}
		/// <summary>
		/// Create a new Sitemap file
		/// </summary>
		/// <param name="filepath">path to where to store the output file</param>
		public void Create(string filepath) {

			if(!string.IsNullOrEmpty(filepath)) {
				var url = new Url(filepath);
				filename = url.Filename;
				filepath = url.PathOnly;
			}
			// Open appropriate stream types
			switch( type ) {
			case SitemapStreamType.XmlMemoryStream:
				mem = new MemoryStream();
				xml = new XmlTextWriter( mem, Encoding.UTF8 );
				break;
			case SitemapStreamType.XmlTextFile:
				xml = new XmlTextWriter( Context.Server.MapPath('~' + Filepath), Encoding.UTF8 );
				break;
			case SitemapStreamType.XmlGZipFile:
				mem = new MemoryStream();
				xml = new XmlTextWriter( mem, Encoding.UTF8 );
				break;
			}

			xml.WriteStartDocument();
			xml.WriteStartElement( "urlset" );
			xml.WriteAttributeString( "xmlns", xmlns );
//			xml.WriteAttributeString( "xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance" );
//			xml.WriteAttributeString("xsi:schemaLocation", "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd");
			xml.WriteString( "\n" );

			xml.Flush();
		}

		private bool bstopped = false;

		/// <summary>
		/// Identify new Page to add to SiteMap
		/// </summary>
		public void AddPage( GooglePage page ) {
			if( bstopped )
				return;
			// Validate it's at least valid
			var url = page.Url;
			if( string.IsNullOrEmpty( url ) )
				return;

			xml.WriteStartElement( "url" );

			xml.WriteElementString( "loc", Context.Server.HtmlEncode(url) );

			if( page.LastModifiedDateTime != default(DateTime) )
				xml.WriteElementString( "lastmod", page.LastModifiedString );
			if( page.ChangeFreq != GooglePage.SiteMapFreq.none )
				xml.WriteElementString( "changefreq", page.ChangeFreq.ToString() );
			if( page.Priority != 0.0M )
				xml.WriteElementString( "priority", page.Priority.ToString( "0.0" ) );

			xml.WriteEndElement();
			xml.WriteString( "\n" );

			xml.Flush();

			// Update sitemap last modified time to last page last modified time
			if( page.LastModifiedDateTime > this.lastModifiedDateTime )
				this.lastModifiedDateTime = page.LastModifiedDateTime;

			// Increment number of pages
			cnt++;

			// Check if we've reached overflow
			if( cnt >= GoogleSiteIndex.MAX_PAGES-1 )
				OnOverflow( this, EventArgs.Empty );
			else if( Length > GoogleSiteIndex.MAX_FILESIZE )
				OnOverflow( this, EventArgs.Empty );
		}

		/// <summary>
		/// Stop file from growing any further
		/// </summary>
		protected void Stop( object sender, EventArgs e ) {
			// Imposes a stop so that the sitemap file doesn't grow beyond it's limits
			bstopped = true;
		}

		private bool isClosed = false;
		/// <summary>
		/// Close the SiteMap streams
		/// </summary>
		public void Close() {
			if( xml != null ) {
				if( isClosed == false ) {
					// Write end of file if not already
					xml.WriteString( "\n" );
					xml.WriteEndElement();
					xml.WriteEndDocument();
					xml.WriteString( "\n" );
					xml.Flush();

					// If this file is compressed
					if( this.type == SitemapStreamType.XmlGZipFile ) {
						// Copy stream in xml format sitemap to a byte buffer
						var basestream = xml.BaseStream;
						if( basestream == null )
							throw new Exception( "Could not write GZip Sitemap" );
						if( basestream.Length > int.MaxValue )
							throw new Exception( "GZip file too large" );
						int size = (int)basestream.Length;
						byte[] buffer = new byte[size];
						basestream.Position = 0;
						basestream.Read( buffer, 0, size );
						xml.Close();

						// Now write the byte buffer to a gzip file
						using( FileStream fs = new FileStream( Context.Server.MapPath( '~' + Filepath ), FileMode.Create ) ) {
							using( GZipStream gz = new GZipStream( fs, CompressionMode.Compress, false ) ) {
								gz.Write( buffer, 0, size );
								gz.Close();
							}
							fs.Close();
						}
					}
				}
				// Cleanup open resources
				xml.Close();
				xml = null;
			}
			if( mem != null ) {
				mem.Close();
				mem.Dispose();
				mem = null;
			}
			isClosed = true;
		}

		public void Dispose() {
			Close();
		}

	}


	public class GoogleSiteIndex : ISiteMap {
		public const int MAX_PAGES = 50000;
		public const long MAX_FILESIZE = 10000000;

		private HttpContext Context;

		private string filename = @"sitemap_index.xml";
		private string filepath = @"/";

		public string Filename {
			get { return filename; }
			set { filename = value; }
		}
		public string Filepath {
			get {
				int len = filepath.Length;
				return ( len == 0 ) ? filename : ( filepath[len - 1] == '/' ) ? filepath + filename : filepath + '/' + filename;
			}
		}
		public DateTime FileDate {
			get {
				var dt = System.IO.File.GetLastWriteTime( Context.Server.MapPath( '~' + Filepath ) );
				return ( dt > new DateTime( 1602, 1, 1 ) ) ? dt : default( DateTime );
			}
		}

		public string ResponseType { get { return "text/xml"; } }

		public GoogleSiteMap map = null;
		private XmlTextWriter xml = null;

		private int cnt = 0;

		private const string xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";

		public GoogleSiteIndex( HttpContext context, string path = null ) {
			this.Context = context;
			if( !string.IsNullOrEmpty( path ) )
				filepath = path;
		}

		public void Create(  ) {

			xml = new XmlTextWriter( Context.Server.MapPath( '~' + Filepath ), Encoding.UTF8 );

			xml.WriteStartDocument();
			xml.WriteStartElement( "sitemapindex" );
			xml.WriteAttributeString( "xmlns", xmlns );
//			xml.WriteAttributeString( "xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance" );
//			xml.WriteAttributeString( "xsi:schemaLocation", "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/siteindex.xsd" );
			xml.WriteString( "\n" );

			xml.Flush();

			SetupCurrentGoogleSiteMap();

			StartIndex();
		}

		protected void SetupCurrentGoogleSiteMap() {
//!! Error handling -if cannot open or create file, increment and go to next
			// Set up the current GoogleSiteMap File
			map = new GoogleSiteMap( Context, filepath, GoogleSiteMap.SitemapStreamType.XmlGZipFile );
			map.Filename = string.Format( "sitemap{0:00}.xml", cnt );
			//map.Filepath = filepath;
			map.Create();
			map.OnOverflow += OnOverflow;
			// Increment cur index count
			cnt++;
		}

		public void AddPage( GooglePage page ) {
			map.AddPage( page );
		}

		private bool bOpenIndex = false;
		public void StartIndex() {
			if( bOpenIndex )
				EndIndex();

			xml.WriteStartElement( "sitemap" );

			var url = new Url( Url.Schemes.http, Context.Request.Url.Host, map.Filepath, null );
			xml.WriteElementString( "loc", Context.Server.HtmlEncode(url.ToString()) );

			bOpenIndex = true;
			xml.Flush();
		}

		protected void EndIndex() {
			// Needed to break index in half in order to save only after the LAST last modified date from the map was 
			// discovered before closing
			if( map.lastModifiedDateTime != default( DateTime ) )
				xml.WriteElementString( "lastmod", map.LastModifiedString );
			xml.WriteEndElement();
			bOpenIndex = false;
		}

		public void OnOverflow( object sender, EventArgs e ) {
			// Close current gzip file and open the next
			map.Close();
			SetupCurrentGoogleSiteMap();
			// Add the new index
			StartIndex();
		}

		private bool isClosed = false;
		public void Close() {
			// If we get to close and haven't closed the last index, do so now
			if( bOpenIndex ) 
				EndIndex();

			if( xml != null && isClosed == false ) {
				// Write end of file if not already
				xml.WriteEndElement();
				xml.WriteEndDocument();
			}
			if( map != null ) {
				map.Dispose();// does close as well
				map = null;
			}
			// Cleanup open resources
			if( xml != null ) {
				xml.Flush();
				xml.Close();
				xml = null;
			}
			isClosed = true;
		}

		public void Dispose() {
			Close();
		}
	
	}

}
