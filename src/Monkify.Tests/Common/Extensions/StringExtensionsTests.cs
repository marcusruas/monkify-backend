using Monkify.Common.Extensions;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.Common.Extensions
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("café", "cafe")]
        [InlineData("açúcar", "acucar")]
        [InlineData("Español", "Espanol")]
        public void RemoveAccents_ShouldRemoveAccentsFromString(string input, string expected)
        {
            var result = input.RemoveAccents();
            result.ShouldBe(expected);
        }

        [Fact]
        public void RemoveAccents_ShouldReturnNullIfInputIsNull()
        {
            string input = null;
            var result = input.RemoveAccents();
            result.ShouldBeNull();
        }

        [Theory]
        [InlineData("hello world", true)]
        [InlineData("helloworld", false)]
        [InlineData(" ", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void HasWhitespaces_ShouldCorrectlyIdentifyWhitespaces(string input, bool expected)
        {
            var result = input.HasWhitespaces();
            result.ShouldBe(expected);
        }

        [Theory]
        [InlineData("olá", true)]
        [InlineData("mundo", false)]
        [InlineData(null, false)]
        public void ContemAcentos_ShouldCorrectlyIdentifyAccents(string input, bool expected)
        {
            var result = input.ContemAcentos();
            result.ShouldBe(expected);
        }

        [Fact]
        public void ToSHA256_ShouldReturnCorrectHash()
        {
            var input = "hello";
            var expected = "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824";
            var result = input.ToSHA256();
            result.ShouldBe(expected);
        }

        [Theory]
        [InlineData("hello", true)]
        [InlineData("world", false)]
        [InlineData("a", false)]
        public void ContainsDuplicateCharacters_ShouldDetectDuplicates(string input, bool expected)
        {
            var result = input.ContainsDuplicateCharacters();
            result.ShouldBe(expected);
        }
    }
}
