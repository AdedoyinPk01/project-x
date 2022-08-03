using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P21IntegrationWindowsService.Models
{
    public class Rootobject
    {
        public Datum[] data { get; set; }
        public bool has_next_page { get; set; }
        public bool has_previous_page { get; set; }
        public object next_cursor { get; set; }
        public object previous_cursor { get; set; }
    }

    public class Datum
    {
        public bool announcements_opt_out { get; set; }
        public object company_id { get; set; }
        public string cursor { get; set; }
        public object email_address { get; set; }
        public object external_id { get; set; }
        public object first_name { get; set; }
        public object[] groups { get; set; }
        public string id { get; set; }
        public DateTime inserted_at { get; set; }
        public string inserted_by_user_id { get; set; }
        public object last_name { get; set; }
        public object notes { get; set; }
        public object notes_updated_at { get; set; }
        public object notes_updated_by_user_id { get; set; }
        public string phone_number { get; set; }
        public bool prioritize_rep { get; set; }
        public object rep_user_id { get; set; }
        public DateTime updated_at { get; set; }
        public string updated_by_user_id { get; set; }
    }
}
