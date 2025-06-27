using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.IntegrationTests.Shared
{
    [CollectionDefinition(nameof(MonkifyTestsCollection))]
    public class MonkifyTestsCollection : ICollectionFixture<ApplicationFixture> { }
}
