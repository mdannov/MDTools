//#define TestCode
using System;
using System.Collections.Generic;
using System.Text;
using MDTools.Extension;

namespace MDTools.Math {

	public static class Base2toN {
		// base: 2 Bin, 8 Oct, 10 Dec, 16 Hex, 36 0-Z, 62 0-Z-z, 64 Url, 79 PW
		//                                           1         2         3         4         5         6         7         8          9
		//                                  1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678 90 1234
		public const string URL64_TABLE =  "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_-~!@#$%^&*()+=:?;,./{}[]|\\'\"<>`";
		public const string BASE64_TABLE = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz+/_-~!@#$%^&*()=:?;,.{}[]|\\'\"<>`";

		private static int[] lengths64 = { 64,41,32,28,25,23,22,21,20,19,18,18,17,17,16,16,16,16,15,15,15,15,14,14,14,14,14,14,14,13,13,13,13,13,13,13,13,13,13,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,10,10,10,10,10,10,10,10,10 };
			/*{ 64, 41, 32, 28, 25, 23, 22, 21, 20, 19, 18, 18, 17, 17, 16, 16, 16, 16, 15, 15, 15, 15, 14, 14, 14, 14, 14, 14, 14, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 11, 11, 11, 11, 11, 11, 11, 11 };*/
		private static int[] lengths32 = { 32,21,16,14,13,12,11,11,10,10,9,9,9,9,8,8,8,8,8,8,8,8,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,5,5,5,5,5,5,5,5,5 };
			/*{ 32, 21, 16, 14, 13, 12, 11, 11, 10, 10, 9, 9, 9, 9, 8, 8, 8, 8, 8, 8, 8, 8, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6 };*/
		public const int MaxBase = 94;

		/// <summary>
		/// Convert number to another base starting from 0 to uppercase letters to lowercase letters
		/// </summary>
		/// <param name="numbase">2-64</param>
		/// <returns></returns>
		public static string ToBase( this UInt64 iDec, uint numbase, string sTable = Base2toN.URL64_TABLE ) {
			if( iDec == 0 ) return "0";
			int maxlen = lengths64[numbase - 2];//!! If exception here, base was too large > MaxBase
			char[] str = new char[maxlen];
			int pos = maxlen;
			for( ; iDec > 0; iDec /= numbase ) {
				str[--pos] = sTable[(int)( iDec % numbase )];
			}
			return new string( str, pos, maxlen - pos );
		}
		public static string ToBase( this UInt32 iDec, uint numbase, string sTable = Base2toN.URL64_TABLE ) {
			if( iDec == 0 ) return "0";
			int maxlen = lengths32[numbase - 2];
			char[] str = new char[maxlen];
			int pos = maxlen;
			for( ; iDec > 0; iDec /= numbase ) {
				str[--pos] = sTable[(int)( iDec % numbase )];
			}
			return new string( str, pos, maxlen - pos );
		}
		//!! Needs to be tested
		public static string ToBase( this Guid guid, uint numbase, string sTable = Base2toN.URL64_TABLE ) {
			if( guid == default(Guid) )
				return "0";
			var gbytes = guid.ToByteArray();
			int maxlen = 64;
			char[] str = new char[maxlen];
			int pos = maxlen;
			UInt64 iDec = gbytes[0];
			int gpos = 1;
			while( iDec > 0 ) {
				if(iDec<=255 && gpos<16)
					// add another 8 bits until empty
					iDec=iDec<<4 + gbytes[gpos++];
				str[--pos] = sTable[(int)( iDec % numbase )];
				iDec /= numbase;
			}
			return new string( str, pos, maxlen - pos );
		}


		public static Dictionary<char, uint> CreateLookupTable( string StringTable ) {
			var res = new Dictionary<char,uint>(StringTable.Length);
			for( int i = 0, len = StringTable.Length; i < len; i++ ) 
				res.Add( StringTable[i], (uint)i );
			return res;
		}

