using Microsoft.AspNetCore.Http;
using Moq;
using NSign;

namespace Pandacap.ActivityPub.HttpSignatures.Validation.Tests
{
    [TestClass]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Makes tests difficult to read")]
    public class MastodonVerifierTests
    {
        [TestMethod]
        public void VerifyRequestSignature_VerifiesPandacapSignature()
        {
            var httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock
                .Setup(request => request.Method)
                .Returns("get");
            httpRequestMock
                .Setup(request => request.Scheme)
                .Returns("https");
            httpRequestMock
                .Setup(request => request.Host)
                .Returns(new HostString("mastodon.art"));
            httpRequestMock
                .Setup(request => request.PathBase)
                .Returns(PathString.Empty);
            httpRequestMock
                .Setup(request => request.Path)
                .Returns("/users/bakertoons/statuses/116173893479806610");
            httpRequestMock
                .Setup(request => request.QueryString)
                .Returns(QueryString.Empty);
            httpRequestMock
                .Setup(request => request.Headers["host"])
                .Returns(new[]
                {
                    "mastodon.art"
                });
            httpRequestMock
                .Setup(request => request.Headers["date"])
                .Returns(new[]
                {
                    "Fri, 20 Mar 2026 03:10:41 GMT"
                });
            httpRequestMock
                .Setup(request => request.Headers["signature"])
                .Returns(new[]
                {
                    @"keyId=""https://pandacap.azurewebsites.net#main-key"",algorithm=""rsa-sha256"",headers=""(request-target) host date"",signature=""Qf79tgX/DizD2EjfWqu2vw/iTqyWhG+nH9b7BVhtvQvVSBm1zfqAPMtrprjyQe1thUy0MPVM6HKeu4W3OCNSt44DIUMghTUOJ5SCd5Yaq5j4L+IBn2SELWSUpu+UjaXyIGHCykwu0DviF2T01jFRTdeQu8A4m2lJPFDFVi5jrYPMbjoOJ7DaXxLVrqfHczeUhmYCWwr1BXu1nKZP2scHnm84YkbPYC2kEQeYQOVE5TJwFZrxMx/IWt5plIjyEDOJgf8oV59/kgWv1i5AnqL27oAuhJZX/CNs83ilwKUcBwoMhEQXa5IwfiOt02ANXcBZq5zP+RXzb80OfYvwCqxGdw=="""
                });

            var keyMock = new Mock<IKey>(MockBehavior.Strict);
            keyMock
                .Setup(key => key.KeyId)
                .Returns("https://pandacap.azurewebsites.net#main-key");
            keyMock
                .Setup(key => key.KeyPem)
                .Returns("-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAvl6GB0lDhDcCVIIRdqFo\nlf4OaKvIIbrhoZdea/Dk/WsSbmmimVx4lblgyGKCTp9AXg86jmhjLCnQmB4qfqv7\nPA92ewh/NsgGgmOJ3eHwsSKVuBlUmzUc/ldVbgT1zUKgCw88amZDsWqMg0w0zt4I\n8q8CQOQQzVoWibEGy+WgJIA/ELs/fOnN+iWZlHePWC37k1njm+HOQtStxRJWUKaG\nZzcAvtWSX7G+OQGlUFnPLwQp3TnfcitF9zeoFsl/s3VfViAr+RiJ2UCtE12yePus\neOoj4fskwCj2SsdEKaL+PtFe3ko0MGxYgwMdFUK8q5gyzUbGzbcHQuAoqN+PRiEc\nRQIDAQAB\n-----END PUBLIC KEY-----\n");

            var verifier = new MastodonVerifier();

            Assert.AreEqual(
                actual: verifier.VerifyRequestSignature(
                    httpRequestMock.Object,
                    keyMock.Object),
                expected: VerificationResult.SuccessfullyVerified);
        }

