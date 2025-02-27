using Flattening.Protocol.Tests.Attributes;
using Magic.Flattening.Toolkit.Attributes;
using Magic.Flattening.Toolkit.Validation;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Flattening.Protocol.Tests
{
    public class MagicFlattenValidatorTests
    {
        private readonly ITestOutputHelper _output;

        public MagicFlattenValidatorTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ValidateFlattenMappings_ShouldDetectErrorsCorrectly()
        {
            var allDtoTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttribute<MagicViewDtoAttribute>() != null)
                .ToList();

            var expectedResults = new Dictionary<string, bool>();

            foreach (var type in allDtoTypes)
            {
                var shouldPassAttr = type.GetCustomAttribute<ShouldPassAttribute>();

                if (shouldPassAttr == null)
                {
                    _output.WriteLine($"ERROR: Class {type.FullName} has MagicViewDtoAttribute but is missing ShouldPassAttribute.");
                    Assert.Fail($"Class {type.FullName} has MagicViewDtoAttribute but is missing ShouldPassAttribute.");
                }

                expectedResults[type.FullName] = shouldPassAttr.ShouldPass;  // 🔥 Store FULLY QUALIFIED Name
            }

            List<(string className, string errorMessage)> results = MagicFlattenValidator.ValidateFlattenMappings();

            // Track actual validation failures (FULLY QUALIFIED)
            var actualFailures = results.ToDictionary(r => r.className, r => r.errorMessage);
            var unexpectedFailures = new List<string>();
            var unexpectedSuccesses = new List<string>();

            _output.WriteLine("\n======= Validation Errors =======");
            foreach (var result in results)
            {
                bool expectedToPass = expectedResults.TryGetValue(result.className, out var shouldPass) ? shouldPass : false;
                bool passed = !expectedToPass; // If it was expected to fail, but is in errors, it's correct

                _output.WriteLine($"************");
                _output.WriteLine($"Class: {result.className}");
                _output.WriteLine($"Error: {result.errorMessage}");
                _output.WriteLine($"Expected Result: {expectedToPass}");
                _output.WriteLine($"Passed: {passed}");

                if (expectedToPass)
                    unexpectedFailures.Add(result.className);
            }

            _output.WriteLine("\n======= Successful Validations =======");
            foreach (var kvp in expectedResults)
            {
                var className = kvp.Key;
                var shouldPass = kvp.Value;

                // 🚨 **NEW FILTERING: Use FULLY QUALIFIED NAME**
                if (!actualFailures.ContainsKey(className))
                {
                    bool passed = shouldPass; // If expected to pass and is in successful list, it's correct

                    _output.WriteLine($"************");
                    _output.WriteLine($"Class: {className}");
                    _output.WriteLine($"Expected Result: {shouldPass}");
                    _output.WriteLine($"Passed: {passed}");

                    if (!shouldPass)
                        unexpectedSuccesses.Add(className);
                }
            }

            // Determine test pass/fail condition
            if (unexpectedFailures.Any() || unexpectedSuccesses.Any())
            {
                var failureMessage = new StringBuilder("Validation test failed:\n");

                if (unexpectedFailures.Any())
                {
                    failureMessage.AppendLine("Unexpected Failures:");
                    failureMessage.AppendLine(string.Join(", ", unexpectedFailures));
                }

                if (unexpectedSuccesses.Any())
                {
                    failureMessage.AppendLine("Unexpected Successes:");
                    failureMessage.AppendLine(string.Join(", ", unexpectedSuccesses));
                }

                Assert.Fail(failureMessage.ToString());
            }
        }
    }
}