using JsonLD.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Newtonsoft.Json.Linq;
using Pandacap.ActivityPub.HttpSignatures.Discovery;
using Pandacap.ActivityPub.HttpSignatures.Discovery.Interfaces;
using Pandacap.ActivityPub.Models;
using Pandacap.ActivityPub.Services.Interfaces;

namespace Pandacap.ActivityPub.HttpSignatures.Discovery.Tests
{
    [TestClass]
    public sealed class ActivityAuthenticatorTests
    {
        private const string MASTODON_SIGNATURE = "keyId=\"https://activitypub.academy/users/dubonus_ladinut#main-key\",algorithm=\"rsa-sha256\",headers=\"(request-target) host date digest content-type\",signature=\"idfUYIKIPSpydlYhnH6JC3/IU9uSu1R1ALHaaoLBx9DRy28serhFpBIq9j3hMZYEzQ7VVOJmKoEQGj2lZSgiq81lnpzDxzVdUeOLK4BfcW8eKE1aXt4kaN4T/DZsn/aeWxJ0iLtCctHGhauavgEq6IflzdoQ6qcos7HxeohCoPpce60vViydlAzzyQy56kV0OAzC31du1KmzfUbqdqoHBhbmiKY9mBMvjBczFBlBwZ5vKpB36JLA3wp22D3Li4LTsY/PPP1/sjkfcTIUM7Npov8PPMk/+q60VNzEEXmuHIvhxO60JGmtn7kmOZLqaxsFVmfFjQATvWdzqrqD6jJ/pQ==\"";
        private const string MASTODON_PEM_KEY = "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxtMke49NEZcyP0jgZdKH\n+uyqxBOLKBZOOO9q7zeOhcT+M2+ce6bT0QXFYEPK+bfhN1g9bkFso/hj4v9pauvq\nkVfSwqKOh5HywMDMjQVlsDD3uVJwHQtjnybkMAamIZcRfIGyiTiKkz0gpnN5jipi\nwpIq8QBW6E7h1QiupiCmq4Um4y1qsXwDSGDGUwu3AQ9A5HVujKtuNxPlSFnMj8y8\nHIs1YN14F3KybU38x0DlZtd9rpuDgQcrwQyTPy91rBPN/Cttd6vwDL8rlBmiTFJX\nJs/ai+eMNWDzSM45RNWY9SZT0N4AY4ZmShZrd6ESSrRFD9M+8FbC5D7NPmJEqlds\nTwIDAQAB\n-----END PUBLIC KEY-----\n";

        [TestMethod]
        public async Task AcquireKeyAsync_FindsActorObject_ContainsSingleKey()
        {
            var cancellationToken = CancellationToken.None;

            var handlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            handlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new("https://activitypub.academy/users/dubonus_ladinut"),
                    cancellationToken))
                .ReturnsAsync(@"{
                    ""@context"": [
                        ""https://www.w3.org/ns/activitystreams"",
                        ""https://w3id.org/security/v1""
                    ],
                    ""id"": ""https://activitypub.academy/users/dubonus_ladinut"",
                    ""type"": ""Person"",
                    ""publicKey"": {
                        ""id"": ""https://activitypub.academy/users/dubonus_ladinut#main-key"",
                        ""owner"": ""https://activitypub.academy/users/dubonus_ladinut"",
                        ""publicKeyPem"": ""-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxtMke49NEZcyP0jgZdKH\n+uyqxBOLKBZOOO9q7zeOhcT+M2+ce6bT0QXFYEPK+bfhN1g9bkFso/hj4v9pauvq\nkVfSwqKOh5HywMDMjQVlsDD3uVJwHQtjnybkMAamIZcRfIGyiTiKkz0gpnN5jipi\nwpIq8QBW6E7h1QiupiCmq4Um4y1qsXwDSGDGUwu3AQ9A5HVujKtuNxPlSFnMj8y8\nHIs1YN14F3KybU38x0DlZtd9rpuDgQcrwQyTPy91rBPN/Cttd6vwDL8rlBmiTFJX\nJs/ai+eMNWDzSM45RNWY9SZT0N4AY4ZmShZrd6ESSrRFD9M+8FbC5D7NPmJEqlds\nTwIDAQAB\n-----END PUBLIC KEY-----\n""
                    }
                }");

