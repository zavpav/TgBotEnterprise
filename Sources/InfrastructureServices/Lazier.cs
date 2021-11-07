﻿using System;
using Microsoft.Extensions.DependencyInjection;

namespace CommonInfrastructure
{
    public class Lazier<T> : Lazy<T>
        where T : class
    {
        public Lazier(IServiceProvider provider)
            : base(provider.GetRequiredService<T>)
        {
        }
    }
}