using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Configuration;
#if Yui
using Yahoo.Yui.Compressor;
using System.Configuration;
#endif

namespace MDTools.Web.Utils {

	public enum CleanType {
		None,
		Html,
		Script,
		Style,
		ScriptTag,
		StyleTag,
		ScriptSrc
	}
	public struct CleanSection {
		public CleanType type;
		public string content;
	}

	public static class MinifySettings {
		public readonly static string AS_GenStaticDirectory = ConfigurationManager.AppSettings["GenerateDirectory"];
		public readonly static bool AS_MinifyActive = ConfigurationManager.AppSettings["Minify"] == "1";
	}



	public static class MinifyHtml {

		public static string CleanHtmlAndInlines( string literal ) {
			// If nothing to do
			if( !MinifySettings.AS_MinifyActive && !VersionParameter.AS_VersionActive )
				return literal;

			string result = string.Empty;
#if Yui
			// Split literal into sections
			var list = SplitSections( literal, MinifySettings.AS_MinifyActive, VersionParameter.AS_VersionActive );
			if( list != null ) {
				string str;
				StringBuilder sb = new StringBuilder( literal.Length );
				for( int i = 0, len = list.Count; i < len; i++ ) {
					var item = list[i];
					switch( item.type ) {
					case CleanType.Html:
						str = MinifySettings.AS_MinifyActive ? CleanHtml( item.content ) : item.content;
						break;
					case CleanType.Script:
#if Yui
						str = MinifySettings.AS_MinifyActive ? new JavaScriptCompressor().Compress( item.content ) : item.content;
#else
						str = MinifySettings.AS_MinifyActive ? new JsminCs().Minify( item.content, true, false ) : item.content;
#endif
						break;
					case CleanType.Style:
#if Yui
						str = MinifySettings.AS_MinifyActive ? new CssCompressor().Compress( item.content ) : item.content;
#else
						str = MinifySettings.AS_MinifyActive ? new JsminCs().Minify( item.content, false, true ) : item.content;
#endif
						break;
					case CleanType.StyleTag:
					case CleanType.ScriptTag:
						str = MinifySettings.AS_MinifyActive ? CleanTag( item.content ) : item.content;
						break;
					case CleanType.ScriptSrc:
						str = VersionParameter.ProcessScript( item.content );
						break;
					default:
						str = item.content;
						break;
					}
					if( !string.IsNullOrEmpty( str ) )
						sb.Append( str );
				}
				result = sb.ToString();
			} else
#endif
				// If there were no script or styles, just shortcut to html clean
				result = MinifySettings.AS_MinifyActive ? CleanHtml( literal ) : literal;

			return result;
		}

		public static string CleanHtml( string literal ) {
			// Remove any situation with whitespaces up to a new line and after with a single newline
			string result = Regex.Replace( literal, @"[ \t]*[\r\n][\r\n \t]*", "\n" );//Regex.Replace( literal, @"\s*\n\s*", "\n", RegexOptions.Multiline );
			// Remove all whitespaces that appear at least twice 
			result = Regex.Replace( result, @"[ \t]{2,}", " " );// Regex.Replace( result, @"\s{2,}", " ", RegexOptions.Multiline );
#if removecomments
			// Remove comments
			result = Regex.Replace( literal, "<!--.*?-->", "", RegexOptions.Singleline );
#endif
			// No need to publish an empty line
			return result == "\n" ? string.Empty : result;
		}

		public static string CleanTag( string literal ) {
			// Remove all whitespaces that appear at least twice !! Is this safe?
			string result = Regex.Replace( literal, @"[ \t]{2,}", " " );//Regex.Replace( literal, @"\s{2,}", " ", RegexOptions.Multiline );
			return result;
		}

		private static List<CleanSection> SplitSections( string literal, bool supportMinify=true, bool supportScriptVersion =false) {
			if(!supportMinify && !supportScriptVersion)
				return null;
			List<CleanSection> list = null;
			int seekpos = 0;
			int pos = 0;
			CleanType type = CleanType.None;
			int tagstart;
			for( tagstart = literal.IndexOf( "<s", seekpos, StringComparison.OrdinalIgnoreCase ); tagstart >= 0; tagstart = literal.IndexOf( "<s", seekpos, StringComparison.OrdinalIgnoreCase ) ) {
				// Determine if this tag is script or style
				if( string.Compare( "cript", 0, literal, tagstart + 2, 5, StringComparison.OrdinalIgnoreCase ) == 0 ) {
					// Item is javascript
					type = CleanType.Script;
				} else if( supportMinify && string.Compare( "tyle", 0, literal, tagstart + 2, 4, StringComparison.OrdinalIgnoreCase ) == 0 ) {
					// Item is style css
					type = CleanType.Style;
				} else {
					// Isn't a tag we care about, so continue to next
					seekpos = tagstart + 2;
					type = CleanType.None;
					continue;
				}
				// Tag was found so process it
				var tagend = literal.IndexOf( ">", tagstart + 1 );
				if( tagend <= 0 ) {
					// If tag wasn't properly formed, we can't do more
					break;
				}
				// Check that it's end-closed tag
				if( literal[tagend - 1] == '/' ) {
					// we don't need to special process end-close tags, so continue to next
					seekpos = tagend + 1;
					continue;
				}
				var closepos = literal.IndexOf( type == CleanType.Script ? "</script>" : "</style>", tagend + 1 );
				if( closepos <= 0 ) {
					// If that tag wasn't closed, we can't do more
					break;
				}
				if( type == CleanType.Script ) {
					// if it's a script with a source, we can include it in html and continue to next
					if( literal.IndexOf( "src=", tagstart + 8, tagend - tagstart - 7 ) >= 0 ) {
						if( supportScriptVersion ) {
							// Create Script as ScriptSrc; reposition tagend to beginning of script
							tagend = tagstart - 1;
							type = CleanType.ScriptSrc;
						} else {
							// If not supporting ScriptVersion, include as html
							seekpos = closepos + 9;
							continue;
						}
					}
				}

				// Check if we need to process anything
				if( !( supportMinify || ( type == CleanType.ScriptSrc && supportScriptVersion ) ) ) {
					seekpos = closepos + ( type == CleanType.Script ? 9 : 8 );
					//pos = closepos;
					continue;
				}
				// Tag was fully formed, so we can minify it
				if( list == null )
					list = new List<CleanSection>();
				// Add the html to process later
				if( pos < tagend )
					list.Add( new CleanSection() {
						content = literal.Substring( pos, tagend + 1 - pos ),
						type = CleanType.Html
					} );
				// Add the script or style to process later
				list.Add( new CleanSection() {
					content = literal.Substring( tagend + 1, closepos - tagend - 1 ),
					type = type
				} );
				// Reposition current and continue
				seekpos = closepos + ( type == CleanType.Script ? 9 : 8 );
				pos = closepos;
			}
			if( list == null || list.Count <= 0 )
				return null;
			// Check if there is end html that wasn't added to the list
			if( tagstart <= 0 && pos > 0 )
				list.Add( new CleanSection() {
					content = literal.Substring( pos ),
					type = CleanType.Html
				} );
			return list;
		}
	}

}
