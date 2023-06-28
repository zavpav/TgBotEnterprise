using System;

namespace CommonInfrastructure
{
    public class Lazier<T> : Lazy<T>
        where T : class
    {
        public Lazier(IServiceProvider provider)
            : base(
                  () => (T?)provider.GetService(typeof(T)) 
                        ?? throw new NotSupportedException($"Can't resolve service '{typeof(T)}'.")
             )
        {
        }
    }
}