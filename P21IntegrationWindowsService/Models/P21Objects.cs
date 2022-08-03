using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace P21IntegrationWindowsService.Models
{
    public class P21Objects
    {
        // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
        public partial class ArrayOfContact
        {

            public Contact[] contactField;

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("Contact")]
            public Contact[] Contact
            {
                get
                {
                    return this.contactField;
                }
                set
                {
                    this.contactField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class Contact
        {

            public string salutationField;

            public string firstNameField;

            public string miField;

            public string lastNameField;
            
            public string titleField;
            
            public uint addressIdField;
            
            public string mailstopField;
            
            public string noOfCycleDaysField;
            
            public string commentsField;
            
            public string directPhoneField;
            
            public string phoneExtField;
            
            public string directFaxField;
            
            public string faxExtField;
            
            public string beeperField;
            
            public string cellularField;
            
            public string class1idField;
            
            public object class2idField;
            
            public object class3idField;
            
            public object class4idField;
            
            public object class5idField;
            
            public string homeAddress1Field;
            
            public string homeAddress2Field;
            
            public string homePhoneField;
            
            public string homeFaxField;
            
            public string homeEmailAddressField;
            
            public string birthdayField;
            
            public string anniversaryField;
            
            public string emailAddressField;

            public string urlField;

            public string cellularExtField;

            public string idField;

            /// <remarks/>
            public string Salutation
            {
                get
                {
                    return this.salutationField;
                }
                set
                {
                    this.salutationField = value;
                }
            }

            /// <remarks/>
            public string FirstName
            {
                get
                {
                    return this.firstNameField;
                }
                set
                {
                    this.firstNameField = value;
                }
            }

            /// <remarks/>
            public string Mi
            {
                get
                {
                    return this.miField;
                }
                set
                {
                    this.miField = value;
                }
            }

            /// <remarks/>
            public string LastName
            {
                get
                {
                    return this.lastNameField;
                }
                set
                {
                    this.lastNameField = value;
                }
            }

            /// <remarks/>
            public string Title
            {
                get
                {
                    return this.titleField;
                }
                set
                {
                    this.titleField = value;
                }
            }

            /// <remarks/>
            public uint AddressId
            {
                get
                {
                    return this.addressIdField;
                }
                set
                {
                    this.addressIdField = value;
                }
            }

            /// <remarks/>
            public string Mailstop
            {
                get
                {
                    return this.mailstopField;
                }
                set
                {
                    this.mailstopField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
            public string NoOfCycleDays
            {
                get
                {
                    return this.noOfCycleDaysField;
                }
                set
                {
                    this.noOfCycleDaysField = value;
                }
            }

            /// <remarks/>
            public string Comments
            {
                get
                {
                    return this.commentsField;
                }
                set
                {
                    this.commentsField = value;
                }
            }

            /// <remarks/>
            public string DirectPhone
            {
                get
                {
                    return this.directPhoneField;
                }
                set
                {
                    this.directPhoneField = value;
                }
            }

            /// <remarks/>
            public string PhoneExt
            {
                get
                {
                    return this.phoneExtField;
                }
                set
                {
                    this.phoneExtField = value;
                }
            }

            /// <remarks/>
            public string DirectFax
            {
                get
                {
                    return this.directFaxField;
                }
                set
                {
                    this.directFaxField = value;
                }
            }

            /// <remarks/>
            public string FaxExt
            {
                get
                {
                    return this.faxExtField;
                }
                set
                {
                    this.faxExtField = value;
                }
            }

            /// <remarks/>
            public string Beeper
            {
                get
                {
                    return this.beeperField;
                }
                set
                {
                    this.beeperField = value;
                }
            }

            /// <remarks/>
            public string Cellular
            {
                get
                {
                    return this.cellularField;
                }
                set
                {
                    this.cellularField = value;
                }
            }

            /// <remarks/>
            public string Class1id
            {
                get
                {
                    return this.class1idField;
                }
                set
                {
                    this.class1idField = value;
                }
            }

            /// <remarks/>
            public object Class2id
            {
                get
                {
                    return this.class2idField;
                }
                set
                {
                    this.class2idField = value;
                }
            }

            /// <remarks/>
            public object Class3id
            {
                get
                {
                    return this.class3idField;
                }
                set
                {
                    this.class3idField = value;
                }
            }

            /// <remarks/>
            public object Class4id
            {
                get
                {
                    return this.class4idField;
                }
                set
                {
                    this.class4idField = value;
                }
            }

            /// <remarks/>
            public object Class5id
            {
                get
                {
                    return this.class5idField;
                }
                set
                {
                    this.class5idField = value;
                }
            }

            /// <remarks/>
            public string HomeAddress1
            {
                get
                {
                    return this.homeAddress1Field;
                }
                set
                {
                    this.homeAddress1Field = value;
                }
            }

            /// <remarks/>
            public string HomeAddress2
            {
                get
                {
                    return this.homeAddress2Field;
                }
                set
                {
                    this.homeAddress2Field = value;
                }
            }

            /// <remarks/>
            public string HomePhone
            {
                get
                {
                    return this.homePhoneField;
                }
                set
                {
                    this.homePhoneField = value;
                }
            }

            /// <remarks/>
            public string HomeFax
            {
                get
                {
                    return this.homeFaxField;
                }
                set
                {
                    this.homeFaxField = value;
                }
            }

            /// <remarks/>
            public string HomeEmailAddress
            {
                get
                {
                    return this.homeEmailAddressField;
                }
                set
                {
                    this.homeEmailAddressField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
            public string Birthday
            {
                get
                {
                    return this.birthdayField;
                }
                set
                {
                    this.birthdayField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(IsNullable = true)]
            public string Anniversary
            {
                get
                {
                    return this.anniversaryField;
                }
                set
                {
                    this.anniversaryField = value;
                }
            }

            /// <remarks/>
            public string EmailAddress
            {
                get
                {
                    return this.emailAddressField;
                }
                set
                {
                    this.emailAddressField = value;
                }
            }

            /// <remarks/>
            public string Url
            {
                get
                {
                    return this.urlField;
                }
                set
                {
                    this.urlField = value;
                }
            }

            /// <remarks/>
            public string CellularExt
            {
                get
                {
                    return this.cellularExtField;
                }
                set
                {
                    this.cellularExtField = value;
                }
            }

            /// <remarks/>
            public string Id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }
        }
    }
}
