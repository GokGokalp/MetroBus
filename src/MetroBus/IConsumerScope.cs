using System;
using System.Collections.Generic;
using System.Text;

namespace MetroBus
{
    public interface IConsumerScope : IDisposable
    {
        IConsumerScope CreateConsumerScope();
        TType CreateConsumerInstance<TType>();
        object CreateConsumerInstance(Type type);
    }
}