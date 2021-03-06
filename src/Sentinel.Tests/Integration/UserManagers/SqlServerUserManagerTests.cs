﻿namespace Sentinel.Tests.Integration.UserManagers
{
    using System;
    using System.Data;
    using System.Data.SqlLocalDb;

    using Dapper;

    using NUnit.Framework;

    using Sentinel.OAuth.Core.Interfaces.Providers;
    using Sentinel.OAuth.Implementation.Managers;
    using Sentinel.OAuth.Implementation.Providers;
    using Sentinel.OAuth.UserManagers.SqlServerUserRepository.Implementation;

    [TestFixture]
    [Category("Integration")]
    public class SqlServerUserManagerTests : UserManagerTests
    {
        private TemporarySqlLocalDbInstance instance;

        private string databaseName;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            if (!SqlLocalDbApi.IsLocalDBInstalled())
            {
                throw new Exception("LocalDB is not installed!");
            }

            this.databaseName = "SqlServerUserManagerTests_" + Guid.NewGuid().ToString("N");

            // Configure dapper to support datetime2
            SqlMapper.AddTypeMap(typeof(DateTime), DbType.DateTime2);

            // Create test instance
            this.instance = TemporarySqlLocalDbInstance.Create(deleteFiles: true);

            // Seed test data
            using (var connection = this.instance.CreateConnection())
            {
                connection.Open();

                try
                {
                    connection.Execute("CREATE DATABASE " + this.databaseName);
                    connection.Execute("USE " + this.databaseName);
                    connection.Execute("CREATE TABLE Users (Id bigint NOT NULL PRIMARY KEY IDENTITY(1,1), UserId VARCHAR(255) NOT NULL, Password VARCHAR(MAX) NOT NULL, Enabled BIT NULL, FirstName NVARCHAR(255) NOT NULL, LastName NVARCHAR(255) NOT NULL, LastLogin DATETIMEOFFSET, Created DATETIMEOFFSET)");
                    connection.Execute("INSERT INTO Users (UserId, Password, FirstName, LastName) VALUES ('azzlack', '10000:gW7zpVeugKl8IFu7TcpPskcgQjy4185eAwBk9fFlZK6JNd1I45tLyCYtJrzWzE+kVCUP7lMSY8o808EjUgfavBzYU/ZtWypcdCdCJ0BMfMcf8Mk+XIYQCQLiFpt9Rjrf5mAY86NuveUtd1yBdPjxX5neMXEtquNYhu9I6iyzcN4=:Lk2ZkpmTDkNtO/tsB/GskMppdAX2bXehP+ED4oLis0AAv3Q1VeI8KL0SxIIWdxjKH0NJKZ6qniRFkfZKZRS2hS4SB8oyB34u/jyUlmv+RZGZSt9nJ9FYJn1percd/yFA7sSQOpkGljJ6OTwdthe0Bw0A/8qlKHbO2y2M5BFgYHY=', 'Ove', 'Andersen')");
                    connection.Execute("CREATE TABLE UserApiKeys (Id bigint NOT NULL PRIMARY KEY IDENTITY(1,1), UserId VARCHAR(255) NOT NULL, ApiKey VARCHAR(MAX) NOT NULL, Name VARCHAR(255) NOT NULL, Description VARCHAR(MAX) NOT NULL, Created DATETIMEOFFSET, LastUsed DATETIMEOFFSET)");
                    connection.Execute("INSERT INTO UserApiKeys (UserId, ApiKey, Name, Description) VALUES ('azzlack', 'PFJTQUtleVZhbHVlPjxNb2R1bHVzPnlidFpyM0pWS0p1L2hlUFMrV0Zla1kyYmRYVDlJMU1MeHZheTlIMW9IenRwRmI4QzJtQmUzY1EzVDhjUzE0ajJ4bk9lRkt2YVZ4Ukw5S2ozd0tOL1B3PT08L01vZHVsdXM+PEV4cG9uZW50PkFRQUI8L0V4cG9uZW50PjwvUlNBS2V5VmFsdWU+', 'Test', 'This is a test')");
                }
                finally
                {
                    connection.Close();
                }
            }

            base.TestFixtureSetUp();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            this.instance.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            var connectionStringBuilder = this.instance.CreateConnectionStringBuilder();
            connectionStringBuilder.SetInitialCatalogName(this.databaseName);

            this.UserManager = new UserManager(
                new PBKDF2CryptoProvider(),
                new AsymmetricCryptoProvider(), 
                new SqlServerUserRepository(connectionStringBuilder.ToString()),
                new SqlServerUserApiKeyRepository(connectionStringBuilder.ToString()));

            base.SetUp();
        }
    }
}