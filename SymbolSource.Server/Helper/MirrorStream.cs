using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SymbolSource.Server.Helper
{
    internal class MirrorStream : Stream
    {
        private readonly Stream primary;
        private readonly Stream secondary;

        public MirrorStream(Stream primary, Stream secondary)
        {
            this.primary = primary;
            this.secondary = secondary;
        }

        public override void Close()
        {
            primary.Close();
            secondary.Flush();
        }

        public override void Flush()
        {
            primary.Flush();
            secondary.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.WhenAll(
                primary.FlushAsync(cancellationToken),
                secondary.FlushAsync(cancellationToken));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            secondary.Seek(offset, origin);
            return primary.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            primary.SetLength(value);
            secondary.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return secondary.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return secondary.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            primary.Write(buffer, offset, count);
            secondary.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.WhenAll(
                primary.WriteAsync(buffer, offset, count, cancellationToken),
                secondary.WriteAsync(buffer, offset, count, cancellationToken));
        }

        public override bool CanRead
        {
            get { return secondary.CanRead; }
        }

        public override bool CanSeek
        {
            get { return primary.CanSeek && secondary.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return primary.CanWrite && secondary.CanWrite; }
        }

        public override long Length
        {
            get { return primary.Length; }
        }

        public override long Position
        {
            get { return primary.Position; }
            set
            {
                primary.Position = value;
                secondary.Position = value;
            }
        }
    }
}