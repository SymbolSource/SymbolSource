using System.IO;
using System.Threading.Tasks;

namespace SymbolSource.Contract.Storage.Local
{
    public class LinkFileEntity
    {
        public string UserName { get; set; }
    }

    public class LinkFile : StorageFile
    {
        public LinkFile(string path, int depth)
            : base(path, depth)
        {
        }

        public async Task<LinkFileEntity> Retrieve()
        {
            if (!Exists())
                return null;

            using (var reader = File.OpenText(path))
            {
                return new LinkFileEntity
                {
                    UserName = await reader.ReadLineAsync()
                };
            }
        }

        public async Task Store(LinkFileEntity entity)
        {
            using (var writer = File.CreateText(path))
            {
                await writer.WriteLineAsync(entity.UserName).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }
    }
}