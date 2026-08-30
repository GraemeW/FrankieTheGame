using NUnit.Framework;

namespace LowDefMustard.Saving.Tests.Editor
{
    // Note:  Not testing as a security guarantee -- SymmetricEncryptor is for obfuscation, not a security-grade scheme
    // Tests confirm the round trip is lossless
    
    public class SymmetricEncryptorTests
    {
        [Test]
        public void RoundTrip_BasicString_ReturnsOriginal()
        {
            string original = "hello save file";
            string encrypted = SymmetricEncryptor.EncryptString(original);
            string decrypted = SymmetricEncryptor.DecryptToString(encrypted);

            Assert.AreEqual(original, decrypted);
        }

        [Test]
        public void RoundTrip_EmptyString_ReturnsOriginal()
        {
            string original = "";
            string encrypted = SymmetricEncryptor.EncryptString(original);
            string decrypted = SymmetricEncryptor.DecryptToString(encrypted);

            Assert.AreEqual(original, decrypted);
        }

        [Test]
        public void RoundTrip_JsonLikePayloadWithUnicode_ReturnsOriginal()
        {
            string original = "{\"name\":\"Gr\u00e6me\",\"level\":12,\"emoji\":\"\ud83c\udfae\"}";
            string encrypted = SymmetricEncryptor.EncryptString(original);
            string decrypted = SymmetricEncryptor.DecryptToString(encrypted);

            Assert.AreEqual(original, decrypted);
        }

        [Test]
        public void EncryptString_ProducesDifferentOutputThanInput()
        {
            string original = "hello save file";
            string encrypted = SymmetricEncryptor.EncryptString(original);

            Assert.AreNotEqual(original, encrypted);
        }
    }
}
