using System;
using System.Web;
using System.Web.UI;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;

namespace MDTools.Data {

    public static class DataObj {
        #region short-cut static instance creation functions
        public static DataObjT<List<string>, object>
            NewListS() { return new DataObjT<List<string>, object>( new List<string>() ); }
        public static DataObjT<List<object>, object>
            NewListO() { return new DataObjT<List<object>, object>( new List<object>() ); }
        public static DataObjT<List<string>, List<object>>
            NewListSO() { return new DataObjT<List<string>, List<object>>( new List<string>(), new List<object>() ); }

        public static DataObjT<Dictionary<string, string>, object>
            NewDictSS() { return new DataObjT<Dictionary<string, string>, object>( new Dictionary<string, string>() ); }
        public static DataObjT<Dictionary<string, object>, object>
            NewDictSO() { return new DataObjT<Dictionary<string, object>, object>( new Dictionary<string, object>() ); }
        public static DataObjT<Dictionary<string, object>, Dictionary<string, object>>
            NewDictSOSO() { return new DataObjT<Dictionary<string, object>, Dictionary<string, object>>( new Dictionary<string, object>(), new Dictionary<string, object>() ); }

        public static DataListSc NewListSc() { return new DataListSc( new List<string>() ); }
        public static DataListOc NewListOc() { return new DataListOc( new List<object>() ); }
        public static DataDictSSc NewDictSSc() { return new DataDictSSc( new Dictionary<string, string>() ); }
        public static DataDictSOc NewDictSOc() { return new DataDictSOc( new Dictionary<string, object>() ); }

        public static DataObjT<ArrayList, object>
            NewArrayList() { return new DataObjT<ArrayList, object>( new ArrayList() ); }
        public static DataObjT<Hashtable, object>
            NewHashTable() { return new DataObjT<Hashtable, object>( new Hashtable() ); }
        public static DataObjT<OrderedDictionary, object>
            NewOrderedDictionary() { return new DataObjT<OrderedDictionary, object>( new OrderedDictionary() ); }
        #endregion
    }
    public interface IDataObj {
    }

    [Serializable]
    public class DataObjT<PRIM> : DataObjT<PRIM, Object> {
        public DataObjT( PRIM p ) : base(p) {}
    }

    [Serializable]
    public class DataObjT<PRIM, SEC> : IDataObj { //where PRIM : ISerializable where SEC : ISerializable {
        public PRIM Data = default( PRIM ); // null
        public SEC Store = default( SEC );  // null

        public DataObjT( PRIM p ) { Data = p; }
        public DataObjT( PRIM p, SEC s ) { Data = p; Store = s; }
/*
 protected DataObjT( SerializationInfo info, StreamingContext context ) {
            Serialization.SerializationReader reader = new Serialization.SerializationReader( (byte[])info.GetValue( "dl", typeof( byte[] ) ) );
            Data = (PRIM)reader.ReadObject();
            Store = (SEC)reader.ReadObject();
        }
		[SecurityCritical]
        public virtual void GetObjectData( SerializationInfo info, StreamingContext context ) {
            Serialization.SerializationWriter writer = new Serialization.SerializationWriter();
            writer.WriteObject( Data );
            writer.WriteObject( Store );
            info.AddValue( "dl", writer.ToArray() );
        }
 */
    }
    [Serializable]
    public class DataListSc : DataObjT<List<string>, DataListSc> {
        public DataListSc( List<string> li ) : base( li ) { }
/*
		[SecurityCritical]
		public virtual void GetObjectData( SerializationInfo info, StreamingContext context ) {
            SerializationWriter writer = new Serialization.SerializationWriter();
            writer.Write<List<string>>(Data);
            info.AddValue( "dl", writer.ToArray() );
            Store.GetObjectData( info, context );
        }
 */
    }
    [Serializable]
    public class DataListOc : DataObjT<List<object>, DataListOc> {
        public DataListOc( List<object> li ) : base( li ) { }
    }
    [Serializable]
    public class DataDictSSc : DataObjT<Dictionary<string, string>, DataDictSSc> {
        public DataDictSSc( Dictionary<string, string> di ) : base( di ) { }
    }
    [Serializable]
    public class DataDictSOc : DataObjT<Dictionary<string, object>, DataDictSOc> {
        public DataDictSOc( Dictionary<string, object> di ) : base( di ) { }
    }

