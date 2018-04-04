using System;
using System.Collections.Generic;
using System.Linq;
using SpanJson.Benchmarks;
using SpanJson.Benchmarks.Fixture;
using SpanJson.Benchmarks.Models;
using SpanJson.Formatters;
using Xunit;
using Xunit.Abstractions;

namespace SpanJson.Tests
{
    public class Tests
    {

        [Theory]
        [MemberData(nameof(GetModels))]
        public void CanSerializeAll(Type modelType)
        {
            var fixture = new ExpressionTreeFixture();
            var model = fixture.Create(modelType);
            var serialized = JsonSerializer.NonGeneric.Serialize(model);
            Assert.NotNull(serialized);
        }

        public static IEnumerable<object[]> GetModels()
        {
            var models = typeof(AccessToken).Assembly
                .GetTypes()
                .Where(t => t.Namespace == typeof(AccessToken).Namespace && !t.IsEnum && !t.IsInterface && !t.IsAbstract)
                .ToList();
            return models.Select(a => new object[] {a});
        }


        [Theory]
        [InlineData("Hello \"World", "\"Hello \\\"World\"")]
        [InlineData("Hello \"Univ\"erse", "\"Hello \\\"Univ\\\"erse\"")]
        public void WriteEscaped(string input, string output)
        {
            var serialized = JsonSerializer.Generic.Serialize(input);
            var jilSerialized = Jil.JSON.Serialize(input);
            Assert.Equal(output, serialized);
            Assert.Equal(jilSerialized, serialized);
        }
    }
}
