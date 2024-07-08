using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MDTools.Extension;
using MDTools.Math;


/*	Code Copyright Michael Dannov 2012-2013
 * 
 *	The Classes in this file are created, owned and copyrighted by Michael Dannov. 
 *	If you possess this file or it is part of your software library resources, you may
 *	need to verify with the author if you have been granted authorization and license to use. 
 */

/// Version 1.3 - 09/18/13 - Performance improvements to Match: Added cache to Matches already run; now only LazyLoad until first fail
/// Version 1.2 - 07/30/13 - Minor performance improvements to multi-Property resets; optimized Pass/Fail Tests; bits in separate class
/// Version 1.1 - 11/21/12 - Performance improvement to use enum index instead of dictionary for NAMEKEY_TYPE & DeBruijn LazyLoad/Debug
/// Version 1.0 - 11/18/12 - High-performance Decision Table for testing multiple dimensions based on a single name


namespace MDTools.Data {

	/// <summary>
	///	public enum DateStatusEnums { None, IsDatePast };
	///	public enum MenuEnums { None, Del };
	///	public static DecisionTableInit<DateStatusHelper, DateStatusEnums, MenuEnums> DSTester;
	///	static Test() {
	///		// Initialize and bind static DSTester to DateStatusHelper
	///		DSTester = new DecisionTableInit<DateStatusHelper, DateStatusEnums, MenuEnums>();
	///		// Map the call that tests the condition
	///		DSTester.BindPropToTest( DateStatusEnums.IsDatePast, ds => ds.IsDatePast );
	///		// Set the object name you wish to map to an enumerator and the condition (true/false) required to match
	///		DSTester.MapNameReqs( MenuEnums.Del, false, DateStatusEnums.IsDatePast );
	///	}
	///	public void testc() {
	///		var ds = new DateStatusHelper( null );	// init internal object
	///		var test = DSTester.Get( ds );	// get the TestKit
	///		// Use result
	///		DelBtn.Visible=test.Match( UICode.Del );	// Tests if named item's conditions required are met
	///	}
	/// </summary>
	/// <typeparam name="INTERNAL_TYPE">internal object that comparisons will run against</typeparam>
	/// <typeparam name="PROPERTIES_ENUM">Enum must start from position 0 and work upwards to maximum position 63</typeparam>
	public class DecisionTable<INTERNAL_TYPE, PROPERTIES_ENUM, NAMEKEY_TYPE> where PROPERTIES_ENUM : struct, IConvertible /* enum */ {

		public INTERNAL_TYPE internalobject = default( INTERNAL_TYPE );
		public TestBool<INTERNAL_TYPE>[] testlist = null;		// Test delegate associated to each property
		public delegate List<BitsAndMask> getbits( NAMEKEY_TYPE name, out int pos, out int size );
		public getbits GetBitsList = null;	// Test delegate associated to each property

#if ver1
		public Dictionary<NAMEKEY_TYPE, List<TestBitsAndMask>> reqlist = null;	// List of multiple Bit requirements based on name
#endif
		protected ulong loadbits = 0;// bits set are used exact match
		protected ulong loadmask = 0;// bits not set are not used
		protected BitsAndMaskFlags[] matchflags = null;
		public ulong MaskedBits { get { return loadbits & loadmask; } }
		public int NamedMatches { get; private set; }

		public DecisionTable( INTERNAL_TYPE obj, bool fullinit = false ) {
			this.internalobject = obj;
			if( fullinit )
				InitializeFullLoad();
		}

		#region Bits Set & Get

		protected void SetBit( int pos, bool bset ) {
			ulong bval = 1ul << pos;
			if( bset )	// Set to 1/true
				loadbits |= bval;
			else			// Set to 0/false
				loadbits &= ~bval;
			loadmask |= bval;	// mark this entry as set
		}
		protected void SetBit( PROPERTIES_ENUM epos, bool bset ) {
			int pos = (int)( epos as object );
			ulong bval = 1ul << pos;
			if( bset )	// Set to 1/true
				loadbits |= bval;
			else		// Set to 0/false
				loadbits &= ~bval;
			loadmask |= bval;	// mark this entry as set
		}
		protected void SetBit( PropSet<PROPERTIES_ENUM> ps ) {
			SetBit( ps.prop, ps.set );
		}

		/// <summary>
		/// Get the bool of the bit at position; lazy load and set if required before returning result
		/// </summary>
		protected bool GetBit( int pos ) {
			ulong val = 1ul << pos;
			// Find out if lazy load required from required mask to populate any unresolved bits
			if( ( val & ~loadmask ) > 0 )
				LazyLoad( pos );
			return ( loadbits & val ) > 0;
		}
		protected bool GetBit( PROPERTIES_ENUM epos ) {
			return GetBit( (int)( epos as object ) );
		}

