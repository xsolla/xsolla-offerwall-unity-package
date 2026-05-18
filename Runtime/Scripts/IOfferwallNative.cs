using System;

namespace Xsolla.Offerwall
{
    /// <summary>
    /// Interface for platform-specific offerwall implementations.
    /// </summary>
    internal interface IOfferwallNative
    {
        void Show(OfferwallSettings settings, Action<string> onDismissed);
        void Dismiss();
    }
}
