using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleHandler.TextBodyHandler
{
    public class TextFormatter : System.Messaging.IMessageFormatter
    {

        public Encoding Encoding { get; set; }

        public TextFormatter()
            : this(Encoding.UTF8) { }

        public TextFormatter(Encoding encoding)
        {
            Encoding = encoding;
        }

        public object Clone()
        {
            return new TextFormatter();
        }

        public void Write(System.Messaging.Message msg, object obj)
        {
            throw new NotImplementedException();
        }

        public bool CanRead(System.Messaging.Message msg)
        {
            return msg.BodyStream != null;
        }

        public object Read(System.Messaging.Message msg)
        {
            if (!CanRead(msg))
            {
                throw new ArgumentNullException();
            }

            msg.BodyStream.Position = 0;

            using (var sr = new System.IO.StreamReader(msg.BodyStream, Encoding))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
