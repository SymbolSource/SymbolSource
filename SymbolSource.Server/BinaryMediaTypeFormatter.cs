using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SymbolSource.Server
{
    public class BinaryMediaTypeFormatter : MediaTypeFormatter
    {
        public BinaryMediaTypeFormatter()
        {
            MediaTypeMappings.Add(new DefaultMediaTypeMapping("application/octet-stream"));
        }
        
        public override bool CanReadType(Type type)
        {
            return false;
        }

        public override bool CanWriteType(Type type)
        {
            return type == typeof(StreamContent);
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content,
            TransportContext transportContext)
        {
            if (type != typeof(StreamContent))
                throw new NotSupportedException(type.FullName);

            var streamContent = (StreamContent)value;
            return streamContent.CopyToAsync(writeStream, transportContext);
        }
    }

    public class DefaultMediaTypeMapping : MediaTypeMapping
    {
        public DefaultMediaTypeMapping(string mediaType) 
            : base(mediaType)
        {
        }


        public override double TryMatchMediaType(HttpRequestMessage request)
        {
            if (request.Content.Headers.ContentType == null)
                return 1.0;

            return 0.0;
        }
    }
}