using System;

namespace MDTools.Extension {

	#region BoxStructures For Null Value

	// Consider instead using nullable types int? Though this type you can compare to null
	public class CDateTime  { public DateTime DateTime; }
	public class CTimeSpan  { public TimeSpan TimeSpan; }
	// Nullable<int> i;

	#endregion
	
	#region other struct extension classes

	/// <summary>
	/// Enumeration Extension Class - May not need this clasa after dotNet 4.0
	/// </summary>
	public static class EnumExt {
		/*
		public static byte ToByte( this System.Enum val ) { return (byte)(val as object); }
		public static void Set( this ENUM val, byte value ) { val = (ENUM)(value as object); }
		*/

		public static TYPE To<TYPE>( this System.Enum val ) { return (TYPE)(val as object); }
		public static Int16 ToInt16( this System.Enum val ) { return (Int16)( val as object ); }
		public static Int32 ToInt32( this System.Enum val ) { return (Int32)( val as object ); }
		public static Int64 ToInt64( this System.Enum val ) { return (Int64)( val as object ); }
		public static byte ToByte( this System.Enum val ) { return (byte)( val as object ); }
		public static UInt16 ToUInt16( this System.Enum val ) { return (UInt16)( val as object ); }
		public static UInt32 ToUInt32( this System.Enum val ) { return (UInt32)( val as object ); }
		public static UInt64 ToUInt64( this System.Enum val ) { return (UInt64)( val as object ); }
		public static sbyte ToSByte( this System.Enum val ) { return (sbyte)( val as object ); }

		public static bool IsSet<ENUM>( this ENUM value, ENUM flags ) where ENUM : IConvertible {
			UInt64 longFlags = Convert.ToUInt64( flags ); 
			return ( Convert.ToUInt64( value ) & longFlags ) == longFlags;
		}
		//!! needs to be tested with byte and other smaller types
		public static bool IsSet( this Enum value, Enum flags ) {
			UInt64 Flags = (UInt64)( flags as object );
			return ( (UInt64)( value as object ) & Flags ) == Flags;
		}

		public static void Set<TYPE>( this System.Enum val, ref System.Enum thisref, TYPE value ) { thisref = (System.Enum)( value as object ); }
		public static void Set<TYPE, ENUM>( this System.Enum val, ref ENUM thisref, TYPE value ) { thisref = (ENUM)( value as object ); }
/*
		public static System.Enum SetFlags<TYPE,ENUM>( this System.Enum val, System.Enum flags ) { val.To<TYPE> |= flags; }
		public static void UnsetFlags<ENUM>( this ENUM val, ENUM flags ) { val &= ~flags; }
		public static bool IsFlagged<ENUM>( this ENUM val, ENUM flags ) { return ( val & flags ) != 0; }
*/
		/// These were found: http://www.codeproject.com/KB/cs/ExtendEnum.aspx
		public static bool IsDefined( this System.Enum value ) { return System.Enum.IsDefined( value.GetType(), value ); }
	}

	/// <summary>
	/// Utility class to cache now globally and only check every resolution worth of seconds.
	/// </summary>
	public static class DateTimeFast {
		private static int _lastTickCount = Environment.TickCount;
		private static DateTime _now = DateTime.Now;
		private static int _resnow = 20000;	// every 20 seconds

		public static DateTime Now {
			get {
				int lastTickCount = _lastTickCount;
				_lastTickCount = Environment.TickCount;//!! should be faster than DateTime.Now & DateTime.Ticks
				int dif = _lastTickCount - lastTickCount;
				if( dif > _resnow || dif < 0 )	// <0 deals with rollover
					_now = DateTime.Now;
				return _now;
			}
		}

		public static DateTime Today {
			get {
				return Now.Date;
			}
		}
#if bugfixed
		private static DateTime _today = DateTime.Now.Date;
		private static int _ticksTilNewDay = _lastTickCount;

		public static DateTime Today {
			get {
				_lastTickCount = Environment.TickCount;//!! should be faster than DateTime.Now & DateTime.Ticks
				if( _lastTickCount > _ticksTilNewDay  )	// !! BUG: need to handle rollover
					_now = DateTime.Now;
				return _now;
			}
		}
		public static int nextDayTicks() {
			return (_ticksTilNewDay = (_now.Date.AddDays( 1 ) - _now).Milliseconds);
		}
#endif

	}
	#endregion
}
