using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;

namespace InjectedWorker
{
    class Find
    {
        public static T ByAutomationId<T>(Window window, string automationId)
            where T : DependencyObject
        {
            return EnumerateVisualTree(window).OfType<T>().Single(i => AutomationProperties.GetAutomationId(i) == automationId);
        }

        public static T ByName<T>(Window window, string name)
            where T : FrameworkElement
        {
            return EnumerateVisualTree(window).OfType<T>().Single(i => i.Name == name);
        }

        public static IEnumerable<DependencyObject> EnumerateVisualTree(DependencyObject root)
        {
            yield return root;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);

                foreach (var element in EnumerateVisualTree(child))
                {
                    yield return element;
                }
            }
        }
    }
}
