using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDTools.Collection {

	/// <summary>
	/// Makes a class behave like a single item or a list
	/// </summary>
	public class ItemOrList<BASECLASS> : IList<BASECLASS> where BASECLASS:class {
		public ItemOrList() : base() { }

		protected List<BASECLASS> list = null;

		public int IndexOf( BASECLASS item ) {
			throw new NotImplementedException();
		}

		public void Insert( int index, BASECLASS item ) {
			throw new NotImplementedException();
		}

		public void RemoveAt( int index ) {
			throw new NotImplementedException();
		}

		public BASECLASS this[int index] {
			get {
				throw new NotImplementedException();
			}
			set {
				throw new NotImplementedException();
			}
		}

		public void Add( BASECLASS item ) {
			throw new NotImplementedException();
		}

		public void Clear() {
			throw new NotImplementedException();
		}

		public bool Contains( BASECLASS item ) {
			throw new NotImplementedException();
		}

		public void CopyTo( BASECLASS[] array, int arrayIndex ) {
			throw new NotImplementedException();
		}

		public int Count {
			get { throw new NotImplementedException(); }
		}

		public bool IsReadOnly {
			get { throw new NotImplementedException(); }
		}

		public bool Remove( BASECLASS item ) {
			throw new NotImplementedException();
		}

		public IEnumerator<BASECLASS> GetEnumerator() {
			throw new NotImplementedException();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			throw new NotImplementedException();
		}
	}

}
