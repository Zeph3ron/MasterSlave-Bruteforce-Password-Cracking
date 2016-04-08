using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using BruteForceClient.Models;
using BruteForceClient.Utilities;

namespace BruteForceClient
{
    public class Cracking
    {
        private readonly string _ipAddress;
        public List<UserInfoClearText> CrackingResults = new List<UserInfoClearText>();

        /// <summary>
        ///     The algorithm used for encryption.
        ///     Must be exactly the same algorithm that was used to encrypt the passwords in the password file
        /// </summary>
        private readonly HashAlgorithm _messageDigest;

        private readonly int _serverPort;

        public Cracking(string ipAddress, int serverPort)
        {
            _ipAddress = ipAddress;
            _serverPort = serverPort;
            _messageDigest = new SHA1CryptoServiceProvider();
            //_messageDigest = new MD5CryptoServiceProvider();
            // seems to be same speed
        }

        /// <summary>
        ///     Runs the password cracking algorithm
        /// </summary>
        public void RunCracking()
        {
            var stopwatch = Stopwatch.StartNew();

            var passwordsToCrack = PasswordFileHandler.GetPasswordsFromServer(_serverPort, _ipAddress);
            if (passwordsToCrack == null)
            {
                return;
            }
            Console.WriteLine("passwords from server received");

            CrackingResults = new List<UserInfoClearText>();

            GetCrackingResults(_serverPort, CrackingResults, passwordsToCrack);
            stopwatch.Stop();
            Console.WriteLine(string.Join(", ", CrackingResults));
            Console.WriteLine("Out of {0} password {1} was found ", passwordsToCrack.Count, CrackingResults.Count);
            Console.WriteLine();
            Console.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);


            Console.WriteLine("Attempting to get the next set of words from the server...");
            GetNextSubset(CrackingResults, passwordsToCrack);
        }

