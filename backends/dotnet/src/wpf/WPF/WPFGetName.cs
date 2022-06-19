using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;

namespace InjectedWorker.WPF
{
    class WPFGetName : GetName
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

        protected virtual string GetNameString(TextBlock o)
        {
            return o.Text;
        }

        protected virtual string GetNameString(GridViewColumn o)
        {
            if (o.Header == null)
            {
                return "";
            }
            dynamic source = o.Header;
            return GetNameString(source);
        }

        protected virtual string GetNameString(DataGridColumn o)
        {
            if (o.Header == null)
            {
                return "";
            }
            dynamic source = o.Header;
            return GetNameString(source);
        }
    }
}
