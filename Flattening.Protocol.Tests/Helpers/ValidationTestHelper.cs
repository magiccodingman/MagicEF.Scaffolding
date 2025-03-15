using Truth.Protocol.Tests.Attributes;
using Magic.Truth.Toolkit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Truth.Protocol.Tests.Helpers
{
    internal static class ValidationTestHelper
    {
        public static void RunValidationTest<TAttribute>(
            ITestOutputHelper output,
            List<ValidationResponse> validationResults)
            where TAttribute : Attribute
        {
            var allDtoTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttribute<TAttribute>() != null)
                .ToList();

            var expectedResults = new Dictionary<string, bool>();

            foreach (var type in allDtoTypes)
            {
                var shouldPassAttr = type.GetCustomAttribute<ShouldPassAttribute>();

                if (shouldPassAttr == null)
                {
                    output.WriteLine($"ERROR: Class {type.FullName} has {typeof(TAttribute).Name} but is missing ShouldPassAttribute.");
                    Assert.Fail($"Class {type.FullName} has {typeof(TAttribute).Name} but is missing ShouldPassAttribute.");
                }

                expectedResults[type.FullName] = shouldPassAttr.ShouldPass;  // Store FULLY QUALIFIED Name
            }

            // Track actual validation failures (FULLY QUALIFIED)
            var actualFailures = validationResults.ToDictionary(r => r.ClassName, r => r.ErrorMessage);
            var unexpectedFailures = new List<string>();
            var unexpectedSuccesses = new List<string>();

            output.WriteLine("\n======= Validation Errors =======");
            foreach (var result in validationResults)
            {
                bool expectedToPass = expectedResults.TryGetValue(result.ClassName, out var shouldPass) ? shouldPass : false;
                bool passed = !expectedToPass; // If it was expected to fail but is in errors, it's correct

                output.WriteLine($"************");
                output.WriteLine($"Class: {result.ClassName}");
                output.WriteLine($"Error: {result.ErrorMessage}");
                output.WriteLine($"Expected Result: {expectedToPass}");
                output.WriteLine($"Passed: {passed}");

                if (expectedToPass)
                    unexpectedFailures.Add(result.ClassName);
            }

            output.WriteLine("\n======= Successful Validations =======");
            foreach (var kvp in expectedResults)
            {
                var className = kvp.Key;
                var shouldPass = kvp.Value;

                if (!actualFailures.ContainsKey(className))
                {
                    bool passed = shouldPass;

                    output.WriteLine($"************");
                    output.WriteLine($"Class: {className}");
                    output.WriteLine($"Expected Result: {shouldPass}");
                    output.WriteLine($"Passed: {passed}");

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