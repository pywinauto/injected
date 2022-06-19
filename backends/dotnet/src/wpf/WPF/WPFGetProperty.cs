using System;
using System.Collections.Generic;
using System.Windows;
using System.Reflection;

namespace InjectedWorker.WPF
{
    class WPFGetProperty : GetProperty
    {
        public override Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args)
        {
            CheckParamExists(args, "element_id");
            CheckValidControlId<T>(args["element_id"], controls);
            CheckParamExists(args, "name");

            object c = controls.GetControl(args["element_id"]);

            bool isExists = false;
            DynamicValueReply reply = null;
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                PropertyInfo prop = c.GetType().GetProperty(args["name"]);
                FieldInfo field = c.GetType().GetField(args["name"]);
                if (prop != null)
                {
                    isExists = true;
                    reply = new DynamicValueReply(prop.GetValue(c, null));
                }
                else if (field != null)
                {
                    isExists = true;
                    reply = new DynamicValueReply(field.GetValue(c));
                }
            });
            if (isExists)
                return reply;
            else
                throw new ErrorReplyException(ErrorCodes.NOT_FOUND, "no such field or property: " + args["name"]);
        }
    }
}