		#endregion

		#region Resolve Pre-templated Named Results

		protected bool Match( ulong reqbits, ulong reqmask ) {
			// Find out if lazy load required from required mask to populate any unresolved bits
			var lazymask = ( reqmask & ~loadmask ); // eg: 11001100 & ~01101010 = 10000100 (r=1, l=0, required but not)
			if( lazymask > 0 )
				if(!LazyLoad( lazymask, reqbits ) )
					return false;// One inside LazyLoad failed so we don't need to continue to load remaining
			// Now test that ones match from req mask & then zeros match from req mask
			return ( ( loadbits & reqmask ) == ( reqbits & reqmask ) );
		}
		public bool Match( NAMEKEY_TYPE name ) {//!! add cache for matches already calculated
			// Get match from presets
			int pos, size;
			var list = GetBitsList( name, out pos, out size );
			// Check if already evaluated
			BitsAndMaskFlags flags;
			if( matchflags == null ) {
				// Initialize cache if first time use
				matchflags = new BitsAndMaskFlags[size];
			} else {
				flags = matchflags[pos];
				if( ( flags & BitsAndMaskFlags.Evaluated ) == BitsAndMaskFlags.Evaluated ) {
					var res = ( flags == BitsAndMaskFlags.EvaluationMatched );
					if( res )
						NamedMatches++;
					return res;
				}
			}
			// Not already evaluated, so walk through OR list
			for( int i = 0, len = list.Count; i < len; i++ ) {
				var req = list[i];
				// Find out if lazy load required from required mask to populate any unresolved bits
				var lazymask = ( req.mask & ~loadmask ); // eg: 11001100 & ~01101010 = 10000100 (r=1, l=0, required but 0)
				if( lazymask > 0 )
					if(!LazyLoad( lazymask, req.bits ) )
						continue; // One inside LazyLoad failed so we don't need to continue to load remaining
				// Now test that ones match from req mask & then zeros match from req mask
				var res = ( ( loadbits & req.mask ) == ( req.bits & req.mask ) );
				if( res ) {// if any in OR list match, return true
					NamedMatches++;
					// Set matchflags cache and return result
					matchflags[pos] = BitsAndMaskFlags.EvaluationMatched;
					return true;
				}
			}
			// Set matchflags cache and return result
			matchflags[pos] = BitsAndMaskFlags.EvaluationUnmatched;
			return false;
		}
		public bool Match( NAMEKEY_TYPE name, Action action ) {
			bool res = Match( name );
			if( res && action != null )
				action();
			return res;
		}

		public void ResetNamedMatches() {
			NamedMatches = 0;
		}

		#endregion

		#region Test Results of Individual Properties or Test against new set of Property requirements

		/// <summary>
		/// Check single property passes test (ie. is true); lazy load if required - Same as GetBit
		/// returns true if passed
		/// </summary>
		public bool Pass( PROPERTIES_ENUM prop ) {
			return GetBit( prop );
		}
		/// <summary>
		/// Check single property and invoke action if it passes test (ie. is true); lazy load if required
		/// returns true if passed
		/// </summary>
		public bool Pass( PROPERTIES_ENUM prop, Action action ) {
			if( GetBit( prop ) ) {
				action();
				return true;
			}
			return false;
		}
		/// <summary>
		/// Check single property fails test (ie. is false); lazy load if required - Same as GetBit
		/// returns true if it failed
		/// </summary>
		public bool Fail( PROPERTIES_ENUM prop ) {
			return !GetBit( prop );
		}
		/// <summary>
		/// Check single property and invoke action if it fails test (ie. is false); lazy load if required
		/// returns true if failed
		/// </summary>
		public bool Fail( PROPERTIES_ENUM prop, Action action ) {
			if( !GetBit( prop ) ) {
				action();
				return true;
			}
			return false;
		}
		/// <summary>
		/// Check single property and invoke action based on whether result passes or fails; lazy load if required
		/// returns true if action invoked
		/// </summary>
		public bool Test( PROPERTIES_ENUM prop, Action passAction, Action failAction ) {
			bool res = GetBit( prop );
			if( res && passAction != null ) {
				passAction();
				return true;
			} else
				if( !res && failAction != null ) {
					failAction();
					return true;
				}
			return false;
		}

