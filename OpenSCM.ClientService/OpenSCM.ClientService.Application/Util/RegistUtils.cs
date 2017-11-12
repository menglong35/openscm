using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenSCM.ClientService.Application
{
    /// <summary>
    /// 注册表帮助类
    /// </summary>
    internal class RegistUtils
    {

        /// <summary>
        /// 获取字符类型值
        /// </summary>
        /// <param name="subKeyName">注册项名称</param>
        /// <param name="optionName">键名称</param>
        /// <returns></returns>
        internal static string GetStringValue(string subKeyName, string optionName)
        {
            var key = GetRegistKey(subKeyName, false);
            return key.GetValue(optionName).ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subKeyName"></param>
        /// <param name="optionName"></param>
        /// <returns></returns>
        internal static object GetValue(string subKeyName, string optionName)
        {
            var key = GetRegistKey(subKeyName, false);
            return key.GetValue(optionName);
        }

        /// <summary>
        /// 获取注册值
        /// </summary>
        /// <param name="subKeyName">注册项名称</param>
        /// <param name="writable">获取的注册表是否可写</param>
        /// <returns>是否获取成功</returns>
        private static RegistryKey GetRegistKey(string subKeyName, bool writable)
        {
            return Registry.LocalMachine.OpenSubKey(subKeyName, writable);
        }

        /// <summary>
        /// 创建注册项
        /// </summary>
        /// <param name="subKeyName">创建的注册表项的路径</param>
        /// <returns>创建的注册项</returns>
        private static RegistryKey CreateRegistKey(string subKeyName)
        {
            var key = Registry.LocalMachine.CreateSubKey(subKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree);
            return key;
        }

        /// <summary>
        /// 设置值
        /// </summary>
        /// <param name="subKeyName">注册项名称</param>
        /// <param name="optionName">键名称</param>
        /// <param name="value">设置的值</param>
        /// <returns>是否设置成功</returns>
        internal static void SetValue(string subKeyName, string optionName, object value)
        {
            var key = GetRegistKey(subKeyName, true);
            key.SetValue(optionName, value);
        }

        /// <summary>
        /// 删除注册项
        /// </summary>
        /// <param name="subKeyName"></param>
        internal static void DeleteRegistKey(string subKeyName)
        {
            Registry.LocalMachine.DeleteSubKeyTree(subKeyName);
        }

    }
}
