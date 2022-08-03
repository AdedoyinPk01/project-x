using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P21IntegrationWindowsService.Models
{
    public class ProkeepServiceModel
    {
        public class Rootobject
        {
            public string customer_name { get; set; }
            public Contact[] contact { get; set; }
        }

        public class Contact
        {
            public string type { get; set; }
            public string pk_id { get; set; }
            public string customer_id { get; set; }
            public string email_address { get; set; }
            public string external_id { get; set; }
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string phone_number { get; set; }
            public string fax_number { get; set; }
        }

        public class PSvcResponseObject
        {
            public PSvcResponse[] contact { get; set; }
        }

        public class PSvcResponse
        {
            public string pk_id { get; set; }
            public string external_id { get; set; }
            public string inserted_by_user_id { get; set; }
            public string updated_by_user_id { get; set; }
        }


        public class AckObject
        {
            public AckBody[] contact { get; set; }
        }

        public class AckBody
        {
            public string pk_id { get; set; }
            public string external_id { get; set; }
            public string status { get; set; }
        }

    }
}
