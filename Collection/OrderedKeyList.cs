using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace MDTools.Collections {
    public interface IOrderedKeyList { }

    public class OrderedKeyList<K, V> : IDictionary<K, V>, IList<V>, IOrderedKeyList {
        Dictionary<K, V> dict = new Dictionary<K, V>();
        List<K> list = new List<K>();

        // 'System.Collections.Generic.IDictionary<K,V>.ContainsKey(K)'
        public bool ContainsKey( K key ) { return dict.ContainsKey( key ); }
        // 'System.Collections.Generic.IDictionary<K,V>.Add(K, V)'
        public void Add( K key, V value ) { dict.Add( key, value ); list.Add( key ); }
        // 'System.Collections.Generic.IDictionary<K,V>.Remove(K)'
        public bool Remove( K key ) {
            if( dict.Remove( key ) )
                return list.Remove( key );
            return false;
        }
        // 'System.Collections.Generic.IDictionary<K,V>.TryGetValue(K, out V)'
        public bool TryGetValue( K key, out V value ) { return dict.TryGetValue( key, out value ); }
        // 'System.Collections.Generic.IDictionary<K,V>.this[K]'
        public V this[K key] {
            get { return dict[key]; }
            set {
                if( !dict.ContainsKey( key ) )
                    list.Add( key );
                dict[key] = value;
            }
        }
        // 'System.Collections.Generic.IDictionary<K,V>.Keys'
        public ICollection<K> Keys { get { return list; } }
        // 'System.Collections.Generic.IDictionary<K,V>.Values'
        public ICollection<V> Values {
            get {
                int size = list.Count;
                List<V> arr = new List<V>( size );
                for( int i = 0; i < size; i++ )
                    arr.Add( dict[list[i]] );
                return arr;
            }
        }
        // 'System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<K,V>>.Add(System.Collections.Generic.KeyValuePair<K,V>)'
        public void Add( KeyValuePair<K, V> k ) { Add( k.Key, k.Value ); }
        // 'System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<K,V>>.Contains(System.Collections.Generic.KeyValuePair<K,V>)'
        public bool Contains( KeyValuePair<K, V> kv ) {
            V value = this[kv.Key];
            return EqualityComparer<V>.Default.Equals( value, kv.Value );
        }
        // 'System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<K,V>>.CopyTo(System.Collections.Generic.KeyValuePair<K,V>[], int)'
        public void CopyTo( KeyValuePair<K, V>[] kv, int arrayIndex ) {
            int size = list.Count;
            List<KeyValuePair<K, V>> arr = new List<KeyValuePair<K, V>>( size - arrayIndex );
            for( int i = arrayIndex; i < size; i++ ) {
                K key = list[i];
                arr.Add( new KeyValuePair<K, V>( key, dict[key] ) );
            }
            arr.CopyTo( kv );
        }

        // 'System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<K,V>>.Remove(System.Collections.Generic.KeyValuePair<K,V>)'
        public bool Remove( KeyValuePair<K, V> kv ) { return Remove( kv.Key ); }

        // 'System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<K,V>>.GetEnumerator()'
        IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator() {
            return dict.GetEnumerator();
            /*			KeyValuePair<K,V>[] kv = new KeyValuePair<K,V>[list.Count];
                        CopyTo(kv, 0);
                        return kv.GetEnumerator();*/
        }

        // Necessary to create unique key from the value in case Dictionary is not used
        protected virtual K _ConvertToKey( V value ) { K key = list[0]; return (K)Convert.ChangeType( value.GetHashCode(), key.GetType() ); }
        // 'System.Collections.Generic.IList<V>.IndexOf(V)'
        public int IndexOf( V value ) {
            IDictionaryEnumerator e = dict.GetEnumerator();
            while( e.MoveNext() ) {
                if( e.Value.Equals( value ) ) {
                    return list.IndexOf( (K)e.Key );
                }
            }
            return -1;
        }
        // 'System.Collections.Generic.IList<V>.Insert(int, V)'
        //;throw notsupportedexception
        public void Insert( int index, V value ) { K key = _ConvertToKey( value ); dict.Add( key, value ); list.Insert( index, key ); }
        // 'System.Collections.Generic.IList<V>.RemoveAt(int)'
        public void RemoveAt( int index ) { dict.Remove( list[index] ); list.RemoveAt( index ); }
        // 'System.Collections.Generic.IList<V>.this[int]'
        public V this[int index] {
            get { return dict[list[index]]; }
            set {
                K key = list[index];
                dict[key] = value;
            }
        }
        // 'System.Collections.Generic.ICollection<V>.Add(V)'
        //;throw notsupportedexception
        public void Add( V value ) { K key = _ConvertToKey( value ); dict.Add( key, value ); list.Add( key ); }
        // 'System.Collections.Generic.ICollection<V>.Clear()'
        public void Clear() { dict.Clear(); list.Clear(); }
        // 'System.Collections.Generic.ICollection<V>.Contains(V)'
        public bool Contains( V value ) { return dict.ContainsValue( value ); }
        // 'System.Collections.Generic.ICollection<V>.CopyTo(V[], int)'
        public void CopyTo( V[] array, int arrayIndex ) {
            int size = list.Count;
            List<V> arr = new List<V>( size - arrayIndex );
            for( int i = arrayIndex; i < size; i++ )
                arr.Add( dict[list[i]] );
            arr.CopyTo( array );
        }
        // 'System.Collections.Generic.ICollection<V>.Remove(V)'
        public bool Remove( V value ) {
            int pos = IndexOf( value );
            RemoveAt( pos );
            return pos >= 0;
        }
        // 'System.Collections.Generic.ICollection<V>.Count'
        public int Count { get { return list.Count; } }
        // 'System.Collections.Generic.ICollection<V>.IsReadOnly'
        public bool IsReadOnly { get { return false; } }

        // 'System.Collections.Generic.IEnumerable<V>.GetEnumerator()'
        IEnumerator<V> IEnumerable<V>.GetEnumerator() {
            return dict.Values.GetEnumerator();
        }
        // 'System.Collections.IEnumerable.GetEnumerator()'
        IEnumerator IEnumerable.GetEnumerator() {
            return list.GetEnumerator();
        }

        // 'System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<K,V>>.Count'
        // 'System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<K,V>>.IsReadOnly'
        // 'System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<K,V>>.Clear()'
    }
    /*
        public class OrderedKeyListEnumerator : IEnumerator<V> {

            private int index = -1;
            Dictionary<string, object> objList = new Dictionary<string, object>();
            List<string> keyList = new List<string>();
            DictionaryEntry de = null;

            internal OrderedKeyListEnumerator(Dictionary<string, object> objList, List<string> keyList) { this.objList = objList; keyList = keyList; }
            public bool MoveNext() { index++; de = null;  return !(index >= keyList.Count); }
            public void Reset() { index = -1; }
            public object Current {
                get {
                    if (index < 0 || index >= keyList.Count)
                        throw new InvalidOperationException();
                    return Entry;
                }
            }
            public DictionaryEntry Entry {
                get {
                    if (de != null)
                        return de;
                    string key = keyList[index];
                    DictionaryEntry de = new DictionaryEntry(key, objList[key]); 
                    return (DictionaryEntry)de;
                }
            }
            public object Key { get { return Entry.Key; } }
            public object Value { get { return Entry.Value; } }
        }*/

    /*
        public class QueuedTree : List {
            private object storeObj = null;
            private int nCur = 0;
            public void Reset() { nCur = 0; }

            #region properties
            public int Pos { get { return nCur; } set { nCur = value; } }
            #endregion

            /// <summary>
            /// Get current object off the stored array without incrementing position
            /// </summary>
            public object GetItem() {
                return this[nCur];
            }
            /// <summary>
            /// Get the next inner item from the current position and increment position
            /// </summary>
            public object PopItem() {
                return this[nCur++];
            }

            /// <summary>
            /// Get current object off the stored array without incrementing position
            /// </summary>
            public IList Get() {
                if(nCur==0) return this;
                return( Get( nCur, int.MaxValue) );
            }
            /// <summary>
            /// Get the next inner item from the current position and increment position
            /// </summary>
            public IList Pop() {
                if(nCur >= this.Count)
                    return null;
                int nPrev = nCur;
                nCur = this.Count;
                if(nPrev==0)
                    return this;
                return ( Pop(nPrev, this.Count-nPrev+1) );
            }

            /// <summary>
            /// Get a subset array from inside the stored array from start
            /// </summary>
            /// <param name="start">where to start in the array</param>
            /// <param name="count">number of items to include if available</param>
            /// <returns>ArrayList</returns>
            public IList Get(int start, int count) {
                List result = new List<V>();
                int j=0;
                int acount = this.Count;
                for(int i=start; j<count && i<acount; i++, j++)
                    result.Add(this[i]);
                return result;
            }
            /// <summary>
            /// Get a subset array from inside the stored array from start
            /// </summary>
            /// <param name="start">where to start in the array</param>
            /// <param name="count">number of items to include if available</param>
            /// <returns>ArrayList</returns>
            public IList Pop(int start, int count) {
                List<V> result = new List<V>();
                int j = 0;
                int acount = this.Count;
                for(int i=start; j<count && i<acount; i++, j++)
                    result.Add(this[i]);
                nCur += j;
                return result;
            }

            /// <summary>
            /// Get a subset array from inside the stored array from current
            /// </summary>
            /// <param name="count">number of items to include if available</param>
            /// <returns>ArrayList</returns>
            public IList Get(int count) {
                return Get(nCur, count);
            }
            /// <summary>
            /// Get a subset array from inside the stored array from current
            /// </summary>
            /// <param name="count">number of items to include if available</param>
            /// <returns>ArrayList</returns>
            public IList Pop(int count) {
                return Pop(nCur, count);
            }

            /// <summary>
            /// Reshape into an array (grid) where each row contains an inner single row array of horiz elements
            /// </summary>
            /// <param name="start"></param>
            /// <param name="horiz"></param>
            /// <param name="vert"></param>
            /// <param name="bal"></param>
            /// <returns></returns>
            public IList Reshape(int start, int horiz, int vert, bool bal) {
                List<V> result = new List<V>();
                List<V> arr = null;
                int rows=0;
                int i=0;
                int cnt = this.Count;
                for(; i+start<cnt; i++) {
                    if(i%horiz == 0) {
                        rows++;
                        if(rows>vert)
                            break;
                        arr = new List<V>();
                        result.Add(arr);
                    }
                    arr.Add(this[i+start]);
                }
                nCur += i;
                if(bal && rows>1 && result.Count*horiz > i) // if uneven
                    result[result.Count-1] = Balance(result[result.Count-1], horiz);
                return result;
            }
            // Reshape from current in a grid "horiz" wide and up to "vert" tall and maybe balance last row
            public IList Reshape(int horiz, int vert, bool bal) {
                return Reshape(nCur, horiz, vert, bal);
            }
            // Reshape from current in a grid "horiz" wide and as tall as nec, and maybe balance last row
            public IList Reshape(int horiz, bool bal) {
                return Reshape(nCur, horiz, int.MaxValue, bal);
            }

            public IList Balance(IList arr, int cols) {
                int cnt = arr.Count;
                byte grid = ArrayListShaperBalancer.bitgrid[cols-1, cnt-1];
                List<V> res = new List<V>(cols);
                int cur=0;
                for(int i=0; i<cols; i++) {
                    if(( grid & ( 1<<i ) ) > 0)
                        res.Add(arr[cur++]);
                    else
                        res.Add(null);
                }
                return res;
            }


            public static IList ReshapeArrayList(IList arr, int horiz, bool bal) {
                QueuedList<V> ql = new QueuedList<V>(arr);
                return ql.Reshape(horiz, bal);
            }
            public static IList ReshapeArrayList(IList arr, int horiz, int vert, bool bal) {
                QueuedList<V> ql = new QueuedList<V>(arr);
                return ql.Reshape(horiz, vert, bal);
            }
            public static IList ReshapeArrayList(IList arr, int start, int horiz, int vert, bool bal) {
                QueuedList<V> ql = new QueuedList<V>(arr);
                return ql.Reshape(start, horiz, vert, bal);
            }
        }


        public class ListShaperBalancer {
            // Used to balance last rows up to 8 columns
            public readonly static byte[,] bitgrid = { 
            { 0,   0,   0,   0,   0,   0,   0 },// 1
            { 1,   0,   0,   0,   0,   0,   0 },// 2
            { 2,   5,   0,   0,   0,   0,   0 },// 3
            { 1,   9,   8,   0,   0,   0,   0 },// 4
            { 4,  10,  21,  27,   0,   0,   0 },// 5
            { 1,  33,  21,  51,  31,   0,   0 },// 6
            { 8,  65,  73,  85,  93,  63,   0 },// 7 
            { 1, 129,  93, 195, 227, 189, 127 } // 8 
        };

            //             136 
            //Col-rem= 1248624
            // 3 - 1 = 010 = 2
            // 3 - 2 = 101 = 5
            // 4 - 1 = 1000 = 1
            // 4 - 2 = 1001 = 9
            // 4 - 3 = 1110 = 8
            // 5 - 1 = 00100 = 4
            // 5 - 2 = 01010 = 10
            // 5 - 3 = 10101 = 21
            // 5 - 4 = 11011 = 27
            // 6 - 1 = 100000 = 1
            // 6 - 2 = 100001 = 33
            // 6 - 3 = 101010 = 21
            // 6 - 4 = 110011 = 51
            // 6 - 5 = 111110 = 31
            // 7 - 1 = 0001000 = 8
            // 7 - 2 = 1000001 = 65
            // 7 - 3 = 1001001 = 73
            // 7 - 4 = 1010101 = 85
            // 7 - 5 = 1011101 = 93
            // 7 - 6 = 1111110 = 63
            // 8 - 1 = 10000000 = 1
            // 8 - 2 = 10000001 = 129
            // 8 - 3 = 10010001 = 93
            // 8 - 4 = 11000011 = 195
            // 8 - 5 = 11100011 = 227
            // 8 - 6 = 10111101 = 189
            // 8 - 7 = 11111110 = 127
        }
    */
}
