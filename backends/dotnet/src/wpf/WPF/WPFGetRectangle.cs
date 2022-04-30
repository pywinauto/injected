using System;
using System.Collections.Generic;
using System.Windows;

namespace InjectedWorker.WPF
{
    class WPFGetRectangle : GetRectangle<DependencyObject>
    {
        public override Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args)
        {
            CheckParamExists(args, "element_id");
            CheckValidControlId<T>(args["element_id"], controls);

            dynamic c = controls.GetControl(args["element_id"]);
            DynamicValueReply reply = new DynamicValueReply(this.GetRect(c));
            return reply;
        }

        protected Rect GetRect(FrameworkElement control)
        {
            Rect rect = new Rect(0,0,0,0);
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                // avoid InvalidOperationException "This Visual is not connected to a PresentationSource."
                // Usually this means that control exists, but not rendered yet (as example, placed on an other tab of TabControl)
                if (PresentationSource.FromVisual(control) != null)
                {
                    Point topLeft = control.PointToScreen(new Point(0, 0));
                    rect = new Rect(topLeft.X, topLeft.Y, control.ActualWidth, control.ActualHeight);
                }
            });
            return rect;
        }

        protected Rect GetRect(Window control)
        {
            Rect rect = new Rect(0, 0, 0, 0);
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                rect = new Rect(control.Left, control.Top, control.ActualWidth, control.ActualHeight);
            });
            return rect;
        }

        protected Rect GetRect(object c)
        {
            return new Rect(0,0,0,0);
        }
    }
}
