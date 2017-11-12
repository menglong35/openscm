using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenSCM.ClientService.Application
{

    /// <summary>
    ///     一些路径 服务名称等的定义
    /// </summary>
    internal static class PathUtils
    {

        static PathUtils()
        {
            try
            {
                IssueDirectoryPathExist(ConfigPath);
                IssueDirectoryPathExist(LogBasePath);
                IssueDirectoryPathExist(CollectedDataPath);
                IssueDirectoryPathExist(SendDataPath);
                IssueDirectoryPathExist(RunTimePath);
            }
            catch (Exception)
            {

            }
        }

        #region 常量

        //进程名
        private const string YFMainMenuex = "MainMenuex";
        private const string SystemControl = "SystemControl";
        private const string APServerProcess = "Digiwin.Mars.ServerStart";
        private const string APServerWindowsService = "Digiwin.Mars.ServerStart.WindowsService";
        
        private const string LicenseCenterProcess = "Digiwin.Mars.License.Management";
        private const string LicenseCenterWindowsService = "Digiwin.Mars.License.WindowsService";

        //服务注册表
        private const string APServerServiceRegisterPath = @"SYSTEM\CurrentControlSet\Services\DIGIWIN.E10.APSERVICE";

        private const string LicenseCenterServiceRegisterPath =
            @"SYSTEM\CurrentControlSet\Services\DIGIWIN.E10.LICENSESERVICE";

        private const string ImagePathOption = "ImagePath";

        private const string LicenseCenterInstallRegisterPath = @"SOFTWARE\Wow6432Node\Digiwin\E10\LicenseCenter";
        private const string APServerInstallRegisterPath = @"SOFTWARE\Wow6432Node\Digiwin\E10\APServer";

        private const string InstallDirOption = "InstallDir";
        private const string StatusOption = "Status";
        private const string InstalledStatus = "Installed";

        private static readonly object OperateLock = new object();

        #endregion

        #region 基本路径

        private static readonly DirectoryInfo BaseDirectoryInfo =
            new DirectoryInfo(AppDomain.CurrentDomain.SetupInformation.ApplicationBase); //运行目录

        private static readonly string ApplicationPath = BaseDirectoryInfo.Parent.FullName; //根目录

        /// <summary>
        ///     当前版本:根据安装目录获取当前版本
        /// </summary>
        public static string CurrentVersion
        {
            get
            {
                return BaseDirectoryInfo.Name;
            }
        }

        #endregion

        #region 客户端相关目录

        /// <summary>
        ///     待发文件目录
        /// </summary>
        internal static string SendDataPath
        {
            get
            {
                return Path.Combine(ApplicationPath, "SendData");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal static string RandTempPath
        {
            get
            {
                return Path.Combine(Path.GetTempPath(), string.Format("Temp{0}", Guid.NewGuid().ToString()));
            }
        }


        /// <summary>
        ///     收集的数据文件存放目录
        /// </summary>
        internal static string CollectedDataPath
        {
            get
            {
                return Path.Combine(ApplicationPath, "CollectedData");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal static string LogBasePath
        {
            get
            {
                return Path.Combine(ApplicationPath, "Log");
            }
        }

        /// <summary>
        ///     日志文件目录
        /// </summary>
        internal static string LogPath
        {
            get
            {
                return Path.Combine(Path.Combine(ApplicationPath, "Log"), DateTime.Now.ToString("yyyyMMdd"));
            }
        }

        /// <summary>
        ///     配置文件目录
        /// </summary>
        internal static string ConfigPath
        {
            get
            {
                return Path.Combine(ApplicationPath, "Config");
            }
        }

        internal static string RunTimePath
        {
            get
            {
                return Path.Combine(ApplicationPath, "Runtime");
            }
        }

        /// <summary>
        ///     插件运行时记录文件目录
        /// </summary>
        internal static string PluginExecuteRecordPath
        {
            get
            {
                return Path.Combine(RunTimePath, "PluginExecuteRecord");
            }
        }

        /// <summary>
        ///     客户端配置文件
        /// </summary>
        internal static string ClientConfigPath
        {
            get
            {
                return Path.Combine(ConfigPath, "Client.cfg");
            }
        }

        /// <summary>
        ///     客户端更新配置文件
        /// </summary>
        internal static string UpdateConfigPath
        {
            get
            {
                return Path.Combine(ConfigPath, "Update.cfg");
            }
        }

        /// <summary>
        ///     插件配置文件路径
        /// </summary>
        internal static string PluginDefinitionPath
        {
            get
            {
                return Path.Combine(ConfigPath, "pluginDefinition.list");
            }
        }

        internal static string PluginDefinitionPathBackup
        {
            get
            {
                return Path.Combine(ConfigPath, "pluginDefinition.list.backup");
            }
        }

        internal static string PluginDefinitionPathError
        {
            get
            {
                return Path.Combine(ConfigPath, "pluginDefinition.list.error");
            }
        }


        #endregion

        #region E10 Path Common

        private static bool CheckInstall(string installRegisterPath)
        {
            return string.Equals(
                RegistUtils.GetStringValue(installRegisterPath, StatusOption),
                InstalledStatus);
        }

        private static bool TryGetPath(string installRegisterPath, string serviceRegisterPath,
            string windowsServiceProcessName,
            string processName, string relativePath, out string detectedPath, out int step)
        {
            //0.根据安装的注册表获取路径
            try
            {
                if (CheckInstall(installRegisterPath))
                {
                    detectedPath = RegistUtils.GetStringValue(installRegisterPath, InstallDirOption);
                    step = 1;
                    return true;
                }
            }
            catch (Exception)
            {
            }
            //1.根据服务名获取服务安装路径
            try
            {
                var serviceExeFileName =
                    RegistUtils.GetStringValue(serviceRegisterPath, ImagePathOption).Replace(
                        "\"",
                        "");
                var fileInfo = new FileInfo(serviceExeFileName);
                if (fileInfo.Exists)
                {
                    //获取到的是 
                    //E:\E10\Server\Control\Digiwin.Mars.ServerStart.WindowsService.exe
                    detectedPath = fileInfo.Directory.FullName;
                    step = 2;
                    return true;
                }
            }
            catch (Exception)
            {
            }
            //2.根据 Windows Service 进程获取
            try
            {
                var ps = Process.GetProcessesByName(windowsServiceProcessName);
                if (ps.Length != 0)
                {
                    var fileInfo = new FileInfo(ps[0].MainModule.FileName);
                    if (fileInfo.Exists)
                    {
                        detectedPath = fileInfo.Directory.FullName;
                        step = 3;
                        return true;
                    }
                }
            }
            catch (Exception)
            {
            }
            try
            {
                //3.根据启动的进程获取
                var process = Process.GetProcessesByName(processName);
                if (process.Length != 0)
                {
                    var fileInfo = new FileInfo(process[0].MainModule.FileName);
                    if (fileInfo.Exists)
                    {
                        detectedPath = fileInfo.Directory.FullName;
                        step = 4;
                        return true;
                    }
                }
            }
            catch (Exception)
            {
            }
            try
            {
                detectedPath = Path.Combine(BaseDirectoryInfo.Parent.Parent.FullName, relativePath);
                //4.根据当前安装目录的平行目录获取
                if (Directory.Exists(detectedPath))
                {
                    step = 5;
                    return true;
                }
            }
            catch (Exception)
            {
            }
            detectedPath = string.Empty;
            step = 0;
            return false;
        }

        #endregion

        #region AP Server

        /// <summary>
        ///     检查是否安装了 AP Server
        /// </summary>
        /// <returns></returns>
        internal static bool CheckAPServerInstalled()
        {
            return CheckInstall(APServerInstallRegisterPath);
        }

        /// <summary>
        ///     检测当前服务器是否存在AP Server
        /// </summary>
        /// <returns></returns>
        internal static bool CheckAPServerExists()
        {
            string installPath;
            return TryGetAPServerPath(out installPath);
        }


        /// <summary>
        ///     获取AP Server 的根路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static bool TryGetAPServerPath(out string path)
        {
            const string installRegisterPath = APServerInstallRegisterPath;
            const string serviceRegisterPath = APServerServiceRegisterPath;
            const string windowsServiceProcessName = APServerWindowsService;
            const string processName = APServerProcess;
            const string relativePath = "Server";
            int step;
            var find = TryGetPath(
                installRegisterPath, serviceRegisterPath, windowsServiceProcessName, processName, relativePath,
                out path, out step);
            if (step == 2
                || step == 3
                || step == 4)
            {
                path = new DirectoryInfo(path).Parent.FullName;
            }
            return find;
        }

        /// <summary>
        ///     获取 AP Server Control路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns>是否获取成功</returns>
        internal static bool TryGetAPServerControlPath(out string path)
        {
            if (TryGetAPServerPath(out path))
            {
                path = Path.Combine(path, "Control");
                if (Directory.Exists(path))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     获取Application 路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static bool TryGetAPServerApplicationPath(out string path)
        {
            if (TryGetAPServerPath(out path))
            {
                path = Path.Combine(path, "Application");
                if (Directory.Exists(path))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     获取AccountSetsConfiguration配置文件路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static bool TryGetAccountSetsConfigurationPath(out string path)
        {
            if (TryGetAPServerControlPath(out path))
            {
                path = Path.Combine(path, "AccountSetsConfiguration.xml");
                if (File.Exists(path))
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool TryGetVersionPath(out string path)
        {
            if (TryGetAPServerApplicationPath(out path))
            {
                path = Path.Combine(path, "Version.xml");
                if (File.Exists(path))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region License Server

        /// <summary>
        ///     检查本机是否安装了授权
        /// </summary>
        /// <returns></returns>
        internal static bool CheckLicenseCenterInstalled()
        {
            return CheckInstall(LicenseCenterInstallRegisterPath);
        }

        /// <summary>
        ///     检查本机是否存在License Center
        /// </summary>
        /// <returns></returns>
        internal static bool CheckLicenseCenterExists()
        {
            string installPath;
            return TryGetLicenseCenterPath(out installPath);
        }

        /// <summary>
        ///     获取本地授权中心的安装目录
        /// </summary>
        /// <returns></returns>
        internal static bool TryGetLicenseCenterPath(out string path)
        {
            const string installRegisterPath = LicenseCenterInstallRegisterPath;
            const string serviceRegisterPath = LicenseCenterServiceRegisterPath;
            const string windowsServiceProcessName = LicenseCenterWindowsService;
            const string processName = LicenseCenterProcess;
            const string relativePath = "LicenseCenter";
            int step;
            var find = TryGetPath(
                installRegisterPath, serviceRegisterPath, windowsServiceProcessName, processName, relativePath,
                out path, out step);
            return find;
        }

        /// <summary>
        ///     获取授权配置路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static bool TryGetLicenseCenterConfigPath(out string path)
        {
            if (TryGetLicenseCenterPath(out path))
            {
                path = Path.Combine(path, "Digiwin.Mars.License.WindowsService.exe.Config");
                if (File.Exists(path))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     获取授权文件路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static bool TryGetLicenseInfoPath(out string path)
        {
            if (TryGetLicenseCenterPath(out path))
            {
                path = Path.Combine(path, "Digiwin.E10.License.xml");
                if (File.Exists(path))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     获取授权SN文件路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static bool TryGetLicenseSnPath(out string path)
        {
            if (TryGetLicenseCenterPath(out path))
            {
                path = Path.Combine(path, "Digiwin.E10.SN.xml");
                if (File.Exists(path))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion      

        
        
        #region 操作目录的方法

        /// <summary>
        ///     确保一个文件所在的路径存在 如果不存在响应的路径 则创建
        /// </summary>
        /// <param name="filePath">需要确认的路径</param>
        internal static void IssueFilePathExist(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (!Directory.Exists(fileInfo.DirectoryName))
            {
                lock (OperateLock)
                {
                    if (!Directory.Exists(fileInfo.DirectoryName))
                    {
                        Directory.CreateDirectory(fileInfo.DirectoryName);
                    }
                }
            }
        }

        /// <summary>
        ///     确保某个文件夹存在
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        internal static void IssueDirectoryPathExist(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                lock (OperateLock)
                {
                    if (!Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }
                }
            }
        }

        //
        internal static void Clear(string dirPath)
        {
            lock (OperateLock)
            {
                if (Directory.Exists(dirPath))
                {
                    Directory.Delete(dirPath, true);
                }
                IssueDirectoryPathExist(dirPath);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="filePath"></param>
        internal static void IssueFileNormal(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return;
                }
                //去除文件的所有属性 设置为 Normal
                File.SetAttributes(filePath, FileAttributes.Normal);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("IssueFileNormal:{0}", filePath), ex);
            }
        }

        #endregion
    }

}
