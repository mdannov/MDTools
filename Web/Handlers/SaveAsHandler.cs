using System;
using System.Web;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using MDTools.Web;
using MDTools.Images;

namespace MDTools.Web.Handler {

	/// <summary>
	/// Send file so that File Save As dialog appears on client's browser.
	///
	/// Security Concern: SaveAs can be used by a hacker to potentially receive any file on your drive if not properly set up.
	/// 
	///   Do not set this handler in root of your website. 
	///   Example as follows:
	///      <add path="saveas.axd" verb="*" name="SaveAsRequestHandler" preCondition="integratedMode" type="MDTools.Web.Handler.SaveAsHandler" />
	///
	///   If you're going to use it as above, instead create a web.config entry in a subdirectory 
	///   where you intend to house content files you wish to allow save access.
	///   
	///   When using saveas.axd, link the file in your html as follows
	///      <a href="/saveas.axd?file=/img/MyFile.pdf" title="Download MyFile.pdf">Download pdf</a>
	///   
	///   Alternately, (even safer) specify each file you wish to individually allow access to in web.config 
	///   by specifying the file to the SaveAsHandler directly 
	///      <add path="/MyFile.pdf" verb="*" name="PdfSaveAsRequestHandler" preCondition="integratedMode" type="MDTools.Web.Handler.SaveAsHandler" />
	///      
	/// </summary>
	public class SaveAsHandler : IHttpHandler {

		public void ProcessRequest( System.Web.HttpContext Context ) {
			var Request = Context.Request;
			var Response = Context.Response;
			var Server = Context.Server;

			// Use the file being specified as a parameter ("file=") or is the actual file to transmit
			var url = HandlerHelper.FileParmeterOrRequest( Request );

			// Check that file exists
			Context.Send404IfFileDoesntExist( url.Path );

			Context.Response.Cache.SetCacheability( HttpCacheability.Public );

			// Set ContentType
			var contenttype = ContentTypes.FromExtension( url.Extension );
			Context.Response.ContentType = contenttype;

			// Setup response header for save as
			Response.AddHeader( "Content-Disposition", "attachment; filename=" + url.Filename );

			// Send the file
			ServerCache.SendBinaryFile( Context, url.Path, ContentTypes.CanCompress(contenttype) );

			Response.End();
		}

		public bool IsReusable {
			get {
				return false;
			}
		}
	}

}