        [TestMethod]
        public void VerifyRequestSignature_VerifiesMastodonSignature()
        {
            var httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock
                .Setup(request => request.Method)
                .Returns("post");
            httpRequestMock
                .Setup(request => request.Scheme)
                .Returns("https");
            httpRequestMock
                .Setup(request => request.Host)
                .Returns(new HostString("pandacap-demo-gsasaqfrfqffa6b4.eastus-01.azurewebsites.net"));
            httpRequestMock
                .Setup(request => request.PathBase)
                .Returns(PathString.Empty);
            httpRequestMock
                .Setup(request => request.Path)
                .Returns("/ActivityPub/Inbox");
            httpRequestMock
                .Setup(request => request.QueryString)
                .Returns(QueryString.Empty);
            httpRequestMock
                .Setup(request => request.Headers["host"])
                .Returns(new[]
                {
                    "pandacap-demo-gsasaqfrfqffa6b4.eastus-01.azurewebsites.net"
                });
            httpRequestMock
                .Setup(request => request.Headers["date"])
                .Returns(new[]
                {
                    "Sat, 28 Mar 2026 21:57:08 GMT"
                });
            httpRequestMock
                .Setup(request => request.Headers["content-type"])
                .Returns(new[]
                {
                    "application/activity+json"
                });
            httpRequestMock
                .Setup(request => request.Headers["digest"])
                .Returns(new[]
                {
                    "SHA-256=sxfaSXxPDG9U7sVUqJQskaqy+5Wsx5jgTbmKqM5BnR0="
                });
            httpRequestMock
                .Setup(request => request.Headers["user-agent"])
                .Returns(new[]
                {
                    "http.rb/5.1.1 (Mastodon/4.1.18; +https://activitypub.academy/)"
                });
            httpRequestMock
                .Setup(request => request.Headers["signature"])
                .Returns(new[]
                {
                    "keyId=\"https://activitypub.academy/users/afritin_bagrast#main-key\",algorithm=\"rsa-sha256\",headers=\"(request-target) host date digest content-type\",signature=\"cxQb85nAaQR3Z5fS1gx0LeI8icrsuW+zOdYJZ//WDkIGRnyMRXd7qJRR0eL0hhTG00j0wTXjVgm7jZ320QSlu1jTHGYvpCAE4Y4QgPM9370YJuenwaE47vsYKhgOcd3qd7ylqCD3UOigBBbqFXTNYboNNci9KwR0AofqFHR4PxwPs/SHsRAEygGZOtghQSnrR7GLCnpPJdfjrV3hQjSV/2n0IlHGeGyHHyTD5CmVNUYr/xvsRgEvG29v+ppzYMgw/F9ZSa/6WGFN05ocsxCxIuvc9b9ayDhuxHaU+M+KiFkwW0lfsbfacJHB+dYHoby31dpp9OcFCRq1+OMAm+Y6ZQ==\""
                });

            var keyMock = new Mock<IKey>(MockBehavior.Strict);
            keyMock
                .Setup(key => key.KeyId)
                .Returns("https://activitypub.academy/users/afritin_bagrast#main-key");
            keyMock
                .Setup(key => key.KeyPem)
                .Returns("-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAmcLc5iiK8/XPYUjM8jap\n9Kcfa9qUmT8iGT1XzkkIcx7CLsgQwg7cfWdwS35tNfNX9ma0w2ka4rxw8Wa6S0ge\nLLq6Ve5BG0iGkxzs5Yy5EmXjz/GGU1bX31zFR+lJSkhV89sT+drXJRvxnxMbAxjp\n+dAyweLze54OxEgmRilHJ2fG4WgFvh7diMja0NAjDNPVZ1yGbdKksWuX0YqNtPd7\nfBWXUFP+yjNmjRVXp7XbsCUQxW55oKsN3hZntEc9J7CI9NDhpXaZbnvTz84M7nqX\n3RTRmUw813jvXzAvZBMbYnGMxScGQnhtKYxntffuyaHRXT+w9aMZFysBbGJ+kp7+\n4wIDAQAB\n-----END PUBLIC KEY-----\n");

            var verifier = new MastodonVerifier();

            Assert.AreEqual(
                actual: verifier.VerifyRequestSignature(
                    httpRequestMock.Object,
                    keyMock.Object),
                expected: VerificationResult.SuccessfullyVerified);
        }

