using Bogus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.Shared
{
    public abstract class UnitTestsClass
    {
        public UnitTestsClass()
        {
            CancellationToken = new CancellationToken();
            Faker = new Faker("en-us");
            Random = new Random();
        }

        protected CancellationToken CancellationToken;
        protected readonly Faker Faker;
        protected Random Random;
    }
}