		/// <summary>
		/// Check multiple properties pass test (ie. is true); lazy load if required
		/// returns true if passed
		/// </summary>
		public bool Pass( params PROPERTIES_ENUM[] props ) {
			ulong bits = 0;
			ulong mask = 0;
			// Set bits and mask based on passed in property enums
			DecisionTableInit<INTERNAL_TYPE, PROPERTIES_ENUM, NAMEKEY_TYPE>.SetBits( true, ref bits, ref mask, props );
			return Match( bits, mask );
		}
		/// <summary>
		/// Check multiple properties pass test (ie. is true); lazy load if required
		/// returns true if failed
		/// </summary>
		public bool Fail( params PROPERTIES_ENUM[] props ) {
			ulong bits = 0;
			ulong mask = 0;
			// Set bits and mask based on passed in property enums
			DecisionTableInit<INTERNAL_TYPE, PROPERTIES_ENUM, NAMEKEY_TYPE>.SetBits( true, ref mask, props );
			return Match( bits, mask );
		}
		/// <summary>
		/// Check multiple properties and invoke action if all pass test (ie. is true); lazy load if required
		/// returns true if passed
		/// </summary>
		public bool Pass( Action action, params PROPERTIES_ENUM[] props ) {
			bool res = Pass( props );
			if( res )
				action();
			return res;
		}
		/// <summary>
		/// Check multiple properties and invoke action if all fail test (ie. is false); lazy load if required
		/// returns true if failed
		/// </summary>
		public bool Fail( Action action, params PROPERTIES_ENUM[] props ) {
			bool res = Fail( props );
			if( res && action != null )
				action();
			return res;
		}
		/// <summary>
		/// Check multiple properties and invoke action based on whether all pass or all fail or neither; lazy load if required
		/// returns true if pass or fail action called
		/// </summary>
		public bool Test( Action passAction, Action failAction, Action neitherAction, params PROPERTIES_ENUM[] props ) {
			bool res = Pass( props );
			if( res && passAction != null ) {
				passAction();
				return true;
			}
			if(!res) {
				res = Fail( props );
				if( res && failAction != null ) {
					failAction();
					return true;
				} 
			}
			if( !res && neitherAction != null ) 
				neitherAction();
			return false;
		}


		/// <summary>
		/// Check multiple properties pass test (ie. each property matches bool passed); lazy load if required
		/// Pass parameters as Property, bool, Property, bool or Property, Property pattern. Bools are optional (true if not present)
		///   Example: Test( PropertyEnum.Item0, false, PropertyEnum.Item1, true );
		///   Example: Test( PropertyEnum.Item0, PropertyEnum.Item1 );// true assumed for each property
		/// returns true if passed
		/// </summary>
		public bool Test( params object[] proporset ) {
			ulong bits = 0;
			ulong mask = 0;
			// Set bits and mask based on passed in property enums
			DecisionTableInit<INTERNAL_TYPE, PROPERTIES_ENUM, NAMEKEY_TYPE>.SetBits( ref bits, ref mask, proporset );
			return Match( bits, mask );
		}
		/// <summary>
		/// Check multiple properties and invokes action if passes test (ie. each property matches bool passed); lazy load if required
		/// Pass parameters as Property, bool, Property, bool or Property, Property pattern. Bools are optional (true if not present)
		///   Example: Test( PropertyEnum.Item0, false, PropertyEnum.Item1, true );
		///   Example: Test( PropertyEnum.Item0, PropertyEnum.Item1 );// true assumed for each property
		/// returns true if passed
		/// </summary>
		public bool Test( Action action, params object[] proporset ) {
			bool res = Test( proporset );
			if( res && action != null )
				action();
			return res;
		}


		/// <summary>
		/// Check single property passes test (ie. matches PropSet.set); lazy load if required
		/// </summary>
		public bool Test( params PropSet<PROPERTIES_ENUM>[] ps ) {
			// Set bits and mask based on passed in property enums
			var bm = DecisionTableInit<INTERNAL_TYPE, PROPERTIES_ENUM, NAMEKEY_TYPE>.SetBits( ps );
			return Match( bm.bits, bm.mask );
		}
		/// <summary>
		/// Check multiple properties pass test (ie. is true); lazy load if required
		/// Check multiple properties pass all tests (ie. matches PropSet.set for each item); lazy load if required
		/// </summary>
		public bool Test( Action action, params PropSet<PROPERTIES_ENUM>[] ps ) {
			bool res = Test( ps );
			if( res && action != null )
				action();
			return res;
		}

		#endregion

		#region Loader Based on Bits

