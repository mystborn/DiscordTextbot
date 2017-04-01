using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;

namespace TextBot
{
    public class ContactInfo
    {
        #region Xml Properties

        [XmlArray("ToContacts")]
        [XmlArrayItem(typeof(ContactItem), ElementName = "Contact")]
        public ContactItem[] ToContacts
        {
            get
            {
                return new List<ContactItem>(from kvp in ToContactsDict
                                             select new ContactItem() { Name = kvp.Key, Number = kvp.Value }).ToArray();
            }
            set
            {
                ToContactsDict = new Dictionary<string, string>();
                foreach (var pair in value)
                {
                    ToContactsDict[pair.Name] = pair.Number;
                }
            }
        }

        [XmlArray("FromContacts")]
        [XmlArrayItem(typeof(ContactItem), ElementName = "Contact")]
        public ContactItem[] FromContacts
        {
            get
            {
                return new List<ContactItem>(from kvp in FromContactsDict
                                             select new ContactItem() { Name = kvp.Key, Number = kvp.Value }).ToArray();
            }
            set
            {
                FromContactsDict = new Dictionary<string, string>();
                foreach (var pair in value)
                {
                    FromContactsDict[pair.Name] = pair.Number;
                }
            }
        }

        #endregion

        #region Properties

        [XmlIgnore]
        public Dictionary<string, string> ToContactsDict { get; set; } = new Dictionary<string, string>();

        [XmlIgnore]
        public Dictionary<string, string> FromContactsDict { get; set; } = new Dictionary<string, string>();

        #endregion

        #region Serialization

        public static (Dictionary<string, string> to, Dictionary<string, string> from) LoadAddressBooks()
        {
            var path = Path.Combine(Settings.FolderPath, "ContactBook.xml");
            if (File.Exists(path))
            {
                ContactInfo info;
                using (FileStream stream = File.OpenRead(path))
                {
                    XmlSerializer cereal = new XmlSerializer(typeof(ContactInfo));
                    info = (ContactInfo)cereal.Deserialize(stream);
                }
                return (info.ToContactsDict, info.FromContactsDict);
            }
            else
            {
                return (new Dictionary<string, string>(), new Dictionary<string, string>());
            }
        }

        public static void SaveAddressBooks(Dictionary<string, string> to, Dictionary<string, string> from)
        {
            var path = Path.Combine(Settings.FolderPath, "ContactBook.xml");
            if (File.Exists(path))
                File.Delete(path);

            using (var stream = File.Create(path))
            {
                using (var writer = new StreamWriter(stream))
                {
                    XmlSerializer cereal = new XmlSerializer(typeof(ContactInfo));
                    ContactInfo info = new ContactInfo()
                    {
                        ToContactsDict = to,
                        FromContactsDict = from
                    };
                    cereal.Serialize(writer, info);
                }
            }
        }

        #endregion
    }

    public class ContactItem
    {
        public string Name { get; set; }
        public string Number { get; set; }
    }
}
