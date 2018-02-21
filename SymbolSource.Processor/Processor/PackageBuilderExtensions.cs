using System.IO;
using NuGet;

namespace SymbolSource.Processor.Processor
{
    public static class PackageBuilderExtensions
    {
        public static void SaveBuffered(this PackageBuilder builder, Stream stream)
        {
            using (var buffer = new MemoryStream())
            {
                builder.Save(buffer);
                buffer.Position = 0;
                buffer.CopyTo(stream);
            }
        }
    }
}