		/// <summary>
		/// Initializes for full load
		/// </summary>
		/// <param name="obj"></param>
		public void InitializeFullLoad() {
			// Fill in binary values by calling each Test delegate
			for( int i = 0, len = testlist.Length; i < len; i++ ) {
				var test = testlist[i];
				if( test != null )
					SetBit( i, test.Invoke( internalobject ) );
			}
		}
		/// <summary>
		/// Set bit at pos based on result from Load/Invoke
		/// </summary>
		protected void LazyLoad( int pos ) {
			SetBit( pos, testlist[pos].Invoke( internalobject ) );
		}
		/// <summary>
		/// Set all bits required by loadbits based from Load/Invoke
		/// </summary>
		protected void LazyLoad( ulong loadbits ) {
			// Use DeBruijn to lookup position of bits
			ulong ubits = loadbits;
			while( ubits > 0 ) {
				int pos = DeBruijn.FindFirstBit( ubits );
				SetBit( pos, testlist[pos].Invoke( internalobject ) );
				ubits &= ~( 1u << pos );// remove bit at found position
			}
#if naivemethod
			// Fill in binary values by calling each Test delegate
			for( int pos = 0; loadbits > 0; pos++ ) {
				// Check if this bit needs load
				if( ( loadbits & 1 ) > 0 )
					SetBit( pos, testlist[pos].Invoke( internalobject ) );
				loadbits >>= 1;
			}
#endif
		}
		/// <summary>
		/// Set all bits required by loadbits based from Load/Invoke until one fails condition
		/// Returns true if all passed, returns false on first fail; if false, Match failed and no other loads required
		/// </summary>
		protected bool LazyLoad( ulong loadbits, ulong reqbits ) {
			// Use DeBruijn to lookup position of bits
			ulong ubits = loadbits;
			while( ubits > 0 ) {
				int pos = DeBruijn.FindFirstBit( ubits );
				bool res = testlist[pos].Invoke( internalobject );
				SetBit( pos, res );
				ubits &= ~( 1u << pos );// remove bit at found position
				if( ( ( reqbits & ( 1u << pos ) ) != 0 ) != res )
					return false; // first fail occured, no longer need to load any remaining loadbits
			}
			return true; // All loadbits were loaded
		}
		public void ForceReload() {
			loadmask = 0;// resets all properties to reload
		}
		public void ForceReload( ulong dirtybits ) {
			// Make Dirty: Set mask flags to zero -- to force reload
			loadmask &= ~dirtybits;
		}
		public void ForceReload( params PROPERTIES_ENUM[] props ) {
			// Make Dirty: Set mask flags to zero -- to force reload
			DecisionTableInit<INTERNAL_TYPE, PROPERTIES_ENUM, NAMEKEY_TYPE>.SetBits( false, ref loadmask, props );
		}

		#endregion

		#region Debug Stuff

#if ver1
		public string Debug() {
			string res = string.Empty;
			foreach( var req in reqlist )
				res += Debug( req.Key );
			return res;
		}
#endif

		public string Debug() {
			return Debug( loadbits & loadmask );
		}
		public string Debug( ulong bits ) {
			string res = string.Empty;
			while( bits > 0 ) {
				int pos = DeBruijn.FindFirstBit( bits );
				ulong uval = 1ul << pos;
				var mat = string.Empty;
				// Check if this bit is required load
				res += ( (PROPERTIES_ENUM)( pos as object ) ).ToString();
				bits &= ~( uval );// remove bit at found position
				res += ' ';
			}
			return res;
		}
		public string Debug( NAMEKEY_TYPE name ) {
			// Get match from presets
			string res = string.Empty;
			var savecnt = this.NamedMatches;

			int bpos, bsize;
			var list = GetBitsList( name, out bpos, out bsize );
			// Walk through OR list
			for( int i = 0, len = list.Count; i < len; i++ ) {
				var req = list[i];
				res += name + ":";
				res += " ";
				// Indicate requirements
				ulong abits = loadbits;
				ulong amask = loadmask;
				ulong rbits = req.bits;
				ulong rmask = req.mask;
				bool match = true;

				while( rmask > 0 ) {
					int pos = DeBruijn.FindFirstBit( rmask );
					ulong uval = 1ul << pos;
#if naivemethod
				for( int pos = 0; pos < DecisionTableInit<INTERNAL_TYPE, PROPERTIES_ENUM, NAMEKEY_TYPE>.PropSize; pos++ ) {
#endif
					var mat = string.Empty;
					// Check if this bit is required load
					if( ( rmask & uval ) > 0 ) {
						mat += 'R';
						mat += ( rbits & uval ) > 0 ? '1' : '0';
						if( ( ( amask & uval ) > 0 && ( abits & uval ) == ( rbits & uval ) ) )
							mat += '=';
						else {
							match = false;
							mat += '!';
						}
						mat += 'A';
						mat += ( abits & uval ) > 0 ? '1' : '0';
					}
					if( !string.IsNullOrEmpty( mat ) ) {
						res += ( (PROPERTIES_ENUM)( pos as object ) ).ToString();
						res += "(" + mat + ") ";
					}
#if naivemethod
					abits >>= 1;
					amask >>= 1;
					rbits >>= 1;
					rmask >>= 1;
#endif
					rmask &= ~( uval );// remove bit at found position
				}
				res += "- Match: " + match.ToString() + "\n\r";
			}
			NamedMatches = savecnt;
			return res;
		}

		#endregion

	}

