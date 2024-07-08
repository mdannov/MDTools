using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDTools.Collection {

	/// <summary>
	/// Adds List with ability to Add/Insert if Unique - Use with light lists - Large Lists uses HashSet
	/// </summary>
	public class UniqueList<T> : List<T> {
		public UniqueList() : base() { }
		public UniqueList( IEnumerable<T> collection ) : base( collection ) {}
		public UniqueList( int capacity ) : base(capacity) {}

		/// <summary>
		/// Add a new item if item is unique based on specified condition
		/// </summary>
		public bool AddUnique( T item ) {
			if( !base.Contains( item ) ) {
				base.Add( item );
				return true;
			}
			return false;
		}

		public bool AddRangeUnique( IEnumerable<T> collection ) {
			bool res = false;
			foreach( var item in collection )
				res |= AddUnique( item );
			return res;
		}

		/// <summary>
		/// Insert a new item if item is unique based on specified condition
		/// </summary>
		public bool InsertUnique( int index, T item ) {
			if( !base.Contains( item ) ) {
				base.Insert( index, item );
				return true;
			}
			return false;
		}

		public bool InsertRangeUnique( int index, IEnumerable<T> collection ) {
			bool res = false;
			foreach( var item in collection )
				if( InsertUnique( index, item ) ) {
					res = true;
					index++;
				}
			return res;
		}

	}

#if completed
	public class HashList<T> : HashSet<T>, IList<T> {

		public HashList() : base() {}
		public HashList( IEnumerable<T> collection ) : base( collection ) { }
		public HashList( IEqualityComparer<T> comparer ) : base( comparer ) {}
		public HashList( IEnumerable<T> collection, IEqualityComparer<T> comparer ) : base( collection, comparer ) { }

		#region IList<T> Members

		public int IndexOf( T item ) {
			throw new NotImplementedException();
		}

		public void Insert( int index, T item ) {
			throw new NotImplementedException();
		}

		public void RemoveAt( int index ) {
			throw new NotImplementedException();
		}

		public T this[int index] {
			get {
				throw new NotImplementedException();
			}
			set {
				throw new NotImplementedException();
			}
		}

		#endregion
	}
#endif
}
