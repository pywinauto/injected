using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                yield return VisualTreeHelper.GetChild(o, i);
            }
        }

        public List<long> GetChildrenOf(DependencyObject o)
        {
            var ret = new List<long>();
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                foreach (DependencyObject child in GetChildrenByVisualTreeHelper(o))
                {
                    ret.Add(ControlsStrorage.RegisterControl(child));
                }
            });
            return ret;
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
                    ret.Add(ControlsStrorage.RegisterControl(child));
                }
            });
            return ret;
        }
    }
}