	/// <summary>
	/// Used to set up template for binding internal objects to in advance using a static one-time initializer
	/// </summary>
	/// <typeparam name="INTERNAL_TYPE">internal object that comparisons will run against</typeparam>
	/// <typeparam name="PROPERTIES_ENUM">Enum must start from position 0 and work upwards to maximum position 63</typeparam>
	public class DecisionTableInit<INTERNAL_TYPE, PROPERTIES_ENUM, NAMEKEY_TYPE> where PROPERTIES_ENUM : struct, IConvertible /* enum */ {

		protected TestBool<INTERNAL_TYPE>[] testlist = null;
		protected Dictionary<NAMEKEY_TYPE, BitsAndMaskList> reqlist = null;	// lookup used if NAMEKEY_TYPE is enum (faster)
		protected List<BitsAndMask>[] reqarr = null;						// lookup used if NAMEKEY_TYPE is another type
		//!! convert this logic to an ArrayedDictionary class

		public static int PropSize = Enum.GetValues( typeof( PROPERTIES_ENUM ) ).Length;
		public static int NameSize = 0;

		public DecisionTableInit() {
			// Set size and check if enum is within size rules
			if( PropSize > 64 )
				throw new ArgumentException( "Enum cannot exceed 64 positions" );
			if( Enum.GetValues( typeof( PROPERTIES_ENUM ) ).Cast<int>().Last() > 63 )
				throw new ArgumentException( "Enum values cannot exceed 63" );
			if( Enum.GetValues( typeof( PROPERTIES_ENUM ) ).Cast<int>().First() < 0 )
				throw new ArgumentException( "Enum values must start from 0" );
			try {
				// If NAMEKEY_TYPE was enum, array is used (fastest)
				NameSize = Enum.GetValues( typeof( NAMEKEY_TYPE ) ).Cast<int>().Last() + 1;
				reqarr = new List<BitsAndMask>[NameSize];
			}
			catch {
				// Otherwise the dictionary is used for other types
				reqlist = new Dictionary<NAMEKEY_TYPE, BitsAndMaskList>();
			}
			testlist = new TestBool<INTERNAL_TYPE>[PropSize];
		}

		public DecisionTable<INTERNAL_TYPE, PROPERTIES_ENUM, NAMEKEY_TYPE> Get( INTERNAL_TYPE obj ) {
			// Create new kit
			var kit = new DecisionTable<INTERNAL_TYPE, PROPERTIES_ENUM, NAMEKEY_TYPE>( obj );
			// Copy its lists to the kit
			kit.testlist = this.testlist;
			kit.GetBitsList = this.GetBitsByName;
#if DEBUG
			// Test that testlist has a test assigned for every property through PropertiesBoundToTest
			for(int i=0, len=this.testlist.Length; i<len; i++ )
				if( this.testlist[i] == null )
					throw new ArgumentException( this.testlist[i].ToString() + " was not assigned a Test" );
#endif
#if ver1
			kit.reqlist = this.reqlist;
#endif
			return kit;
		}

		protected List<BitsAndMask> GetBitsByName( NAMEKEY_TYPE name, out int pos, out int size ) {
			// If NAMEKEY_TYPE was enum, array was used (fastest)
			if( reqarr != null ) {
				pos = (int)( name as object );
				size = reqarr.Length;
				return reqarr[pos];
			}
			// Otherwise the dictionary was used for other types
			var reqlist = this.reqlist[name];
			pos = reqlist.pos;
			size = reqlist.Count;
			return reqlist;
		}
		protected List<BitsAndMask> BindBitsToName( NAMEKEY_TYPE name ) {
			// If NAMEKEY_TYPE was enum, array was used (fastest)
			if( reqarr != null ) {
				List<BitsAndMask> list = null;
				list = reqarr[(int)( name as object )];
				if( list == null ) {
					// First-time init
					list = new List<BitsAndMask>();
					reqarr[(int)( name as object )] = list;
				}
				return list;
			}
			// Otherwise the dictionary was used for other types
			BitsAndMaskList bmlist = null;
			reqlist.TryGetValue( name, out bmlist );
			if( bmlist == null ) {
				// First-time init
				bmlist = new BitsAndMaskList( reqlist.Count );// incrementing counter
				reqlist.Add( name, bmlist );
			}
			// Otherwise the dictionary was used for other types
			return bmlist;
			// Reset evaluation
		}