        [TestMethod]
        public void VerifyRequestSignature_VerifiesPixelfedSignature()
        {
            var httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock
                .Setup(request => request.Method)
                .Returns("post");
            httpRequestMock
                .Setup(request => request.Scheme)
                .Returns("https");
            httpRequestMock
                .Setup(request => request.Host)
                .Returns(new HostString("pandacap-demo-gsasaqfrfqffa6b4.eastus-01.azurewebsites.net"));
            httpRequestMock
                .Setup(request => request.PathBase)
                .Returns(PathString.Empty);
            httpRequestMock
                .Setup(request => request.Path)
                .Returns("/ActivityPub/Inbox");
            httpRequestMock
                .Setup(request => request.QueryString)
                .Returns(QueryString.Empty);
            httpRequestMock
                .Setup(request => request.Headers["host"])
                .Returns(new[]
                {
                    "pandacap-demo-gsasaqfrfqffa6b4.eastus-01.azurewebsites.net"
                });
            httpRequestMock
                .Setup(request => request.Headers["date"])
                .Returns(new[]
                {
                    "Sat, 28 Mar 2026 21:57:25 GMT"
                });
            httpRequestMock
                .Setup(request => request.Headers["content-type"])
                .Returns(new[]
                {
                    "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""
                });
            httpRequestMock
                .Setup(request => request.Headers["digest"])
                .Returns(new[]
                {
                    "SHA-256=jxDReNRcNVeHhlmBNIaWOwBLdXmIzPvPnyfrf95auJo="
                });
            httpRequestMock
                .Setup(request => request.Headers["user-agent"])
                .Returns(new[]
                {
                    "(Pixelfed/0.12.6+glitch.1.12.1; +https://pixelfed.furryfandom.me)"
                });
            httpRequestMock
                .Setup(request => request.Headers["signature"])
                .Returns(new[]
                {
                    "keyId=\"https://pixelfed.furryfandom.me/users/lizard-socks#main-key\",headers=\"(request-target) host date digest content-type user-agent\",algorithm=\"rsa-sha256\",signature=\"bN4E9pat0gavQ+xS/jJSh2tN9tbv0qpMD21lLt4pBiaHWroDrKCy6o1PpCIRjKFoRopVWnqdddblaMKI73aMd6QSHLVm38Y12VlgkxhMkWyC2m+4xBGpAoX4phhYnzTcy2jNmYCuKmBOfg0yArupGxuAGONHTMoWv+8cY+FxzFGdM4Y02nSoGayo3PgRP5ZX+tJJExxoH50J4RP5P7py5Q4r7tO1qNRs68myr/7vnH/2FiMw1qg1QyWky25AU15s0c52M1w7pk2szVQAYGpVbR0C5ZMcqz9x9GhvoR/oYE8uVjZLtTfLzY2uGDbQxmS5pQAJw54xonqT99eEfLPhag==\""
                });

            var keyMock = new Mock<IKey>(MockBehavior.Strict);
            keyMock
                .Setup(key => key.KeyId)
                .Returns("https://pixelfed.furryfandom.me/users/lizard-socks#main-key");
            keyMock
                .Setup(key => key.KeyPem)
                .Returns("-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAswpp2Bcrvc3or1sQFTlU\naoY46CULzMlu+1eGKZYqMaNyuDFLnXYhT/lgu/sUuw9wweQ4a0mPi6jcKwX0v+Fh\nXUlfmq2taTnAm+M0hYkT/Thw/GipeQTmulNsc6xB8Dzpi8rHGc9YIl8n3z8kIdgO\n+MlgC+DxVX2anEDQGUmqIXp3QYVeDFC8KbKf49x4As3o7XNU7HHqF1lNydhrSLVW\no6v/zJRQAfUHrj39vgayYBK+wNhW/HymsYvnOWz/+53yTBCo3aCDZE1gTpBzGl8q\nva1u3PlEHBHuZKJSLBHdCUIVrcL+VBM6UKJ94Su1cu7Bd3dDtAusq2Wf2nDA++UE\nSwIDAQAB\n-----END PUBLIC KEY-----\n");

            var verifier = new MastodonVerifier();

            Assert.AreEqual(
                actual: verifier.VerifyRequestSignature(
                    httpRequestMock.Object,
                    keyMock.Object),
                expected: VerificationResult.SuccessfullyVerified);
        }