    /*    /// <summary>
        /// DataLayer data that last the life of the Page
        /// </summary>
        public class PageData : DLData<IDictionary> {
            public PageData( HttpContext context ) : base(context.Items) { }
            public static PageData Get( HttpContext context ) { return new PageData(context); }
            public PageData( IDictionary dict ) : base( dict ) { }
            public static PageData Get( IDictionary dict ) { return new PageData( dict ); }
        }

        /// <summary>
        /// DataLayer data that last the life of the worker thread process
        /// </summary>
        public class CacheData : DLData<Cache> {
            public CacheData( HttpContext context ) : base( context.Cache ) { }
            public static CacheData Get( HttpContext context ) { return new CacheData( context ); }
            public CacheData( Cache cache ) : base( cache ) { }
            public static CacheData Get( Cache cache ) { return new CacheData( cache ); }
        }

        /// <summary>
        /// DataLayer data that last the life of the current session - sessionstate must be turned on
        /// </summary>
        public class SessionData : DLData<System.Web.SessionState.HttpSessionState> {
            public SessionData( Page page ) : base( page.Session ) { }
            public SessionData( System.Web.SessionState.HttpSessionState session ) : base( session ) { }
            public static SessionData Get( Page page ) { return new SessionData( page ); }
            public static SessionData Get( System.Web.SessionState.HttpSessionState session ) { return new SessionData( session ); }
        }
    */

    /*
        [Serializable]
        public class PageData {

            /// <summary>
            /// Top-Level Data-Layer Objects - Tree
            /// </summary>
            public DataObjT<Dictionary<string, object>, IDataObj> DataLayer = new DataObjT<Dictionary<string, object>, IDataObj>( new Dictionary<string, object>() );
            /// <summary>
            /// Local Values - Strings
            /// </summary>
            public Dictionary<string, string> Names = new Dictionary<string, string>() ;
            /// <summary>
            /// Local Values - Objects
            /// </summary>
            public Dictionary<string, object> Objects = new Dictionary<string, object>();

            protected const string KEY_PAGEDATA = "$PD";
            public static PageData Get( HttpContext context ) {
                PageData p = context.Items[KEY_PAGEDATA] as PageData;
                // Create if not already existing
                if(p == null) {//!! not thread-safe
                    p = new PageData();
                    context.Items[KEY_PAGEDATA] = p;
                }
                return p;
            }
    */
    /*
        public class PageData {
			[SecurityCritical]
            public virtual void GetObjectData( SerializationInfo info, StreamingContext context ) {
                DataLayer.GetObjectData( info, context );
                Serialization.SerializationWriter writer = new Serialization.SerializationWriter();
                writer.WriteObject( DataLayer );
                info.AddValue( "page", writer.ToArray() );
            }
        }
    */

    /*	public class DataLayer  {
            public HttpContext Context;
            public IDictionary Data;

            public DataLayer() { 
                Context = HttpContext.Current;
                Data = Context.Items;
            }
            public DataLayer(HttpContext context) { 
                Context = context;
                Data = Context.Items;
            }
            public DataObj this[string lookup] {
                get { return (DataObj)Data[lookup]; }
                set { Data[lookup] = value; }
            }
        }
	
        [Serializable]
        public class DataObj {

            public Object StoreObj = null;
            public IDictionary Dict = null;
            public IList List = null;

            public DataObj(IList il) { List = il; }
            public DataObj(IDictionary id) { Dict = id; }
    //		public DataObj(IOrderedKeyList ikl) { Dict = (IDictionary)ikl; List = (IList)ikl; }

            public object this[string lookup] {
                get { return Dict[lookup]; }
                set { Dict[lookup] = value; }
            }
            public object this[int lookup] {
                get { return List[lookup]; }
                set { List[lookup] = value; }
            }
            public object this[object lookup] {
                get { return Dict[lookup]; }
                set { Dict[lookup] = value; }
            }

            public static DataObj NewListS() { return new DataObj(new List<string>()); }
            public static DataObj NewListO() { return new DataObj(new List<object>()); }
            public static DataObj NewArrayList() { return new DataObj(new ArrayList()); }

            public static DataObj NewDictionaryS() { return new DataObj(new Dictionary<string, string>()); }
            public static DataObj NewDictionaryO() { return new DataObj(new Dictionary<string, object>()); }
            public static DataObj NewHashtable() { return new DataObj(new Hashtable()); }

    //		public static DataObj NewOrderedKeyListS() { return new DataObj(new OrderedKeyList<string, string>()); }
    //		public static DataObj NewOrderedKeyListO() { return new DataObj(new OrderedKeyList<string, object>()); }

        }*/
}
