﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
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

        protected IEnumerable<T> FindChildrenOfType<T>(DependencyObject o)
            where T : class
        {
            foreach (var child in GetChildrenByVisualTreeHelper(o))
            {
                T castedChild = child as T;
                if (castedChild != null)
                {
                    yield return castedChild;
                }
                else
                {
                    foreach (var internalChild in FindChildrenOfType<T>(child))
                    {
                        yield return internalChild;
                    }
                }
            }
        }

        protected IEnumerable<DependencyObject> GetChildrenByVisualTreeHelper(DependencyObject o)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(o);

            for (int i = 0; i < childCount; i++)
            {
                yield return VisualTreeHelper.GetChild(o, i);
            }
        }

        public List<long> GetChildrenOf(HeaderedContentControl o)
        {
            List<long> ret = null;
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                ret = new List<long>();
                if (o.Header is DependencyObject)
                    ret.Add(ControlsStrorage.RegisterControl(o.Header as DependencyObject));
                if (o.Content is DependencyObject)
                    ret.Add(ControlsStrorage.RegisterControl(o.Content as DependencyObject));
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

        public List<long> GetChildrenOf(HeaderedItemsControl o)
        {
            List<long> ret = null;
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                ret = new List<long>();
                if (o.Header is DependencyObject)
                    ret.Add(ControlsStrorage.RegisterControl(o.Header as DependencyObject));
                foreach (var item in o.Items)
                {
                    if (item is DependencyObject)
                        ret.Add(ControlsStrorage.RegisterControl(item as DependencyObject));
                }
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

        public List<long> GetChildrenOf(ListView o)
        {
            List<long> ret = null;

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                ret = new List<long>();
                foreach (var child in FindChildrenOfType<ListViewItem>(o))
                {
                    ret.Add(ControlsStrorage.RegisterControl(child as DependencyObject));
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
            this.KnownTypes.Add(typeof(System.Windows.Controls.TabControl), "Tab");
            this.KnownTypes.Add(typeof(System.Windows.Controls.TabItem), "TabItem");
            this.KnownTypes.Add(typeof(System.Windows.Controls.TreeView), "Tree");
            this.KnownTypes.Add(typeof(System.Windows.Controls.TreeViewItem), "TreeItem");
            this.KnownTypes.Add(typeof(System.Windows.Controls.ListBox), "List");
            this.KnownTypes.Add(typeof(System.Windows.Controls.ListBoxItem), "ListItem");
            this.KnownTypes.Add(typeof(System.Windows.Controls.DataGrid), "DataGrid");
            this.KnownTypes.Add(typeof(System.Windows.Controls.ComboBox), "ComboBox");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Label), "Text");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Button), "Button");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Primitives.ToggleButton), "Button");
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

    class WPFGetFocusedElement : GetFocusedElement<DependencyObject>
    {
        public override Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args)
        {
            DynamicValueReply reply = new DynamicValueReply(-1);

            // In WPF there are two main concepts that pertain to focus: keyboard focus and logical focus.
            // Keyboard focus is used here (more: https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/focus-overview?view=netframeworkdesktop-4.8)
            IInputElement inputElem = null;
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                inputElem = Keyboard.FocusedElement;
            });

            if (inputElem != null && inputElem is DependencyObject)
            {
                DependencyObject focusedElem = inputElem as DependencyObject;
                reply = new DynamicValueReply((controls as ControlsStorage<DependencyObject>).RegisterControl(focusedElem));
            }

            return reply;
        }
    }

    class WPFSetFocus : SetFocus<DependencyObject>
    {
        public override Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args)
        {
            CheckParamExists(args, "element_id");
            CheckValidControlId<T>(args["element_id"], controls);

            dynamic c = controls.GetControl(args["element_id"]);

            if (!(c is IInputElement))
            {
                throw new ErrorReplyException(ErrorCodes.UNSUPPORTED_ACTION, String.Format("Can not cast element with ID {0} to IInputElement. Not focusable?", args["element_id"]));
            }
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                Keyboard.Focus(c as IInputElement);
            });

            Reply reply = new Reply(ErrorCodes.OK);
            return reply;
        }
    }

    class WPFGetName : GetName<DependencyObject>
    {
        public override Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args)
        {
            CheckParamExists(args, "element_id");
            CheckValidControlId<T>(args["element_id"], controls);

            dynamic c = controls.GetControl(args["element_id"]);
            DynamicValueReply reply = null;
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                reply = new DynamicValueReply(GetNameString(c));
            });
            return reply;
        }

        protected virtual string GetNameString(object o)
        {
            if (o == null)
            {
                return "";
            }
            return o.ToString();
        }

        protected virtual string GetNameString(DependencyObject c)
        {
            foreach (string name in new List<string> { "Title" })
            {
                PropertyInfo prop = c.GetType().GetProperty(name);
                FieldInfo field = c.GetType().GetField(name);
                if (prop != null)
                {
                    return prop.GetValue(c, null).ToString();
                }
                else if (field != null)
                {
                    return field.GetValue(c).ToString();
                }
            }
            return "";
        }

        protected virtual string GetNameString(Window o)
        {
            if (o.Title == null)
            {
                return "";
            }
            return o.Title;
        }


        protected virtual string GetNameString(ContentControl o)
        {
            if (o.Content == null)
            {
                return "";
            }
            dynamic source = o.Content;
            return GetNameString(source);
        }

        protected virtual string GetNameString(HeaderedContentControl o)
        {
            if (o.Header == null)
            {
                return "";
            }
            dynamic source = o.Header;
            return GetNameString(source);
        }

        protected virtual string GetNameString(HeaderedItemsControl o)
        {
            if (o.Header == null)
            {
                return "";
            }
            dynamic source = o.Header;
            return GetNameString(source);
        }
    }

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

    class WPFRaiseEvent : RaiseEvent<DependencyObject>
    {
        public override Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args)
        {
            CheckParamExists(args, "element_id");
            CheckValidControlId<T>(args["element_id"], controls);
            CheckParamExists(args, "name");

            object c = controls.GetControl(args["element_id"]);

            if (!(c is UIElement))
            {
                throw new ErrorReplyException(ErrorCodes.UNSUPPORTED_ACTION, "This element is not UIElement");
            }

            bool isFound = false;
            for (Type t = c.GetType(); t != null; t = t.BaseType)
            {
                RoutedEvent[] events = EventManager.GetRoutedEventsForOwner(t);
                if (events != null)
                {
                    RoutedEvent e = Array.Find(events, x => x.Name == args["name"]);
                    if (e != null) 
                    {
                        isFound = true;
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            (c as UIElement).RaiseEvent(new RoutedEventArgs(e));
                        });
                    }
                }
            }
            if (!isFound)
                throw new ErrorReplyException(ErrorCodes.NOT_FOUND, "can not find such event");
            return new Reply(ErrorCodes.OK);
        }

    }
}
