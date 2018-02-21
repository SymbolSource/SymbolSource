using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Cci.Pdb;

namespace SymbolSource.Processor.Legacy
{
    public class ManagedSourceExtractor : ISourceExtractor
    {
        public IList<string> ReadSources(Stream pdbStream)
        {
            var result = new List<string>();
            Dictionary<uint, PdbTokenLine> mapping;

            foreach (var obj1 in PdbFile.LoadFunctions(pdbStream, out mapping))
                if (obj1.lines != null)
                    foreach (var obj2 in obj1.lines)
                        result.Add(obj2.file.name); 
           
            return result.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        }
    }
}