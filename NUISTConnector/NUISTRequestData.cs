using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUISTConnector
{
    public struct NUISTRequestData
    {
        public string username { get; set; }
        public string password { get; set; }
        public string channel { get; set; }
        public string ifautologin { get; set; }
        public string pagesign { get; set; }
        public string usripadd { get; set; }
    }
}