		public void BindPropToTest( PROPERTIES_ENUM prop, TestBool<INTERNAL_TYPE> test ) {
			testlist[(int)( prop as object )] = test;
		}
		public void BindPropToTest( params PropCond<INTERNAL_TYPE, PROPERTIES_ENUM>[] pcs ) {
			for( int i = 0, len = pcs.Length; i < len; i++ ) {
				var pc = pcs[i];
				testlist[(int)( pc.prop as object )] = pc.test;
			}
		}
#if youcanevermakethiswork
		/// <summary>
		/// Bind Property, Test, Property, Test, ... 
		/// </summary>
		public void BindPropToTest( PROPERTIES_ENUM prop, TestBool<INTERNAL_TYPE> test, params object[] pcs ) {
			testlist[(int)( prop as object )] = test;
			for( int i = 1, len = pcs.Length; i < len; i += 2 ) {
				int iprop = (int)pcs[i];// PRPOERTIES_ENUM should cast to int
				var itest = (TestBool<INTERNAL_TYPE>)pcs[i + 1];
				testlist[iprop] = itest;
			}
		}
#endif
		/// <summary>
		/// Bind tests to Properties in order of Properties in the Enum - Make sure they match
		/// </summary>
		public void BindPropToTest( params TestBool<INTERNAL_TYPE>[] tests ) {
			if(tests.Length != PropSize )
				throw new ArgumentException( "The number of tests must match the property size. All properties must be filled if calling this method." );
			for( int i = 1; i < PropSize; i += 2 )
				testlist[i] = tests[i];
		}

		/// <summary>
		/// Set single bit and mask at Property position
		/// </summary>
		public static void SetBit( PROPERTIES_ENUM epos, bool bset, ref ulong bits, ref ulong mask ) {
			ulong bval = 1ul << (int)( epos as object );
			if( bset )
				// Set to 1/true
				bits |= bval;
			else
				// Set to 0/false
				bits &= ~bval;
			mask |= bval;	// mark this entry as set
		}
		/// <summary>
		/// Set single bits at Property position
		/// </summary>
		public static void SetBit( PROPERTIES_ENUM epos, bool bset, ref ulong bits ) {
			ulong bval = 1ul << (int)( epos as object );
			if( bset )
				// Set to 1/true
				bits |= bval;
			else
				// Set to 0/false
				bits &= ~bval;
		}
		/// <summary>
		/// Set multiple bits and mask by Properties passed
		/// </summary>
		public static void SetBits( bool bset, ref ulong bits, ref ulong mask, params PROPERTIES_ENUM[] props ) {
			// Set bits and mask based on passed in property enums
			for( int i = 0, len = props.Length; i < len; i++ )
				SetBit( props[i], bset, ref bits, ref mask );
		}
		/// <summary>
		/// Set multiple bits by Properties passed
		/// </summary>
		public static void SetBits( bool bset, ref ulong bits, params PROPERTIES_ENUM[] props ) {
			if( bset )
				for( int i = 0, len = props.Length; i < len; i++ )
					bits |= 1ul << (int)( props[i] as object );
			else
				for( int i = 0, len = props.Length; i < len; i++ )
					bits &= ~( 1ul << (int)( props[i] as object ) );
		}

		/// <summary>
		/// Construct ulong from Property, bool, Property, bool or Property, Property pattern. Bools are optional (true if not present)
		///   Example: SetBits( ref bits, ref mask, PropertyEnum.Item0, false, PropertyEnum.Item1, true );
		///   Example: SetBits( ref bits, ref mask, PropertyEnum.Item0, PropertyEnum.Item1 );// true assumed for each property
		/// </summary>
		public static void SetBits( ref ulong bits, ref ulong mask, params object[] proporset ) {
			// Set bits and mask based on passed in property enums
			for( int i = 0, len = proporset.Length; i < len; i++ ) {
				PROPERTIES_ENUM prop = (PROPERTIES_ENUM)proporset[i];
				bool b = true;
				// Check if next item is bool
				if( i + 1 < len )
					try {
						b = (bool)proporset[i + 1];
						i++; // only increments if b was a bool
					}
					catch { }
				SetBit( prop, b, ref bits, ref mask );
			}
		}
		public static BitsAndMask SetBits( params PropSet<PROPERTIES_ENUM>[] ps ) {
			var bm = new BitsAndMask();
			// Set bits and mask based on passed in property enums
			for( int i = 0, len = ps.Length; i < len; i++ ) {
				var p = ps[i];
				SetBit( p.prop, p.set, ref bm.bits, ref bm.mask );
			}
			return bm;
		}

