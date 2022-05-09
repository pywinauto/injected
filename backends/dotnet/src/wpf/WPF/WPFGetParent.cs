using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace InjectedWorker.WPF
{
    class WPFGetParent : GetParent<DependencyObject>
    {
        public override Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args)
        {
            CheckParamExists(args, "element_id");
            CheckValidControlId<T>(args["element_id"], controls);
 
            object c = controls.GetControl(args["element_id"]);

            DynamicValueReply reply = null;
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                Visual control = c as Visual;
                if (control != null)
                {
                    DependencyObject parentControl = VisualTreeHelper.GetParent(control);
                    if (parentControl != null)
                    {
                        reply = new DynamicValueReply((controls as ControlsStorage<DependencyObject>).RegisterControl(parentControl));
                    }
                    else
                    {
                        reply = new DynamicValueReply(null);
                    }
                }
                else
                {
                    reply = new DynamicValueReply(null);
                }

            });

            return reply;
        }
    }
}