        /// <summary>
        ///     This method should send the crackingResults to the server, and attempt to get a new subset.
        ///     Returns false if there is no subset left to work on.
        /// </summary>
        /// <param name="crackingResults"></param>
        /// <param name="passwordsToCrack"></param>
        /// <returns></returns>
        private void GetNextSubset(List<UserInfoClearText> crackingResults, List<UserInfo> passwordsToCrack)
        {
            try
            {
                using (var client = new TcpClient(_ipAddress, _serverPort))
                {
                    using (var networkStream = client.GetStream())
                    {
                        using (var sr = new StreamReader(networkStream))
                        {
                            using (var sw = new StreamWriter(networkStream) {AutoFlush = true})
                            {
                                var userInfo = new StringBuilder("get.nextset#");
                                foreach (var clearText in crackingResults)
                                {
                                    userInfo.Append(clearText.UserName + ":" + clearText.Password + "#");
                                }
                                sw.WriteLine(userInfo);

                                //Find out if the server is still running
                                if (!sr.EndOfStream)
                                {
                                    var stopwatch = Stopwatch.StartNew();
                                    Console.WriteLine("New set received, starting password cracking...");
                                    while (!sr.EndOfStream)
                                    {
                                        var dictionaryEntry = sr.ReadLine();
                                        var partialResult = CheckWordWithVariations(dictionaryEntry,
                                            passwordsToCrack);
                                        CrackingResults.AddRange(partialResult);
                                    }
                                    stopwatch.Stop();
                                    Console.WriteLine(string.Join(", ", crackingResults));
                                    Console.WriteLine("Out of {0} password {1} was found ", passwordsToCrack.Count,
                                        crackingResults.Count);
                                    Console.WriteLine();
                                    Console.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);
                                    GetNextSubset(CrackingResults, passwordsToCrack);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("The server has stopped working or is not responding, please try again later...");
            }
        }

        public void GetCrackingResults(int port, List<UserInfoClearText> result, List<UserInfo> userInfos)
        {
            using (var client = new TcpClient(_ipAddress, port))
            {
                using (var networkStream = client.GetStream())
                {
                    using (var sw = new StreamWriter(networkStream) {AutoFlush = true})
                    {
                        using (var sr = new StreamReader(networkStream))
                        {
                            //Writes to the server first
                            //(get.words) activates the 'SendDictionarySubset();' on the server
                            sw.WriteLine("get.words");
                            while (!sr.EndOfStream)
                            {
                                var dictionaryEntry = sr.ReadLine();
                                var partialResult = CheckWordWithVariations(dictionaryEntry,
                                    userInfos);
                                result.AddRange(partialResult);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Generates a lot of variations, encrypts each of the and compares it to all entries in the password file
        /// </summary>
        /// <param name="dictionaryEntry">A single word from the dictionary</param>
        /// <param name="userInfos">List of (username, encrypted password) pairs from the password file</param>
        /// <returns>A list of (username, readable password) pairs. The list might be empty</returns>
        private IEnumerable<UserInfoClearText> CheckWordWithVariations(string dictionaryEntry, List<UserInfo> userInfos)
        {
            var result = new List<UserInfoClearText>(); //might be empty

            var possiblePassword = dictionaryEntry;
            var partialResult = CheckSingleWord(userInfos, possiblePassword);
            result.AddRange(partialResult);

            var possiblePasswordUpperCase = dictionaryEntry.ToUpper();
            var partialResultUpperCase = CheckSingleWord(userInfos, possiblePasswordUpperCase);
            result.AddRange(partialResultUpperCase);

            var possiblePasswordCapitalized = StringUtilities.Capitalize(dictionaryEntry);
            var partialResultCapitalized = CheckSingleWord(userInfos, possiblePasswordCapitalized);
            result.AddRange(partialResultCapitalized);

            var possiblePasswordReverse = StringUtilities.Reverse(dictionaryEntry);
            var partialResultReverse = CheckSingleWord(userInfos, possiblePasswordReverse);
            result.AddRange(partialResultReverse);

            for (var i = 0; i < 100; i++)
            {
                var possiblePasswordEndDigit = dictionaryEntry + i;
                var partialResultEndDigit = CheckSingleWord(userInfos, possiblePasswordEndDigit);
                result.AddRange(partialResultEndDigit);
            }

            for (var i = 0; i < 100; i++)
            {
                var possiblePasswordStartDigit = i + dictionaryEntry;
                var partialResultStartDigit = CheckSingleWord(userInfos, possiblePasswordStartDigit);
                result.AddRange(partialResultStartDigit);
            }

            for (var i = 0; i < 10; i++)
            {
                for (var j = 0; j < 10; j++)
                {
                    var possiblePasswordStartEndDigit = i + dictionaryEntry + j;
                    var partialResultStartEndDigit = CheckSingleWord(userInfos, possiblePasswordStartEndDigit);
                    result.AddRange(partialResultStartEndDigit);
                }
            }

            return result;
        }

        /// <summary>
        ///     Checks a single word (or rather a variation of a word): Encrypts and compares to all entries in the password file
        /// </summary>
        /// <param name="userInfos"></param>
        /// <param name="possiblePassword">List of (username, encrypted password) pairs from the password file</param>
        /// <returns>A list of (username, readable password) pairs. The list might be empty</returns>
        private IEnumerable<UserInfoClearText> CheckSingleWord(IEnumerable<UserInfo> userInfos, string possiblePassword)
        {
            var charArray = possiblePassword.ToCharArray();
            var passwordAsBytes = Array.ConvertAll(charArray, PasswordFileHandler.GetConverter());

            var encryptedPassword = _messageDigest.ComputeHash(passwordAsBytes);
            //string encryptedPasswordBase64 = System.Convert.ToBase64String(encryptedPassword);

            var results = new List<UserInfoClearText>();

            foreach (var userInfo in userInfos)
            {
                if (CompareBytes(userInfo.EntryptedPassword, encryptedPassword)) //compares byte arrays
                {
                    results.Add(new UserInfoClearText(userInfo.Username, possiblePassword));
                    Console.WriteLine(userInfo.Username + " " + possiblePassword);
                }
            }
            return results;
        }

        /// <summary>
        ///     Compares to byte arrays. Encrypted words are byte arrays
        /// </summary>
        /// <param name="firstArray"></param>
        /// <param name="secondArray"></param>
        /// <returns></returns>
        private static bool CompareBytes(IList<byte> firstArray, IList<byte> secondArray)
        {
            //if (secondArray == null)
            //{
            //    throw new ArgumentNullException("firstArray");
            //}
            //if (secondArray == null)
            //{
            //    throw new ArgumentNullException("secondArray");
            //}
            if (firstArray.Count != secondArray.Count)
            {
                return false;
            }
            for (var i = 0; i < firstArray.Count; i++)
            {
                if (firstArray[i] != secondArray[i])
                    return false;
            }
            return true;
        }
    }
}