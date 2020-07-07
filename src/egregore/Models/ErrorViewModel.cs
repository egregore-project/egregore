namespace egregore.Models
{
    public sealed class ErrorViewModel
    {
        public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
        public string RequestId { get; set; }
    }
}
