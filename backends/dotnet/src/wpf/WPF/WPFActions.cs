using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Reflection;

namespace InjectedWorker.WPF
{
    class WPFGetChildren : GetChildrenAction<DependencyObject>
    {
        private ControlsStorage<DependencyObject> ControlsStrorage;

        public override Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args)
        {
            ControlsStrorage = controls as ControlsStorage<DependencyObject>;

            CheckParamExists(args, "element_id");
            CheckValidControlId<T>(args["element_id"], null);

            if (args["element_id"] == 0)
            {
                return new ElementsReply(GetWindows());
            }
            else
            {
                CheckValidControlId<T>(args["element_id"], controls);
                dynamic c = controls.GetControl(args["element_id"]);
                if (c == null)
                    throw new ErrorReplyException(ErrorCodes.NOT_FOUND, "element not found: " + args["element_id"]);
                else
                    return new ElementsReply(GetChildrenOf(c));
            }
        }

        public List<long> GetWindows()
        {
            var ret = new List<long>();

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                foreach (Window w in Application.Current.Windows)
                {
                    ret.Add(ControlsStrorage.RegisterControl(w));
                }
            });

            return ret;
        }

        public List<long> GetChildrenOf(DependencyObject o)
        {
            var ret = new List<long>();
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
                {
                    var child = VisualTreeHelper.GetChild(o, i);
                    ret.Add(ControlsStrorage.RegisterControl(child));
                }
            });
            return ret;
        }

        public List<long> GetChildrenOf(ContentControl o)
        {
            List<long> ret = null;
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                ret = new List<long>();
                if (o.Content is DependencyObject)
                    ret.Add(ControlsStrorage.RegisterControl(o.Content as DependencyObject));
            });
            return ret;
        }

        public List<long> GetChildrenOf(ItemsControl o)
        {
            List<long> ret = null;
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                ret = new List<long>();
                foreach (var item in o.Items)
                {
                    if (item is DependencyObject)
                        ret.Add(ControlsStrorage.RegisterControl(item as DependencyObject));
                }
            });
            return ret;
        }
    }

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
                FrameworkElement control = c as FrameworkElement;
                if (control != null)
                {
                    DependencyObject parentControl = control.Parent;
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

    class WPFGetProperty : GetProperty<DependencyObject>
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

        protected Rect GetRect(object c)
        {
            return new Rect(0,0,0,0);
        }
    }

    class WPFGetHandle : GetHandle<DependencyObject>
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

    class WPFGetControlType : GetControlType<DependencyObject>
    {
        public WPFGetControlType()
        {
            this.DefaultType = "Custom";

            this.KnownTypes.Add(typeof(System.Windows.Window), "Window");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Panel), "Pane");
            this.KnownTypes.Add(typeof(System.Windows.Controls.ToolBar), "ToolBar");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Menu), "Menu");
            this.KnownTypes.Add(typeof(System.Windows.Controls.MenuItem), "MenuItem");
            this.KnownTypes.Add(typeof(System.Windows.Controls.TabControl), "TabControl");
            this.KnownTypes.Add(typeof(System.Windows.Controls.TabItem), "TabItem");
            this.KnownTypes.Add(typeof(System.Windows.Controls.TreeView), "TreeView");
            this.KnownTypes.Add(typeof(System.Windows.Controls.TreeViewItem), "TreeItem");
            this.KnownTypes.Add(typeof(System.Windows.Controls.ListBox), "ListBox");
            this.KnownTypes.Add(typeof(System.Windows.Controls.ListBoxItem), "ListItem");
            this.KnownTypes.Add(typeof(System.Windows.Controls.DataGrid), "DataGrid");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Label), "Static");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Button), "Button");
            this.KnownTypes.Add(typeof(System.Windows.Controls.TextBox), "Edit");
            this.KnownTypes.Add(typeof(System.Windows.Controls.ScrollViewer), "ScrollBar");
            this.KnownTypes.Add(typeof(System.Windows.Controls.CheckBox), "CheckBox");
            this.KnownTypes.Add(typeof(System.Windows.Controls.RadioButton), "RadioButton");
            this.KnownTypes.Add(typeof(System.Windows.Controls.PasswordBox), "Edit");
            this.KnownTypes.Add(typeof(System.Windows.Controls.RichTextBox), "Document");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Slider), "Slider");
        }

        public override Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args)
        {
            CheckParamExists(args, "element_id");
            CheckValidControlId<T>(args["element_id"], controls);

            object c = controls.GetControl(args["element_id"]);
            DynamicValueReply reply = new DynamicValueReply(FindControlType(c));
            return reply;
        }

        protected override string FindControlType(object obj)
        {
            return base.FindControlType(obj);
        }
    }

}
