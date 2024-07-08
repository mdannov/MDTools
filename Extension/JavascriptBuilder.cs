using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using System.Net;
using System.Threading;
using System.Collections;
using MDTools.Extension;

// Collection of Primitive Type Extensions from a number of web contributors

namespace MDTools.Web {

	public class JavascriptBuilder {

		protected class JavaStack {
			public JavaType type;
			public string name;
			public int pos;
			public JavaStack( JavaType type, string name, int pos=0 ) {
				this.type = type;
				this.name = name;
				this.pos = pos;
			}
			public override bool Equals( object obj ) {
				return Equals( obj as JavaStack );
			}
			public bool Equals( JavaStack obj ) {
				return ( obj != null && obj.type == this.type && obj.name == this.name );
			}
		}

		public enum JavaType { Start, Script, Object, Function, Array, Variable, InitArray, InitObject };

		public StringBuilder sb = new StringBuilder(1024);
		public bool Minify = true;

		public JavascriptBuilder() {
			stack.Push( jsstart ); 
		}

		public bool AtObject { get { return stack.Peek().type == JavaType.Object; } }
		public bool AtFunction { get { return stack.Peek().type == JavaType.Function; } }
		public bool AtStart { get { return stack.Peek().type == JavaType.Start; } }
		public bool AtScript { get { return stack.Peek().type == JavaType.Script; } }
		public bool AtArray { get { return stack.Peek().type == JavaType.Array; } }

		public int Length { get { var len = sb.Length; return sb == null ? len : sb.Length; } set { if( sb != null ) sb.Length = value; } }
		public int classnest = 0;
		public int membercnt = 0;
		public int functionnest = 0;
		protected Stack<JavaStack> stack = new Stack<JavaStack>( 5 );
		public bool scriptstart = false;
		public bool inScript = false;

		private static JavaStack jsstart = new JavaStack( JavaType.Start, "_Start" );
		private static JavaStack jsscript = new JavaStack( JavaType.Script, "Script" );

		public JavascriptBuilder StartScript() {
			if( scriptstart )
				throw new FormatException( "Attempt to Start a script within a script" );
			sb.Append( "<script type=\"text/javascript\">" );
			scriptstart = true;
			stack.Push( jsscript );
			return this;
		}

		public JavascriptBuilder EndScript() {
			if( scriptstart==false )
				throw new FormatException( "Attempt to End a script before it is started" );
			EndTo( jsscript );
			sb.Append( "</script>" );
			scriptstart = true;
			return this;
		}

		public JavascriptBuilder StartObject( string classname ) {
			stack.Push( new JavaStack( JavaType.Object, classname ) );
			if( stack.Peek().type != JavaType.Object)
				sb.Append( "var " );
			sb.Append( classname );
			if( classnest > 0 ) {
				sb.Append( ':' );
				if( !Minify )
					sb.Append( ' ' );
			}
			if(!Minify)
				sb.Append(' ');
			sb.Append( '{' );
			if(!Minify)
				sb.Append( "\r\n" );
			classnest++;
			return this;
		}

		public JavascriptBuilder EndObject( string classname = null ) {
			if( classname != null )
				EndTo( new JavaStack( JavaType.Object, classname ) );
			else
				// IF the name isn't specified, all we can do is make sure that the item on the stack is correct
				if( stack.Peek().type == JavaType.Object )
					stack.Pop();
				else
					throw new FormatException( "Attempt to End a class before one started" );
			sb.Append( "};" );
			if(!Minify)
				sb.Append( "\r\n" );
			return this;
		}

		public JavascriptBuilder StartFunction( string functionname) {
			return StartFunction( functionname, null );
		}
		public JavascriptBuilder StartFunction( string functionname, params string[] parameters ) {
			stack.Push( new JavaStack( JavaType.Function, functionname ) );
			if( classnest == 0 ) {
				// Build standard Function
				sb.Append( "function " );
				sb.Append( functionname );
			} else {
				// Build class Function
				if( membercnt > 0 )
					sb.Append( ',' );
				sb.Append( functionname );
				membercnt++;
			}
			if( parameters == null || parameters.Length == 0 )
				sb.Append( "()" );
			else {
				sb.Append( '(' );
				for( int i = 0, len = parameters.Length; i < len; i++ ) {
					var p = parameters[i];
					if( p == null )
						continue;
					if( i > 0 )
						sb.Append( ',' );
					sb.Append( p );
				}
				sb.Append( ')' );
			}
			if( !Minify )
				sb.Append( ' ' );
			sb.Append( '{' );
			functionnest++;
			return this;
		}

