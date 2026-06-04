namespace Xsolla.Offerwall
{
    /// <summary>
    /// Represents an error returned by the Xsolla Offerwall SDK.
    /// </summary>
    public class XOError
    {
        /// <summary>
        /// A human-readable description of the error.
        /// </summary>
        public string Message { get; }

        public XOError(string message)
        {
            Message = message;
        }

        public override string ToString() => Message;
    }
}
