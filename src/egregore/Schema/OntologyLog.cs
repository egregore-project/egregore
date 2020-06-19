using System.Collections.Generic;

namespace egregore.Schema
{
    public sealed class OntologyLog
    {
        public List<string> Namespaces { get; set; }

        public OntologyLog()
        {
            Namespaces = new List<string> {Constants.DefaultNamespace};
        }

        public OntologyLog(ILogStore store, ulong startingFrom = 0UL, byte[] secretKey = null)
        {
            Namespaces = new List<string> {Constants.DefaultNamespace};

            foreach (var entry in store.StreamEntries(startingFrom, secretKey))
            {
                foreach (var @object in entry.Objects)
                {
                    if (@object.Data is Namespace ns)
                    {
                        Namespaces.Add(ns.Value);
                    }
                }
            }
        }
    }
}
