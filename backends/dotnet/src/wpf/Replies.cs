using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace InjectedWorker
{
    public class Reply
    {
        public Reply(ErrorCodes statusCode)
        {
            Fields["status_code"] = (long)statusCode;
        }

        // TODO incapsulation
        public Dictionary<string, dynamic> Fields = new Dictionary<string, dynamic>();
    }

    class ElementsReply : Reply
    {
        public ElementsReply(IEnumerable<long> elements)
            : base(ErrorCodes.OK)
        {
            Fields["elements"] = new List<long>(elements);
        }
    }

    class DynamicValueReply : Reply
    {
        public DynamicValueReply(dynamic value)
            : base(ErrorCodes.OK)
        {
            if (value == null || value.GetType().IsPrimitive || value.GetType() == typeof(string))
                Fields["value"] = value;
            else
                Serialize(value);
        }

        protected void Serialize(object o)
        {
            if (o == null)
            {
                Fields["value"] = "";
            }
            Fields["value"] = o.ToString();
        }
        protected void Serialize(Rect rect)
        {
            Fields["left"] = (int)rect.Left;
            Fields["right"] = (int)rect.Right;
            Fields["top"] = (int)rect.Top;
            Fields["bottom"] = (int)rect.Bottom;
        }

    }

    // ========= Error handling =============

    public enum ErrorCodes : int
    {
        OK,
        PARSE_ERROR,
        UNSUPPORTED_ACTION,
        MISSING_PARAM,
        RUNTIME_ERROR,
        NOT_FOUND,
        UNSUPPORTED_TYPE,
        INVALID_VALUE,
    }

    class ErrorReply : Reply
    {
        public ErrorReply(ErrorCodes statusCode, string message)
            : base(statusCode)
        {
            Fields["message"] = message;
        }
    }
    class ErrorReplyException : Exception
    {
        public ErrorReply Reply;

        public ErrorReplyException(ErrorCodes statusCode, string message)
        {
            Reply = new ErrorReply(statusCode, message);
        }
    }
}
