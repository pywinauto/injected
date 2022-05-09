using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace InjectedWorker.WPF
{
    class WPFGetChildren : GetChildrenAction<DependencyObject>
    {
        private ControlsStorage<DependencyObject> ControlsStorage;

        public override Reply Run<T>(ControlsStorage<T> controls, IDictionary<string, dynamic> args)
        {
            ControlsStorage = controls as ControlsStorage<DependencyObject>;

            CheckParamExists(args, "element_id");
            CheckValidControlId<T>(args["element_id"], null);

            if (args["element_id"] == 0)
            {
                List<long> windows = null;
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    windows = new List<long>();
                    GetWindows(windows);
                });
                return new ElementsReply(windows);
            }
            else
            {
                CheckValidControlId<T>(args["element_id"], controls);
                dynamic c = controls.GetControl(args["element_id"]);
                if (c == null)
                    throw new ErrorReplyException(ErrorCodes.NOT_FOUND, "element not found: " + args["element_id"]);
                else
                {
                    List<long> children = null;
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        children = new List<long>();
                        GetChildrenOf(children, c);
                    });
                    return new ElementsReply(children);
                }       
            }
        }

        public void GetWindows(List<long> result)
        {
            foreach (Window w in Application.Current.Windows)
            {
                result.Add(ControlsStorage.RegisterControl(w));
            }
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

        public void GetChildrenOf(List<long> result, DependencyObject o)
        {
            if (o is Visual || o is Visual3D)
            {
                foreach (DependencyObject child in GetChildrenByVisualTreeHelper(o))
                {
                    result.Add(ControlsStorage.RegisterControl(child));
                }
            }
            else
            {
                //foreach (object child in LogicalTreeHelper.GetChildren(o))
                //{
                //    if (child is DependencyObject)
                //    {
                //        ret.Add(ControlsStorage.RegisterControl(child as DependencyObject));
                //    }
                //}
            }
        }

        public void GetChildrenOf(List<long> result, HeaderedContentControl o)
        {
            if (o.Header is DependencyObject)
                result.Add(ControlsStorage.RegisterControl(o.Header as DependencyObject));

            GetChildrenOf(result, o as ContentControl);
        }

        public void GetChildrenOf(List<long> result, ContentControl o)
        {
            if (o.Content is DependencyObject)
            {
                result.Add(ControlsStorage.RegisterControl(o.Content as DependencyObject));
            }
            else if (o.Content != null)
            {
                // if property value is not control (for example, string or DateTime object), try to search in visual tree
                GetChildrenOf(result, o as DependencyObject);
            }
        }

        public void GetChildrenOf(List<long> result, HeaderedItemsControl o)
        {
            if (o.Header is DependencyObject)
                result.Add(ControlsStorage.RegisterControl(o.Header as DependencyObject));
            GetChildrenOf(result, o as ItemsControl);
        }

        public void GetChildrenOf(List<long> result, ItemsControl o)
        {
            bool hasControlInItems = false;
            foreach (var item in o.Items)
            {
                if (item is DependencyObject)
                {
                    hasControlInItems = true;
                    result.Add(ControlsStorage.RegisterControl(item as DependencyObject));
                }
            }

            if (!hasControlInItems && o.Items.Count > 0)
            {
                GetChildrenOf(result, o as DependencyObject);
            }
        }

        public void GetChildrenOf(List<long> result, ListView o)
        {
            if (o.View is GridView)
            {
                GridView gridView = o.View as GridView;
                foreach (GridViewColumn c in gridView.Columns)
                {
                    result.Add(ControlsStorage.RegisterControl(c));
                }
            }
            foreach (var child in FindChildrenOfType<ListViewItem>(o))
            {
                result.Add(ControlsStorage.RegisterControl(child));
            }
        }

        public void GetChildrenOf(List<long> result, ListViewItem o)
        {
            foreach (var child in FindChildrenOfType<GridViewRowPresenter>(o))
            {
                result.Add(ControlsStorage.RegisterControl(child));
            }
        }

        public void GetChildrenOf(List<long> result, DataGrid o)
        {
            foreach (DataGridColumn c in o.Columns)
            {
                result.Add(ControlsStorage.RegisterControl(c));
            }
            foreach (var child in FindChildrenOfType<DataGridRow>(o))
            {
                result.Add(ControlsStorage.RegisterControl(child));
            }
        }

        public void GetChildrenOf(List<long> result, DataGridRow o)
        {
            foreach (var child in FindChildrenOfType<DataGridCell>(o))
            {
                result.Add(ControlsStorage.RegisterControl(child));
            }
            foreach (var child in FindChildrenOfType<DataGridDetailsPresenter>(o))
            {
                result.Add(ControlsStorage.RegisterControl(child));
            }
        }
    }
}
