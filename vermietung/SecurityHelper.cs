/*
 * Created by SharpDevelop.
 * User: puder
 * Date: 28.03.2025
 * Time: 10:57
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Security.Cryptography;
using System.Text;

namespace CarRentalHttpServer
{
    public static class SecurityHelper
    {
        public static string GenerateSalt(int size = 16)
        {
            var rng = new RNGCryptoServiceProvider();
            var saltBytes = new byte[size];
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        public static string HashPassword(string password, string salt)
        {
            var sha = SHA256.Create();
            var inputBytes = Encoding.UTF8.GetBytes(password + salt);
            var hashBytes = sha.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
