using System.Collections.Generic;

namespace SymbolSource.Processor.Legacy.Projects
{
    public class BinaryInfo : IBinaryInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public string Hash { get; set; }
        public string SymbolHash { get; set; }

        public IPackageEntry File { get; set; }
        public IDocumentationInfo DocumentationInfo { get; set; }
        public ISymbolInfo SymbolInfo { get; set; }
    }

    public class DocumentationInfo : IDocumentationInfo
    {
        public string Type { get; set; }
        public IPackageEntry File { get; set; }
    }

    public class SymbolInfo : ISymbolInfo
    {
        public string Type { get; set; }
        public string Hash { get; set; }

        public IPackageEntry File { get; set; }
        public IEnumerable<ISourceInfo> SourceInfos { get; set; }
    }

    public class SourceInfo : ISourceInfo
    {
        public string OriginalPath { get; set; }
        public IPackageEntry File { get; set; }
        public string Hash { get; set; }
    }
}
