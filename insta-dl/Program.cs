using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace insta_dl
{
    class Program
    {
        // await user_login();
        // await get_uinfo("uur_yvs");
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.ReadKey();
        }
        public static string username,password;
        public static dynamic Login;
        private static async Task<string> get_csrftoken()
        {
            var handler = new HttpClientHandler();
            handler.UseCookies = false;
            using (var httpClient = new HttpClient(handler))
            {
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://www.instagram.com/"))
                {
                    var response = await httpClient.SendAsync(request);
                    foreach (var item in response.Headers.GetValues("Set-Cookie"))
                    {
                        Regex regex = new Regex(@"(?<=csrftoken=).*?(?=;);");
                        var match2 = regex.Match(item);
                        if (match2.Success)
                        {
                            return match2.Value;
                        }
                    }
                }
            }
            return null;
        }
        private static async Task user_login()
        {
            Console.Write("Username: ");
            username = Console.ReadLine();
            Console.Write("Password: ");
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }else if (key.Key == ConsoleKey.Backspace && password.Length >= 0)
                {
                    password = password.Remove(password.Length - 1);
                }else
                {
                    password = password + key.KeyChar;  
                }
            }
            Int32 utime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            string encpass = $"#PWD_INSTAGRAM_BROWSER:0:{utime}:{password}";
            string cstoken = await get_csrftoken();
            var handler = new HttpClientHandler();
            handler.UseCookies = false;
             using (var httpClient = new HttpClient(handler))
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://www.instagram.com/accounts/login/ajax/"))
                {
                    request.Headers.TryAddWithoutValidation("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.102 Safari/537.36");
                    request.Headers.TryAddWithoutValidation("x-csrftoken", cstoken);
                    var contentList = new List<string>();
                    contentList.Add($"username={username}");
                    contentList.Add($"enc_password={encpass}");
                    request.Content = new StringContent(string.Join("&", contentList));
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");
                    var response = await httpClient.SendAsync(request);
                    Console.WriteLine(response.StatusCode);
                    var content = await response.Content.ReadAsStringAsync();
                    var status = JObject.Parse(content);
                   //Console.Write(status);
                   if (Convert.ToBoolean(status["two_factor_required"]))
                   {
                      Console.WriteLine("`(*>﹏<*)′ Disable 2fa!");
                      Console.WriteLine("Currently Not Supported (´。＿。｀)");
                      Environment.Exit(0);
                   }
                    if (Convert.ToBoolean(status["user"]) == false && Convert.ToBoolean(status["two_factor_required"]) == false)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("The username you entered doesn't belong to an account. Please check your username and try again.");
                        Console.ResetColor();
                        Environment.Exit(0);
                    }
                    if (Convert.ToBoolean(status["user"])&&Convert.ToBoolean(status["authenticated"]) == false)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Your password was incorrect. Please double-check your password.");
                        Console.ResetColor();
                        Environment.Exit(0);
                    }
                    if (Convert.ToString(status["status"]) == "fail")
                    {
                        Console.WriteLine(status["message"] +"\n"+ status["status"]);
                        Environment.Exit(0);
                    }
                    else
                    {
                     //   Console.WriteLine(status["message"] + "\n" + status["status"]);
                    }
                    
                    foreach (var item in response.Headers.GetValues("Set-Cookie"))
                    {
                        Login = Login + item;
                    }
                    
                    Regex regex = new Regex(@"(?:csrftoken=|ds_user_id=|ig_did=|sessionid=).*?;");
                    var match = regex.Matches(Login);
                   // Console.WriteLine($"{match[0].ToString()}  {match[1].ToString()}  {match[2].ToString()}  {match[3].ToString()}");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("login success");
                    Console.ResetColor();
                    
                    RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\insta-dl");
                    key.SetValue("cookie", Encrypt(match[0].ToString() + match[1].ToString() + match[2].ToString() + match[3].ToString()));
                    key.Close(); 
                }
            }
        }
        private static string enc_key()
        {
            string cpuid = string.Empty;
            ManagementClass mc = new ManagementClass("win32_processor");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                cpuid = mo.Properties["processorID"].Value.ToString();
                break;
            }
            return cpuid;
        }
        private static string Encrypt(string input)  
        {  
            byte[] inputArray = Encoding.UTF8.GetBytes(input);  
            TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider();  
            tripleDES.Key = Encoding.UTF8.GetBytes(enc_key());  
            tripleDES.Mode = CipherMode.ECB;  
            tripleDES.Padding = PaddingMode.PKCS7;  
            ICryptoTransform cTransform = tripleDES.CreateEncryptor();  
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);  
            tripleDES.Clear();  
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);  
        }  
        private static string Decrypt(string input)  
        {  
            byte[] inputArray = Convert.FromBase64String(input);  
            TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider();  
            tripleDES.Key = UTF8Encoding.UTF8.GetBytes(enc_key());  
            tripleDES.Mode = CipherMode.ECB;  
            tripleDES.Padding = PaddingMode.PKCS7;  
            ICryptoTransform cTransform = tripleDES.CreateDecryptor();  
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);  
            tripleDES.Clear();   
            return UTF8Encoding.UTF8.GetString(resultArray);  
        }  
        private static string regis_data()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\insta-dl");
            if (key != null)
            {
                string cookie = Decrypt(key.GetValue("cookie").ToString());
                key.Close();
                if (cookie == String.Empty)
                {
                    Environment.Exit(0);
                    return null;
                }else
                {
                    return cookie;
                }
            }
            Console.WriteLine("You need to login (´。＿。｀)");
            Console.WriteLine("( ﾟдﾟ)つType insta-dl login");
            Environment.Exit(0);
            return null;
        }
        public static async Task get_uinfo(string uname)
        {
            dynamic user_data,info; 
            try
            {
                var handler = new HttpClientHandler();
                handler.UseCookies = false;
                using (var httpClient = new HttpClient(handler))
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://www.instagram.com/" + uname + "/?__a=1"))
                    {
                        request.Headers.TryAddWithoutValidation("Cookie",regis_data());
                        var response = await httpClient.SendAsync(request);
                        user_data = JObject.Parse(await response.Content.ReadAsStringAsync());
                        info = user_data.graphql.user;
                        Console.WriteLine("User Information");
                        Console.WriteLine($"User:       {info.username}");
                        Console.WriteLine($"Private:    {info.is_private}");
                        Console.WriteLine($"Post Count: {info.edge_owner_to_timeline_media.count}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}