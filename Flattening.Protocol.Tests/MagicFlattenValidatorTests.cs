using Flattening.Protocol.Tests.Attributes;
using Flattening.Protocol.Tests.Helpers;
using Magic.Truth.Toolkit.Attributes;
using Magic.Truth.Toolkit.Models;
using Magic.Truth.Toolkit.Validation;
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
        public void ValidateFlattenMappings()
        {
            List<ValidationResponse> results = MagicFlattenValidator.ValidateFlattenMappings();
            ValidationTestHelper.RunValidationTest<MagicMapAttribute>(_output, results);
        }
    }
}