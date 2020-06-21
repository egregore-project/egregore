using System.Collections.Generic;

namespace egregore.Ontology
{
    public sealed class OntologyLog
    {
        private static readonly Namespace Default = new Namespace(Constants.DefaultNamespace);

        public List<Namespace> Namespaces { get; }
        public Dictionary<string, Dictionary<ulong, List<Schema>>> Schemas { get; }
        public Dictionary<string, Dictionary<string, ulong>> Revisions { get; }

        public OntologyLog()
        {
            Namespaces = new List<Namespace> { Default };
            
            Schemas = new Dictionary<string, Dictionary<ulong, List<Schema>>>();
            Schemas.TryAdd(Constants.DefaultNamespace, new Dictionary<ulong, List<Schema>>());

            Revisions = new Dictionary<string, Dictionary<string, ulong>>();
            Revisions.TryAdd(Constants.DefaultNamespace, new Dictionary<string, ulong>());
        }

        public OntologyLog(ILogStore store, ulong startingFrom = 0UL, byte[] secretKey = null) : this()
        {
            var @namespace = Default;

            foreach (var entry in store.StreamEntries(startingFrom, secretKey))
            {
                foreach (var @object in entry.Objects)
                {
                    switch (@object.Data)
                    {
                        case Namespace ns:
                        {
                            var key = ns;
                            Namespaces.Add(key);
                            @namespace = key;
                            if(!Revisions.ContainsKey(@namespace.Value))
                                Revisions.Add(@namespace.Value, new Dictionary<string, ulong>());
                            break;
                        }
                        case Schema schema:
                        {
                            var key = schema.Name;

                            if(!Revisions[@namespace.Value].TryGetValue(key, out var revision))
                                Revisions[@namespace.Value].Add(key, revision = 1);

                            if(!Schemas.TryGetValue(key, out var schemaMap))
                                Schemas.Add(key, schemaMap = new Dictionary<ulong, List<Schema>>());

                            if(!schemaMap.TryGetValue(revision, out var list))
                                schemaMap.Add(revision, list = new List<Schema>());

                            list.Add(schema);
                            break;
                        }
                    }
                }
            }
        }
    }
}
