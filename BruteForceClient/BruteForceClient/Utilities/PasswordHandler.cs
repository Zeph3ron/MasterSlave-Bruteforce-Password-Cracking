using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace BruteForceClient.Utilities
{
    internal class PasswordFileHandler
    {
        private static readonly Converter<char, byte> Converter = CharToByte;

        /// <summary>
        ///     With this method you can make you own password file
        /// </summary>
        /// <param name="filename">Name of password file</param>
        /// <param name="usernames">List of usernames</param>
        /// <param name="passwords">List of passwords in clear text</param>
        /// <exception cref="ArgumentException">if usernames and passwords have different lengths</exception>
        public static void WritePasswordFile(string filename, string[] usernames, string[] passwords)
        {
            HashAlgorithm messageDigest = new SHA1CryptoServiceProvider();
            if (usernames.Length != passwords.Length)
            {
                throw new ArgumentException("usernames and passwords must be same lengths");
            }
            using (var fs = new FileStream(filename, FileMode.CreateNew, FileAccess.Write))
            using (var sw = new StreamWriter(fs))
            {
                for (var i = 0; i < usernames.Length; i++)
                {
                    var passwordAsBytes = Array.ConvertAll(passwords[i].ToCharArray(), GetConverter());
                    var encryptedPassword = messageDigest.ComputeHash(passwordAsBytes);
                    var line = usernames[i] + ":" + Convert.ToBase64String(encryptedPassword) + "\n";
                    sw.WriteLine(line);
                }
            }
        }

        /// <summary>
        ///     Reads all the username + encrypted password from the password file
        /// </summary>
        /// <param name="filename">the name of the password file</param>
        /// <returns>A list of (username, encrypted password) pairs</returns>
        public static List<UserInfo> ReadPasswordFile(string filename)
        {
            var result = new List<UserInfo>();

            var fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            using (var sr = new StreamReader(fs))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    var parts = line.Split(":".ToCharArray());
                    var userInfo = new UserInfo(parts[0], parts[1]);
                    result.Add(userInfo);
                }
                return result;
            }
        }

        public static List<UserInfo> GetPasswordsFromServer(int port, string ipAddress)
        {
            var result = new List<UserInfo>();
            try
            {
                using (var client = new TcpClient(ipAddress, port))
                {
                    using (var networkStream = client.GetStream())
                    {
                        using (var sr = new StreamReader(networkStream))
                        {
                            using (var sw = new StreamWriter(networkStream) {AutoFlush = true})
                            {
                                //Writes to the server first
                                //(get.pass) activates the 'SendPasswords();' on the server
                                sw.WriteLine("get.pass");
                                while (!sr.EndOfStream)
                                {
                                    var line = sr.ReadLine();
                                    var parts = line.Split(":".ToCharArray());
                                    var userInfo = new UserInfo(parts[0], parts[1]);
                                    result.Add(userInfo);
                                }
                            }
                            return result;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Converter<char, byte> GetConverter()
        {
            return Converter;
        }

        /// <summary>
        ///     Converting a char to a byte can be done in many ways.
        ///     This is one way ...
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static byte CharToByte(char ch)
        {
            return Convert.ToByte(ch);
        }
    }
}