using System;
using System.Collections.Generic;
using System.Windows;
using System.Reflection;

namespace InjectedWorker.WPF
{
    class WPFGetProperties : GetProperties
    {
        public override Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args)
        {
            CheckParamExists(args, "element_id");
            CheckValidControlId<T>(args["element_id"], controls);

            object c = controls.GetControl(args["element_id"]);

            DynamicValueReply reply = null;

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                Dictionary<string, string> props = new Dictionary<string, string>();
                foreach (PropertyInfo prop in c.GetType().GetProperties())
                {
                    props.Add(prop.Name, prop.PropertyType.Name);
                }
                foreach (FieldInfo field in c.GetType().GetFields())
                {
                    // can a property and a field have same name?
                    if (!props.ContainsKey(field.Name))
                    {
                        props.Add(field.Name, field.FieldType.Name);
                    }
                }
                reply = new DynamicValueReply(props);
            });
            return reply;
        }
    }
}
