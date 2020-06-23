using System;

namespace egregore.Ontology.Exceptions
{
    public sealed class CannotRemoveSingleOwnerException : InvalidOperationException
    {
        public CannotRemoveSingleOwnerException(string message) : base(message) { }
    }
}
