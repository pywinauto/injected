using System;
using System.Collections.Generic;
using System.Windows;
using System.Reflection;

namespace InjectedWorker.WPF
{
    class WPFInvokeMethod : InvokeMethod<DependencyObject>
    {
        public override Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args)
        {
            CheckParamExists(args, "element_id");
            CheckValidControlId<T>(args["element_id"], controls);
            CheckParamExists(args, "name");

            object c = controls.GetControl(args["element_id"]);

            // TODO params support (array of pairs function_type-value?)
            MethodInfo method = c.GetType().GetMethod(args["name"]);

            dynamic ret = null;
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                ret = method.Invoke(c, null);
            });
            return new DynamicValueReply(ret);
        }
    }
}
