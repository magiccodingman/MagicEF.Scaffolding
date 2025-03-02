using Flattening.Protocol.Tests.Attributes;
using Flattening.Protocol.Tests.Helpers;
using Magic.Truth.Toolkit.Attributes;
using Magic.Truth.Toolkit.Models;
using Magic.Truth.Toolkit.Extensions.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Flattening.Protocol.Tests
{
    public class MagicTruthValidatorTests
    {
        private readonly ITestOutputHelper _output;

        public MagicTruthValidatorTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ValidateMappedTruthAttributes()
        {
            List<ValidationResponse> results = MagicTruthValidator.ValidateMagicMappings();
            ValidationTestHelper.RunValidationTest<MagicTruthAttribute>(_output, results);
        }

        [Fact]
        public void ValidateBindingTruthAttributes()
        {
            List<ValidationResponse> results = MagicTruthValidator.ValidateMagicTruthBindings();
            ValidationTestHelper.RunValidationTest<TruthBindingTestAttribute>(_output, results);
        }

        [Fact]
        public void ValidateMapImplemented()
        {
            List<ValidationResponse> results = MagicTruthValidator.ValidateMagicMapImplementations();
            ValidationTestHelper.RunValidationTest<MapImpTestAttribute>(_output, results);
        }
    }
}
