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
    [Apartment(ApartmentState.STA)] // Required for WPF tests
    public class PasswordBoxHelperTests
    {
        [SetUp]
        public void Setup()
        {
            // Ensure WPF Application is created (needed for some binding scenarios)
            if (Application.Current == null)
            {
                new Application();
            }
        }

        [Test]
        public void SetBoundPassword_ShouldUpdatePasswordBoxPassword()
        {
            var box = new PasswordBox();

            PasswordBoxHelper.SetBoundPassword(box, "secret");

            ClassicAssert.AreEqual("secret", box.Password);
        }

        [Test]
        public void SetBoundPassword_SameValue_ShouldNotTriggerUnnecessaryChanges()
        {
            var box = new PasswordBox { Password = "secret" };
            PasswordBoxHelper.SetBoundPassword(box, "secret");

            ClassicAssert.AreEqual("secret", box.Password);
            // No exception or infinite loop should occur
        }

        [Test]
        public void TypingInPasswordBox_ShouldUpdateBoundPassword()
        {
            var box = new PasswordBox();
            PasswordBoxHelper.SetBoundPassword(box, string.Empty);

            box.Password = "typed";
            // Manually trigger event
            box.RaiseEvent(new RoutedEventArgs(PasswordBox.PasswordChangedEvent));

            var bound = PasswordBoxHelper.GetBoundPassword(box);
            ClassicAssert.AreEqual("typed", bound);
        }

        [Test]
        public void BoundPassword_ShouldHandleEmptyOrNull()
        {
            var box = new PasswordBox();
            PasswordBoxHelper.SetBoundPassword(box, null);

            ClassicAssert.AreEqual(string.Empty, box.Password);

            box.Password = "";
            box.RaiseEvent(new RoutedEventArgs(PasswordBox.PasswordChangedEvent));

            var bound = PasswordBoxHelper.GetBoundPassword(box);
            ClassicAssert.AreEqual(null, bound);
        }

        [Test]
        public void TwoPasswordBoxes_ShouldWorkIndependently()
        {
            var box1 = new PasswordBox();
            var box2 = new PasswordBox();

            PasswordBoxHelper.SetBoundPassword(box1, "one");
            PasswordBoxHelper.SetBoundPassword(box2, "two");

            ClassicAssert.AreEqual("one", box1.Password);
            ClassicAssert.AreEqual("two", box2.Password);
        }

        [Test]
        public void SettingBoundPassword_ShouldReattachPasswordChangedHandler()
        {
            var box = new PasswordBox();
            PasswordBoxHelper.SetBoundPassword(box, "initial");

            // Change attached property again
            PasswordBoxHelper.SetBoundPassword(box, "updated");

            ClassicAssert.AreEqual("updated", box.Password);

            // Ensure PasswordChanged still works
            box.Password = "typed";
            box.RaiseEvent(new RoutedEventArgs(PasswordBox.PasswordChangedEvent));

            ClassicAssert.AreEqual("typed", PasswordBoxHelper.GetBoundPassword(box));
        }

        [Test]
        public void BoundPassword_ShouldUpdateSource_WhenUsedWithBinding()
        {
            var vm = new TestViewModel { Password = "initial" };
            var box = new PasswordBox();

            var binding = new System.Windows.Data.Binding("Password")
            {
                Source = vm,
                Mode = System.Windows.Data.BindingMode.TwoWay,
                UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
            };

            box.SetBinding(PasswordBoxHelper.BoundPassword, binding);

            // ViewModel → UI
            vm.Password = "fromVM";
            ClassicAssert.AreEqual("fromVM", box.Password);

            // UI → ViewModel
            box.Password = "fromUI";
            box.RaiseEvent(new RoutedEventArgs(PasswordBox.PasswordChangedEvent));
            ClassicAssert.AreEqual("fromUI", vm.Password);
        }

        private class TestViewModel : System.ComponentModel.INotifyPropertyChanged
        {
            private string _password;
            public string Password
            {
                get => _password;
                set
                {
                    if (_password != value)
                    {
                        _password = value;
                        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Password)));
                    }
                }
            }

            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        }
    }
}
