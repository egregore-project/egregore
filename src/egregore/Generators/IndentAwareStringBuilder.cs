using System.Text;

namespace egregore.Generators
{
    internal sealed class IndentAwareStringBuilder : IStringBuilder
    {
        private readonly StringBuilder _inner;

        public int Indent { get; set; }

        public int Capacity
        {
            get => _inner.Capacity;
            set => _inner.Capacity = value;
        }

        public int Length
        {
            get => _inner.Length;
            set => _inner.Length = value;
        }

        public IndentAwareStringBuilder() : this(new StringBuilder()) { }
        public IndentAwareStringBuilder(int capacity) : this(new StringBuilder(capacity)) { }

        public IndentAwareStringBuilder(StringBuilder inner)
        {
            _inner = inner;
        }

        public IStringBuilder AppendLine(string message)
        {
            _inner.AppendLine(Indent, message);
            return this;
        }

        public IStringBuilder AppendLine()
        {
            _inner.AppendLine();
            return this;
        }

        public IStringBuilder Clear()
        {
            _inner.Clear();
            return this;
        }

        public override string ToString()
        {
            return _inner.ToString();
        }

        public IStringBuilder Insert(int index, object value)
        {
            _inner.Insert(0, value);
            return this;
        }

        public IStringBuilder OpenNamespace(string @namespace)
        {
            _inner.AppendLine(Indent, $"namespace {@namespace}");
            _inner.AppendLine(Indent, "{");
            Indent++;
            return this;
        }

        public IStringBuilder CloseNamespace()
        {
            Indent--;
            _inner.AppendLine(Indent, "}");
            return this;
        }

        public IStringBuilder Append(string value)
        {
            _inner.Append(value);
            return this;
        }
    }
}
