using BibliotecaAPI.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaAPITests.UnitTesting.Validations
{
    [TestClass]
    public class FirstLetterUppercaseAttributeTests
    {
        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(null)]
        [DataRow("Felipe")]
        public void IsValid_ReturnsSuccess_IfValueDoesNotStartWithLowercaseLetter(string value)
        {
            // Arrange
            var firstLetterUppercaseAttribute = new FirstLetterUppercaseAttribute();
            var validationContext = new ValidationContext(new object());

            // Act
            var result = firstLetterUppercaseAttribute.GetValidationResult(value, validationContext);
            
            // Assert
            Assert.AreEqual(expected: ValidationResult.Success, actual: result);
        }

        [TestMethod]
        [DataRow("felipe")]
        public void IsValid_ReturnsError_IfValueStartsWithLowercaseLetter(string value)
        {
            // Arrange
            var firstLetterUppercaseAttribute = new FirstLetterUppercaseAttribute();
            var validationContext = new ValidationContext(new object());

            // Act
            var result = firstLetterUppercaseAttribute.GetValidationResult(value, validationContext);

            // Assert
            Assert.AreEqual(expected: "The first letter must be uppercase", actual: result!.ErrorMessage);
        }
    }
}
