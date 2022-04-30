using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace InjectedWorker.WPF
{
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
}
