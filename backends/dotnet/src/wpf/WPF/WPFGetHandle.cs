using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace InjectedWorker.WPF
{
    class WPFGetHandle : GetHandle
    {
        public override Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args)
        {
            CheckParamExists(args, "element_id");
            CheckValidControlId<T>(args["element_id"], controls);

            DynamicValueReply reply = new DynamicValueReply(-1);

            dynamic c = controls.GetControl(args["element_id"]);
            if (c is Visual)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    HwndSource source = (HwndSource)HwndSource.FromVisual(c);
                    if (source != null)
                    {
                        IntPtr hWnd = source.Handle;
                        reply = new DynamicValueReply(hWnd.ToInt64());
                    }
                    else
                    {
                        reply = new DynamicValueReply(null);
                    }
                });
            }
            return reply;
        }
    }
}
