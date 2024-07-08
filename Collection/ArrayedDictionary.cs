using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDTools.Collection {

	public class ArrayedDictionary<TKey, TValue> {/* IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IDictionary, ICollection, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, ISerializable, IDeserializationCallback*/

		protected Dictionary<TKey, TValueWithPos> reqlist = null;	// lookup used if TKey is enum (faster)
		protected TValue[] reqarr = null;							// lookup used if TKey is another type
		
		protected int size = 0;

		protected struct TValueWithPos {
			public int pos;
			public TValue Value;
			public TValueWithPos( int pos, TValue value ) { this.pos = pos; this.Value = value;  }
		}

		ArrayedDictionary() {
			Clear();
		}

		public int Pos(TKey key) {
			if( reqarr != null )
				return (int)( key as object );
			return reqlist[key].pos;
		}
		public int CurrentSize {
			get {
				if( reqarr != null )
					return reqarr.Length;
				return reqlist.Count;
			}
		}

		#region IDictionary<TKey,TValue> Members

		public void Add( TKey key, TValue value ) {
			if( reqarr != null )
				reqarr[(int)( key as object )] = value;
			else
				reqlist.Add( key, new TValueWithPos( reqlist.Count, value ) );
		}

		public ICollection<TValue> Values {
			get { throw new NotImplementedException(); }
		}

		public TValue this[TKey key] {
			get {
				if( reqarr != null )
					return reqarr[(int)( key as object )];
				return reqlist[key].Value;
			}
			set {
				if( reqarr != null )
					reqarr[(int)( key as object )] = value;
				else {
					TValueWithPos item;
					if( reqlist.TryGetValue(key, out item ) )
						item.Value = value;
					else
						reqlist.Add( key, new TValueWithPos( reqlist.Count, value ) );
				}

			}
		}

		public bool ContainsKey( TKey key ) {
			if( reqlist != null )
				return reqlist.ContainsKey( key );
			throw new NotSupportedException();
		}

		public ICollection<TKey> Keys {
			get {
				if( reqlist != null )
					return reqlist.Keys;
				throw new NotSupportedException();
			}
		}

		public bool Remove( TKey key ) {
			if( reqlist != null )
				return reqlist.ContainsKey( key );
			throw new NotSupportedException();
		}

		#endregion

		#region ICollection<KeyValuePair<TKey,TValue>> Members

		public void Add( KeyValuePair<TKey, TValue> item ) {
			Add( item.Key, item.Value );
		}

		public void Clear() {
			try {
				// If TKey was enum, array is used (fastest)
				size = Enum.GetValues( typeof( TKey ) ).Cast<int>().Last() + 1;
				reqarr = new TValue[size];
			}
			catch {
				// Otherwise the dictionary is used for other types
				reqlist = new Dictionary<TKey, TValueWithPos>();
			}
		}

		public bool Contains( KeyValuePair<TKey, TValue> item ) {
			if( reqarr != null )
				return reqarr[(int)( item.Key as object )].Equals( item.Value );
			return reqlist[ item.Key ].Equals( item.Value );
		}

		public void CopyTo( KeyValuePair<TKey, TValue>[] array, int arrayIndex ) {
			throw new NotImplementedException();
		}

		public int Count {
			get {
				if( reqlist != null )
					return reqlist.Count;
				throw new NotImplementedException();
			}
		}

		public bool IsReadOnly {
			get { return false; }
		}

		public bool Remove( KeyValuePair<TKey, TValue> item ) {
			throw new NotImplementedException();
		}

		#endregion

		#region IEnumerable<KeyValuePair<TKey,TValue>> Members

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
			throw new NotSupportedException();
		}

		#endregion

		#region IEnumerable Members
#if how
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			if( reqarr != null )
				return reqarr.GetEnumerator();
			return reqlist.GetEnumerator();
		}
#endif
		#endregion
	}
}