		/// <summary>
		/// Bind Name to to property requirements identifying only the properties that are required true or false by property list and set to whenall
		/// Repeating the same name will add an additional qualifier condition to match (OR condition)
		/// DSTester.BindNameToPropReqs( "Add", true, Attributes.IsFruit, Attributes.IsBlack );
		/// </summary>
		public void BindNameToPropReqs( NAMEKEY_TYPE name, bool whenAll, params PROPERTIES_ENUM[] props ) {
			// Check if item already exists
			BitsAndMask bm = new BitsAndMask();
			// Set bits and mask based on passed in property enums
			for( int i = 0, len = props.Length; i < len; i++ )
				SetBit( props[i], whenAll, ref bm.bits, ref bm.mask );
			// No need to add name if no conditions set
			if( bm.mask > 0 )
				BindBitsToName( name ).Add( bm );
		}
		/// <summary>
		/// Bind Name to to property requirements identifying only the properties that are required true or false in property followed by bool pattern, true is assumed if bool does not follow each property
		/// Repeating the same name will add an additional qualifier condition to match (OR condition)
		/// DSTester.BindNameToPropReqs( "Add", Attributes.IsFruit, true, Attributes.IsBlack );
		///
		/// Pass parameters as Property, bool, Property, bool or Property, Property pattern. Bools are optional (true if not present)
		///   Example: BindNameToPropReqs( name, PropertyEnum.Item0, false, PropertyEnum.Item1, true );
		///   Example: BindNameToPropReqs( name, PropertyEnum.Item0, PropertyEnum.Item1 );// true assumed for each property
		/// </summary>
		public void BindNameToPropReqs( NAMEKEY_TYPE name, params object[] proporset ) {
			// Check if item already exists
			var bm = new BitsAndMask();
			// Set bits and mask based on passed in property enums
			SetBits( ref bm.bits, ref bm.mask, proporset );
			// No need to add name if no conditions set
			if( bm.mask > 0 )
				BindBitsToName( name ).Add( bm );
		}
		/// <summary>
		/// Bind Name to to property requirements identifying only the properties that are required true or false in PropSet array
		/// DSTester.BindNameToPropReqs( "Add", new PropSet<Attributes>(Attributes.IsFruit, true), new PropSet<Attributes>(Attributes.IsBlack, true) );
		/// </summary>
		public void BindNameToPropReqs( NAMEKEY_TYPE name, params PropSet<PROPERTIES_ENUM>[] ps ) {
			// Check if item already exists
			var bm = new BitsAndMask();
			// Set bits and mask based on passed in property enums
			for( int i = 0, len = ps.Length; i < len; i++ ) {
				var p = ps[i];
				bool b = true;
				// Check if next item is bool
				if( i + 1 < len )
					try {
						b = (bool)p.set;
						i++; // only increments if b was a bool
					}
					catch { }
				SetBit( p.prop, b, ref bm.bits, ref bm.mask );
			}
			// No need to add name if no conditions set
			if( bm.mask > 0 )
				BindBitsToName( name ).Add( bm );
		}
		protected void BindNameToPropReqs( NAMEKEY_TYPE name, ulong bits, ulong mask ) {
			// Do not add empty bit named references
			if( mask == 0 )
				return;
			// Set bits and mask based on passed in property enums
			var bm = new BitsAndMask();
			bm.bits = bits;
			bm.mask = mask;
			BindBitsToName( name ).Add( bm );
		}
		/// <summary>
		/// Bind Name to to property requirements using table with attributes on top and names along vertical
		/// DSTester.BindNameToPropReqs(
		///		new string[] {										 "Add", "Del" },
		///		new DecisionTableRow<Attributes>(Attributes.IsPast,  " x      1" ),
		///		new DecisionTableRow<Attributes>(Attributes.IsFruit, " 1      0" ),
		///		new DecisionTableRow<Attributes>(Attributes.IsBlack, " 1      x" ),
		///		
		/// If using LazyLoad, it's best to order most expensive tests towards the end or the most likely to fail operations towards the beginning
		/// </summary>
		/// <param name="namecol">Names on the horizontal</param>
		/// <param name="proprow">Properties in vertical</param>
		public void BindNameToPropReqs( NAMEKEY_TYPE[] namecol, params DecisionTableRow<PROPERTIES_ENUM>[] proprow ) {
			int collen = namecol.Length;
			int rowlen = proprow.Length;

			for( int row = 0; row < rowlen; row++ ) {
				// Validate items in string match number of columns
				string fix = proprow[row].setval.RemoveAllBut( "01xX?" );// Remove all values but legit chars from strings
				if( fix.Length != collen )
					throw new ArgumentException( proprow[row].Ref.ToString() + " does not have enough 0,1 or X characters to match name column" );
				proprow[row].setval = fix;
			}

			for( int col = 0; col < collen; col++ ) {
				// Associate props for each nametype by column based on string vals
				ulong bits = 0;
				ulong mask = 0;
				for( int row = 0; row < rowlen; row++ ) {
					switch( proprow[row].setval[col] ) {
					case '0':
						// off
						SetBit( proprow[row].Ref, false, ref bits, ref mask );
						break;
					case '1':
						// on
						SetBit( proprow[row].Ref, true, ref bits, ref mask );
						break;
					}
				}
				BindNameToPropReqs( namecol[col], bits, mask );
			}
			//!! Add diagnostics: noneset, competes with another
		}
		/// <summary>
		///!! untested
		/// Bind Name to to property requirements using table with attributes on top and names along vertical
		/// DSTester.BindNameToPropReqs(
		///		new Attributes[] {					Attributes.IsPast, Attributes.IsFruit, Attributes.IsBlack },
		///		new DecisionTableRow<string>("Add", "          x                  1                   1" ),
		///		new DecisionTableRow<string>("Del", "          1                  0                   x" ),
		/// </summary>
		/// <param name="propcol">Properties on the horizontal</param>
		/// <param name="namerow">Names in vertical</param>
		public void BindNameToPropReqs( PROPERTIES_ENUM[] propcol, params DecisionTableRow<NAMEKEY_TYPE>[] namerow ) {
			int collen = propcol.Length;
			int rowlen = namerow.Length;
			for( int row = 0; row < rowlen; row++ ) {
				ulong bits = 0;
				ulong mask = 0;
				for( int col = 0; col < collen; col++ ) {
					// Validate items in string match number of columns
					string fix = namerow[col].setval.RemoveAllBut( "01xX?" );// Remove all values but legit chars from strings
					if( fix.Length != collen )
						throw new ArgumentException( namerow[col].Ref.ToString() + " does not have enough 0,1 or X characters to match name column" );
					namerow[col].setval = fix;
					// Associate props for each nametype by row based on string vals
					var prop = propcol[col];
					switch( fix[col] ) {//namerow[row].setval[col] ) {
					case '0':
						// off
						SetBit( prop, false, ref bits, ref mask );
						break;
					case '1':
						// on
						SetBit( prop, true, ref bits, ref mask );
						break;
					}
				}
				BindNameToPropReqs( namerow[row].Ref, bits, mask );
			}
		}


