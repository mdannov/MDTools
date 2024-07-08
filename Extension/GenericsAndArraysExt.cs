using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace MDTools.Extension {

	#region Generic Extensions
	
	public static class GenericExt {

		public static List<TYPE> Subset<TYPE>( this IList<TYPE> thisList, int start, int count ) {
			int len = thisList.Count -start;
			if( len <= 0 ) return null;// if start beyond length
			len = count > len ? len : count;
			var res = new List<TYPE>(len);
			len += start;// move to position
			for( int i = start; i < len; i++ )
				res.Add(thisList[i]);
			return res;
		}
		public static List<TYPE> Subset<TYPE>( this IList<TYPE> thisList, int start=0 ) {
			int len = thisList.Count - start;
			if( len <= 0 ) return null;// if start beyond length
			var res = new List<TYPE>( len );
			len += start;// move to position
			for( int i = start; i < len; i++ )
				res.Add( thisList[i] );
			return res;
		}
		public static void AddRange<TYPE>( this IList<TYPE> thisList, TYPE[] inlist, int instart, int count ) {
			if( instart > inlist.Length ) return;
			for( int i = instart, len = instart + count; i < len; i++ ) {//!! performance test against subset TYPE[] then AddRange
				thisList.Add( inlist[i] );
			}
		}
		public static void AddRange<TYPE>( this IList<TYPE> thisList, TYPE[] list, int instart ) {
			if( instart > list.Length ) return;
			for( int i = instart, len = list.Length; i < len; i++ ) {
				thisList.Add( list[i] );
			}
		}
		public static TYPE[] ToArray<TYPE>( this IList<TYPE> thisList, int start, int count ) {
			int len = thisList.Count - start;
			if( len <= 0 ) return null;// if start beyond length
			len = count > len ? len : count;
			var res = new TYPE[len];
			for( int i = 0; i < len; i++ )
				res[i] = thisList[i + start];
			return res;
		}
		public static TYPE[] ToArray<TYPE>( this ICollection<TYPE> thisList, int start=0 ) {
			int len = thisList.Count - start;
			if( len <= 0 ) return null;// if start beyond length
			var res = new TYPE[len];
			thisList.CopyTo( res, start );
			return res;
		}
		[Obsolete( "ToArray() is equivalent" )]
		public static string[] ToStringArray( this IList<string> thisList, int start, int count ) {
			return thisList.ToArray<string>( start, count );
		}
		[Obsolete( "ToArray() is equivalent" )]
		public static string[] ToStringArray( this IList<string> thisList, int start=0) {
			return thisList.ToArray<string>( start);
		}
		[Obsolete( "ToArray() is equivalent" )]
		public static string[] ToStringArray( this List<string> thisList) {
			return thisList.ToArray();
		}
		public static string[] ToStringArray( this IList thisList, int start, int count ) {
			int len = thisList.Count - start;
			if( len <= 0 ) return null;// if start beyond length
			len = count > len ? len : count;
			var res = new string[len];
			for( int i = 0; i < len; i++ )
				res[i] = thisList[i + start].ToString();
			return res;
		}
		public static string[] ToStringArray( this IList thisList, int start=0 ) {
			int len = thisList.Count - start;
			if( len <= 0 ) return null;// if start beyond length
			var res = new string[ len ];
			len += start;// move to position
			for( int i = start; i < len; i++ )
				res[i] = thisList[i].ToString();
			return res;
		}
		public static string[] ToStringArray( this IList thisList) {
			int len = thisList.Count;
			if( len <= 0 ) return null;// if start beyond length
			var res = new string[len];
			for( int i = 0; i < len; i++ )
				res[i] = thisList[i].ToString();
			return res;
		}
		public static string Delimit( this List<string> thisList, char delim ) {
			return string.Join( delim.ToString(), thisList.ToArray() );// uses List.ToArray()
		}
		public static string Delimit( this List<string> thisList, string delim ) {
			return string.Join( delim, thisList.ToArray() );// uses List.ToArray()
		}
		public static string Delimit( this IList<string> thisList, char delim, int start = 0 ) {
			return string.Join( delim.ToString(), thisList.ToArray( start ) );
		}
		public static string Delimit( this IList<string> thisList, string delim, int start = 0 ) {
			return string.Join( delim, thisList.ToArray( start ) );
		}
		public static string Delimit( this IList<string> thisList, char delim, int start, int count ) {
			return string.Join( delim.ToString(), thisList.ToArray( start, count ) );
		}
		public static string Delimit( this IList<string> thisList, string delim, int start, int count ) {
			return string.Join( delim, thisList.ToArray( start, count ) );
		}
#if inconsistentToString
		public static string Delimit( this IList thisList, string delim, int start, int count ) {
			return string.Join( delim, thisList.ToStringArray(start, count) );
		}
		public static string Delimit( this IList thisList, string delim, int start ) {
			return string.Join( delim, thisList.ToStringArray( start ) );
		}
		public static string Delimit( this IList thisList, string delim ) {
			return string.Join( delim, thisList.ToStringArray() );
		}
#endif
		public static string Delimit<TYPE>( this IList<TYPE> source, char delim, Converter<TYPE, string> action ) {
			string res = string.Empty;
			for( int i = 0, len = source.Count; i < len; i++ ) {
				if( i > 0 )
					res += delim;
				res += action( source[i] );
			}
			return res;
		}
		public static string Delimit<TYPE>( this IList<TYPE> source, string delim, Converter<TYPE, string> action ) {
			//!! Test Performance if this is Fastest 
			string res = string.Empty;
			for( int i=0, len=source.Count; i<len; i++ ) {
				if( i>0 )
					res += delim;
				res += action( source[i] );
			}
			return res;
		}
		public static string Delimit<TYPE>( this IEnumerable<TYPE> source, char delim, Converter<TYPE, string> action ) {
			//!! Test Performance if this is Fastest 
			string res = string.Empty;
			bool bset = false;
			foreach( var item in source ) {
				if( bset )
					res += delim;
				res += action( item );
				bset = true;
			}
			return res;
		}
		public static string Delimit<TYPE>( this IEnumerable<TYPE> source, string delim, Converter<TYPE, string> action ) {
			//!! Test Performance if this is Fastest 
			string res = string.Empty;
			bool bset = false;
			foreach( var item in source) {
				if(bset)
					res += delim;
				res += action(item);
				bset = true;
			}
			return res;
		}/*
		public static string Delimit<TYPE>( this IEnumerable<TYPE> source, string delim, Func<TYPE, string> action ) {
			// guard clauses for arguments omitted for brevity
			return string.Join( delim, source.Select( action ).ToArray() );
		}*/

		public static void CopyTo<TYPE>( this IList<TYPE> to, IList<TYPE> from, int count ) {
			int start = 0;
			for( ; count > 0; count-- )
				to[start] = from[start];
		}
		public static void CopyTo<TYPE>( this IList<TYPE> to, IList<TYPE> from, int fromstart, int count ) {
			int tostart = 0;
			for( ; count > 0; count-- )
				to[tostart] = from[fromstart];
		}
		public static void CopyTo<TYPE>( this IList<TYPE> to, int start, IList<TYPE> from, int count ) {
			for( ; count > 0; count-- )
				to[start] = from[start];
		}
		public static void CopyTo<TYPE>( this IList<TYPE> to, int tostart, IList<TYPE> from, int fromstart, int count ) {
			for( ; count > 0; count-- )
				to[tostart++] = from[fromstart++];
		}

		public static bool Contains<TYPE>( this TYPE[] keys, string key ) {
			for( int i = 0, len=keys.Length; i < len; i++ )
				if( keys[i].Equals(key) )
					return true;
			return false;
		}
		public static int IndexOf<TYPE>( this TYPE[] keys, string key ) {
			for( int i = 0, len = keys.Length; i < len; i++ )
				if( keys[i].Equals( key ) )
					return i;
			return -1;
		}
		public static int IndexOf<TYPE>( this TYPE[] keys, TYPE key ) {
			for( int i = 0, len = keys.Length; i < len; i++ )
				if( keys[i].Equals( key ) )
					return i;
			return -1;
		}
		/// <summary>
		/// Similar to First with predicate, except faster because it uses index instead of foreach
		/// </summary>
		public static TYPE Find<TYPE>( this IList<TYPE> items, TYPE key, Func<TYPE> predicate ) {
			for( int i = 0, len = items.Count; i < len; i++ ) {
				var item = items[i];
				if( key.Equals( predicate.Invoke() ) )
					return item;
			}
			return default(TYPE);
		}

		/// <summary>
		/// Reposition Item in a IList
		/// </summary>
		/// <param name="list">IList</param>
		/// <param name="item">item in list to move</param>
		/// <param name="topos">position to move item in list to; 0 moves to front; -1 moves to-end</param>
		public static void Reposition<TYPE>( this IList<TYPE> list, TYPE item, int topos = -1 ) {
			list.Remove( item );
			if( topos > 0 )
				topos--;
			if( topos < 0 || topos >= list.Count )
				list.Add( item );
			else
				list.Insert( topos, item );
			return;
		}
		/// <summary>
		/// Reposition Item in a IList
		/// </summary>
		/// <param name="list">IList</param>
		/// <param name="frompos">position to move from</param>
		/// <param name="topos">position to move item in list to; 0 moves to front; -1 moves to-end</param>
		public static void Reposition<TYPE>( this IList<TYPE> list, int frompos, int topos = -1 ) {
			if( frompos == topos )
				return; // do nothing since position hasn't changed
			Reposition<TYPE>( list, list[frompos], topos );
		}
		/// <summary>
		/// Reposition Item in a IList to a position before another item
		/// </summary>
		/// <param name="list">IList</param>
		/// <param name="item">item in list to move</param>
		/// <param name="beforectl">item in list to position before</param>
		public static void RepositionBefore<TYPE>( this IList<TYPE> list, TYPE item, TYPE beforeItem ) {
			int pos = list.IndexOf( beforeItem );
			if( pos < 0 )
				return; // do nothing since item wasn't found
			Reposition( list, item, pos );
		}
		/// <summary>
		/// Reposition Item in a IList to a position before another item
		/// </summary>
		/// <param name="list">IList</param>
		/// <param name="frompos">position to move from</param>
		/// <param name="beforectl">Control to position before</param>
		public static void RepositionBefore<TYPE>( this IList<TYPE> list, int frompos, TYPE beforeItem ) {
			int pos = list.IndexOf( beforeItem );
			if( pos < 0 || frompos == pos )
				return; // do nothing since item wasn't found or doesn't need to move
			Reposition( list, list[frompos], pos );
		}
		/// <summary>
		/// Reposition Item in a IList to a position after another item
		/// </summary>
		/// <param name="list">IList</param>
		/// <param name="item">item in list to move</param>
		/// <param name="afterctl">item in list to position after</param>
		public static void RepositionAfter<TYPE>( this IList<TYPE> list, TYPE item, TYPE afterItem ) {
			int pos = list.IndexOf( afterItem );
			if( pos < 0 )
				return; // do nothing since item wasn't found
			Reposition( list, item, pos+1 );
		}
		/// <summary>
		/// Reposition Item in a IList to a position before another item
		/// </summary>
		/// <param name="list">IList</param>
		/// <param name="frompos">position to move from</param>
		/// <param name="beforectl">Control to position before</param>
		public static void RepositionAfter<TYPE>( this IList<TYPE> list, int frompos, TYPE afterItem ) {
			int pos = list.IndexOf( afterItem );
			if( pos < 0 || frompos == pos+1 )
				return; // do nothing since item wasn't found or doesn't need to move
			Reposition( list, list[frompos], pos+1 );
		}

		/// <summary>
		/// Convert Dictionary to NameValueCollection
		/// </summary>
		public static NameValueCollection ToNameValueCollection( this IDictionary<string, string> dict ) {
			var nameValueCollection = new NameValueCollection( dict.Count );
			foreach( var kvp in dict ) {
				nameValueCollection.Add( kvp.Key.ToString(), kvp.Value );
			}
			return nameValueCollection;
		}
		/// <summary>
		/// Convert Dictionary to NameValueCollection
		/// </summary>
		public static NameValueCollection ToNameValueCollection<TValue>( this IDictionary<string, TValue> dict ) {
			return ToNameValueCollection<string, TValue>( dict );
		}
		/// <summary>
		/// Convert Dictionary to NameValueCollection
		/// http://stackoverflow.com/questions/7230383/c-convert-dictionary-to-namevaluecollection
		/// </summary>
		public static NameValueCollection ToNameValueCollection<TKey, TValue>( this IDictionary<TKey, TValue> dict ) {
			var nameValueCollection = new NameValueCollection( dict.Count );
			foreach( var kvp in dict ) {
				string value = null;
				if( kvp.Value != null )
					value = kvp.Value.ToString();
				nameValueCollection.Add( kvp.Key.ToString(), value );
			}
			return nameValueCollection;
		}

	}

	#endregion


	#region Array Extensions

	public static class ArrayExt {

		public static TYPE[] Subset<TYPE>( this TYPE[] thisList, int start, int count ) {
			return thisList.ToArray<TYPE>(start, count);
/*			int len = thisList.Length-start;
			if( len <= 0 ) return null;// if start beyond length
			len = count > len ? len : count;
			var res = new TYPE[len];
			res.
			for( int i = 0; i < len; i++ )
				res[i] = thisList[i + start];
			return res;*/
		}
		public static TYPE[] Subset<TYPE>( this TYPE[] thisList, int start) {
			int len = thisList.Length - start;
			if( len <= 0 ) return null;// if start beyond length
			var res = new TYPE[len];
			thisList.CopyTo( res, start );
#if nocopyto
			for( int i = 0; i < len; i++ )
				res[i] = list[i + start];*/
#endif
			return res;
		}

		/// <summary>
		/// Append elements to an array one at a time or combine 2 arrays
		/// </summary>
		public static TYPE[] Expand<TYPE>( this TYPE[] thisList, params TYPE[] items ) {
			int len = thisList.Length;
			var res = new TYPE[len + items.Length];
			thisList.CopyTo(res, 0);
			items.CopyTo(res, len);
			return res;
		}

		public static string GetString( this byte[] val ) {
			System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
			return enc.GetString( val );
		}
		/// <summary>
		/// Similar to Join except null strings are not included, empties are
		/// </summary>
		public static string Delimit( this string[] source, char delim ) {
			//!! Test Performance if this is Fastest 
			string res = string.Empty;
			for( int i = 0, len = source.Length; i < len; i++ ) {
				var item = source[i];
				if( item == null )
					continue;
				if( i>0 ) res += delim; 
				res += item;
			}
			return res;
		}
		/// <summary>
		/// Similar to Join except null strings are not included, empties are
		/// </summary>
		public static string Delimit( this string[] source, string delim ) {
			//!! Test Performance if this is Fastest 
			string res = string.Empty;
			for( int i = 0, len = source.Length; i < len; i++ ) {
				var item = source[i];
				if( item == null )
					continue;
				if( i>0 ) res += delim; 
				res += item;
			}
			return res;
		}


	}

	#endregion

}
