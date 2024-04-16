using Bogus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.Shared
{
    public abstract class StubBuilder<T> where T : class
    {
        public StubBuilder()
        {
            Faker = new Faker();
            Random = new Random();
            Object = new();

            RulesForObject();
        }

        protected readonly Faker<T> Object;
        protected readonly Faker Faker;
        protected readonly Random Random;

        public abstract void RulesForObject();

        public T BuildFirst()
            => Object.Generate();

        public IEnumerable<T> BuildToList(int quantity)
            => Object.Generate(quantity);

    }
}
