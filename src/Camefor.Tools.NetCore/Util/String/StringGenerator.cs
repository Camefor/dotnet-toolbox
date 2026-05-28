using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Camefor.Tools.Text
{
    public static class StringGenerator
    {
        /// <summary>
        /// 生成随机密码
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string GenerateRandomPassword(int length)
        {
            if (length < 4)
            {
                throw new Exception("parameter invalid. password cannot be less than 4 digits");
            }

            const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz";
            const string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string numericChars = "0123456789";
            const string specialChars = "!@#$%&*?";
            string allChars = lowercaseChars + uppercaseChars + numericChars + specialChars;
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {

                byte[] randomBytes = new byte[length];
                rng.GetBytes(randomBytes);

                StringBuilder password = new StringBuilder(length);

                for (int i = 0; i < length; i++)
                {
                    int randomIndex = randomBytes[i] % allChars.Length;
                    password.Append(allChars[randomIndex]);
                }

                // 确保密码包含至少一个小写字母、一个大写字母、一个特殊字符和一个数字
                if (!password.ToString().Intersect(lowercaseChars).Any() ||
                    !password.ToString().Intersect(uppercaseChars).Any() ||
                    !password.ToString().Intersect(numericChars).Any() ||
                    !password.ToString().Intersect(specialChars).Any())
                {
                    // return GenerateRandomPassword(length); // 重新生成密码
                    password.Remove(0, 4);
                    password.Append(numericChars[new Random().Next(0, numericChars.Length - 1)]);
                    password.Append(specialChars[new Random().Next(0, specialChars.Length - 1)]);
                    password.Append(uppercaseChars[new Random().Next(0, uppercaseChars.Length - 1)]);
                    password.Append(lowercaseChars[new Random().Next(0, lowercaseChars.Length - 1)]);
                }

                return password.ToString();
            }
        }
    }
}
