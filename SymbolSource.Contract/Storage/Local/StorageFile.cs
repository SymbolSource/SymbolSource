using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SymbolSource.Contract.Storage.Local
{
    public class StorageFile
    {
        protected readonly string path;
        private readonly int depth;

        public StorageFile(string path, int depth)
        {
            this.path = path;
            this.depth = depth;
        }

        public void EnsureParent()
        {
            var file = new FileInfo(path);
            file.Directory.Create();
        }

        public bool Exists()
        {
            return File.Exists(path);
        }

        public Stream OpenRead()
        {
            if (!File.Exists(path))
                return null;

            return File.OpenRead(path);
        }

        public Stream OpenWrite()
        {
            return File.OpenWrite(path);
        }

        public void MoveTo(StorageFile destFile)
        {
            destFile.EnsureParent();
            File.Move(path, destFile.path);
            Delete();
        }

        public void CopyTo(StorageFile destFile)
        {
            destFile.EnsureParent();
            File.Copy(path, destFile.path);
        }

        public bool Delete()
        {
            var file = new FileInfo(path);
            var exists = file.Exists;

            if (exists)
                file.Delete();

            var directory = file.Directory;
            var depth = this.depth;

            while (true)
            {
                if (directory == null || !directory.Exists)
                    break;

                Debug.WriteLine("Trying to delete {0}", directory);
                var children = directory.EnumerateFileSystemInfos();

                using ((IDisposable)children)
                    if (children.Any())
                        break;

                directory.Delete();
                depth--;

                if (depth == 0)
                    break;

                directory = directory.Parent;
            }

            return exists;
        }
    }
}