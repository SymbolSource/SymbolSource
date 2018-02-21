using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SymbolSource.Processor.Legacy
{
    public interface IProgramComponent
    {
        string Name { get; }
        Stream GetStream();
    }

    public class EmbeddedProgramComponent : IProgramComponent
    {
        private readonly string name;
        private readonly Assembly assembly;
        private readonly string resourceName;

        public static IEnumerable<IProgramComponent> GetAll(Type type, string prefix)
        {
            prefix = type.Namespace + "." + prefix.Trim('/').Replace('/', '.') + ".";

            return type.Assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(prefix))
                .Select(name => new EmbeddedProgramComponent(name.Replace(prefix, ""), type.Assembly, name));
        }
        
        private EmbeddedProgramComponent(string name, Assembly assembly, string resourceName)
        {
            this.name = name;
            this.assembly = assembly;
            this.resourceName = resourceName;
        }

        public string Name
        {
            get { return name; }
        }

        public Stream GetStream()
        {
            return assembly.GetManifestResourceStream(resourceName);
        }
    }
}