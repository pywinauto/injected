using System;
using System.Collections.Generic;
using System.Windows;
using System.Reflection;

namespace InjectedWorker.WPF
{
    class WPFSetProperty : SetProperty<DependencyObject>
    {
        public override Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args)
        {
            CheckParamExists(args, "element_id");
            CheckValidControlId<T>(args["element_id"], controls);
            CheckParamExists(args, "name");
            CheckParamExists(args, "value");

            object c = controls.GetControl(args["element_id"]);

            bool isExists = false;

            Reply reply = null;

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                try
                {
                    PropertyInfo prop = c.GetType().GetProperty(args["name"]);
                    FieldInfo field = c.GetType().GetField(args["name"]);
                    if (prop != null)
                    {
                        isExists = true;

                        dynamic value = args["value"];
                        if (args.ContainsKey("is_enum") && args["is_enum"])
                        {
                            value = Enum.Parse(prop.PropertyType, args["value"]);
                        }

                        try
                        {
                            value = ConvertType(value, prop.PropertyType);
                        }
                        catch (Exception e)
                        {
                            reply = new ErrorReply(ErrorCodes.UNSUPPORTED_TYPE, "can not convert to property type: " + e.ToString());
                        }
                        prop.SetValue(c, value, null);
                    }
                    else if (field != null)
                    {
                        isExists = true;
                        dynamic value = args["value"];
                        if (args.ContainsKey("is_enum") && args["is_enum"])
                        {
                            value = Enum.Parse(field.FieldType, args["value"]);
                        }

                        try
                        {
                            value = ConvertType(value, field.FieldType);
                        }
                        catch (Exception e)
                        {
                            reply = new ErrorReply(ErrorCodes.UNSUPPORTED_TYPE, "can not convert to field type: " + e.ToString());
                        }

                        field.SetValue(c, value);
                    }

                    reply = new Reply(ErrorCodes.OK);
                }
                catch (Exception e)
                {
                    reply = new ErrorReply(ErrorCodes.RUNTIME_ERROR, e.ToString());
                }
            });
            if (isExists)
                return reply;
            else
                throw new ErrorReplyException(ErrorCodes.NOT_FOUND, "no such field or property: " + args["name"]);
        }
    }
}
