﻿using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TableDependency.EventArgs;
using TableDependency.IntegrationTest.Helpers;
using TableDependency.IntegrationTest.Helpers.SqlServer;
using TableDependency.IntegrationTest.Models;
using TableDependency.Mappers;
using TableDependency.SqlClient;

namespace TableDependency.IntegrationTest
{
    [TestClass]
    public class DatabaseObjectCleanUpTestSqlServer
    {
        private static string _dbObjectsNaming;
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["SqlServerConnectionString"].ConnectionString;
        private static string TableName = "Check_Model";

        [ClassInitialize()]
        public static void ClassInitialize(TestContext testContext)
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"IF OBJECT_ID('{TableName}', 'U') IS NOT NULL DROP TABLE [{TableName}];";
                    sqlCommand.ExecuteNonQuery();

                    sqlCommand.CommandText =
                        $"CREATE TABLE [{TableName}]( " +
                        "[Id][int] IDENTITY(1, 1) NOT NULL, " +
                        "[First Name] [nvarchar](50) NOT NULL, " +
                        "[Second Name] [nvarchar](50) NOT NULL, " +
                        "[Born] [datetime] NULL)";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        [TestInitialize()]
        public void TestInitialize()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"DELETE FROM [{TableName}]";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"IF OBJECT_ID('{TableName}', 'U') IS NOT NULL DROP TABLE [{TableName}];";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        [TestMethod]
        public void DatabaseObjectCleanUpTest()
        {
            var domaininfo = new AppDomainSetup();
            domaininfo.ApplicationBase = Environment.CurrentDirectory;
            var adevidence = AppDomain.CurrentDomain.Evidence;
            var domain = AppDomain.CreateDomain("RunsInAnotherAppDomain_Check_DatabaseObjectCleanUp", adevidence, domaininfo);
            var otherDomainObject = (RunsInAnotherAppDomain_Check_DatabaseObjectCleanUp)domain.CreateInstanceAndUnwrap(typeof(RunsInAnotherAppDomain_Check_DatabaseObjectCleanUp).Assembly.FullName, typeof(RunsInAnotherAppDomain_Check_DatabaseObjectCleanUp).FullName);
            _dbObjectsNaming = otherDomainObject.RunTableDependency(ConnectionString, TableName);
            Thread.Sleep(5000);
            AppDomain.Unload(domain);

            Thread.Sleep(3 * 60 * 1000);
            Assert.IsTrue(SqlServerHelper.AreAllDbObjectDisposed(ConnectionString, _dbObjectsNaming));
        }
    }

    public class RunsInAnotherAppDomain_Check_DatabaseObjectCleanUp : MarshalByRefObject
    {
        public string RunTableDependency(string connectionString, string tableName)
        {
            var mapper = new ModelToTableMapper<Check_Model>();
            mapper.AddMapping(c => c.Name, "First Name").AddMapping(c => c.Surname, "Second Name");

            var tableDependency = new SqlTableDependency<Check_Model>(connectionString, tableName, mapper);
            tableDependency.OnChanged += TableDependency_Changed;
            tableDependency.Start(60, 120);
            return tableDependency.DataBaseObjectsNamingConvention;
        }

        private static void TableDependency_Changed(object sender, RecordChangedEventArgs<Check_Model> e)
        {
        }
    }
}