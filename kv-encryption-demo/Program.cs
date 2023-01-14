using System.Security.Cryptography;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.development.json", true)
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build();

var keyVaultName = config.GetValue<string>("KeyVaultName");
var keyName = config.GetValue<string>("KeyName");
var credentials = new DefaultAzureCredential();
var keyClient = new KeyClient(new Uri($"https://{keyVaultName}.vault.azure.net/"), credentials);

var text = "This is a message";
Console.WriteLine($"Text: {text}");

var encryptedText = Encrypt(text);

Decrypt(encryptedText);

var hash = Hash(text);
var signed = Sign(hash);

Verify(hash, signed);

string Encrypt(string text)
{
    var publicKey = keyClient.GetKey(keyName);

    using (var rsa = publicKey.Value.Key.ToRSA())
    {      
        var byteData = Encoding.Unicode.GetBytes(text);

        var encryptedText = rsa.Encrypt(byteData, RSAEncryptionPadding.OaepSHA256);

        var result = Convert.ToBase64String(encryptedText);

        return result;
    } 
}

void Decrypt(string encryptedText)
{
    var publicKey = keyClient.GetKey(keyName);

    var cryptoClient = new CryptographyClient(publicKey.Value.Id, credentials);

    var encryptedBytes = Convert.FromBase64String(encryptedText);
    var dencryptResult = cryptoClient.Decrypt(EncryptionAlgorithm.RsaOaep256, encryptedBytes);
    var decryptedText = Encoding.Unicode.GetString(dencryptResult.Plaintext);
    
    Console.WriteLine($"Decrypted text: {decryptedText}");
}

byte[] Sign(byte[] hash)
{
    var publicKey = keyClient.GetKey(keyName);

    var cryptoClient = new CryptographyClient(publicKey.Value.Id, credentials);

    var result = cryptoClient.Sign(SignatureAlgorithm.RS256, hash);
    
    return result.Signature;
}

bool Verify(byte[] hash, byte[] signature)
{
    var publicKey = keyClient.GetKey(keyName);

    using (var rsa = publicKey.Value.Key.ToRSA())
    {      
        var byteData = Encoding.Unicode.GetBytes(text);

        bool verified = rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        Console.WriteLine($"Verified: {verified}");

        return verified;
    } 
}

byte[] Hash(string text)
{
    var bytes = Encoding.ASCII.GetBytes(text);
    var sha = SHA256.Create();

    return sha.ComputeHash(bytes);
}