            var headersMock = new Mock<IHeaderDictionary>(MockBehavior.Strict);
            headersMock
                .Setup(headers => headers["signature"])
                .Returns(MASTODON_SIGNATURE);
            var requestMock = new Mock<HttpRequest>(MockBehavior.Strict);
            requestMock
                .Setup(req => req.Headers)
                .Returns(headersMock.Object);

            var authenticator = GetActivityPubKeyFinder(
                handlerMock.Object);

            var results = await authenticator
                .AcquireKeysAsync(
                    requestMock.Object,
                    cancellationToken)
                .ToListAsync(cancellationToken);

            var result = results.Single();

            Assert.AreEqual(
                "https://activitypub.academy/users/dubonus_ladinut",
                result.Owner);

            Assert.AreEqual(
                "https://activitypub.academy/users/dubonus_ladinut#main-key",
                result.KeyId);

            Assert.AreEqual(
                MASTODON_PEM_KEY,
                result.KeyPem);
        }

        [TestMethod]
        public async Task AcquireKeyAsync_FindsActorObject_ContainsMultipleKeys()
        {
            var cancellationToken = CancellationToken.None;

            var handlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            handlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new("https://activitypub.academy/users/dubonus_ladinut"),
                    cancellationToken))
                .ReturnsAsync(@"{
                    ""@context"": [
                        ""https://www.w3.org/ns/activitystreams"",
                        ""https://w3id.org/security/v1""
                    ],
                    ""id"": ""https://activitypub.academy/users/dubonus_ladinut"",
                    ""type"": ""Person"",
                    ""publicKey"": [
                        {
                            ""id"": ""https://activitypub.academy/users/dubonus_ladinut#alternate-key"",
                            ""owner"": ""https://activitypub.academy/users/dubonus_ladinut"",
                            ""publicKeyPem"": ""abc""
                        },
                        {
                            ""id"": ""https://activitypub.academy/users/dubonus_ladinut#main-key"",
                            ""owner"": ""https://activitypub.academy/users/dubonus_ladinut"",
                            ""publicKeyPem"": ""-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxtMke49NEZcyP0jgZdKH\n+uyqxBOLKBZOOO9q7zeOhcT+M2+ce6bT0QXFYEPK+bfhN1g9bkFso/hj4v9pauvq\nkVfSwqKOh5HywMDMjQVlsDD3uVJwHQtjnybkMAamIZcRfIGyiTiKkz0gpnN5jipi\nwpIq8QBW6E7h1QiupiCmq4Um4y1qsXwDSGDGUwu3AQ9A5HVujKtuNxPlSFnMj8y8\nHIs1YN14F3KybU38x0DlZtd9rpuDgQcrwQyTPy91rBPN/Cttd6vwDL8rlBmiTFJX\nJs/ai+eMNWDzSM45RNWY9SZT0N4AY4ZmShZrd6ESSrRFD9M+8FbC5D7NPmJEqlds\nTwIDAQAB\n-----END PUBLIC KEY-----\n""
                        },
                        {
                            ""id"": ""https://activitypub.academy/users/dubonus_ladinut#bonus-key"",
                            ""owner"": ""https://activitypub.academy/users/dubonus_ladinut"",
                            ""publicKeyPem"": ""xyz""
                        }
                    ]
                }");

            var headersMock = new Mock<IHeaderDictionary>(MockBehavior.Strict);
            headersMock
                .Setup(headers => headers["signature"])
                .Returns(MASTODON_SIGNATURE);
            var requestMock = new Mock<HttpRequest>(MockBehavior.Strict);
            requestMock
                .Setup(req => req.Headers)
                .Returns(headersMock.Object);

            var authenticator = GetActivityPubKeyFinder(
                handlerMock.Object);

            var results = await authenticator
                .AcquireKeysAsync(
                    requestMock.Object,
                    cancellationToken)
                .ToListAsync(cancellationToken);

            var result = results.Single();

            Assert.AreEqual(
                "https://activitypub.academy/users/dubonus_ladinut",
                result.Owner);

            Assert.AreEqual(
                "https://activitypub.academy/users/dubonus_ladinut#main-key",
                result.KeyId);

            Assert.AreEqual(
                MASTODON_PEM_KEY,
                result.KeyPem);
        }

        [TestMethod]
        public async Task AcquireKeyAsync_FindsActorObject_ContainsKeyId_FetchesKey()
        {
            var cancellationToken = CancellationToken.None;

            var handlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            handlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new("https://activitypub.academy/users/dubonus_ladinut"),
                    cancellationToken))
                .ReturnsAsync(@"{
                    ""@context"": [
                        ""https://www.w3.org/ns/activitystreams"",
                        ""https://w3id.org/security/v1""
                    ],
                    ""id"": ""https://activitypub.academy/users/dubonus_ladinut"",
                    ""type"": ""Person"",
                    ""publicKey"": ""https://activitypub.academy/users/dubonus_ladinut/test-key""
                }");
            handlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new("https://activitypub.academy/users/dubonus_ladinut/test-key"),
                    cancellationToken))
                .ReturnsAsync(@"{
                    ""@context"": ""https://w3id.org/security/v1"",
                    ""@id"": ""https://activitypub.academy/users/dubonus_ladinut/test-key"",
                    ""@type"": ""Key"",
                    ""owner"": ""https://activitypub.academy/users/dubonus_ladinut"",
                    ""publicKeyPem"": ""-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxtMke49NEZcyP0jgZdKH\n+uyqxBOLKBZOOO9q7zeOhcT+M2+ce6bT0QXFYEPK+bfhN1g9bkFso/hj4v9pauvq\nkVfSwqKOh5HywMDMjQVlsDD3uVJwHQtjnybkMAamIZcRfIGyiTiKkz0gpnN5jipi\nwpIq8QBW6E7h1QiupiCmq4Um4y1qsXwDSGDGUwu3AQ9A5HVujKtuNxPlSFnMj8y8\nHIs1YN14F3KybU38x0DlZtd9rpuDgQcrwQyTPy91rBPN/Cttd6vwDL8rlBmiTFJX\nJs/ai+eMNWDzSM45RNWY9SZT0N4AY4ZmShZrd6ESSrRFD9M+8FbC5D7NPmJEqlds\nTwIDAQAB\n-----END PUBLIC KEY-----\n""
                }");

            var headersMock = new Mock<IHeaderDictionary>(MockBehavior.Strict);
            headersMock
                .Setup(headers => headers["signature"])
                .Returns(MASTODON_SIGNATURE.Replace(
                    "https://activitypub.academy/users/dubonus_ladinut#main-key",
                    "https://activitypub.academy/users/dubonus_ladinut/test-key"));
            var requestMock = new Mock<HttpRequest>(MockBehavior.Strict);
            requestMock
                .Setup(req => req.Headers)
                .Returns(headersMock.Object);

            var authenticator = GetActivityPubKeyFinder(
                handlerMock.Object);

            var results = await authenticator
                .AcquireKeysAsync(
                    requestMock.Object,
                    cancellationToken)
                .ToListAsync(cancellationToken);

            var result = results.Single();

            Assert.AreEqual(
                "https://activitypub.academy/users/dubonus_ladinut",
                result.Owner);

            Assert.AreEqual(
                "https://activitypub.academy/users/dubonus_ladinut/test-key",
                result.KeyId);

            Assert.AreEqual(
                MASTODON_PEM_KEY,
                result.KeyPem);
        }

        [TestMethod]
        public async Task AcquireKeyAsync_FindsRawKeyObject_FetchesActor()
        {
            var cancellationToken = CancellationToken.None;

            var handlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            handlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new("https://www.example.com/keys/dubonus_ladinut"),
                    cancellationToken))
                .ReturnsAsync(@"{
                    ""@context"": ""https://w3id.org/security/v1"",
                    ""@id"": ""https://www.example.com/keys/dubonus_ladinut"",
                    ""@type"": ""Key"",
                    ""owner"": ""https://www.example.com/keys/dubonus_ladinut/actor"",
                    ""publicKeyPem"": ""-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxtMke49NEZcyP0jgZdKH\n+uyqxBOLKBZOOO9q7zeOhcT+M2+ce6bT0QXFYEPK+bfhN1g9bkFso/hj4v9pauvq\nkVfSwqKOh5HywMDMjQVlsDD3uVJwHQtjnybkMAamIZcRfIGyiTiKkz0gpnN5jipi\nwpIq8QBW6E7h1QiupiCmq4Um4y1qsXwDSGDGUwu3AQ9A5HVujKtuNxPlSFnMj8y8\nHIs1YN14F3KybU38x0DlZtd9rpuDgQcrwQyTPy91rBPN/Cttd6vwDL8rlBmiTFJX\nJs/ai+eMNWDzSM45RNWY9SZT0N4AY4ZmShZrd6ESSrRFD9M+8FbC5D7NPmJEqlds\nTwIDAQAB\n-----END PUBLIC KEY-----\n""
                }");
            handlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new("https://www.example.com/keys/dubonus_ladinut/actor"),
                    cancellationToken))
                .ReturnsAsync(@"{
                    ""@context"": [
                        ""https://www.w3.org/ns/activitystreams"",
                        ""https://w3id.org/security/v1""
                    ],
                    ""id"": ""https://www.example.com/keys/dubonus_ladinut/actor"",
                    ""type"": ""Person"",
                    ""publicKey"": ""https://www.example.com/keys/dubonus_ladinut""
                }");

            var headersMock = new Mock<IHeaderDictionary>(MockBehavior.Strict);
            headersMock
                .Setup(headers => headers["signature"])
                .Returns(MASTODON_SIGNATURE.Replace(
                    "https://activitypub.academy/users/dubonus_ladinut#main-key",
                    "https://www.example.com/keys/dubonus_ladinut"));
            var requestMock = new Mock<HttpRequest>(MockBehavior.Strict);
            requestMock
                .Setup(req => req.Headers)
                .Returns(headersMock.Object);

            var authenticator = GetActivityPubKeyFinder(
                handlerMock.Object);

            var results = await authenticator
                .AcquireKeysAsync(
                    requestMock.Object,
                    cancellationToken)
                .ToListAsync(cancellationToken);

            var result = results.Single();

            Assert.AreEqual(
                "https://www.example.com/keys/dubonus_ladinut/actor",
                result.Owner);

            Assert.AreEqual(
                "https://www.example.com/keys/dubonus_ladinut",
                result.KeyId);

            Assert.AreEqual(
                MASTODON_PEM_KEY,
                result.KeyPem);
        }

        [TestMethod]
        public async Task AcquireKeyAsync_FindsActorObject_SingleKeyIdDoesNotMatch()
        {
            var cancellationToken = CancellationToken.None;

            var handlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            handlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new("https://activitypub.academy/users/dubonus_ladinut"),
                    cancellationToken))
                .ReturnsAsync(@"{
                    ""@context"": [
                        ""https://www.w3.org/ns/activitystreams"",
                        ""https://w3id.org/security/v1""
                    ],
                    ""id"": ""https://activitypub.academy/users/dubonus_ladinut"",
                    ""type"": ""Person"",
                    ""publicKey"": {
                        ""id"": ""https://activitypub.academy/users/dubonus_ladinut#wrong-key"",
                        ""owner"": ""https://activitypub.academy/users/dubonus_ladinut"",
                        ""publicKeyPem"": ""-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxtMke49NEZcyP0jgZdKH\n+uyqxBOLKBZOOO9q7zeOhcT+M2+ce6bT0QXFYEPK+bfhN1g9bkFso/hj4v9pauvq\nkVfSwqKOh5HywMDMjQVlsDD3uVJwHQtjnybkMAamIZcRfIGyiTiKkz0gpnN5jipi\nwpIq8QBW6E7h1QiupiCmq4Um4y1qsXwDSGDGUwu3AQ9A5HVujKtuNxPlSFnMj8y8\nHIs1YN14F3KybU38x0DlZtd9rpuDgQcrwQyTPy91rBPN/Cttd6vwDL8rlBmiTFJX\nJs/ai+eMNWDzSM45RNWY9SZT0N4AY4ZmShZrd6ESSrRFD9M+8FbC5D7NPmJEqlds\nTwIDAQAB\n-----END PUBLIC KEY-----\n""
                    }
                }");

            var headersMock = new Mock<IHeaderDictionary>(MockBehavior.Strict);
            headersMock
                .Setup(headers => headers["signature"])
                .Returns(MASTODON_SIGNATURE);
            var requestMock = new Mock<HttpRequest>(MockBehavior.Strict);
            requestMock
                .Setup(req => req.Headers)
                .Returns(headersMock.Object);

            var authenticator = GetActivityPubKeyFinder(
                handlerMock.Object);

            var results = await authenticator
                .AcquireKeysAsync(
                    requestMock.Object,
                    cancellationToken)
                .ToListAsync(cancellationToken);

            var result = results.Single();

            Assert.AreEqual(
                "https://activitypub.academy/users/dubonus_ladinut",
                result.Owner);

            Assert.AreEqual(
                "https://activitypub.academy/users/dubonus_ladinut#wrong-key",
                result.KeyId);

            Assert.AreEqual(
                MASTODON_PEM_KEY,
                result.KeyPem);
        }

        [TestMethod]
        public async Task AcquireKeyAsync_FindsActorObject_KeyIdsDoNotMatch()
        {
            var cancellationToken = CancellationToken.None;

            var handlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            handlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new("https://activitypub.academy/users/dubonus_ladinut"),
                    cancellationToken))
                .ReturnsAsync(@"{
                    ""@context"": [
                        ""https://www.w3.org/ns/activitystreams"",
                        ""https://w3id.org/security/v1""
                    ],
                    ""id"": ""https://activitypub.academy/users/dubonus_ladinut"",
                    ""type"": ""Person"",
                    ""publicKey"": [
                        ""https://activitypub.academy/users/dubonus_ladinut#wrong-key-1"",
                        ""https://activitypub.academy/users/dubonus_ladinut/wrong-key-2""
                    ]
                }");
            handlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new("https://activitypub.academy/users/dubonus_ladinut/wrong-key-2"),
                    cancellationToken))
                .Throws<ActivityJsonNotFoundException>();

            var headersMock = new Mock<IHeaderDictionary>(MockBehavior.Strict);
            headersMock
                .Setup(headers => headers["signature"])
                .Returns(MASTODON_SIGNATURE);
            var requestMock = new Mock<HttpRequest>(MockBehavior.Strict);
            requestMock
                .Setup(req => req.Headers)
                .Returns(headersMock.Object);

            var authenticator = GetActivityPubKeyFinder(
                handlerMock.Object);

            var results = await authenticator
                .AcquireKeysAsync(
                    requestMock.Object,
                    cancellationToken)
                .ToListAsync(cancellationToken);

            Assert.IsEmpty(results);
        }

        [TestMethod]
        public async Task AcquireKeyAsync_CachesResponses()
        {
            var cancellationToken = CancellationToken.None;

            var handlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            handlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new("https://activitypub.academy/users/dubonus_ladinut"),
                    cancellationToken))
                .ReturnsAsync(@"{
                    ""@context"": [
                        ""https://www.w3.org/ns/activitystreams"",
                        ""https://w3id.org/security/v1""
                    ],
                    ""id"": ""https://activitypub.academy/users/dubonus_ladinut"",
                    ""type"": ""Person"",
                    ""publicKey"": {
                        ""id"": ""https://activitypub.academy/users/dubonus_ladinut#main-key"",
                        ""owner"": ""https://activitypub.academy/users/dubonus_ladinut"",
                        ""publicKeyPem"": ""-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxtMke49NEZcyP0jgZdKH\n+uyqxBOLKBZOOO9q7zeOhcT+M2+ce6bT0QXFYEPK+bfhN1g9bkFso/hj4v9pauvq\nkVfSwqKOh5HywMDMjQVlsDD3uVJwHQtjnybkMAamIZcRfIGyiTiKkz0gpnN5jipi\nwpIq8QBW6E7h1QiupiCmq4Um4y1qsXwDSGDGUwu3AQ9A5HVujKtuNxPlSFnMj8y8\nHIs1YN14F3KybU38x0DlZtd9rpuDgQcrwQyTPy91rBPN/Cttd6vwDL8rlBmiTFJX\nJs/ai+eMNWDzSM45RNWY9SZT0N4AY4ZmShZrd6ESSrRFD9M+8FbC5D7NPmJEqlds\nTwIDAQAB\n-----END PUBLIC KEY-----\n""
                    }
                }");

            var headersMock = new Mock<IHeaderDictionary>(MockBehavior.Strict);
            headersMock
                .Setup(headers => headers["signature"])
                .Returns(MASTODON_SIGNATURE);
            var requestMock = new Mock<HttpRequest>(MockBehavior.Strict);
            requestMock
                .Setup(req => req.Headers)
                .Returns(headersMock.Object);

            var authenticator = GetActivityPubKeyFinder(
                handlerMock.Object);

            await authenticator
                 .AcquireKeysAsync(
                     requestMock.Object,
                     cancellationToken)
                 .ToListAsync(cancellationToken);
            await authenticator
                 .AcquireKeysAsync(
                     requestMock.Object,
                     cancellationToken)
                 .ToListAsync(cancellationToken);

            handlerMock.Verify(
                handler => handler.GetJsonAsync(
                    new("https://activitypub.academy/users/dubonus_ladinut"),
                    cancellationToken),
                Times.Once);
        }

        [TestMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MSTEST0049:Flow TestContext.CancellationToken to async operations", Justification = "Testing situation where token is not provided")]
        public async Task AcquireKeyAsync_DoesNotRequireExplicitCancellationToken()
        {
            var handlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            handlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new("https://activitypub.academy/users/dubonus_ladinut"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"{
                    ""@context"": [
                        ""https://www.w3.org/ns/activitystreams"",
                        ""https://w3id.org/security/v1""
                    ],
                    ""id"": ""https://activitypub.academy/users/dubonus_ladinut"",
                    ""type"": ""Person"",
                    ""publicKey"": {
                        ""id"": ""https://activitypub.academy/users/dubonus_ladinut#main-key"",
                        ""owner"": ""https://activitypub.academy/users/dubonus_ladinut"",
                        ""publicKeyPem"": ""-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxtMke49NEZcyP0jgZdKH\n+uyqxBOLKBZOOO9q7zeOhcT+M2+ce6bT0QXFYEPK+bfhN1g9bkFso/hj4v9pauvq\nkVfSwqKOh5HywMDMjQVlsDD3uVJwHQtjnybkMAamIZcRfIGyiTiKkz0gpnN5jipi\nwpIq8QBW6E7h1QiupiCmq4Um4y1qsXwDSGDGUwu3AQ9A5HVujKtuNxPlSFnMj8y8\nHIs1YN14F3KybU38x0DlZtd9rpuDgQcrwQyTPy91rBPN/Cttd6vwDL8rlBmiTFJX\nJs/ai+eMNWDzSM45RNWY9SZT0N4AY4ZmShZrd6ESSrRFD9M+8FbC5D7NPmJEqlds\nTwIDAQAB\n-----END PUBLIC KEY-----\n""
                    }
                }");

            var headersMock = new Mock<IHeaderDictionary>(MockBehavior.Strict);
            headersMock
                .Setup(headers => headers["signature"])
                .Returns(MASTODON_SIGNATURE);
            var requestMock = new Mock<HttpRequest>(MockBehavior.Strict);
            requestMock
                .Setup(req => req.Headers)
                .Returns(headersMock.Object);

            var authenticator = GetActivityPubKeyFinder(
                handlerMock.Object);

            await authenticator
                 .AcquireKeysAsync(
                     requestMock.Object)
                 .ToListAsync();

            handlerMock.Verify(
                handler => handler.GetJsonAsync(
                    new("https://activitypub.academy/users/dubonus_ladinut"),
                    CancellationToken.None),
                Times.Once);
        }

        [TestMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MSTEST0049:Flow TestContext.CancellationToken to async operations", Justification = "Testing situation where token is not provided")]
        public async Task AcquireKeyAsync_GetsCancellationTokenFromAsyncEnumerable()
        {
            var handlerMock = new Mock<IActivityPubRequestHandler>(MockBehavior.Strict);
            handlerMock
                .Setup(handler => handler.GetJsonAsync(
                    new("https://activitypub.academy/users/dubonus_ladinut"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(@"{
                    ""@context"": [
                        ""https://www.w3.org/ns/activitystreams"",
                        ""https://w3id.org/security/v1""
                    ],
                    ""id"": ""https://activitypub.academy/users/dubonus_ladinut"",
                    ""type"": ""Person"",
                    ""publicKey"": {
                        ""id"": ""https://activitypub.academy/users/dubonus_ladinut#main-key"",
                        ""owner"": ""https://activitypub.academy/users/dubonus_ladinut"",
                        ""publicKeyPem"": ""-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxtMke49NEZcyP0jgZdKH\n+uyqxBOLKBZOOO9q7zeOhcT+M2+ce6bT0QXFYEPK+bfhN1g9bkFso/hj4v9pauvq\nkVfSwqKOh5HywMDMjQVlsDD3uVJwHQtjnybkMAamIZcRfIGyiTiKkz0gpnN5jipi\nwpIq8QBW6E7h1QiupiCmq4Um4y1qsXwDSGDGUwu3AQ9A5HVujKtuNxPlSFnMj8y8\nHIs1YN14F3KybU38x0DlZtd9rpuDgQcrwQyTPy91rBPN/Cttd6vwDL8rlBmiTFJX\nJs/ai+eMNWDzSM45RNWY9SZT0N4AY4ZmShZrd6ESSrRFD9M+8FbC5D7NPmJEqlds\nTwIDAQAB\n-----END PUBLIC KEY-----\n""
                    }
                }");

            var headersMock = new Mock<IHeaderDictionary>(MockBehavior.Strict);
            headersMock
                .Setup(headers => headers["signature"])
                .Returns(MASTODON_SIGNATURE);
            var requestMock = new Mock<HttpRequest>(MockBehavior.Strict);
            requestMock
                .Setup(req => req.Headers)
                .Returns(headersMock.Object);

            var authenticator = GetActivityPubKeyFinder(
                handlerMock.Object);
            
            using var cts = new CancellationTokenSource();

            await authenticator
                 .AcquireKeysAsync(
                     requestMock.Object)
                 .ToListAsync(cts.Token);

            handlerMock.Verify(
                handler => handler.GetJsonAsync(
                    new("https://activitypub.academy/users/dubonus_ladinut"),
                    CancellationToken.None),
                Times.Never);

            handlerMock.Verify(
                handler => handler.GetJsonAsync(
                    new("https://activitypub.academy/users/dubonus_ladinut"),
                    cts.Token),
                Times.Once);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Testing interface, not public class methods")]
        private static IActivityPubKeyFinder GetActivityPubKeyFinder(
            IActivityPubRequestHandler activityPubRequestHandler)
        => new ActivityPubKeyFinder(
            UseOrMock(activityPubRequestHandler),
            new JsonLdExpansionService(),
            new MemoryCache(new MemoryCacheOptions()));

        private static T UseOrMock<T>(T dependency) where T : class =>
            dependency ?? new Mock<T>(MockBehavior.Strict).Object;

        private class JsonLdExpansionService : IJsonLdExpansionService
        {
            public JToken ExpandFirst(JObject jObject) =>
                JsonLdProcessor.Expand(jObject).Single();
        }
    }
}