		public JavascriptBuilder EndFunction( string functionname = null ) {
			if( functionname != null )
				EndTo( new JavaStack( JavaType.Function, functionname ) );
			else
				// IF the name isn't specified, all we can do is make sure that the item on the stack is correct
				if( stack.Peek().type == JavaType.Function )
					stack.Pop();
				else
					throw new FormatException( "Attempt to End a function before one started" );
			sb.Append( "};" );
			if( !Minify )
				sb.Append( "\r\n" );
			return this;
		}

		public JavascriptBuilder StartVariable( string variablename ) {
			if( stack.Peek().type != JavaType.Object ) {
				sb.Append("var ");
			}
			sb.Append(variablename);
			sb.Append('=');
			return this;
		}

		public JavascriptBuilder SetVariable( string variablename, string value ) {
			sb.Append( variablename );
			sb.Append( "='" );
			sb.Append( value == null ? "null" : value );
			sb.Append( "';" );
			return this;
		}
		public JavascriptBuilder SetVariable( string variablename, object value ) {
			sb.Append( variablename );
			sb.Append( '=' );
			sb.Append( value == null ? "null" : value.ToString() );
			sb.Append( ';' );
			return this;
		}

		public JavascriptBuilder StartSetArray( string variablename ) {
			stack.Push( new JavaStack( JavaType.InitArray, variablename, 0 ) );
			sb.Append( variablename );
			sb.Append( "=[" );
			return this;
		}
		public JavascriptBuilder EndSetArray( string variablename = null) {
			if( variablename != null )
				EndTo( new JavaStack( JavaType.InitArray, variablename ) );
			else
				// IF the name isn't specified, all we can do is make sure that the item on the stack is correct
				if( stack.Peek().type == JavaType.InitArray )
					stack.Pop();
				else
					throw new FormatException( "Attempt to End a set array before one started" );
			sb.Append( variablename );
			sb.Append( "];" );
			return this;
		}

		public JavascriptBuilder SetArray( string variablename, params object[] list ) {
			StartSetArray( variablename);
			Array( list as IList);
			EndSetArray();
			return this;
		}
		public JavascriptBuilder SetArray( string variablename, IList list ) {
			StartSetArray( variablename );
			Array( list );
			EndSetArray();
			return this;
		}

		public JavascriptBuilder Array( params object[] list ) {
			return Array( list as IList );
		}
		public JavascriptBuilder Array( IList list ) {
			var s = stack.Peek();
			switch( s.type ) {
			case JavaType.InitArray:
			case JavaType.InitObject:
				if( s.pos > 0 )
					sb.Append( ',' );
				if( !Minify )
					sb.Append( "\r\n" );
				s.pos++;
				break;
			}
			sb.Append( '[' );
			for( int i = 0, len = list.Count; i < len; i++ ) {
				var item = list[i];
				if( i > 0 )
					sb.Append( ',' );
				if( item == null )
					sb.Append( "null" );
				else if( item.IsNumericType() )
					sb.Append( item.ToString() );
				else {
					sb.Append( '\'' );
					sb.Append( item );
					sb.Append( '\'' );
				}
			}
			sb.Append( ']' );
			return this;
		}

		protected JavascriptBuilder EndTo( JavaType type ) {
			if( inScript )
				return this;
			int len = stack.Count;
			inScript = true;
			while( stack.Peek().type != type ) {
				var cur = stack.Pop();
				switch( cur.type ) {
				case JavaType.Function:
					EndFunction();
					break;
				case JavaType.Object:
					EndObject();
					break;
				case JavaType.Script:
					EndScript();
					break;
				}
			}
			// Check if item was even found
			if( stack.Count > 0 && stack.Peek().type != type )
				throw new FormatException( string.Concat( "Script cannot close to ", type.ToString(),  "." ) ) ;
			inScript = false;
			return this;
		}

		protected JavascriptBuilder EndTo( JavaStack js ) {
			if( inScript )
				return this;
			int len = stack.Count;
			inScript = true;
			while( stack.Peek() != js ) {
				var cur = stack.Pop();
				switch( cur.type ) {
				case JavaType.Function:
					EndFunction();
					break;
				case JavaType.Object:
					EndObject();
					break;
				case JavaType.Script:
					EndScript();
					break;
				}
			}
			inScript = false;
			return this;
		}
		
		public string ToString() {
			EndTo( jsstart );
			return sb.ToString();
		}
	}

}
