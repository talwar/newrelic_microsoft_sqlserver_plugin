using System;
using System.Linq;
using System.Reflection;
using NSubstitute;
using NUnit.Framework;
using NewRelic.Microsoft.SqlServer.Plugin.Core;
using NewRelic.Microsoft.SqlServer.Plugin.Core.Extensions;
using NewRelic.Microsoft.SqlServer.Plugin.QueryTypes;
using NewRelic.Platform.Binding.DotNET;

namespace NewRelic.Microsoft.SqlServer.Plugin
{
    [TestFixture]
    public class QueryLocatorTests
    {
        [SqlMonitorQuery("NewRelic.Microsoft.SqlServer.Plugin.Core.ExampleEmbeddedFile.sql")]
        private class QueryTypeWithExactResourceName : FakeQueryResultBase {}

        [SqlMonitorQuery("Queries.ExampleEmbeddedFile.sql")]
        private class QueryTypeWithPartialResourceName : FakeQueryResultBase {}

        [SqlMonitorQuery("AnotherQuery.sql")]
        private class QueryTypeWithJustFileName : FakeQueryResultBase {}

        [SqlMonitorQuery("AnotherQuery.sql")]
        [SqlMonitorQuery("Queries.ExampleEmbeddedFile.sql")]
        private class QueryTypeWithTwoQueries : FakeQueryResultBase {}

        [SqlMonitorQuery("Foo.sql", Enabled = false)]
        private class QueryTypeDisabled : FakeQueryResultBase {}

        [SqlMonitorQuery("Foo.sql", Enabled = false)]
        [SqlMonitorQuery("AnotherQuery.sql", QueryName = "This is enabled")]
        private class QueryTypeSomeEnabled : FakeQueryResultBase {}

        [SqlMonitorQuery("AnotherQuery.sql", QueryName = "This is enabled")]
        private class QueryThatIsNotIQueryResult {}

        private class FakeQueryResultBase : IQueryResult
        {
            public void AddMetrics(ComponentData componentData) {}
        }


        [Test]
        public void Assert_funcs_are_correctly_configured()
        {
            var dapperWrapper = Substitute.For<IDapperWrapper>();

            var queries = new QueryLocator(dapperWrapper).PrepareQueries();
            foreach (var query in queries)
            {
                var results = query.Invoke(null);
                Assert.That(results, Is.EqualTo(new object[0]));
            }
        }

        [Test]
        public void Assert_multiple_query_attributes_yield_multiple_queries()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var queryLocator = new QueryLocator(null, assembly);

            var queries = queryLocator.PrepareQueries(new[] {typeof (QueryTypeWithTwoQueries)});
            Assert.That(queries, Is.Not.Null);
            var queryNames = queries.Select(q => q.ResourceName).ToArray();
            var expected = new[] {"AnotherQuery.sql", "Queries.ExampleEmbeddedFile.sql"};
            Assert.That(queryNames, Is.EquivalentTo(expected));
        }

        [Test]
        [ExpectedException(typeof (ArgumentException))]
        public void Assert_queries_not_IQueryResult_throws_exception()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var queryLocator = new QueryLocator(null, assembly);

            queryLocator.PrepareQueries(new[] {typeof (QueryThatIsNotIQueryResult)});
        }

        [Test]
        public void Assert_resource_with_exact_resource_name_is_located()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var queryLocator = new QueryLocator(null, assembly);

            var queries = queryLocator.PrepareQueries(new[] {typeof (QueryTypeWithExactResourceName)});
            Assert.That(queries, Is.Not.Null);
            var queryNames = queries.Select(q => q.ResultTypeName).ToArray();
            Assert.That(queryNames, Is.EqualTo(new[] {typeof (QueryTypeWithExactResourceName).Name}));
        }

        [Test]
        public void Assert_resource_with_only_file_name_is_located()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var queryLocator = new QueryLocator(null, assembly);

            var queries = queryLocator.PrepareQueries(new[] {typeof (QueryTypeWithJustFileName)});
            Assert.That(queries, Is.Not.Null);
            var queryNames = queries.Select(q => q.ResultTypeName).ToArray();
            Assert.That(queryNames, Is.EqualTo(new[] {typeof (QueryTypeWithJustFileName).Name}));
        }

        [Test]
        public void Assert_resource_with_partial_resource_name_is_located()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var queryLocator = new QueryLocator(null, assembly);

            var queries = queryLocator.PrepareQueries(new[] {typeof (QueryTypeWithPartialResourceName)});
            Assert.That(queries, Is.Not.Null);
            var queryNames = queries.Select(q => q.ResultTypeName).ToArray();
            Assert.That(queryNames, Is.EqualTo(new[] {typeof (QueryTypeWithPartialResourceName).Name}));
        }

        [Test]
        public void Assert_some_query_types_are_found()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var types = assembly.GetTypes();
            Assume.That(types, Is.Not.Empty, "Expected at least one type in the test assembly");

            var typesWithAttribute = types.Where(t => t.GetCustomAttributes<SqlMonitorQueryAttribute>().Any());
            Assert.That(typesWithAttribute, Is.Not.Empty, "Expected at least one QueryType using the " + typeof (SqlMonitorQueryAttribute).Name);
        }

        [Test]
        public void Assert_that_disabled_queries_are_ignored()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var queryLocator = new QueryLocator(null, assembly);

            var queries = queryLocator.PrepareQueries(new[] {typeof (QueryTypeDisabled)});
            Assert.That(queries, Is.Empty);
        }

        [Test]
        public void Assert_that_only_enabled_queries_are_found()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var queryLocator = new QueryLocator(null, assembly);

            var queries = queryLocator.PrepareQueries(new[] {typeof (QueryTypeSomeEnabled)})
                                      .Select(q => q.QueryName)
                                      .ToArray();

            Assert.That(queries, Is.EqualTo(new[] {"This is enabled"}));
        }

        [Test]
        public void Assert_that_queries_are_located()
        {
            var queries = new QueryLocator(new DapperWrapper(), Assembly.GetExecutingAssembly(), new[] {typeof (QueryThatIsNotIQueryResult)}).PrepareQueries();
            Assert.That(queries, Is.Not.Empty, "Expected some queries to be returned");
        }
    }
}