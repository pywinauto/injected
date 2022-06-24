using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace InjectedWorker.WPF
{
    class WPFGetControlType : GetControlType
    {
        public WPFGetControlType()
        {
            this.DefaultType = "Custom";

            this.KnownTypes.Add(typeof(System.Windows.Window), "Window");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Panel), "Pane");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Decorator), "Pane");
            this.KnownTypes.Add(typeof(System.Windows.Controls.ContentPresenter), "Pane");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Primitives.GridViewRowPresenterBase), "Pane");
            this.KnownTypes.Add(typeof(System.Windows.Controls.ToolBar), "ToolBar");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Menu), "Menu");
            this.KnownTypes.Add(typeof(System.Windows.Controls.MenuItem), "MenuItem");
            this.KnownTypes.Add(typeof(System.Windows.Controls.TabControl), "Tab");
            this.KnownTypes.Add(typeof(System.Windows.Controls.TabItem), "TabItem");
            this.KnownTypes.Add(typeof(System.Windows.Controls.TreeView), "Tree");
            this.KnownTypes.Add(typeof(System.Windows.Controls.TreeViewItem), "TreeItem");
            this.KnownTypes.Add(typeof(System.Windows.Controls.ListBox), "List");
            this.KnownTypes.Add(typeof(System.Windows.Controls.ListBoxItem), "ListItem");
            this.KnownTypes.Add(typeof(System.Windows.Controls.ListViewItem), "DataItem");
            this.KnownTypes.Add(typeof(System.Windows.Controls.DataGridRow), "DataItem");
            this.KnownTypes.Add(typeof(System.Windows.Controls.DataGrid), "DataGrid");
            this.KnownTypes.Add(typeof(System.Windows.Controls.ComboBox), "ComboBox");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Label), "Text");
            this.KnownTypes.Add(typeof(System.Windows.Controls.TextBlock), "Text");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Button), "Button");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Primitives.ToggleButton), "Button");
            this.KnownTypes.Add(typeof(System.Windows.Controls.TextBox), "Edit");
            this.KnownTypes.Add(typeof(System.Windows.Controls.PasswordBox), "Edit");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Primitives.ScrollBar), "ScrollBar");
            this.KnownTypes.Add(typeof(System.Windows.Controls.CheckBox), "CheckBox");
            this.KnownTypes.Add(typeof(System.Windows.Controls.RadioButton), "RadioButton");
            this.KnownTypes.Add(typeof(System.Windows.Controls.RichTextBox), "Document");
            this.KnownTypes.Add(typeof(System.Windows.Controls.Slider), "Slider");
            this.KnownTypes.Add(typeof(System.Windows.Controls.GridViewColumn), "HeaderItem");
            this.KnownTypes.Add(typeof(System.Windows.Controls.DataGridTextColumn), "HeaderItem");
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
            string typeFromCustomLogic = "";
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                if (obj is ListView && (obj as ListView).View is GridView)
                {
                    typeFromCustomLogic = "DataGrid";
                }
            });

            if (typeFromCustomLogic.Length > 0)
            {
                return typeFromCustomLogic;
            }
            return base.FindControlType(obj);
        }
    }
}
