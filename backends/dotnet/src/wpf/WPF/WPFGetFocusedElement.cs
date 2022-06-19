using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace InjectedWorker.WPF
{
    class WPFGetFocusedElement : GetFocusedElement
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
}
