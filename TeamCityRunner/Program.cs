using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TeamCityRunner
{
    class Program
    {
        static List<string> testRunIds = new List<string>();
        static List<string> tests = new List<string>();
        static List<string> ips = new List<string>();

        static void Main(string[] args)
        {
            tests.Add("LMIAutomation.Ignition.InstallTest.TC01_DefaultInstall");
            tests.Add("LMIAutomation.Ignition.InstallTest.TC02_Check_Copyright_Year");
            tests.Add("LMIAutomation.Ignition.InstallTest.TC03_Validate_WindowsVersionProvider");
            tests.Add("LMIAutomation.Host.InstallTest.TC01_DefaultInstall");
            tests.Add("LMIAutomation.Host.InstallTest.TC02_ShowHost_ShowClient");
            tests.Add("LMIAutomation.Host.InstallTest.TC03_ShowHost_HideClient");
            ips.Add("192.168.218.128");

            
            Dictionary<string,List<string>> testClasses = new Dictionary<string, List<string>>();

            foreach (var test in tests)
            {
                string testClass = test.Substring(0, test.LastIndexOf("."));
                if (!testClasses.ContainsKey(testClass))
                {
                    testClasses.Add(testClass, new List<string>());
                }
                testClasses[testClass].Add(test);
            }

            UploadFolder(ips[0], @"C:\Users\premenyi\Documents\lmiautomation\bin\Debug\NUnitPlugin\");

            foreach (var testClass in testClasses)
            {
                string testRunId = testClass.Key + "-" + DateTime.Now.ToString("yyyyMMdd-hhmmsstt");
                StartTest(ips[0], String.Join(",",testClass.Value), testRunId);
                WaitTestResult(ips[0], testRunId);
                testRunIds.Add(testRunId); 
            }

            // TODO:
            // - concat results
            // - install teamcity
            // - start with teamcity
            // - get result
            // - show build log of 1 machine
            // - retry logic?
        }

        static void StartTest(string ip, string testName, string postFix)
        {
            string c =
                "http://" + ip + @"/?exec=c:\nunit\bin\nunit3-console.exe%20C:\ftp\nunitPlugin\lmiAutomation.dll%20--test%20"+testName+"%20--out%20testresult.xml%20--result=result"+postFix+".xml%20--out=buildlog"+postFix+".xml";
            System.Diagnostics.Debug.WriteLine(c);
            send(c);
        }

        static void UploadFolder(string ip, string dir)
        {
            var files = Directory.GetFiles(dir, "*.*",SearchOption.AllDirectories);
           

            using (WebClient client = new WebClient())
            { 
                client.Credentials = new NetworkCredential("admin", "12Budapest99");

                foreach (var file in files)
                {
                    var ftpPath = Path.Combine($"ftp://{ip}/" + new DirectoryInfo(dir).Name, file.Replace(dir, ""));
                    client.UploadFile(ftpPath, WebRequestMethods.Ftp.UploadFile, file);
                }
            }
        }

        static bool GetFileFromFtp(string ip, string file)
        {
            System.Diagnostics.Debug.WriteLine("Getting file from ftp:" + file);
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Credentials = new NetworkCredential("admin", "12Budapest99");
                    var ftpPath = Path.Combine($"ftp://{ip}/", file);
                    var downloadedPath = Path.Combine(Directory.GetCurrentDirectory(), file);
                    client.DownloadFile(ftpPath, downloadedPath);
                }
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        static void WaitTestResult(string ip, string postFix)
        {
            var counter = 100; // max 10 minutes
            Boolean testResultReady = false;
            while (counter > 0 && !testResultReady)
            {
                var fileName = "result" + postFix + ".xml";
                if (GetFileFromFtp(ip, fileName))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(Path.Combine(Directory.GetCurrentDirectory(), fileName));
                    foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                    {
                        if (node.Name=="test-suite" && node.Attributes["result"] != null)
                        {
                            testResultReady = true;
                            break;
                        }
                    }
                    foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                    {
                        if (node.Name == "test-case" )
                        {
                            System.Diagnostics.Debug.WriteLine("test result:" + node.Attributes["fullName"] +":" + node.Attributes["result"]);
                            testResultReady = true;
                            break;
                        }
                    }
                }
                counter--;
                Thread.Sleep(6000);
            }
            GetFileFromFtp(ip, "buildlog" + postFix + ".xml");
        }
        static WebResponse send(string command)
        {
            return ((HttpWebRequest)WebRequest.Create(string.Format("{0}", command))).GetResponse();
        }

      


        /*
         var directories = Directory.GetDirectories(dir, "*.*", SearchOption.AllDirectories)
                .OrderBy(p => p).ToList(); 
        foreach (var directory in directories)
           {
               var ftpPath = Path.Combine(@"ftp://192.168.218.128/" + new DirectoryInfo(dir).Name,
                   directory.Replace(dir, ""));

               WebRequest request = WebRequest.Create(ftpPath);
               request.Method = WebRequestMethods.Ftp.MakeDirectory;
               request.Credentials = new NetworkCredential("admin", "12Budapest99");
               using (var resp = (FtpWebResponse)request.GetResponse())
               {
                   Console.WriteLine(resp.StatusCode);
               }
                //client.DownloadFile(@"ftp://192.168.42.219/Ocu.exe", @"C:\Adatok\Ocu2.exe");
           }*/

    }
}
