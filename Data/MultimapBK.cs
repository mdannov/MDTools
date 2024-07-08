using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;



/// Multi-map Generic Collection Class in C# - A Dictionary Collection Class that can Store Duplicate Key-value Pairs
/// Original Source for this component originated from here:
///     http://www.codeproject.com/KB/cs/MultiKeyDictionary.aspx
/// Changes: Updates by Michael Dannov
///     
/// Extend: Add IDictionary<Key, Value> 
namespace MDTools.Data {
	/// <summary>
	/// A MultiMap generic collection class that can store more than one value for a Key.
	/// </summary>
	public class MultimapBK<Key, Value> : IDisposable where Key : IComparable {
		private Dictionary<Key, List<Value>> dictMultiMap;
		private bool dispose;
		private object lockThisToDispose = new object();
		private int CurrentItemIndex;
		private Key CurrentKey;

		/// <summary>
		/// Construction of Multi map
		/// </summary>
		public MultimapBK() {
			CurrentKey = default( Key );
			CurrentItemIndex = 0;
			dispose = false;
			dictMultiMap = new Dictionary<Key, List<Value>>();
		}

		/// <summary>
		/// Construction copying from another Multi map
		/// </summary>
		public MultimapBK( ref MultimapBK<Key, Value> DictToCopy ) {
			CurrentKey = default( Key );
			CurrentItemIndex = 0;
			dispose = false;
			dictMultiMap = new Dictionary<Key, List<Value>>();

			if( DictToCopy != null ) {
				Dictionary<Key, List<Value>>.Enumerator enumCopy = DictToCopy.dictMultiMap.GetEnumerator();

				while( enumCopy.MoveNext() ) {
					List<Value> listValue = new List<Value>();
					List<Value>.Enumerator enumList = enumCopy.Current.Value.GetEnumerator();

					while( enumList.MoveNext() ) {
						listValue.Add( enumList.Current );
					}

					dictMultiMap.Add( enumCopy.Current.Key, listValue );
				}
			}
		}

		/// <summary>
		/// Adds an element to the Multi map.
		/// </summary>
		public void Add( Key KeyElement, Value ValueElement ) {
			List<Value> listToAdd = null;
			if( dictMultiMap.TryGetValue( KeyElement, out listToAdd ) ) {
				listToAdd.Add( ValueElement );
			} else {
				listToAdd = new List<Value>();
				listToAdd.Add( ValueElement );
				dictMultiMap.Add( KeyElement, listToAdd );
			}
		}

		/// <summary>
		/// Gets the first Item in the Multi map.
		/// </summary>
		/// <param name="ItemToFind"></param>
		/// <returns></returns>
		public Value GetFirstItem( Key ItemToFind ) {
			Value retVal = default( Value );
			List<Value> listItems = null;

			if( dictMultiMap.TryGetValue( ItemToFind, out listItems ) ) {
				if( listItems.Count > 0 ) {
					retVal = listItems[0];
					CurrentKey = ItemToFind;
					CurrentItemIndex = 1;
				}
			}

			return retVal;

		}

		/// <summary>
		/// Gets the Next Item in Multi map.  If this method is called first, it returns first item.
		/// </summary>
		/// <param name="ItemToFind"></param>
		/// <returns></returns>
		public Value GetNextItem( Key ItemToFind ) {
			Value retVal = default( Value );

			try {
				List<Value> listItems = null;
				if( dictMultiMap.TryGetValue( ItemToFind, out listItems ) ) {
					if( ItemToFind.CompareTo( CurrentKey ) != 0 ) {
						CurrentItemIndex = 0;
					}

					if( CurrentItemIndex < listItems.Count ) {
						retVal = listItems[CurrentItemIndex];
						CurrentItemIndex++;
						CurrentKey = ItemToFind;
					}
				}
			}
			catch( System.Exception ex ) {
				throw new MultiMapBKException( ex, ex.Message );
			}

			return retVal;
		}

		/// <summary>
		/// Iterates through all the values for the Key one by one.
		/// </summary>
		/// <param name="ItemToFind"></param>
		/// <returns></returns>
		public Value Iterate( Key ItemToFind ) {
			return GetNextItem( ItemToFind );
		}

		/// <summary>
		/// Removes the Key and all the values for an item.
		/// </summary>
		/// <param name="KeyElement"></param>
		public bool DeleteAll( Key KeyElement ) {
			bool retVal = false;
			try {
				List<Value> listToRemove = null;

				if( dictMultiMap.TryGetValue( KeyElement, out listToRemove ) ) {
					listToRemove.Clear();
					dictMultiMap.Remove( KeyElement );
					retVal = true;
				}
			}
			catch( System.Exception ex ) {
				throw new MultiMapBKException( ex, ex.Message );
			}

			return retVal;
		}

		/// <summary>
		/// Deletes one Key and one Value from the Multi map.
		/// </summary>
		/// <param name="KeyElement"></param>
		/// <param name="ValueElement"></param>
		public bool Delete( Key KeyElement, Value ValueElement ) {
			bool retVal = false;
			try {
				List<Value> listToRemove = null;

				if( dictMultiMap.TryGetValue( KeyElement, out listToRemove ) ) {
					listToRemove.Remove( ValueElement );

					if( listToRemove.Count == 0 ) {
						listToRemove = null;
						dictMultiMap.Remove( KeyElement );
						retVal = true;
					}
				}

			}
			catch( System.Exception ex ) {
				throw new MultiMapBKException( ex, ex.Message );
			}

			return retVal;
		}


		/// <summary>
		/// Disposes the Keys and Values.  Useful in case if unmanaged resources are stored here.
		/// </summary>
		public void Dispose() {
			lock( lockThisToDispose ) {
				if( dispose == false ) {
					dispose = true;
					Dictionary<Key, List<Value>>.Enumerator enumDictElements = dictMultiMap.GetEnumerator();

					while( enumDictElements.MoveNext() ) {
						try {
							IDisposable disposeObj = (IDisposable)enumDictElements.Current.Key;

							if( null != disposeObj ) {
								disposeObj.Dispose();
							}
						}
						catch( System.Exception ) { // Object not disposable
						}

						List<Value>.Enumerator enuValue = enumDictElements.Current.Value.GetEnumerator();
						while( enuValue.MoveNext() ) {
							try {
								IDisposable disposeObj = (IDisposable)enuValue.Current;

								if( null != disposeObj ) {
									disposeObj.Dispose();
								}
							}
							catch( System.Exception ) {
								// object not disposable
							}
						}

						enumDictElements.Current.Value.Clear();
					}

					dictMultiMap.Clear();

				}
			}
		}

		/// <summary>
		/// Finalizer
		/// </summary>
		~MultimapBK() {
			Dispose();
		}
	}

	/// <summary>
	/// MultiMap collection's exception class;
	/// </summary>
	public class MultiMapBKException : Exception {
		/// <summary>
		/// 
		/// </summary>
		public MultiMapBKException()
			: base() {
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ExceptionParam"></param>
		/// <param name="ExMessage"></param>
		public MultiMapBKException( System.Exception ExceptionParam, string ExMessage )
			: base( ExMessage, ExceptionParam ) {
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Message"></param>
		public MultiMapBKException( string Message )
			: base( Message ) {

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public MultiMapBKException( System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context )
			: base( info, context ) {
		}


	}

}
