using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// The code in the Jsmin class is substantially based on 
// Douglas Crockford's original jsmin.c.
// Hence the copyright notice below.
//
// Unlike Douglas Crockford's original version which reads from
// the standard input and writes to the standard output,
// this version takes a string and returns the minified string.

/* jsmin.c
   2011-01-22

Copyright (c) 2002 Douglas Crockford  (www.crockford.com)

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

The Software shall be used for Good, not Evil.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

namespace MDTools.Web.Utils {

	public class JsminCs {
		/// <summary>
		/// Uses the JSMIN algorithm to minify a string.
		/// Returns the minified version of the string.
		/// </summary>
		/// <param name="unminified"></param>
		/// <param name="doubleSlashIsComment">
		/// Set true if double slashes ( "//" ) and everything behind them on the line is to be regarded
		/// as a comment and removed. Set false to leave them in place.
		/// 
		/// For JavaScript files, use true.
		/// For CSS files, use false.
		/// </param>
		/// <param name="collapseWsToAtLeastOneWs">
		/// If true, runs of white space will be collapsed into at least one white space.
		/// So if you have two bits of text separated by one or more spaces, newlines, etc., you are
		/// guaranteed that the two bits of text will remain separated by at least one white space character.
		/// 
		/// If false, runs of white space may be completely removed if the bit of text after the white space
		/// is not alphanumeric - for example:
		/// var x=5;y=2;
		/// 
		/// You want to set this to false for JavaScript (where statements on a line are always separated by ;),
		/// but to true for CSS, where you can have constructs such as:
		/// .id1 .id2 { ..... }
		/// which is different from
		/// .id1.id2 { ..... }
		/// </param>
		/// <returns></returns>
		public string Minify( string unminified, bool doubleSlashIsComment, bool collapseWsToAtLeastOneWs ) {
			_unminified = unminified;
			_doubleSlashIsComment = doubleSlashIsComment;
			_collapseWsToAtLeastOneWs = collapseWsToAtLeastOneWs;
			_minifiedSb = new StringBuilder();
			_lengthUnminified = unminified.Length;
			_currentUnminifiedIndex = 0;

			// ----------------

			jsmin();

			return _minifiedSb.ToString();
		}

		private string _unminified = null;
		private bool _doubleSlashIsComment = true;
		private bool _collapseWsToAtLeastOneWs = true;
		private StringBuilder _minifiedSb = null;
		private int _lengthUnminified = -1;
		private int _currentUnminifiedIndex = -1;
		private const char EOF = '\u0000';

		/// <summary>
		/// Simulates getting a character from the standard input
		/// </summary>
		/// <returns></returns>
		private char getc() {
			if( _currentUnminifiedIndex >= _lengthUnminified ) {
				return EOF;
			}

			char nextChar = _unminified[_currentUnminifiedIndex];
			_currentUnminifiedIndex++;
			return nextChar;
		}

		/// <summary>
		/// Simulates writing a character to the standard output
		/// </summary>
		private void putc( char c ) {
			_minifiedSb.Append( c );
		}

		/// <summary>
		/// Code ported from Douglas Crockford's jsmin starts here.
		/// </summary>

		private char theA;
		private char theB;
		private char theLookahead = EOF;

		/// <summary>
		/// Returns true if you cannot remove all white space in front of the given character.
		/// False otherwise.
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		private bool isWsDependent( char c ) {
			if( _collapseWsToAtLeastOneWs ) {
				// If white space runs need to collapse to at least one white space,
				// then you can only remove all white space before c if c itself is white space.
				return ( !Char.IsWhiteSpace( c ) );
			}

			return ( Char.IsLetterOrDigit( c ) || c == '_' || c == '$' || c == '\\' );
		}

		/* get -- return the next character from stdin. Watch out for lookahead. If
				the character is a control character, translate it to a space or
				linefeed.
		*/

		private char get() {
			char c = theLookahead;
			theLookahead = EOF;
			if( c == EOF ) {
				c = getc();
			}
			if( ( !Char.IsControl( c ) ) || c == '\n' || c == EOF ) {
				return c;
			}
			if( c == '\r' ) {
				return '\n';
			}
			return ' ';
		}

		/* peek -- get the next character without getting it.
		*/

		private char peek() {
			theLookahead = get();
			return theLookahead;
		}

		/* next -- get the next character, excluding comments. peek() is used to see
				if a '/' is followed by a '/' or '*'.
		*/

		private char next() {
			char c = get();
			if( c == '/' ) {
				switch( peek() ) {
				case '/':
					if( _doubleSlashIsComment ) {
						for( ; ; ) {
							c = get();
							if( c <= '\n' ) {
								return c;
							}
						}
					}
					break;
				case '*':
					get();
					for( ; ; ) {
						switch( get() ) {
						case '*':
							if( peek() == '/' ) {
								get();
								return ' ';
							}
							break;
						case EOF:
							throw new Exception( "Error: JSMIN Unterminated comment." );
						}
					}
				default:
					return c;
				}
			}
			return c;
		}

		/* action -- do something! What you do is determined by the argument:
				1   Output A. Copy B to A. Get the next B.
				2   Copy B to A. Get the next B. (Delete A).
				3   Get the next B. (Delete B).
		   action treats a string as a single character. Wow!
		   action recognizes a regular expression if it is preceded by ( or , or =.
		*/

		private void action( int d ) {
			if( d <= 1 ) {
				putc( theA );
			}

			if( d <= 2 ) {
				theA = theB;
				if( theA == '\'' || theA == '"' ) {
					for( ; ; ) {
						putc( theA );
						theA = get();
						if( theA == theB ) {
							break;
						}
						if( theA == '\\' ) {
							putc( theA );
							theA = get();
						}
						if( theA == EOF ) {
							throw new Exception( "Error: JSMIN unterminated string literal." );
						}
					}
				}
			}

			if( d <= 3 ) {
				theB = next();
				if( theB == '/' && ( theA == '(' || theA == ',' || theA == '=' ||
									theA == ':' || theA == '[' || theA == '!' ||
									theA == '&' || theA == '|' || theA == '?' ||
									theA == '{' || theA == '}' || theA == ';' ||
									theA == '\n' ) ) {
					putc( theA );
					putc( theB );
					for( ; ; ) {
						theA = get();
						if( theA == '[' ) {
							for( ; ; ) {
								putc( theA );
								theA = get();
								if( theA == ']' ) {
									break;
								}
								if( theA == '\\' ) {
									putc( theA );
									theA = get();
								}
								if( theA == EOF ) {
									throw new Exception( "Error: JSMIN unterminated set in Regular Expression literal." );
								}
							}
						} else if( theA == '/' ) {
							break;
						} else if( theA == '\\' ) {
							putc( theA );
							theA = get();
						}
						if( theA == EOF ) {
							throw new Exception( "Error: JSMIN unterminated Regular Expression literal." );
						}
						putc( theA );
					}
					theB = next();
				}
			}
		}

		/* jsmin -- Copy the input to the output, deleting the characters which are
				insignificant to JavaScript. Comments will be removed. Tabs will be
				replaced with spaces. Carriage returns will be replaced with linefeeds.
				Most spaces and linefeeds will be removed.
		*/

		private void jsmin() {
			theA = '\n';
			action( 3 );
			while( theA != EOF ) {
				switch( theA ) {
				case ' ':
					if( isWsDependent( theB ) ) {
						action( 1 );
					} else {
						action( 2 );
					}
					break;
				case '\n':
					switch( theB ) {
					case '{':
					case '[':
					case '(':
					case '+':
					case '-':
						action( 1 );
						break;
					case ' ':
						action( 3 );
						break;
					default:
						if( isWsDependent( theB ) ) {
							action( 1 );
						} else {
							action( 2 );
						}
						break;
					}
					break;

				default:
					switch( theB ) {
					case ' ':
						if( isWsDependent( theA ) ) {
							action( 1 );
							break;
						}
						action( 3 );
						break;
					case '\n':
						switch( theA ) {
						case '}':
						case ']':
						case ')':
						case '+':
						case '-':
						case '"':
						case '\'':
							action( 1 );
							break;
						default:
							if( isWsDependent( theA ) ) {
								action( 1 );
							} else {
								action( 3 );
							}

							break;
						}
						break;
					default:
						action( 1 );
						break;
					}

					break;
				}
			}
		}
	}
}

