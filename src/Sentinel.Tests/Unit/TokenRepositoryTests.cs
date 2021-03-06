﻿namespace Sentinel.Tests.Unit
{
    using NUnit.Framework;
    using Sentinel.OAuth.Core.Interfaces.Repositories;
    using Sentinel.OAuth.Core.Models.OAuth;
    using System;
    using System.Diagnostics;
    using System.Linq;

    public abstract class TokenRepositoryTests
    {
        private Stopwatch testFixtureStopwatch;

        private Stopwatch testStopwatch;

        public ITokenRepository TokenRepository { get; set; }

        [TestFixtureSetUp]
        public virtual void TestFixtureSetUp()
        {
            this.testFixtureStopwatch = new Stopwatch();
            this.testFixtureStopwatch.Start();
        }

        [TestFixtureTearDown]
        public virtual void TestFixtureTearDown()
        {
            this.testFixtureStopwatch.Stop();

            Console.WriteLine();
            Console.WriteLine($"Time taken for all tests: {this.testFixtureStopwatch.Elapsed.ToString("g")}");
            Console.WriteLine($"##teamcity[buildStatisticValue key='{this.GetType().Name}' value='{this.testFixtureStopwatch.ElapsedMilliseconds}']");
        }

        [SetUp]
        public virtual void SetUp()
        {
            this.testStopwatch = new Stopwatch();
            this.testStopwatch.Start();
        }

        [TearDown]
        public virtual void TearDown()
        {
            this.testStopwatch.Stop();

            Console.WriteLine();
            Console.WriteLine($"Time taken for test: {this.testStopwatch.Elapsed.ToString("g")}");
            Console.WriteLine($"##teamcity[buildStatisticValue key='{this.GetType().Name}.{TestContext.CurrentContext.Test.Name}' value='{this.testStopwatch.ElapsedMilliseconds}']");

            var purgeResult = this.TokenRepository.Purge().Result;
            Assert.IsTrue(purgeResult, "Purge did not work properly");
        }

        [Test]
        public async void GetAuthorizationCode_WhenGivenValidIdentifier_ReturnsCode()
        {
            var insertResult = await this.TokenRepository.InsertAuthorizationCode(new AuthorizationCode { Code = "123456789", Ticket = "abcdef", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "Username" });

            var getResult = await this.TokenRepository.GetAuthorizationCode(insertResult.GetIdentifier());

            Assert.AreEqual(insertResult.GetIdentifier(), getResult.GetIdentifier());
            Assert.AreEqual(insertResult.ClientId, getResult.ClientId);
            Assert.AreEqual(insertResult.RedirectUri, getResult.RedirectUri);
            Assert.AreEqual(insertResult.Subject, getResult.Subject);
            Assert.AreEqual(insertResult.Scope, getResult.Scope);
            Assert.AreEqual(insertResult.ValidTo, getResult.ValidTo);
            Assert.AreEqual(insertResult.Ticket, getResult.Ticket);
            Assert.AreEqual(insertResult.Code, getResult.Code);
        }

        [Test]
        public async void GetAuthorizationCode_WhenGivenInvalidIdentifier_ThrowsException()
        {
            Assert.That(async () => await this.TokenRepository.GetAuthorizationCode(null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public async void GetAuthorizationCodes_WhenValidCodesExist_ReturnsAuthorizationCodes()
        {
            await this.TokenRepository.InsertAuthorizationCode(new AuthorizationCode { Code = "123456789", Ticket = "abcdef", ValidTo = DateTimeOffset.UtcNow, ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "Username" });
            await this.TokenRepository.InsertAuthorizationCode(new AuthorizationCode { Code = "234567890", Ticket = "abcdef", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit2", RedirectUri = "http://localhost", Subject = "Username" });

            var treshold = DateTimeOffset.UtcNow;

            var authorizationCodes = await this.TokenRepository.GetAuthorizationCodes("http://localhost", treshold);

            Assert.GreaterOrEqual(authorizationCodes.Count(), 1);
            Assert.That(authorizationCodes.All(x => x.ValidTo.CompareTo(treshold) > 0), "Got back code that was supposed to be expired");
        }

        [Test]
        public async void InsertAuthorizationCode_WhenGivenValidCode_ReturnsAuthorizationCodes()
        {
            var code = new AuthorizationCode
            {
                Code = "123456789",
                Ticket = "abcdef",
                ValidTo = DateTimeOffset.UtcNow.AddMinutes(1),
                ClientId = "NUnit",
                RedirectUri = "http://localhost",
                Subject = "Username"
            };

            var insertResult = await this.TokenRepository.InsertAuthorizationCode(code);

            Assert.IsNotNull(insertResult);

            Assert.AreEqual(code.ClientId, insertResult.ClientId);
            Assert.AreEqual(code.Code, insertResult.Code);
            Assert.AreEqual(code.RedirectUri, insertResult.RedirectUri);
            Assert.AreEqual(code.Subject, insertResult.Subject);
            Assert.AreEqual(code.Ticket, insertResult.Ticket);
            Assert.AreEqual(code.ValidTo, insertResult.ValidTo);
        }

        [Test]
        public async void InsertAuthorizationCode_WhenGivenInvalidCode_ThrowsException()
        {
            Assert.That(async () => await this.TokenRepository.InsertAuthorizationCode(new AuthorizationCode { Code = "123456789", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost" }), Throws.Exception.TypeOf<ArgumentException>());
            Assert.That(async () => await this.TokenRepository.InsertAuthorizationCode(new AuthorizationCode { Code = "123456789", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", Subject = "Username" }), Throws.Exception.TypeOf<ArgumentException>());
            Assert.That(async () => await this.TokenRepository.InsertAuthorizationCode(new AuthorizationCode { Code = "123456789", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), RedirectUri = "http://localhost", Subject = "Username" }), Throws.Exception.TypeOf<ArgumentException>());
            Assert.That(async () => await this.TokenRepository.InsertAuthorizationCode(new AuthorizationCode { Code = "123456789", ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "Username" }), Throws.Exception.TypeOf<ArgumentException>());
            Assert.That(async () => await this.TokenRepository.InsertAuthorizationCode(new AuthorizationCode { ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "Username" }), Throws.Exception.TypeOf<ArgumentException>());
        }

        [Test]
        public async void DeleteAuthorizationCode_WhenGivenValidCode_ReturnsTrue()
        {
            var insertResult = await this.TokenRepository.InsertAuthorizationCode(new AuthorizationCode { Code = "123456789", Ticket = "abcdef", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "Username" });

            var deleteResult = await this.TokenRepository.DeleteAuthorizationCode(insertResult);
            var authorizationCodes = await this.TokenRepository.GetAuthorizationCodes("http://localhost", DateTimeOffset.UtcNow.AddMinutes(1));

            Assert.IsTrue(deleteResult);
            Assert.IsTrue(authorizationCodes.All(x => !x.Equals(insertResult)), "The authorization code was 'deleted' but is still retrievable");
        }

        [Test]
        public async void DeleteAuthorizationCode_WhenGivenValidIdentifier_ReturnsTrue()
        {
            var insertResult = await this.TokenRepository.InsertAuthorizationCode(new AuthorizationCode { Code = "123456789", Ticket = "abcdef", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "Username" });

            var deleteResult = await this.TokenRepository.DeleteAuthorizationCode(insertResult.GetIdentifier());
            var authorizationCodes = await this.TokenRepository.GetAuthorizationCodes("http://localhost", DateTimeOffset.UtcNow.AddMinutes(1));

            Assert.IsTrue(deleteResult);
            Assert.IsTrue(authorizationCodes.All(x => !x.Equals(insertResult)), "The authorization code was 'deleted' but is still retrievable");
        }

        [Test]
        public async void InsertAccessToken_WhenGivenInvalidToken_ThrowsException()
        {
            Assert.That(async () => await this.TokenRepository.InsertAccessToken(new AccessToken { Token = "123456789", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost" }), Throws.Exception.TypeOf<ArgumentException>());
            Assert.That(async () => await this.TokenRepository.InsertAccessToken(new AccessToken { Token = "123456789", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit" }), Throws.Exception.TypeOf<ArgumentException>());
            Assert.That(async () => await this.TokenRepository.InsertAccessToken(new AccessToken { Token = "123456789", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), RedirectUri = "http://localhost" }), Throws.Exception.TypeOf<ArgumentException>());
            Assert.That(async () => await this.TokenRepository.InsertAccessToken(new AccessToken { Token = "123456789", ClientId = "NUnit", RedirectUri = "http://localhost" }), Throws.Exception.TypeOf<ArgumentException>());
            Assert.That(async () => await this.TokenRepository.InsertAccessToken(new AccessToken { ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost" }), Throws.Exception.TypeOf<ArgumentException>());
        }

        [Test]
        public async void InsertAccessToken_WhenGivenValidAccessToken_ReturnsAccessToken()
        {
            var token = new AccessToken
            {
                Token = "123456789",
                Ticket = "abcdef",
                ValidTo = DateTimeOffset.UtcNow,
                ClientId = "NUnit",
                RedirectUri = "http://localhost",
                Subject = "ovea"
            };
            var result = await this.TokenRepository.InsertAccessToken(token);

            Assert.AreEqual(token.ClientId, result.ClientId);
            Assert.AreEqual(token.RedirectUri, result.RedirectUri);
            Assert.AreEqual(token.Subject, result.Subject);
            Assert.AreEqual(token.Ticket, result.Ticket);
            Assert.AreEqual(token.Token, result.Token);
            Assert.AreEqual(token.ValidTo, result.ValidTo);
        }

        [Test]
        public async void GetAccessToken_WhenGivenValidIdentifier_ReturnsToken()
        {
            var insertResult = await this.TokenRepository.InsertAccessToken(new AccessToken { Token = "123456789", Ticket = "abcdef", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });

            var getResult = await this.TokenRepository.GetAccessToken(insertResult.GetIdentifier());

            Assert.AreEqual(insertResult.GetIdentifier(), getResult.GetIdentifier());
            Assert.AreEqual(insertResult.ClientId, getResult.ClientId);
            Assert.AreEqual(insertResult.RedirectUri, getResult.RedirectUri);
            Assert.AreEqual(insertResult.Subject, getResult.Subject);
            Assert.AreEqual(insertResult.Scope, getResult.Scope);
            Assert.AreEqual(insertResult.ValidTo, getResult.ValidTo);
            Assert.AreEqual(insertResult.Ticket, getResult.Ticket);
            Assert.AreEqual(insertResult.Token, getResult.Token);
        }

        [Test]
        public async void GetAccessToken_WhenGivenInvalidIdentifier_ThrowsException()
        {
            Assert.That(async () => await this.TokenRepository.GetAccessToken(null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public async void GetAccessToken_WhenTokensExists_ReturnsAccessToken()
        {
            var treshold = DateTimeOffset.UtcNow;

            var insertResult = await this.TokenRepository.InsertAccessToken(new AccessToken { Token = "123456789", Ticket = "abcdef", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });

            var accessTokens = await this.TokenRepository.GetAccessTokens(treshold);

            Assert.GreaterOrEqual(accessTokens.Count(), 1);
            Assert.That(accessTokens.All(x => x.ValidTo.CompareTo(treshold) > 0), "Got back token that was supposed to be expired");
        }

        [Test]
        public async void GetAccessTokens_WhenValidTokensExists_ReturnsAccessTokens()
        {
            await this.TokenRepository.InsertAccessToken(new AccessToken { Token = "123456789", Ticket = "abcdef", ValidTo = DateTimeOffset.UtcNow, ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });
            await this.TokenRepository.InsertAccessToken(new AccessToken { Token = "234567890", Ticket = "abcdef", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit2", RedirectUri = "http://localhost", Subject = "ovea" });

            var treshold = DateTimeOffset.UtcNow;

            var accessTokens = await this.TokenRepository.GetAccessTokens(treshold);

            Assert.GreaterOrEqual(accessTokens.Count(), 1);
            Assert.That(accessTokens.All(x => x.ValidTo.CompareTo(treshold) > 0), "Got back token that was supposed to be expired");
        }

        [Test]
        public async void GetAccessTokens_WhenValidTokensExistsForSubject_ReturnsAccessTokens()
        {
            await this.TokenRepository.InsertAccessToken(new AccessToken { Token = "123456789", Ticket = "abcdef", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });
            await this.TokenRepository.InsertAccessToken(new AccessToken { Token = "123456789", Ticket = "abcdef", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit2", RedirectUri = "http://localhost", Subject = "ovea2" });

            var treshold = DateTimeOffset.UtcNow;

            var accessTokens = await this.TokenRepository.GetAccessTokens("ovea", treshold);

            Assert.GreaterOrEqual(accessTokens.Count(), 1);
            Assert.That(accessTokens.All(x => x.Subject == "ovea" && x.ValidTo.CompareTo(treshold) > 0));
        }

        [Test]
        public async void DeleteAccessToken_WhenGivenValidToken_ReturnsTrue()
        {
            var insertResult = await this.TokenRepository.InsertAccessToken(new AccessToken { Token = "123456789", Ticket = "abcdef", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });

            var deleteResult = await this.TokenRepository.DeleteAccessToken(insertResult);
            var accessTokens = await this.TokenRepository.GetAccessTokens(DateTimeOffset.UtcNow.AddMinutes(1));

            Assert.IsTrue(deleteResult);
            Assert.IsTrue(accessTokens.All(x => !x.Equals(insertResult)), "The access token was 'deleted' but is still retrievable");
        }

        [Test]
        public async void DeleteAccessToken_WhenGivenValidIdentifier_ReturnsTrue()
        {
            var insertResult = await this.TokenRepository.InsertAccessToken(new AccessToken { Token = "123456789", Ticket = "abcdef", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });

            var deleteResult = await this.TokenRepository.DeleteAccessToken(insertResult.GetIdentifier());
            var accessTokens = await this.TokenRepository.GetAccessTokens(DateTimeOffset.UtcNow.AddMinutes(1));

            Assert.IsTrue(deleteResult);
            Assert.IsTrue(accessTokens.All(x => !x.Equals(insertResult)), "The access token was 'deleted' but is still retrievable");
        }

        [Test]
        public async void DeleteAccessTokens_WhenGivenExpirationDate_ReturnsNumberOfDeletedTokens()
        {
            var insertResult = await this.TokenRepository.InsertAccessToken(new AccessToken { Token = "123456789", Ticket = "abcdef", ValidTo = DateTimeOffset.UtcNow, ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });

            var deleteResult = await this.TokenRepository.DeleteAccessTokens(DateTimeOffset.UtcNow);
            var accessTokens = await this.TokenRepository.GetAccessTokens(DateTimeOffset.UtcNow);

            Assert.Greater(deleteResult, 0);
            Assert.IsTrue(accessTokens.All(x => !x.Equals(insertResult)), "The access token was 'deleted' but is still retrievable");
        }

        [Test]
        public async void DeleteAccessTokens_WhenGivenValidParams_ReturnsNumberOfDeletedAccessTokens()
        {
            var insertResult = await this.TokenRepository.InsertAccessToken(new AccessToken { Token = "123456789", Ticket = "abcdef", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });

            var deleteResult = await this.TokenRepository.DeleteAccessTokens(insertResult.ClientId, insertResult.RedirectUri, insertResult.Subject);
            var accessTokens = await this.TokenRepository.GetAccessTokens(DateTimeOffset.UtcNow);

            Assert.Greater(deleteResult, 0);
            Assert.IsTrue(accessTokens.All(x => !x.Equals(insertResult)), "The access token was 'deleted' but is still retrievable");
        }

        [Test]
        public async void InsertRefreshToken_WhenGivenInvalidToken_ThrowsException()
        {
            Assert.That(async () => await this.TokenRepository.InsertRefreshToken(new RefreshToken { Token = "123456789", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost" }), Throws.Exception.TypeOf<ArgumentException>());
            Assert.That(async () => await this.TokenRepository.InsertRefreshToken(new RefreshToken { Token = "123456789", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit" }), Throws.Exception.TypeOf<ArgumentException>());
            Assert.That(async () => await this.TokenRepository.InsertRefreshToken(new RefreshToken { Token = "123456789", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), RedirectUri = "http://localhost" }), Throws.Exception.TypeOf<ArgumentException>());
            Assert.That(async () => await this.TokenRepository.InsertRefreshToken(new RefreshToken { Token = "123456789", ClientId = "NUnit", RedirectUri = "http://localhost" }), Throws.Exception.TypeOf<ArgumentException>());
            Assert.That(async () => await this.TokenRepository.InsertRefreshToken(new RefreshToken { ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost" }), Throws.Exception.TypeOf<ArgumentException>());
        }

        [Test]
        public async void InsertRefreshToken_WhenGivenValidTokens_ReturnsRefreshToken()
        {
            var token = new RefreshToken
            {
                Token = "123456789",
                ValidTo = DateTimeOffset.UtcNow.AddMinutes(1),
                ClientId = "NUnit",
                RedirectUri = "http://localhost",
                Subject = "ovea"
            };
            var result = await this.TokenRepository.InsertRefreshToken(token);

            Assert.AreEqual(token.ClientId, result.ClientId);
            Assert.AreEqual(token.RedirectUri, result.RedirectUri);
            Assert.AreEqual(token.Subject, result.Subject);
            Assert.AreEqual(token.Token, result.Token);
            Assert.AreEqual(token.ValidTo, result.ValidTo);
        }

        [Test]
        public async void GetRefreshToken_WhenGivenValidIdentifier_ReturnsToken()
        {
            var insertResult = await this.TokenRepository.InsertRefreshToken(new RefreshToken { Token = "123456789", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });

            var getResult = await this.TokenRepository.GetRefreshToken(insertResult.GetIdentifier());

            Assert.AreEqual(insertResult.GetIdentifier(), getResult.GetIdentifier());
            Assert.AreEqual(insertResult.ClientId, getResult.ClientId);
            Assert.AreEqual(insertResult.RedirectUri, getResult.RedirectUri);
            Assert.AreEqual(insertResult.Subject, getResult.Subject);
            Assert.AreEqual(insertResult.Scope, getResult.Scope);
            Assert.AreEqual(insertResult.ValidTo, getResult.ValidTo);
            Assert.AreEqual(insertResult.Token, getResult.Token);
        }

        [Test]
        public async void GetRefreshToken_WhenGivenInvalidIdentifier_ThrowsException()
        {
            Assert.That(async () => await this.TokenRepository.GetRefreshToken(null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public async void GetRefreshTokens_WhenGivenValidClientIdRedirectUriExpireDate_ReturnsRefreshTokens()
        {
            await this.TokenRepository.InsertRefreshToken(new RefreshToken { Token = "123456789", ValidTo = DateTimeOffset.UtcNow, ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });
            await this.TokenRepository.InsertRefreshToken(new RefreshToken { Token = "234567890", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });

            var treshold = DateTimeOffset.UtcNow;

            var refreshTokens = await this.TokenRepository.GetClientRefreshTokens("NUnit", treshold);

            Assert.GreaterOrEqual(refreshTokens.Count(), 1);
            Assert.That(refreshTokens.All(x => x.ValidTo.CompareTo(treshold) > 0), "Got back token that was supposed to be expired");
        }

        [Test]
        public async void GetRefreshTokens_WhenGivenValidSubjectExpireDate_ReturnsRefreshTokens()
        {
            await this.TokenRepository.InsertRefreshToken(new RefreshToken { Token = "123456789", ValidTo = DateTimeOffset.UtcNow, ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });
            await this.TokenRepository.InsertRefreshToken(new RefreshToken { Token = "234567890", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });

            var treshold = DateTimeOffset.UtcNow;

            var refreshTokens = await this.TokenRepository.GetUserRefreshTokens("ovea", treshold);

            Assert.GreaterOrEqual(refreshTokens.Count(), 1);
            Assert.That(refreshTokens.All(x => x.ValidTo.CompareTo(treshold) > 0), "Got back token that was supposed to be expired");
        }

        [Test]
        public async void DeleteRefreshToken_WhenGivenValidToken_ReturnsTrue()
        {
            var insertResult = await this.TokenRepository.InsertRefreshToken(new RefreshToken { Token = "123456789", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });

            var deleteResult = await this.TokenRepository.DeleteRefreshToken(insertResult);
            var refreshTokens = await this.TokenRepository.GetClientRefreshTokens("NUnit2", DateTimeOffset.UtcNow.AddMinutes(1));

            Assert.IsTrue(deleteResult);
            Assert.IsTrue(refreshTokens.All(x => !x.Equals(insertResult)), "The refresh token was 'deleted' but is still retrievable");
        }

        [Test]
        public async void DeleteRefreshToken_WhenGivenValidIdentifier_ReturnsTrue()
        {
            var insertResult = await this.TokenRepository.InsertRefreshToken(new RefreshToken { Token = "123456789", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });

            var deleteResult = await this.TokenRepository.DeleteRefreshToken(insertResult.GetIdentifier());
            var refreshTokens = await this.TokenRepository.GetClientRefreshTokens("NUnit2", DateTimeOffset.UtcNow.AddMinutes(1));

            Assert.IsTrue(deleteResult);
            Assert.IsTrue(refreshTokens.All(x => !x.Equals(insertResult)), "The refresh token was 'deleted' but is still retrievable");
        }

        [Test]
        public async void DeleteRefreshTokens_WhenGivenExpirationDate_ReturnsNumberOfDeletedRefreshTokens()
        {
            var insertResult = await this.TokenRepository.InsertRefreshToken(new RefreshToken() { Token = "123456789", ValidTo = DateTimeOffset.UtcNow, ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });

            var deleteResult = await this.TokenRepository.DeleteRefreshTokens(DateTimeOffset.UtcNow);
            var refreshTokens = await this.TokenRepository.GetClientRefreshTokens(insertResult.ClientId, DateTimeOffset.UtcNow);

            Assert.Greater(deleteResult, 0);
            Assert.IsTrue(refreshTokens.All(x => !x.Equals(insertResult)), "The refresh token was 'deleted' but is still retrievable");
        }

        [Test]
        public async void DeleteRefreshTokens_WhenGivenClientIdRedirectUriSubject_ReturnsNumberOfDeletedRefreshTokens()
        {
            var insertResult = await this.TokenRepository.InsertRefreshToken(new RefreshToken() { Token = "123456789", ValidTo = DateTimeOffset.UtcNow.AddMinutes(1), ClientId = "NUnit", RedirectUri = "http://localhost", Subject = "ovea" });

            var deleteResult = await this.TokenRepository.DeleteRefreshTokens(insertResult.ClientId, insertResult.RedirectUri, insertResult.Subject);
            var refreshTokens = await this.TokenRepository.GetClientRefreshTokens(insertResult.ClientId, DateTimeOffset.UtcNow);

            Assert.Greater(deleteResult, 0);
            Assert.IsTrue(refreshTokens.All(x => !x.Equals(insertResult)), "The refresh token was 'deleted' but is still retrievable");
        }
    }
}