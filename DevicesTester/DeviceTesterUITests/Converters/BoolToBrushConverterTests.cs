using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceTesterUI.Converters;
using NUnit.Framework;
using System.Windows.Media;
using NUnit.Framework.Legacy;

namespace DeviceTesterUITests.Converters
{
    [TestFixture]
    public class BoolToBrushConverterTests
    {
        private BoolToBrushConverter _converter;

        [SetUp]
        public void Setup()
        {
            _converter = new BoolToBrushConverter();
        }

        [Test]
        public void Convert_True_ReturnsGreenBrush()
        {
            // Arrange
            bool value = true;

            // Act
            var result = _converter.Convert(value, typeof(Brush), null, null);

            // ClassicAssert
            ClassicAssert.AreEqual(Brushes.Green, result);
        }

        [Test]
        public void Convert_False_ReturnsRedBrush()
        {
            // Arrange
            bool value = false;

            // Act
            var result = _converter.Convert(value, typeof(Brush), null, null);

            // ClassicAssert
            ClassicAssert.AreEqual(Brushes.Red, result);
        }

        [Test]
        public void Convert_NonBoolean_ReturnsGrayBrush()
        {
            // Arrange
            var value = "not a bool";

            // Act
            var result = _converter.Convert(value, typeof(Brush), null, null);

            // ClassicAssert
            ClassicAssert.AreEqual(Brushes.Gray, result);
        }

        [Test]
        public void ConvertBack_ThrowsNotImplementedException()
        {
            // Act & ClassicAssert
            ClassicAssert.Throws<NotImplementedException>(() =>
                _converter.ConvertBack(Brushes.Green, typeof(bool), null, null));
        }
    }
}
