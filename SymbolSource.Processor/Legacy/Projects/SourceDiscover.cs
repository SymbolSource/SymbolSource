using System;
using System.Collections.Generic;
using System.Linq;

namespace SymbolSource.Processor.Legacy.Projects
{
    public class SourceDiscover
    {
        private readonly ISourceExtractor pdbSourceExtractor;
        private readonly ISourceStoreManager sourceStoreManager;

        public SourceDiscover(ISourceExtractor pdbSourceExtractor, ISourceStoreManager sourceStoreManager)
        {
            this.pdbSourceExtractor = pdbSourceExtractor;
            this.sourceStoreManager = sourceStoreManager;
        }

        public IEnumerable<ISourceInfo> FindSources(IList<IPackageEntry> fileInfos, ISymbolInfo pdbFile)
        {
            IList<string> originalPaths;
            using (var pdbStream = pdbFile.File.GetStream())
                originalPaths = pdbSourceExtractor.ReadSources(pdbStream);

            var files = fileInfos
                .Select(f =>
                    new
                    {
                        Split = f.FullPath.ToLowerInvariant().Split(new []{'\\', '/'}, StringSplitOptions.RemoveEmptyEntries),
                        Info = f,
                    }
                )
                .ToArray();

            foreach (var originalPath in originalPaths)
            {
                var path = originalPath.ToLowerInvariant().Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

                var proposition = files
                    .Select(f => new
                    {
                        Level = ComputeLevel(path, f.Split),
                        f.Info
                    })
                    .Where(f => f.Level > 0)
                    .OrderByDescending(f => f.Level)
                    .Select(f => f.Info)
                    .FirstOrDefault();

                if (proposition != null)
                {
                    using (var stream = proposition.GetStream())
                        yield return new SourceInfo()
                        {
                            OriginalPath = originalPath,
                            File = proposition,
                            Hash = sourceStoreManager.ReadHash(stream)
                        };
                }
            }
        }

        private int ComputeLevel(string[] original, string[] proposition)
        {
            int levels = Math.Min(original.Length, proposition.Length);

            int level = 0;

            for (int i = 0; i < levels; i++, level++)
            {
                int originalPosition = original.Length - i - 1;
                int propositionPosition = proposition.Length - i - 1;

                if (original[originalPosition] != proposition[propositionPosition])
                    break;
            }

            return level;
        }
    }
}
