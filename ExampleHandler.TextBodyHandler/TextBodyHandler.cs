using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ComponentModel.Composition;
using System.Messaging;

using MessageHandlerContracts = Splunk.ModularInput.Msmq.MessageHandlerContracts;

namespace ExampleHandler.TextBodyHandler
{
    enum EscapeMode { None, AutoEscaped };

    [Export("ExampleHandler.TextBodyHandler", typeof(MessageHandlerContracts.IMessageHandler))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TextBodyHandler : MessageHandlerContracts.IMessageHandler
    {       
        private Encoding Encoding { get; set; }
        private EscapeMode BodyEscapeMode { get; set; }

        public System.Messaging.IMessageFormatter MessageBodyFormatter { get; private set; }
        public System.Messaging.MessagePropertyFilter MessageReadPropertyFilter { get; private set; }

        private StringBuilder _sb = new StringBuilder();

        [ImportingConstructor]
        public TextBodyHandler()
        {  
            MessageReadPropertyFilter = new MessagePropertyFilter();
            MessageReadPropertyFilter.ClearAll();

            MessageReadPropertyFilter.DestinationQueue = true;
            MessageReadPropertyFilter.Id = true;
            MessageReadPropertyFilter.Label = true;
            MessageReadPropertyFilter.Priority = true;
            MessageReadPropertyFilter.MessageType = true;
            MessageReadPropertyFilter.ArrivedTime = true;
            MessageReadPropertyFilter.SentTime = true;
            MessageReadPropertyFilter.Body = true;
        }

        public void SetConfiguration(string handlerArgs)
        {
            var argkv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (handlerArgs == null)
            {
                handlerArgs = String.Empty;
            }

            // quick hack to support extracting key=value and key="value"
            handlerArgs = handlerArgs.Trim();
            string[] args;
            try
            {
                args = System.Text.RegularExpressions.Regex.Split(handlerArgs, "(?<!=\"[^\"]*?)\\s+");
            }
            catch
            {
                throw new ArgumentException(String.Format("Failed splitting handler_args"));
            }

            foreach (var arg in args)
            {
                var match = System.Text.RegularExpressions.Regex.Match(arg, "^(?<key>.*?)=\"?(?<value>.*?)\"?$");
                if (!match.Success)
                {
                    throw new ArgumentException("Failed parsing handler_args");

                }
                string val;
                if (argkv.TryGetValue(match.Groups["key"].Value, out val))
                {
                    throw new ArgumentException(String.Format("Duplicate key: {0}", match.Groups["key"].Value));
                }
                argkv.Add(match.Groups["key"].Value, match.Groups["value"].Value);
            }

            var validArgs = new List<string>(new String[] { "encoding", "escape_mode" });
            foreach (var key in argkv.Keys)
            {
                if (!validArgs.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(String.Format("Invalid key specified in handler_args: {0}", key));
                }

                // thrown errors will be caught/reported as a misconfiguration.
                if (key.Equals("encoding", StringComparison.OrdinalIgnoreCase))
                {
                    Encoding = System.Text.Encoding.GetEncoding(argkv[key]);
                }

                if (key.Equals("escape_mode", StringComparison.OrdinalIgnoreCase))
                {
                    BodyEscapeMode = (EscapeMode)Enum.Parse(typeof(EscapeMode), argkv[key], true);   
                }
                //default
                if (Encoding == null)
                    Encoding = Encoding.UTF8;

                MessageBodyFormatter = new TextFormatter(Encoding);
            }
        }

        private string ConvertToDateTimeOffsetString(DateTimeOffset dto)
        {
            // 'zzz' TimeFormatSpecifier remains static for the original base offset
            if (dto.Offset.Ticks != 0)
            {
                dto = new DateTimeOffset(dto.UtcDateTime);
            }
            return dto.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
        }

        public string GetSplunkEventData(System.Messaging.Message msg)
        {
            _sb.Clear();
            _sb.AppendLine(String.Format("DestinationQueue=\"{0}\"", msg.DestinationQueue.Path));
            _sb.AppendLine(String.Format("Id=\"{0}\"", msg.Id));
            _sb.AppendLine(String.Format("Label=\"{0}\"", msg.Label));
            _sb.AppendLine(String.Format("Priority=\"{0}\" MessageType=\"{1}\"", msg.Priority.ToString(), msg.MessageType.ToString()));
            _sb.AppendLine(String.Format("SentTime=\"{0}\"", ConvertToDateTimeOffsetString(msg.SentTime)));
            _sb.AppendLine(String.Format("ArrivedTime=\"{0}\"", ConvertToDateTimeOffsetString(msg.ArrivedTime)));

            if (MessageBodyFormatter.CanRead(msg))
            {
                if (BodyEscapeMode == EscapeMode.AutoEscaped)
                {
                    _sb.AppendLine(String.Format("Body=\"{0}\"", msg.Body.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"")));
                }
                else
                {
                     _sb.AppendLine(String.Format("Body=\"{0}\"", msg.Body));
                }
            }

            return _sb.ToString();
        }

    }
}
