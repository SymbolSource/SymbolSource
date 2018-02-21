using System.Collections.Generic;

namespace SymbolSource.Processor.Legacy.Projects
{
    public class AddInfoBaseVisitor : IAddInfoVisitor
    {
        public virtual IEnumerable<IBinaryInfo> Visit(IEnumerable<IBinaryInfo> binaryInfos)
        {
            foreach (var binaryInfo in binaryInfos)
                Visit(binaryInfo);
            return binaryInfos;
        }

        public virtual IBinaryInfo Visit(IBinaryInfo binaryInfo)
        {
            if (binaryInfo.SymbolInfo!=null)
                Visit(binaryInfo.SymbolInfo);
            if (binaryInfo.DocumentationInfo != null)
                Visit(binaryInfo.DocumentationInfo);
            return binaryInfo;
        }

        public virtual IDocumentationInfo Visit(IDocumentationInfo documentInfo)
        {
            return documentInfo;
        }

        public virtual ISymbolInfo Visit(ISymbolInfo symbolInfo)
        {
            Visit(symbolInfo.SourceInfos);
            return symbolInfo;
        }

        public virtual IEnumerable<ISourceInfo> Visit(IEnumerable<ISourceInfo> sourceInfos)
        {
            foreach (var sourceInfo in sourceInfos)
                Visit(sourceInfo);
            return sourceInfos;
        }

        public virtual ISourceInfo Visit(ISourceInfo sourceInfo)
        {
            return sourceInfo;
        }      
    }
}
