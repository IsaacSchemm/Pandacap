using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using NSign;
using NSign.Signatures;

namespace Pandacap.ActivityPub.HttpSignatures.Validation.Tests
{
    [TestClass]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Makes tests difficult to read")]
    public class MastodonVerifierTests
    {
        [TestMethod]
        public void VerifyRequestSignature_VerifiesRealSignature()
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

            verifier
                .VerifyRequestSignature(
                    httpRequestMock.Object,
                    keyMock.Object)
                .Should().Be(VerificationResult.SuccessfullyVerified);
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

            verifier
                .VerifyRequestSignature(
                    httpRequestMock.Object,
                    keyMock.Object)
                .Should().Be(VerificationResult.SignatureMismatch);
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

            verifier
                .VerifyRequestSignature(
                    httpRequestMock.Object,
                    keyMock.Object)
                .Should().Be(VerificationResult.NoMatchingVerifierFound);
        }

        [TestMethod]
        public void ParseSignatureValue_Mastodon()
        {
            var val = new MastodonVerifier().ParseSignatureValue([
                "headers = (request-target) host date digest content-type"
            ]);
            val.spec.SignatureParameters.ComponentName.Should().Be("@signature-params");
            val.spec.SignatureParameters.Components.Select(c => new
            {
                c.Type,
                c.ComponentName
            }).Should().BeEquivalentTo([
                new {
                    Type = SignatureComponentType.Derived,
                    ComponentName = "@request-target",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "host",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "date",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "digest",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "content-type",
                }
            ]);
        }


        [TestMethod]
        public void ParseSignatureValue_Pixelfed()
        {
            var val = new MastodonVerifier().ParseSignatureValue([
                "headers = (request-target) host date digest content-type user-agent"
            ]);
            val.spec.SignatureParameters.ComponentName.Should().Be("@signature-params");
            val.spec.SignatureParameters.Components.Select(c => new
            {
                c.Type,
                c.ComponentName
            }).Should().BeEquivalentTo([
                new {
                    Type = SignatureComponentType.Derived,
                    ComponentName = "@request-target",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "host",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "date",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "digest",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "content-type",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "user-agent",
                }
            ]);
        }

        [TestMethod]
        public void ParseSignatureValue_MicroblogPub()
        {
            var val = new MastodonVerifier().ParseSignatureValue([
                "headers = (request-target) user-agent host date digest content-type"
            ]);
            val.spec.SignatureParameters.ComponentName.Should().Be("@signature-params");
            val.spec.SignatureParameters.Components.Select(c => new
            {
                c.Type,
                c.ComponentName
            }).Should().BeEquivalentTo([
                new {
                    Type = SignatureComponentType.Derived,
                    ComponentName = "@request-target",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "user-agent",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "host",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "date",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "digest",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "content-type",
                }
            ]);
        }

        [TestMethod]
        public void ParseSignatureValue_BridgyFed()
        {
            var val = new MastodonVerifier().ParseSignatureValue([
                "headers = date host digest (request-target)"
            ]);
            val.spec.SignatureParameters.ComponentName.Should().Be("@signature-params");
            val.spec.SignatureParameters.Components.Select(c => new
            {
                c.Type,
                c.ComponentName
            }).Should().BeEquivalentTo([
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "date",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "host",
                },
                new {
                    Type = SignatureComponentType.HttpHeader,
                    ComponentName = "digest",
                },
                new {
                    Type = SignatureComponentType.Derived,
                    ComponentName = "@request-target",
                }
            ]);
        }
    }
}