        [TestMethod]
        public void VerifyRequestSignature_FailsVerification()
        {
            var httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock
                .Setup(request => request.Method)
                .Returns("get");
            httpRequestMock
                .Setup(request => request.Scheme)
                .Returns("https");
            httpRequestMock
                .Setup(request => request.Host)
                .Returns(new HostString("example.com"));
            httpRequestMock
                .Setup(request => request.PathBase)
                .Returns(PathString.Empty);
            httpRequestMock
                .Setup(request => request.Path)
                .Returns("/users/bakertoons/statuses/116173893479806610");
            httpRequestMock
                .Setup(request => request.QueryString)
                .Returns(QueryString.Empty);
            httpRequestMock
                .Setup(request => request.Headers["host"])
                .Returns(new[]
                {
                    "example.com"
                });
            httpRequestMock
                .Setup(request => request.Headers["date"])
                .Returns(new[]
                {
                    "Fri, 20 Mar 2026 03:10:41 GMT"
                });
            httpRequestMock
                .Setup(request => request.Headers["signature"])
                .Returns(new[]
                {
                    @"keyId=""https://pandacap.azurewebsites.net#main-key"",algorithm=""rsa-sha256"",headers=""(request-target) host date"",signature=""Qf79tgX/DizD2EjfWqu2vw/iTqyWhG+nH9b7BVhtvQvVSBm1zfqAPMtrprjyQe1thUy0MPVM6HKeu4W3OCNSt44DIUMghTUOJ5SCd5Yaq5j4L+IBn2SELWSUpu+UjaXyIGHCykwu0DviF2T01jFRTdeQu8A4m2lJPFDFVi5jrYPMbjoOJ7DaXxLVrqfHczeUhmYCWwr1BXu1nKZP2scHnm84YkbPYC2kEQeYQOVE5TJwFZrxMx/IWt5plIjyEDOJgf8oV59/kgWv1i5AnqL27oAuhJZX/CNs83ilwKUcBwoMhEQXa5IwfiOt02ANXcBZq5zP+RXzb80OfYvwCqxGdw=="""
                });

            var keyMock = new Mock<IKey>(MockBehavior.Strict);
            keyMock
                .Setup(key => key.KeyId)
                .Returns("https://pandacap.azurewebsites.net#main-key");
            keyMock
                .Setup(key => key.KeyPem)
                .Returns("-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAvl6GB0lDhDcCVIIRdqFo\nlf4OaKvIIbrhoZdea/Dk/WsSbmmimVx4lblgyGKCTp9AXg86jmhjLCnQmB4qfqv7\nPA92ewh/NsgGgmOJ3eHwsSKVuBlUmzUc/ldVbgT1zUKgCw88amZDsWqMg0w0zt4I\n8q8CQOQQzVoWibEGy+WgJIA/ELs/fOnN+iWZlHePWC37k1njm+HOQtStxRJWUKaG\nZzcAvtWSX7G+OQGlUFnPLwQp3TnfcitF9zeoFsl/s3VfViAr+RiJ2UCtE12yePus\neOoj4fskwCj2SsdEKaL+PtFe3ko0MGxYgwMdFUK8q5gyzUbGzbcHQuAoqN+PRiEc\nRQIDAQAB\n-----END PUBLIC KEY-----\n");

            var verifier = new MastodonVerifier();

            Assert.AreEqual(
                actual: verifier.VerifyRequestSignature(
                    httpRequestMock.Object,
                    keyMock.Object),
                expected: VerificationResult.SignatureMismatch);
        }

        [TestMethod]
        public void VerifyRequestSignature_NoMatch()
        {
            var httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock
                .Setup(request => request.Method)
                .Returns("get");
            httpRequestMock
                .Setup(request => request.Scheme)
                .Returns("https");
            httpRequestMock
                .Setup(request => request.Host)
                .Returns(new HostString("mastodon.art"));
            httpRequestMock
                .Setup(request => request.PathBase)
                .Returns(PathString.Empty);
            httpRequestMock
                .Setup(request => request.Path)
                .Returns("/users/bakertoons/statuses/116173893479806610");
            httpRequestMock
                .Setup(request => request.QueryString)
                .Returns(QueryString.Empty);
            httpRequestMock
                .Setup(request => request.Headers["host"])
                .Returns(new[]
                {
                    "mastodon.art"
                });
            httpRequestMock
                .Setup(request => request.Headers["date"])
                .Returns(new[]
                {
                    "Fri, 20 Mar 2026 03:10:41 GMT"
                });
            httpRequestMock
                .Setup(request => request.Headers["signature"])
                .Returns(new[]
                {
                    @"keyId=""https://pandacap.azurewebsites.net#main-key"",algorithm=""rsa-sha256"",headers=""(request-target) host date"",signature=""Qf79tgX/DizD2EjfWqu2vw/iTqyWhG+nH9b7BVhtvQvVSBm1zfqAPMtrprjyQe1thUy0MPVM6HKeu4W3OCNSt44DIUMghTUOJ5SCd5Yaq5j4L+IBn2SELWSUpu+UjaXyIGHCykwu0DviF2T01jFRTdeQu8A4m2lJPFDFVi5jrYPMbjoOJ7DaXxLVrqfHczeUhmYCWwr1BXu1nKZP2scHnm84YkbPYC2kEQeYQOVE5TJwFZrxMx/IWt5plIjyEDOJgf8oV59/kgWv1i5AnqL27oAuhJZX/CNs83ilwKUcBwoMhEQXa5IwfiOt02ANXcBZq5zP+RXzb80OfYvwCqxGdw=="""
                });

            var keyMock = new Mock<IKey>(MockBehavior.Strict);
            keyMock
                .Setup(key => key.KeyId)
                .Returns("https://pandacap.example.com#main-key");

            var verifier = new MastodonVerifier();

            Assert.AreEqual(
                actual: verifier.VerifyRequestSignature(
                    httpRequestMock.Object,
                    keyMock.Object),
                expected: VerificationResult.NoMatchingVerifierFound);
        }
    }
}