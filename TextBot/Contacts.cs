using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace TextBot
{
    public static class Contacts
    {
        private static Dictionary<string, string> _to;
        private static Dictionary<string, string> _from;

        public static int LongestName { get; private set; } = 0;

        private const string NO_ERROR = "This should not be seen.";

        public static bool TryAddContact(string name, string contact, out string error)
        {
            if(_to.ContainsKey(name))
            {
                error = $"You already have a contact names {contact}.";
                return false;
            }

            if(_from.ContainsKey(contact))
            {
                error = $"You already have the number {contact} saved under the name {_from[contact]}.";
                return false;
            }

            var count = name.Split().Length;

            LongestName = count > LongestName ? count : LongestName;

            _to.Add(name, contact);
            _from.Add(contact, name);
            error = NO_ERROR;

            Save();

            return true;
        }

        public static bool TryRemoveContact(string name, out string error)
        {
            if(_to.TryGetValue(name, out var contact))
            {
                _to.Remove(name);
                _from.Remove(contact);
                error = NO_ERROR;
                return true;
            }
            else
            {
                error = $"There was no contact with the name {name}.";
                return false;
            }
        }

        public static bool TryGetNumber(string name, out string contact)
        {
            return _to.TryGetValue(name, out contact);
        }

        public static bool TryGetContact(string number, out string contact)
        {
            return _from.TryGetValue(number, out contact);
        }

        public static IEnumerable<KeyValuePair<string, string>> GetContacts()
        {
            foreach(var contact in _to)
            {
                yield return contact;
            }
        }

        public static void Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "AddressBook.xml");
            _to = new Dictionary<string, string>();
            _from = new Dictionary<string, string>();
            if (File.Exists(path))
            {
                using(var file = new StreamReader(path))
                {
                    using(var reader = XmlReader.Create(file))
                    {
                        reader.Read();
                        while(reader.ReadToFollowing("Contact"))
                        {
                            reader.ReadToDescendant("Name");
                            var name = reader.ReadElementContentAsString();
                            reader.ReadToFollowing("Number");
                            var number = reader.ReadElementContentAsString();
                            _to.Add(name, number);
                            _from.Add(number, name);
                        }
                    }
                }
            }
        }

        public static void Save()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "AddressBook.xml");
            using(var file = new FileStream(path, FileMode.Create))
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true
                };
                using (var writer = XmlWriter.Create(file, settings))
                {
                    writer.WriteStartElement("AddressBook");

                    foreach (var kvp in _to)
                    {
                        writer.WriteStartElement("Contact");

                        writer.WriteStartElement("Name");
                        writer.WriteString(kvp.Key);
                        writer.WriteEndElement();

                        writer.WriteStartElement("Number");
                        writer.WriteString(kvp.Value);
                        writer.WriteEndElement();

                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }
            }
        }
    }
}
