using System;

namespace MetroBus
{
    public interface IConsumerScope : IDisposable
    {
        IConsumerScope CreateConsumerScope();
        TType CreateConsumerInstance<TType>();
        object CreateConsumerInstance(Type type);
    }
}