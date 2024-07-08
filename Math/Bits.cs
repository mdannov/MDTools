using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDTools.Math {

	public struct BitsAndMask {
		public UInt64 bits;
		public ulong mask;
	}

	public static class Bits {

		/// <summary>
		/// Set single bit and mask at position
		/// </summary>
		public static void Set( int pos, bool bset, ref UInt64 bits, ref UInt64 mask ) {
			UInt64 bval = 1ul << pos;
			if( bset )
				// Set to 1/true
				bits |= bval;
			else
				// Set to 0/false
				bits &= ~bval;
			mask |= bval;	// mark this entry as set
		}
		/// <summary>
		/// Set single bit at position
		/// </summary>
		public static void Set( int pos, bool bset, ref UInt64 bits ) {
			UInt64 bval = 1ul << pos;
			if( bset )
				// Set to 1/true
				bits |= bval;
			else
				// Set to 0/false
				bits &= ~bval;
		}
		/// <summary>
		/// Set multiple bits by mask
		/// </summary>
		public static void Set( bool bset, ref UInt64 bits, UInt64 mask ) {
			if( bset )
				bits |= mask;
			else
				bits &= ~mask;
		}
		/// <summary>
		/// Set single bit and mask at position
		/// </summary>
		public static void Set( int pos, bool bset, ref UInt32 bits, ref UInt32 mask ) {
			UInt32 bval = 1u << pos;
			if( bset )
				// Set to 1/true
				bits |= bval;
			else
				// Set to 0/false
				bits &= ~bval;
			mask |= bval;	// mark this entry as set
		}
		/// <summary>
		/// Set single bit at position
		/// </summary>
		public static void Set( int pos, bool bset, ref UInt32 bits ) {
			UInt32 bval = 1u << pos;
			if( bset )
				// Set to 1/true
				bits |= bval;
			else
				// Set to 0/false
				bits &= ~bval;
		}
		/// <summary>
		/// Set multiple bits by mask
		/// </summary>
		public static void Set( bool bset, ref UInt32 bits, UInt32 mask ) {
			if( bset )
				bits |= mask;
			else
				bits &= ~mask;
		}

		/// <summary>
		/// Get the bool of the bit at position
		/// </summary>
		public static bool Get( UInt64 bits, int pos ) {
			return ( bits & ( 1ul << pos ) ) > 0;
		}
		/// <summary>
		/// Get the bits specified by a mask
		/// </summary>
		public static ulong Get( UInt64 bits, UInt64 mask ) {
			return ( bits & mask );
		}
		/// <summary>
		/// Get the bool of the bit at position
		/// </summary>
		public static bool Get( UInt32 bits, int pos ) {
			return ( bits & ( 1ul << pos ) ) > 0;
		}
		/// <summary>
		/// Get the bits specified by a mask
		/// </summary>
		public static ulong Get( UInt32 bits, UInt32 mask ) {
			return ( bits & mask );
		}

		/// <summary>
		/// Hints http://www.catonmat.net/blog/low-level-bit-hacks-you-absolutely-must-know/
		/// </summary>

		public static bool IsEven( this Int32 x ) { return ( x & 1 ) == 0; }
		public static bool IsEven( this UInt32 x ) { return ( x & 1 ) == 0; }
		public static bool IsEven( this Int64 x ) { return ( x & 1 ) == 0; }
		public static bool IsEven( this UInt64 x ) { return ( x & 1 ) == 0; }
		public static bool IsEven( this Int16 x ) { return ( x & 1 ) == 0; }
		public static bool IsEven( this UInt16 x ) { return ( x & 1 ) == 0; }
		public static bool IsEven( this byte x ) { return ( x & 1 ) == 0; }

		public static bool IsSet( this Int32 x, int n ) { return ( x & ( 1 << n ) ) != 0; }
		public static bool IsSet( this UInt32 x, int n ) { return ( x & ( 1u << n ) ) != 0; }
		public static bool IsSet( this Int64 x, int n ) { return ( x & ( 1 << n ) ) != 0; }
		public static bool IsSet( this UInt64 x, int n ) { return ( x & ( 1ul << n ) ) != 0; }
		public static bool IsSet( this Int16 x, int n ) { return ( x & ( 1 << n ) ) != 0; }
		public static bool IsSet( this UInt16 x, int n ) { return ( x & ( 1 << n ) ) != 0; }
		public static bool IsSet( this byte x, int n ) { return ( x & ( 1 << n ) ) != 0; }

		public static Int32 SetOn( this Int32 x, int n ) { return ( x | ( 1 << n ) ); }
		public static UInt32 SetOn( this UInt32 x, int n ) { return ( x | ( 1u << n ) ); }
		public static Int64 SetOn( this Int64 x, int n ) { return ( x | ( 1u << n ) ); }
		public static UInt64 SetOn( this UInt64 x, int n ) { return ( x | ( 1ul << n ) ); }
		public static Int16 SetOn( this Int16 x, int n ) { return (Int16)( x | ( 1 << n ) ); }
		public static UInt16 SetOn( this UInt16 x, int n ) { return (UInt16)( x | ( 1 << n ) ); }
		public static byte SetOn( this byte x, int n ) { return (byte)( x | ( 1 << n ) ); }

		public static Int32 SetOff( this Int32 x, int n ) { return ( x & ~( 1 << n ) ); }
		public static UInt32 SetOff( this UInt32 x, int n ) { return ( x & ~( 1u << n ) ); }
		public static Int64 SetOff( this Int64 x, int n ) { return ( x & ~( 1 << n ) ); }
		public static UInt64 SetOff( this UInt64 x, int n ) { return ( x & ~( 1ul << n ) ); }
		public static Int16 SetOff( this Int16 x, int n ) { return (Int16)( x & ~( 1 << n ) ); }
		public static UInt16 SetOff( this UInt16 x, int n ) { return (UInt16)( x & ~( 1 << n ) ); }
		public static byte SetOff( this byte x, int n ) { return (byte)( x & ~( 1 << n ) ); }

		public static Int32 SetOffRightmostOn( this Int32 x, int n ) { return ( x & ( x - 1 ) ); }
		public static UInt32 SetOffRightmostOn( this UInt32 x, int n ) { return ( x & ( x - 1 ) ); }
		public static Int64 SetOffRightmostOn( this Int64 x, int n ) { return ( x & ( x - 1 ) ); }
		public static UInt64 SetOffRightmostOn( this UInt64 x, int n ) { return ( x & ( x - 1 ) ); }
		public static Int16 SetOffRightmostOn( this Int16 x, int n ) { return (Int16)( x & ( x - 1 ) ); }
		public static UInt16 SetOffRightmostOn( this UInt16 x, int n ) { return (UInt16)( x & ( x - 1 ) ); }
		public static byte SetOffRightmostOn( this byte x, int n ) { return (byte)( x & ( x - 1 ) ); }

		public static Int32 SetOnRightmostOff( this Int32 x, int n ) { return ( x | ( x + 1 ) ); }
		public static UInt32 SetOnRightmostOff( this UInt32 x, int n ) { return ( x | ( x + 1 ) ); }
		public static Int64 SetOnRightmostOff( this Int64 x, int n ) { return ( x | ( x + 1 ) ); }
		public static UInt64 SetOnRightmostOff( this UInt64 x, int n ) { return ( x | ( x + 1 ) ); }
		public static Int16 SetOnRightmostOff( this Int16 x, int n ) { return (Int16)( x | ( x + 1 ) ); }
		public static UInt16 SetOnRightmostOff( this UInt16 x, int n ) { return (UInt16)( x | ( x + 1 ) ); }
		public static byte SetOnRightmostOff( this byte x, int n ) { return (byte)( x | ( x + 1 ) ); }

		public static Int32 Toggle( this Int32 x, int n ) { return ( x ^ ~( 1 << n ) ); }
		public static UInt32 Toggle( this UInt32 x, int n ) { return ( x ^ ~( 1u << n ) ); }
		public static Int64 Toggle( this Int64 x, int n ) { return ( x ^ ~( 1 << n ) ); }
		public static UInt64 Toggle( this UInt64 x, int n ) { return ( x ^ ~( 1u << n ) ); }
		public static Int16 Toggle( this Int16 x, int n ) { return (Int16)( x ^ ~( 1 << n ) ); }
		public static UInt16 Toggle( this UInt16 x, int n ) { return (UInt16)( x ^ ~( 1 << n ) ); }
		public static byte Toggle( this byte x, int n ) { return (byte)( x ^ ~( 1 << n ) ); }

		public static int FindRightmostOn( this Int32 x ) { return DeBruijn.FindFirstBit( (UInt32)( x & ( -x )) ); }
		public static int FindRightmostOn( this UInt32 x ) { return DeBruijn.FindFirstBit( (UInt32)(x & ( -x ) )); }
		public static int FindRightmostOn( this Int64 x ) { return DeBruijn.FindFirstBit( (UInt64)(x & ( -x ) )); }
		public static int FindRightmostOn( this UInt64 x ) { Int64 rx = (Int64)x; return DeBruijn.FindFirstBit( (UInt64)( rx & ( -rx ) )); }
		public static int FindRightmostOn( this Int16 x ) { return DeBruijn.FindFirstBit( (UInt32)(x & ( -x ) )); }
		public static int FindRightmostOn( this UInt16 x ) { return DeBruijn.FindFirstBit( (UInt32)(x & ( -x ) )); }
		public static int FindRightmostOn( this byte x ) { return DeBruijn.FindFirstBit( (UInt32)(x & ( -x ) )); }

		public static int FindRightmostOff( this Int32 x ) { return DeBruijn.FindFirstBit( (UInt32)(~x & ( x + 1 ) )); }
		public static int FindRightmostOff( this UInt32 x ) { return DeBruijn.FindFirstBit( ~x & ( x + 1 ) ); }
		public static int FindRightmostOff( this Int64 x ) { return DeBruijn.FindFirstBit( (UInt64)(~x & ( x + 1 )) ); }
		public static int FindRightmostOff( this UInt64 x ) { return DeBruijn.FindFirstBit( ~x & ( x + 1 ) ); }
		public static int FindRightmostOff( this Int16 x ) { return DeBruijn.FindFirstBit( (UInt32)( ~x & ( x + 1 ) )); }
		public static int FindRightmostOff( this UInt16 x ) { return DeBruijn.FindFirstBit( (UInt32)( ~x & ( x + 1 ) )); }
		public static int FindRightmostOff( this byte x ) { return DeBruijn.FindFirstBit( (UInt32)( ~x & ( x + 1 ) )); }

		public static Int32  FillOnAfterRightmostOn( this Int32 x ) { return x == 0 ? 0 : ( x | ( x - 1 ) ); }
		public static UInt32 FillOnAfterRightmostOn( this UInt32 x ) { return x == 0 ? 0 : ( x | ( x - 1 ) ); }
		public static Int64  FillOnAfterRightmostOn( this Int64 x ) { return x == 0 ? 0 : ( x | ( x - 1 ) ); }
		public static UInt64 FillOnAfterRightmostOn( this UInt64 x ) { return x == 0 ? 0 : ( x | ( x - 1 ) ); }
		public static Int16  FillOnAfterRightmostOn( this Int16 x ) { return (Int16)( x == 0 ? 0 : ( x | ( x - 1 ) ) ); }
		public static UInt16 FillOnAfterRightmostOn( this UInt16 x ) { return (UInt16)( x == 0 ? 0 : ( x | ( x - 1 ) ) ); }
		public static byte   FillOnAfterRightmostOn( this byte x ) { return (byte)( x == 0 ? 0 : ( x | ( x - 1 ) ) ); }

		public static bool IsPowerOf2( this Int32 x )  { return ( x & ( -x ) ) == x; }
		public static bool IsPowerOf2( this UInt32 x ) { return ( x & ( -x ) ) == x; }
		public static bool IsPowerOf2( this Int64 x )  { return ( x & ( -x ) ) == x; }
		public static bool IsPowerOf2( this UInt64 x ) { Int64 rx = (Int64)x; return ( rx & ( -rx ) ) == rx; }
		public static bool IsPowerOf2( this Int16 x )  { return ( x & ( -x ) ) == x; }
		public static bool IsPowerOf2( this UInt16 x ) { return ( x & ( -x ) ) == x; }
		public static bool IsPowerOf2( this byte x )   { return ( x & ( -x ) ) == x; }

		public static int BitPos( this Int32 x )  { return DeBruijn.FindFirstBit( (UInt32)x ); }
		public static int BitPos( this UInt32 x ) { return DeBruijn.FindFirstBit( x ); }
		public static int BitPos( this Int64 x )  { return DeBruijn.FindFirstBit( (UInt64)x ); }
		public static int BitPos( this UInt64 x ) { return DeBruijn.FindFirstBit( x ); }
		public static int BitPos( this Int16 x )  { return DeBruijn.FindFirstBit( (UInt32)x ); }
		public static int BitPos( this UInt16 x ) { return DeBruijn.FindFirstBit( x ); }
		public static int BitPos( this byte x )   { return DeBruijn.FindFirstBit( x ); }

		public static string BitString( this Int32 x ) {
			char[] res = new char[32];
			for( int i = 0; i < 32; i++ )
				res[i] = ( x & ( 1 << i ) ) != 0 ? '1' : '0';
			return new string( res );
		}
		public static string BitString( this UInt32 x ) {
			char[] res = new char[32];
			for( int i = 0; i < 32; i++ )
				res[i] = ( x & ( 1 << i ) ) != 0 ? '1' : '0';
			return new string( res );
		}
		public static string BitString( this Int64 x ) {
			char[] res = new char[64];
			for( int i = 0; i < 64; i++ )
				res[i] = ( x & ( 1 << i ) ) != 0 ? '1' : '0';
			return new string( res );
		}
		public static string BitString( this UInt64 x ) {
			char[] res = new char[64];
			for( int i = 0; i < 64; i++ )
				res[i] = ( x & ( 1u << i ) ) != 0 ? '1' : '0';
			return new string( res );
		}
		public static string BitString( this Int16 x ) {
			char[] res = new char[16];
			for( int i = 0; i < 16; i++ )
				res[i] = ( x & ( 1 << i ) ) != 0 ? '1' : '0';
			return new string( res );
		}
		public static string BitString( this UInt16 x ) {
			char[] res = new char[16];
			for( int i = 0; i < 16; i++ )
				res[i] = ( x & ( 1 << i ) ) != 0 ? '1' : '0';
			return new string( res );
		}
		public static string BitString( this byte x ) {
			char[] res = new char[8];
			for( int i = 0; i < 8; i++ )
				res[i] = ( x & ( 1 << i ) ) != 0 ? '1' : '0';
			return new string( res );
		}

	}
}
