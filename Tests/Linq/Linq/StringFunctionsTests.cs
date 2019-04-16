﻿using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{

	[TestFixture]
	public class StringFunctionsTests : TestBase
	{
		static string AggregateStrings(string separator, IEnumerable<string> arguments)
		{
			var result = arguments.Aggregate((v1, v2) =>
			{
				if (v1 == null && v2 == null)
					return null;
				if (v1 == null)
					return v2;
				if (v2 == null)
					return v1;
				return v1 + separator + v2;
			});
			return result;
		}

		[Table]
		class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column(Length = 50, CanBeNull = true)] public string Value1 { get; set; }
			[Column(Length = 50, CanBeNull = true)] public string Value2 { get; set; }
			[Column(Length = 50, CanBeNull = true)] public string Value3 { get; set; }
		}

		public class StringTestSourcesAttribute : IncludeDataSourcesAttribute
		{
			public StringTestSourcesAttribute(bool includeLinqService = true) : base(includeLinqService,
				TestProvName.AllSqlServer2017Plus,
				TestProvName.AllSQLite,
				TestProvName.AllPostgreSQL,
				ProviderName.SapHana,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				ProviderName.DB2,
				TestProvName.AllFirebird)
			{
			}
		}

		[Test]
		public void AggregationTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = from t in table
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Max = g.Max(),
						Values = g.StringAggregate(" -> ").DefaultOrder(),
					};

				var zz = actual.ToArray();

				var expected = from t in data
					group t.Value1 by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Max = g.Max(),
						Values = AggregateStrings(" -> ", g),
					};

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void AggregationSelectorTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual = from t in table
					group t by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Values = g.StringAggregate(" -> ", e => e.Value1).DefaultOrder(),
					};

				var expected = from t in data
					group t by new {t.Id, Value = t.Value1}
					into g
					select new
					{
						Values = AggregateStrings(" -> ", g.Select(e => e.Value1)),
					};

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void FinalAggregationTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual   = table.Select(t => t.Value1).StringAggregate(" -> ").DefaultOrder();
				var expected = AggregateStrings(" -> ", data.Select(t => t.Value1));
				Assert.AreEqual(expected, actual);
			}
		}

		[Test]
		public void FinalAggregationSelectorTest([StringTestSources] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actual   = table.AsQueryable().StringAggregate(" -> ", t => t.Value1).DefaultOrder();
				var expected = AggregateStrings(" -> ", data.Select(t => t.Value1));
				Assert.AreEqual(expected, actual);
			}
		}

		[Test]
		public void ConcatWSTest([
			IncludeDataSources(
				TestProvName.AllSqlServer,
				TestProvName.AllPostgreSQL,
				TestProvName.AllMySql,
				TestProvName.AllSQLite
			)] string context)
		{
			var data = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var actualOne   = table.Select(t => Sql.ConcatWS(" -> ", t.Value2));
				var expectedOne = data .Select(t => Sql.ConcatWS(" -> ", t.Value2));

				Assert.AreEqual(expectedOne, actualOne);

				var actualOneNull   = table.Select(t => Sql.ConcatWS(" -> ", t.Value3));
				var expectedOneNull = data .Select(t => Sql.ConcatWS(" -> ", t.Value3));

				Assert.AreEqual(expectedOneNull, actualOneNull);

				var actual   = table.Select(t => Sql.ConcatWS(" -> ", t.Value3, t.Value1, t.Value2));
				var expected = data .Select(t => Sql.ConcatWS(" -> ", t.Value3, t.Value1, t.Value2));

				Assert.AreEqual(expected, actual);

				var actualAllEmpty   = table.Select(t => Sql.ConcatWS(" -> ", t.Value3, t.Value3));
				var expectedAllEmpty = data .Select(t => Sql.ConcatWS(" -> ", t.Value3, t.Value3));

				Assert.AreEqual(expectedAllEmpty, actualAllEmpty);
			}
		}

		private static SampleClass[] GenerateData()
		{
			var data = new[]
			{
				new SampleClass { Id = 1, Value1 = "V1", Value2 = "V2"},
				new SampleClass { Id = 2, Value1 = null, Value2 = "Z2"},
				new SampleClass { Id = 3, Value1 = "Z1", Value2 = null}
			};
			return data;
		}
	}
}
