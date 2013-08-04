using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NSemble.Core.Models;
using Nancy.Security;
using Raven.Client;

namespace NSemble.Core.Nancy
{
    public class NSembleUserAuthentication
    {
        private class ApiKeyToken
        {
            public string UserId { get; set; }
            public DateTimeOffset SessionStarted { get; set; }
        }

        private const int SaltSize = 5;

        private static string GetApiKeyDocumentId(string apiKey)
        {
            return "NSemble/APIKeys/" + apiKey;
        }

        public static IUserIdentity GetUserFromApiKey(IDocumentSession ravenSession, string apiKey)
        {
            var activeKey = ravenSession.Include<ApiKeyToken>(x => x.UserId).Load(GetApiKeyDocumentId(apiKey));
            return activeKey == null ? null : ravenSession.Load<User>(activeKey.UserId);
        }

        public static string ValidateUser(IDocumentSession ravenSession, string username, string password)
        {
            // try to get a user from the database that matches the given username and password
            var userRecord = ravenSession.Load<User>("users/" + username);
            if (userRecord == null)
            {
                return null;
            }

            // verify password
            var hashedPassword = GenerateSaltedHash(password, userRecord.Salt);
            if (!CompareByteArrays(hashedPassword, userRecord.Password))
                return null;

            // now that the user is validated, create an api key that can be used for subsequent requests
            var apiKey = Guid.NewGuid().ToString();
            ravenSession.Store(new ApiKeyToken { UserId = userRecord.Id, SessionStarted = DateTimeOffset.UtcNow }, GetApiKeyDocumentId(apiKey));
            ravenSession.SaveChanges();

            return apiKey;
        }

        public static void RemoveApiKey(IDocumentSession ravenSession, string apiKey)
        {
            ravenSession.Advanced.DocumentStore.DatabaseCommands.Delete(GetApiKeyDocumentId(apiKey), null);
        }

        public static User SetUserPassword(User user, string password)
        {
            user.Salt = CreateSalt(SaltSize);
            user.Password = GenerateSaltedHash(password, user.Salt);
            return user;
        }

        private static byte[] CreateSalt(int size)
        {
            //Generate a cryptographic random number.
            var rng = new RNGCryptoServiceProvider();
            var buff = new byte[size];
            rng.GetBytes(buff);
            return buff;
        }

        private static byte[] GenerateSaltedHash(string plainText, byte[] saltBytes)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            HashAlgorithm algorithm = new SHA256Managed();

            var plainTextWithSaltBytes = new byte[plainTextBytes.Length + saltBytes.Length];
            for (var i = 0; i < plainTextBytes.Length; i++)
            {
                plainTextWithSaltBytes[i] = plainTextBytes[i];
            }
            for (var i = 0; i < saltBytes.Length; i++)
            {
                plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];
            }

            return algorithm.ComputeHash(plainTextWithSaltBytes);
        }

        private static bool CompareByteArrays(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
            {
                return false;
            }
            return !array1.Where((t, i) => t != array2[i]).Any();
        }
    }
}
