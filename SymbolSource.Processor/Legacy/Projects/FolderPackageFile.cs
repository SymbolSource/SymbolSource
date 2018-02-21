using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SymbolSource.Processor.Legacy.Projects
{
    public class FolderPackageFile : IPackageFile
    {
        private readonly string path;

        public FolderPackageFile(string path)
        {
            this.path = path;
        }

        public IEnumerable<IPackageEntry> Entries
        {
            get
            {
                return GetAllFolders(path)
                    .SelectMany(Directory.EnumerateFiles)
                    .Select(f => new FilePackageEntry(f));
            }
        }

        private IEnumerable<string> GetAllFolders(string currentPath)
        {
            return Directory.EnumerateDirectories(currentPath)
                .SelectMany(GetAllFolders);
                
        }

        private class FilePackageEntry : IPackageEntry
        {
            private readonly string path;

            public FilePackageEntry(string path)
            {
                this.path = path;
            }

            public string FullPath
            {
                get { return path; }
            }

            public Stream GetStream()
            {
                return File.OpenRead(path);
            }
        }
    }
}