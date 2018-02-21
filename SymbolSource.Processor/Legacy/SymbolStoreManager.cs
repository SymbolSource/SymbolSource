using System;
using System.Globalization;
using System.IO;
using Microsoft.Cci.Pdb;

namespace SymbolSource.Processor.Legacy
{
    public class SymbolStoreManager : ISymbolStoreManager
    {
        public string ReadHash(Stream stream)
        {
            return stream.Execute(ReadHashInternal);
        }

        public void WriteHash(string pdbFilePath, string hash)
        {
            WriteHashInternal(pdbFilePath, hash);
        }

        private string ReadHashInternal(string pdbFileHash)
        {
            var bits = new BitAccess(0);

            using (var read = File.Open(pdbFileHash, FileMode.Open))
            {
                var head = new PdbFileHeader(read, bits);
                var reader = new PdbReader(read, head.pageSize);
                var dir = new MsfDirectory(reader, head, bits);

                bits.MinCapacity(28);
                reader.Seek(dir.streams[1].pages[0], 0);
                reader.Read(bits.Buffer, 0, 28);

                int ver;
                int sig;
                int age;
                Guid guid;
                bits.ReadInt32(out ver); //  0..3  Version
                bits.ReadInt32(out sig); //  4..7  Signature
                bits.ReadInt32(out age); //  8..11 Age
                bits.ReadGuid(out guid); // 12..27 GUID
                return (guid.ToString("N") + age.ToString("x")).ToUpper();
            }
        }


        private void WriteHashInternal(string pdbFileHash, string hash)
        {
            var bits = new BitAccess(0);

            var guid = new Guid(hash.Remove(32));
            int age = int.Parse(hash.Substring(32), NumberStyles.HexNumber);

            using (var read = File.Open(pdbFileHash, FileMode.Open))
            {
                var head = new PdbFileHeader(read, bits);
                var reader = new PdbReader(read, head.pageSize);
                var dir = new MsfDirectory(reader, head, bits);

                reader.Seek(dir.streams[1].pages[0], 8); //bo przeskakujemy 8 znaków na ver i sig

                using (var writer = new BinaryWriter(read))
                {
                    writer.Write(age);
                    writer.Write(guid.ToByteArray());
                }
            }
        }
    }
}