		/// <summary>
		/// Convert base representation string to ulong from BASE64TABLE
		/// Recommend using preloaded static lookup dictionary
		/// </summary>
		public static UInt64 ToUInt64( this string sBase, uint numbase, Dictionary<char, uint> lookupTable ) {
			UInt64 dec = 0;
			UInt64 iProduct = 1;

			for( int i = sBase.Length - 1; i >= 0; i--, iProduct *= numbase ) {
				var c = sBase[i];
				dec += ( lookupTable[c] * iProduct );
			}
			return dec;
		}
		/// <summary>
		/// Convert base representation string to ulong from BASE64TABLE
		/// Recommend using preloaded static lookup dictionary
		/// </summary>
		public static UInt32 ToUInt32( this string sBase, uint numbase, Dictionary<char, uint> lookupTable ) {
			UInt32 dec = 0;
			UInt32 iProduct = 1;

			for( int i = sBase.Length - 1; i >= 0; i--, iProduct *= numbase ) {
				var c = sBase[i];
				dec += ( lookupTable[c] * iProduct );
			}
			return dec;
		}
		/// <summary>
		/// Fastest: Convert base representation string to ulong from BASE64TABLE
		/// </summary>
		public static UInt64 ToUInt64_URL64TABLE( this string sBase, uint numbase ) {
			UInt64 dec = 0;
			UInt64 b = 0;
			UInt64 iProduct = 1;

			for( int i = sBase.Length - 1; i >= 0; i--, iProduct *= numbase ) {
				//var sValue = sBase[i];
				var c = sBase[i];
				if( c <= '9' ) {		   // 39
					if( c >= '0' )	   // 30
						b = (UInt64)( c - '0' );
					else if( c == '+' )// 2B
						b = (UInt64)( c - '_' ) + 62;
					else               // 2F
						b = (UInt64)( c - '-' ) + 63;
				} else if( c <= 'Z' )  // 5A
					b = (UInt64)( c - 'A' ) + 10;
				else // 'a'-'z'
					b = (UInt64)( c - 'a' ) + 36;
				dec += ( b * iProduct );
			}
			return dec;
		}
		/// <summary>
		/// Fastest: Convert base representation string to uint from BASE64TABLE
		/// </summary>
		public static UInt32 ToUInt32_URLTABLE( this string sBase, uint numbase ) {
			UInt32 dec = 0;
			UInt32 b = 0;
			UInt32 iProduct = 1;

			for( int i = sBase.Length - 1; i >= 0; i--, iProduct *= numbase ) {
				//var sValue = sBase[i];
				var c = sBase[i];
				if( c <= '9' ) {		   // 39
					if( c >= '0' )	   // 30
						b = (UInt32)( c - '0' );
					else if( c == '+' )// 2B
						b = (UInt32)( c - '_' ) + 62;
					else               // 2F
						b = (UInt32)( c - '-' ) + 63;
				} else if( c <= 'Z' )  // 5A
					b = (UInt32)( c - 'A' ) + 10;
				else // 'a'-'z'
					b = (UInt32)( c - 'a' ) + 36;
				dec += ( b * iProduct );
			}
			return dec;
		}
		/// <summary>
		/// Fastest: Convert base representation string to ulong from BASE64TABLE
		/// </summary>
		public static UInt64 ToUInt64_BASE64TABLE( this string sBase, uint numbase ) {
			UInt64 dec = 0;
			UInt64 b = 0;
			UInt64 iProduct = 1;

			for( int i = sBase.Length - 1; i >= 0; i--, iProduct *= numbase ) {
				//var sValue = sBase[i];
				var c = sBase[i];
				if( c <= '9' ) {		   // 39
					if( c >= '0' )	   // 30
						b = (UInt64)( c - '0' );
					else if( c == '+' )// 2B
						b = (UInt64)( c - '+' ) + 62;
					else               // 2F
						b = (UInt64)( c - '/' ) + 63;
				} else if( c <= 'Z' )  // 5A
					b = (UInt64)( c - 'A' ) + 10;
				else // 'a'-'z'
					b = (UInt64)( c - 'a' ) + 36;
				dec += ( b * iProduct );
			}
			return dec;
		}
		/// <summary>
		/// Fastest: Convert base representation string to uint from BASE64TABLE
		/// </summary>
		public static UInt32 ToUInt32_BASE64TABLE( this string sBase, uint numbase ) {
			UInt32 dec = 0;
			UInt32 b = 0;
			UInt32 iProduct = 1;

			for( int i = sBase.Length - 1; i >= 0; i--, iProduct *= numbase ) {
				//var sValue = sBase[i];
				var c = sBase[i];
				if( c <= '9' ) {		   // 39
					if( c >= '0' )	   // 30
						b = (UInt32)( c - '0' );
					else if( c == '+' )// 2B
						b = (UInt32)( c - '+' ) + 62;
					else               // 2F
						b = (UInt32)( c - '/' ) + 63;
				} else if( c <= 'Z' )  // 5A
					b = (UInt32)( c - 'A' ) + 10;
				else // 'a'-'z'
					b = (UInt32)( c - 'a' ) + 36;
				dec += ( b * iProduct );
			}
			return dec;
		}

#if perfbadwithstringconversion
		public static string ToBase32( this UInt32 val ) {
			return Convert.ToBase64String( BitConverter.GetBytes(val)).Replace( '/', '-' ).Replace( '+', '_' ).Replace( "=", "" );
		}
		public static UInt32 ToUInt32FromBase64( this string base64 ) {
			base64 = base64.Replace( '-', '/' ).Replace( '_', '+' ) + "==";
			return BitConverter.ToUInt32( Convert.FromBase64String( base64 ), 0 );
		}
#endif
		/// <summary>
		/// http://stackoverflow.com/questions/1032376/guid-to-base64-for-url
		/// </summary>
		public static string ToBase64( this Guid guid ) {
			var enc = Convert.ToBase64String( guid.ToByteArray() ).ReplaceAll( guidT, guidF );// Replace( '/', '-' ).Replace( '+', '_' ).Replace( "=", "" );
			int len = enc.Length;
			if(len>0 && enc[len-1]=='=')  {
				len--;
				if(len>0 && enc[len-1]=='=')
					len--;
				return enc.Substring( 0, len );
			}
			return enc;
		}
		public static Guid GuidFromBase64( this string base64 ) {
			base64 = base64.ReplaceAll( guidF, guidT ) + "==";//Replace( '-', '/' ).Replace( '_', '+' ) + "==";
			return new Guid( Convert.FromBase64String( base64 ) );
		}
		private static char[] guidT = { '/', '+' };
		private static char[] guidF = { '-', '_' };

#if TestCode
		public static string CalcLenStrings() {
			var s = string.Empty;
			for( uint i = 2; i < MaxBase; i++ ) {
				s += ( Base2toN.ToBase( UInt64.MaxValue, i ).Length.ToString() + "," );
			}
			return s;
		}
		#region Unit Test Code
		public static void Test() {

			var r1 = new Random();
			var r2 = new Random();

			string res1 = string.Empty;
			UInt32 res2 = 0;
			DateTime then = DateTime.Now;

			for( int i = 0; i < 1000000; i++ ) {
				UInt32 v = (UInt32)r1.Next();
				uint b = (uint)r2.Next( 2, 64 );
				res1 = Base2to64.ToBase( v, b );
				res2 = Base2to64.ToUInt32( res1, b );
				if( res2 != v )
					Base2to64.ToUInt32( res1, 0 );//forcecrash
			}
			var time = ( DateTime.Now - then ).TotalMilliseconds.ToString();
		}
		public static void PerfTime() {
			string res1 = string.Empty;
			UInt64 res2 = 0;
			DateTime then = DateTime.Now;

			for( int i = 0; i < 1000000; i++ ) {
				res1 = Base2to64.ToBase( UInt32.MaxValue, 64 );	//** "165"
				//res1 = UInt32.MaxValue.ToBase32();//** "90" w/o string conversion, "384" with
			}
			var time = ( DateTime.Now - then ).TotalMilliseconds.ToString();
			then = DateTime.Now;
			for( int i = 0; i < 1000000; i++ ) {
				res2 = Base2to64.ToUInt32 ( res1, 64 );//** 106
				//res2 = res1.ToUInt32FromBase64();//** "156" w/o string conversion, "372" with
			}
			time = ( DateTime.Now - then ).TotalMilliseconds.ToString();
			return;
		}
		#endregion
#endif
	}
}
