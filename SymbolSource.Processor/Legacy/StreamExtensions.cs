using System;
using System.IO;

namespace SymbolSource.Processor.Legacy
{
    public static class StreamExtensions
    {
        public static void CopyTo(this MemoryStream src, Stream dest)
        {
            dest.Write(src.GetBuffer(), (int) src.Position, (int) (src.Length - src.Position));
        }

        public static void CopyTo(this Stream src, MemoryStream dest)
        {
            if (src.CanSeek)
            {
                var pos = (int) dest.Position;
                int length = (int) (src.Length - src.Position) + pos;
                dest.SetLength(length);

                while (pos < length)
                    pos += src.Read(dest.GetBuffer(), pos, length - pos);
            }
            else
                src.CopyTo(dest);
        }

        public static T Execute<T>(this Stream stream, Func<string, T> action)
        {
            return Execute(stream, action, Path.GetRandomFileName());
        }

        public static T Execute<T>(this Stream stream, Func<string, T> action, string fileName)
        {
            var folderPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var tempPath = Path.Combine(folderPath, fileName);

            Directory.CreateDirectory(folderPath);
            stream.Seek(0, SeekOrigin.Begin);
            using (var fileStream = File.OpenWrite(tempPath))
                stream.CopyTo(fileStream);

            var result = action(tempPath);
            
            File.Delete(tempPath);
            Directory.Delete(folderPath);
            
            return result;
        }

    }
}