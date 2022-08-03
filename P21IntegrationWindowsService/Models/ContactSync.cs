using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P21IntegrationWindowsService.Models
{
    [Table("settings")]
    public class Settings
    {
        [Key]
        [Column("icid")]
        public int ICID { get; set; }
        [Column("frequency")]
        public int Frequency { get; set; }
        [Column("company")]
        public string Company { get; set; }
        [Column("svcurl")]
        public string SvcUrl { get; set; }
        [Column("p21url")]
        public string P21Url { get; set; }
        [Column("nextrun")]
        public DateTime? NextRun { get; set; }
        [Column("lastrun")]
        public DateTime? LastRun { get; set; }
    }

    [Table("contactsync")]
    public class ContactSync
    {
        [Key]
        public int id { get; set; }
        [Column("p21_id")]
        public string P21_ID { get; set; }
        [Column("pk_id")]
        public string PK_ID { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string email_address { get; set; }
        public string phone_number { get; set; }
        public int status { get; set; }
        public DateTime? synced_on { get; set; }
        public string sync_trigger_by { get; set; }
        public DateTime? updated_on { get; set; }
        public string update_trigger_by { get; set; }
    }
}
