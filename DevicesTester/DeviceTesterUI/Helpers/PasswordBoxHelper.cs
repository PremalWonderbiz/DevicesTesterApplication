using System.Windows;
using System.Windows.Controls;

namespace DeviceTesterUI.Helpers
{
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty BoundPassword =
            DependencyProperty.RegisterAttached(
                "BoundPassword",
                typeof(string),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

        public static string GetBoundPassword(DependencyObject dp) =>
            (string)dp.GetValue(BoundPassword);

        public static void SetBoundPassword(DependencyObject dp, string value) =>
            dp.SetValue(BoundPassword, value);

        private static void OnBoundPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            if (dp is PasswordBox passwordBox)
            {
                passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;

                string newPassword = e.NewValue?.ToString() ?? string.Empty;

                // Only update PasswordBox if the text is actually different
                if (passwordBox.Password != newPassword)
                    passwordBox.Password = newPassword;

                passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
            }
        }

        private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                // Only update the property if it is actually different
                if (GetBoundPassword(passwordBox) != passwordBox.Password)
                    SetBoundPassword(passwordBox, passwordBox.Password);
            }
        }
    }
}
