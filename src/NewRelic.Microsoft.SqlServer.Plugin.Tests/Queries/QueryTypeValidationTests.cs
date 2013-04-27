using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NewRelic.Microsoft.SqlServer.Plugin.Core;

namespace NewRelic.Microsoft.SqlServer.Plugin.Queries
{
	[TestFixture]
	public class QueryTypeValidationTests
	{
		public IEnumerable<TestCaseData> QueryTypes
		{
			get
			{
				var assembly = typeof (QueryLocator).Assembly;
				var typesWithAttribute = assembly.GetTypes().Where(t => t.GetCustomAttributes(typeof (SqlMonitorQueryAttribute), false).Any());

				return typesWithAttribute.Select(t => new {Type = t, Attribute = t.GetCustomAttributes(typeof (SqlMonitorQueryAttribute), false).FirstOrDefault() as SqlMonitorQueryAttribute})
				                         .Select(t => new TestCaseData(t.Type, t.Attribute).SetName(t.Type.Name));
			}
		}

		[Test]
		[TestCaseSource("QueryTypes")]
		public void Assert_query_type_has_attribute_with_valid_resource_name(Type queryType, SqlMonitorQueryAttribute attribute)
		{
			var rootNamespace = typeof (Program).Namespace;
			var resourceName = rootNamespace + "." + attribute.ResourceName;
			var sql = queryType.Assembly.GetStringResource(resourceName);
			Assert.That(sql, Is.Not.Null);
		}
	}
}