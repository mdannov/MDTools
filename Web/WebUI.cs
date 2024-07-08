using System;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using MDTools.Data;

#if MediumTrust
namespace MDTools.Web {

    /// <summary>
    /// Summary description for Page
    /// </summary>
    public class Page : System.Web.UI.Page {

        protected PageData pageData = null;
        public Guid UserID = MDTools.Web.User.UserID;

        public PageData PageData { 
            get {
                if(pageData!=null)
                    return  pageData;
                return pageData = PageData.Get( Context );
            }
        }

    }

    public class UserControl : System.Web.UI.UserControl {

        public Guid UserID { get { return ( (MDTools.Web.Page) this.Page ).UserID; } }

    }
}
#endif