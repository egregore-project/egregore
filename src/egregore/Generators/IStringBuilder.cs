namespace egregore.Generators
{
    public interface IStringBuilder
    {
        int Indent { get; set; }
        IStringBuilder OpenNamespace(string @namespace);
        IStringBuilder CloseNamespace();

        int Length { get; set; }
        IStringBuilder AppendLine(string message);
        IStringBuilder AppendLine();
        IStringBuilder Clear();
        IStringBuilder Insert(int index, object value);
        IStringBuilder Append(string value);
    }
}
