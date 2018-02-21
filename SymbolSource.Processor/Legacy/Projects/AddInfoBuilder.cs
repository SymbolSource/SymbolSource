using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SymbolSource.Processor.Legacy.Projects
{
    public class AddInfoBuilder : IAddInfoBuilder
    {
        private readonly IBinaryStoreManager binaryStoreManager;
        private readonly ISymbolStoreManager symbolStoreManager;
        private readonly SourceDiscover sourceDiscover;

        public AddInfoBuilder(IBinaryStoreManager binaryStoreManager, ISymbolStoreManager symbolStoreManager, SourceDiscover sourceDiscover)
        {
            this.binaryStoreManager = binaryStoreManager;
            this.symbolStoreManager = symbolStoreManager;
            this.sourceDiscover = sourceDiscover;
        }

        public IEnumerable<IBinaryInfo> Build(IPackageFile directoryInfo)
        {
            var binaryExtensions = new[] { ".dll", ".exe", ".winmd" };

            var items = directoryInfo.Entries.ToArray();

            return items
                .Where(f => binaryExtensions.Any(e => f.FullPath.EndsWith(e, StringComparison.InvariantCultureIgnoreCase)))
                .Select(b => BuildBinaryInfo(items, b))
                ;
        }

        public IEnumerable<IBinaryInfo> Build(IPackageFile directoryInfo, IEnumerable<IPackageEntry> includeFiles)
        {
            var items = directoryInfo.Entries.ToArray();

            return includeFiles
                .Select(b => BuildBinaryInfo(items, b))
                ;
        }

        private IBinaryInfo BuildBinaryInfo(IList<IPackageEntry> fileInfos, IPackageEntry binaryFileInfo)
        {
            var binaryInfo = new BinaryInfo();

            binaryInfo.Name = Path.GetFileNameWithoutExtension(binaryFileInfo.FullPath);
            binaryInfo.Type = Path.GetExtension(binaryFileInfo.FullPath).Substring(1);
            binaryInfo.File = binaryFileInfo;

            using (var stream = binaryFileInfo.GetStream())
            {
                binaryInfo.Hash = binaryStoreManager.ReadBinaryHash(stream);
                stream.Seek(0, SeekOrigin.Begin);
                binaryInfo.SymbolHash = binaryStoreManager.ReadPdbHash(stream);
            }

            string symbolName = Path.ChangeExtension(binaryFileInfo.FullPath, "pdb");
            var symbolFileInfo = fileInfos.SingleOrDefault(s => s.FullPath == symbolName);
            if (symbolFileInfo != null)
            {
                var symbolInfo = new SymbolInfo();
                symbolInfo.Type = Path.GetExtension(symbolFileInfo.FullPath).Substring(1);
                symbolInfo.File = symbolFileInfo;

                using (var stream = symbolFileInfo.GetStream())
                    symbolInfo.Hash = symbolStoreManager.ReadHash(stream);

                symbolInfo.SourceInfos = sourceDiscover.FindSources(fileInfos, symbolInfo);
                binaryInfo.SymbolInfo = symbolInfo;
            }

            string documentationName = Path.ChangeExtension(binaryFileInfo.FullPath, "xml");
            var documentationFileInfo = fileInfos.SingleOrDefault(s => s.FullPath == documentationName);
            if (documentationFileInfo != null)
            {
                var documentationInfo = new DocumentationInfo();
                documentationInfo.Type = Path.GetExtension(documentationFileInfo.FullPath).Substring(1);
                documentationInfo.File = documentationFileInfo;
                binaryInfo.DocumentationInfo = documentationInfo;
            }

            return binaryInfo;
        }
    }
}