		public class BitsAndMaskList : List<BitsAndMask> {
			public int pos;
			public BitsAndMaskList( int pos ) : base() { this.pos = pos;  }
			public BitsAndMaskList( int pos, int size ) : base( size ) { this.pos = pos; }
		}

	}

	[Flags]
	public enum BitsAndMaskFlags {
		NotEvaluated = 0x00,
		Evaluated = 0x02,
		EvaluationMatched = 0x01 | Evaluated,
		EvaluationUnmatched = 0x00 | Evaluated,
	};

	#region Additional helper structures

	public delegate bool TestBool<INTERNAL_TYPE>( INTERNAL_TYPE intobj );

	public struct PropSet<PROPERTIES_ENUM> {
		public PROPERTIES_ENUM prop;
		public bool set;
		public PropSet( PROPERTIES_ENUM p, bool b ) { prop = p; set = b; }
	};
	public struct DecisionTableRow<ROWREF_TYPE> { // ROWREF_TYPE is usually PROPERTIES_ENUM or NAMEKEY_TYPE
		public ROWREF_TYPE Ref;
		public string setval;	// valid characters are 0 for off, 1 for on, x for inactive
		public DecisionTableRow( ROWREF_TYPE r, string val ) { Ref = r; setval = val; }
	};
	public struct PropCond<INTERNAL_TYPE, PROPERTIES_ENUM> {
		public PROPERTIES_ENUM prop;
		public TestBool<INTERNAL_TYPE> test;
		public PropCond( PROPERTIES_ENUM p, TestBool<INTERNAL_TYPE> t ) { prop = p; test = t; }
	}

	#endregion

}
