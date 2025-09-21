using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using DeviceTesterUI.Helpers;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace DeviceTesterTests.Helpers
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]  // <-- Make all tests run in STA
    public class PasswordBoxHelperTests
    {
        private PasswordBox _passwordBox;

        [SetUp]
        public void Setup()
        {
            // Instantiate a real PasswordBox for testing
            _passwordBox = new PasswordBox();
        }

        [Test]
        public void BoundPassword_SetAttachedProperty_UpdatesPasswordBoxPassword()
        {
            // Arrange
            const string expectedPassword = "Test123";

            // Act
            PasswordBoxHelper.SetBoundPassword(_passwordBox, expectedPassword);

            // ClassicAssert
            ClassicAssert.AreEqual(expectedPassword, _passwordBox.Password);
        }

        [Test]
        public void PasswordBox_ChangePassword_UpdatesBoundPassword()
        {
            const string newPassword = "NewPass";

            // Attach handler via SetBoundPassword
            PasswordBoxHelper.SetBoundPassword(_passwordBox, string.Empty);

            // Act
            _passwordBox.Password = newPassword;

            // Directly invoke the private handler
            // Reflection is required because the handler is private
            var method = typeof(PasswordBoxHelper)
                .GetMethod("PasswordBox_PasswordChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { _passwordBox, new RoutedEventArgs() });

            // Assert
            ClassicAssert.That(PasswordBoxHelper.GetBoundPassword(_passwordBox), Is.EqualTo(newPassword));
        }


        [Test]
        public void BoundPassword_SetSameValue_DoesNotCauseInfiniteLoop()
        {
            // Arrange
            const string password = "LoopTest";

            // Act
            PasswordBoxHelper.SetBoundPassword(_passwordBox, password);

            // Change PasswordBox to the same value
            _passwordBox.Password = password;
            var args = new RoutedEventArgs(PasswordBox.PasswordChangedEvent, _passwordBox);
            _passwordBox.RaiseEvent(args);

            // ClassicAssert
            ClassicAssert.AreEqual(password, PasswordBoxHelper.GetBoundPassword(_passwordBox));
        }

        [Test]
        public void BoundPassword_SetNull_ResetsPasswordBoxToEmpty()
        {
            // Arrange
            _passwordBox.Password = "OldPassword";

            // Act
            PasswordBoxHelper.SetBoundPassword(_passwordBox, null);

            // ClassicAssert
            ClassicAssert.AreEqual(string.Empty, _passwordBox.Password);
        }

        [Test]
        public void PasswordBox_PasswordChanged_UpdatesBoundPasswordOnlyIfDifferent()
        {
            // Arrange
            const string initial = "Initial";
            PasswordBoxHelper.SetBoundPassword(_passwordBox, initial);

            // Act: simulate PasswordChanged with same value
            _passwordBox.Password = initial;
            var args = new RoutedEventArgs(PasswordBox.PasswordChangedEvent, _passwordBox);
            _passwordBox.RaiseEvent(args);

            // ClassicAssert: BoundPassword should remain the same
            ClassicAssert.AreEqual(initial, PasswordBoxHelper.GetBoundPassword(_passwordBox));
        }
    }
}
