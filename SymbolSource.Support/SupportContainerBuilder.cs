using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using SymbolSource.Contract.Support;

namespace SymbolSource.Support
{
    public class SupportContainerBuilder
    {
        public static void Register(ContainerBuilder builder, SupportEnvironment environment)
        {
            builder.RegisterType<SupportConfiguration>()
                .WithParameter(TypedParameter.From(environment))
                .As<ISupportConfiguration>();

            builder.RegisterType<SupportService>()
                .As<ISupportService>();
        }
    }
}
