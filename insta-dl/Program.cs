using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace insta_dl
{
    class Program
    {
        
        static async Task Main(string[] args)
        {
            await user_login();
            Console.ReadKey();

        }
        private static string username,password;
        private static dynamic login_cookie;
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
                }else if (key.Key == ConsoleKey.Backspace)
                {
                    password = password.Remove(password.Length - 1);
                }else
                {
                    password += key.KeyChar;  
                }
            }
            Int32 utime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            string encpass = $"#PWD_INSTAGRAM_BROWSER:0:{utime}:{password}";
            var handler = new HttpClientHandler();
            handler.UseCookies = false;
             using (var httpClient = new HttpClient(handler))
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://www.instagram.com/accounts/login/ajax/"))
                {
                    request.Headers.TryAddWithoutValidation("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.102 Safari/537.36");
                    request.Headers.TryAddWithoutValidation("x-csrftoken", await get_csrftoken());
                    var contentList = new List<string>();
                    contentList.Add($"username={username}");
                    contentList.Add($"enc_password={encpass}");
                    request.Content = new StringContent(string.Join("&", contentList));
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                    var response = await httpClient.SendAsync(request);
                    Console.WriteLine(response.StatusCode);
                    var content = await response.Content.ReadAsStringAsync();
                    var status = JObject.Parse(content);
                    if (Convert.ToString(status["status"]) == "fail")
                    {
                        Console.WriteLine(status["message"] +"\n"+ status["status"]);
                        Environment.Exit(0);
                    }
                    else
                    {
                        Console.WriteLine(status["message"] + "\n" + status["status"]);
                    }
                    foreach (var item in response.Headers.GetValues("Set-Cookie"))
                    {
                        login_cookie = login_cookie + item;
                    }
                    Regex regex = new Regex(@"(?:csrftoken=|ds_user_id=|ig_did=|sessionid=).*?;");
                    var match = regex.Matches(login_cookie);
                    Console.WriteLine($"{match[0].ToString()}  {match[1].ToString()}  {match[2].ToString()}  {match[3].ToString()}");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("login success");
                    Console.ResetColor();
                    RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\insta-dl"); 
                    key.SetValue("cookie", match[0].ToString() + match[1].ToString() + match[2].ToString() + match[3].ToString());
                    key.Close(); 
                }
            }
        }
        /*public static async Task get_uinfo(string uname)
        {
            try
            {
                var handler = new HttpClientHandler();
                handler.UseCookies = false;
                using (var client = new HttpClient(handler))
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://www.instagram.com/" + uname + "/?__a=1"))
                    {
                        request.Headers.TryAddWithoutValidation("Cookie","aa");
                        
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }*/
    }
}