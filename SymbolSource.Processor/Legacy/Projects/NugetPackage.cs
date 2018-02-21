using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet;

namespace SymbolSource.Processor.Legacy.Projects
{
    public class NugetPackage : IPackageFile
    {
        private readonly IPackage package;

        public NugetPackage(IPackage package)
        {
            this.package = package;
        }

        public IEnumerable<IPackageEntry> Entries
        {
            get { return package.GetFiles().Select(f => new NugetFile(f)); }
        }

        private class NugetFile : IPackageEntry
        {
            private readonly NuGet.IPackageFile file;

            public NugetFile(NuGet.IPackageFile file)
            {
                this.file = file;
            }

            public string FullPath
            {
                get
                {
                    //?return HttpUtility.UrlDecode(file.FilePath);
                    return file.Path;
                }
            }

            public Stream GetStream()
            {
                return file.GetStream();
            }
        }
    }
}
