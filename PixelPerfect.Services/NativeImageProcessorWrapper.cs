using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PixelPerfect.Services
{
    /// <summary>
    /// 本地图像处理DLL的C#包装类
    /// </summary>
    public class NativeImageProcessorWrapper
    {
        // 添加详细日志记录
        private static readonly string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "native_wrapper_detailed.log");
        private static readonly bool _enableLogging = true;

        private static void LogMessage(string message)
        {
            if (!_enableLogging) return;
            try
            {
                File.AppendAllText(_logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        static NativeImageProcessorWrapper()
        {
            LogMessage("NativeImageProcessorWrapper 静态构造函数开始初始化");
            try
            {
                LogMessage($"应用程序目录: {AppDomain.CurrentDomain.BaseDirectory}");

                // 记录DLL信息
                string[] requiredDlls = new[] {
                    "PixelPerfect.NativeImageProcessing.dll",
                    "jpeg62.dll",
                    "libpng16d.dll",
                    "zlibd1.dll"
                };

                foreach (string dll in requiredDlls)
                {
                    string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dll);
                    bool exists = File.Exists(dllPath);
                    LogMessage($"检查DLL {dll}: {(exists ? "存在" : "不存在")}");

                    if (exists)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(dllPath);
                            LogMessage($"  - 大小: {fileInfo.Length} 字节");
                            LogMessage($"  - 创建时间: {fileInfo.CreationTime}");
                            LogMessage($"  - 修改时间: {fileInfo.LastWriteTime}");
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"  - 获取文件信息失败: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"静态构造函数发生异常: {ex.Message}\n{ex.StackTrace}");
            }
            LogMessage("NativeImageProcessorWrapper 静态构造函数初始化完成");
        }

        // DLL导入 - 添加调用约定、字符集和异常处理设置
        [DllImport("PixelPerfect.NativeImageProcessing.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern bool CompressImage(byte[] input, int inputSize, byte[] output,
                                               ref int outputSize, int quality);

        [DllImport("PixelPerfect.NativeImageProcessing.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int DetectFaces(byte[] imageData, int length);

        [DllImport("PixelPerfect.NativeImageProcessing.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int CompareImages(byte[] imageData1, int length1,
                                               byte[] imageData2, int length2);

        [DllImport("PixelPerfect.NativeImageProcessing.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern bool SharpenImage(byte[] input, int inputSize,
                                              byte[] output, ref int outputSize);

        [DllImport("PixelPerfect.NativeImageProcessing.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetLastErrorMessage();

        /// <summary>
        /// 获取最后一个错误消息
        /// </summary>
        private static string GetLastError()
        {
            try
            {
                LogMessage("调用GetLastErrorMessage获取错误信息");
                IntPtr ptr = GetLastErrorMessage();
                LogMessage($"GetLastErrorMessage返回指针值: {ptr}");

                if (ptr == IntPtr.Zero)
                {
                    LogMessage("ERROR: 错误指针为空");
                    return "未知错误 (空错误指针)";
                }

                string message = Marshal.PtrToStringAnsi(ptr);
                LogMessage($"解析的错误消息: {message ?? "(null)"}");
                return message ?? "未能解析错误消息";
            }
            catch (Exception ex)
            {
                LogMessage($"获取错误消息时发生异常: {ex.Message}\n{ex.StackTrace}");
                return $"获取错误消息出错: {ex.Message}";
            }
        }

        /// <summary>
        /// 压缩图像
        /// </summary>
        /// <param name="imageData">原始图像数据</param>
        /// <param name="quality">质量参数(0-100)</param>
        /// <returns>压缩后的图像数据</returns>
        public static byte[] CompressImage(byte[] imageData, int quality)
        {
            LogMessage($"CompressImage调用开始 - 输入大小: {(imageData?.Length ?? 0)} 字节, 质量: {quality}");

            if (imageData == null || imageData.Length == 0)
            {
                LogMessage("ERROR: 图像数据为空");
                throw new ArgumentException("图像数据不能为空");
            }

            if (quality < 0 || quality > 100)
            {
                LogMessage($"ERROR: 无效的质量参数: {quality}");
                throw new ArgumentException("质量参数必须在0-100范围内");
            }

            try
            {
                LogMessage("创建输出缓冲区");
                // 分配足够大的输出缓冲区 - 多分配一些空间防止缓冲区溢出
                byte[] output = new byte[imageData.Length * 2];
                int outputSize = output.Length;

                LogMessage($"调用本机CompressImage方法 - 输入大小: {imageData.Length}, 输出缓冲区: {outputSize}");
                bool success = false;

                try
                {
                    success = CompressImage(imageData, imageData.Length, output, ref outputSize, quality);
                    LogMessage($"本机方法返回: {success}, 输出大小: {outputSize}");
                }
                catch (Exception ex)
                {
                    LogMessage($"本机方法异常: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                    throw new InvalidOperationException($"调用本机压缩方法时出错: {ex.Message}", ex);
                }

                if (!success)
                {
                    string error = GetLastError();
                    LogMessage($"压缩失败，错误消息: {error}");
                    throw new InvalidOperationException($"图像压缩失败: {error}");
                }

                LogMessage($"压缩成功，调整输出大小为: {outputSize}");
                // 调整输出大小
                if (outputSize < output.Length)
                {
                    byte[] result = new byte[outputSize];
                    Array.Copy(output, result, outputSize);
                    LogMessage($"压缩完成，最终大小: {result.Length} 字节");
                    return result;
                }

                LogMessage($"压缩完成，无需调整大小，大小: {output.Length} 字节");
                return output;
            }
            catch (Exception ex)
            {
                LogMessage($"图像压缩处理异常: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                throw new InvalidOperationException($"图像压缩过程中发生异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检测图像中的人脸数量
        /// </summary>
        /// <param name="imageData">图像数据</param>
        /// <returns>检测到的人脸数量</returns>
        public static int DetectFaces(byte[] imageData)
        {
            LogMessage($"DetectFaces调用开始 - 输入大小: {(imageData?.Length ?? 0)} 字节");

            if (imageData == null || imageData.Length == 0)
            {
                LogMessage("ERROR: 图像数据为空");
                throw new ArgumentException("图像数据不能为空");
            }

            try
            {
                LogMessage("调用本机DetectFaces方法");
                int result = DetectFaces(imageData, imageData.Length);
                LogMessage($"本机方法返回: {result}");

                if (result < 0)
                {
                    string error = GetLastError();
                    LogMessage($"人脸检测失败，错误消息: {error}");
                    throw new InvalidOperationException($"人脸检测失败: {error}");
                }

                LogMessage($"人脸检测完成，检测到 {result} 个人脸");
                return result;
            }
            catch (Exception ex)
            {
                LogMessage($"人脸检测处理异常: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                throw new InvalidOperationException($"人脸检测过程中发生异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 比较两个图像的相似度
        /// </summary>
        /// <param name="imageData1">第一个图像数据</param>
        /// <param name="imageData2">第二个图像数据</param>
        /// <returns>相似度百分比(0-100)</returns>
        public static int CompareImages(byte[] imageData1, byte[] imageData2)
        {
            LogMessage($"CompareImages调用开始 - 输入1大小: {(imageData1?.Length ?? 0)} 字节, 输入2大小: {(imageData2?.Length ?? 0)} 字节");

            if (imageData1 == null || imageData1.Length == 0)
            {
                LogMessage("ERROR: 第一个图像数据为空");
                throw new ArgumentException("第一个图像数据不能为空");
            }

            if (imageData2 == null || imageData2.Length == 0)
            {
                LogMessage("ERROR: 第二个图像数据为空");
                throw new ArgumentException("第二个图像数据不能为空");
            }

            try
            {
                LogMessage("调用本机CompareImages方法");
                int result = CompareImages(imageData1, imageData1.Length, imageData2, imageData2.Length);
                LogMessage($"本机方法返回: {result}");

                if (result < 0)
                {
                    string error = GetLastError();
                    LogMessage($"图像比较失败，错误消息: {error}");
                    throw new InvalidOperationException($"图像比较失败: {error}");
                }

                LogMessage($"图像比较完成，相似度: {result}%");
                return result;
            }
            catch (Exception ex)
            {
                LogMessage($"图像比较处理异常: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                throw new InvalidOperationException($"图像比较过程中发生异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 锐化图像
        /// </summary>
        /// <param name="imageData">原始图像数据</param>
        /// <returns>锐化后的图像数据</returns>
        public static byte[] SharpenImage(byte[] imageData)
        {
            LogMessage($"SharpenImage调用开始 - 输入大小: {(imageData?.Length ?? 0)} 字节");

            if (imageData == null || imageData.Length == 0)
            {
                LogMessage("ERROR: 图像数据为空");
                throw new ArgumentException("图像数据不能为空");
            }

            try
            {
                LogMessage("创建输出缓冲区");
                byte[] output = new byte[imageData.Length];
                int outputSize = output.Length;

                LogMessage("调用本机SharpenImage方法");
                bool success = false;

                try
                {
                    success = SharpenImage(imageData, imageData.Length, output, ref outputSize);
                    LogMessage($"本机方法返回: {success}, 输出大小: {outputSize}");
                }
                catch (Exception ex)
                {
                    LogMessage($"本机方法异常: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                    throw new InvalidOperationException($"调用本机锐化方法时出错: {ex.Message}", ex);
                }

                if (!success)
                {
                    string error = GetLastError();
                    LogMessage($"锐化失败，错误消息: {error}");
                    throw new InvalidOperationException($"图像锐化失败: {error}");
                }

                LogMessage($"锐化成功，调整输出大小为: {outputSize}");
                // 调整输出大小
                if (outputSize < output.Length)
                {
                    byte[] result = new byte[outputSize];
                    Array.Copy(output, result, outputSize);
                    LogMessage($"锐化完成，最终大小: {result.Length} 字节");
                    return result;
                }

                LogMessage($"锐化完成，无需调整大小，大小: {output.Length} 字节");
                return output;
            }
            catch (Exception ex)
            {
                LogMessage($"图像锐化处理异常: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                throw new InvalidOperationException($"图像锐化过程中发生异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 测试本机库是否正常工作
        /// </summary>
        public static bool TestNativeLibrary()
        {
            LogMessage("开始测试本机库");

            try
            {
                // 创建一个非常小的图像数据用于测试
                byte[] testData = new byte[] {
                    0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
                    0x01, 0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43,
                    0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
                };

                LogMessage($"测试GetLastErrorMessage");
                string error = GetLastError();
                LogMessage($"错误消息: {error}");

                LogMessage("测试完成");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"测试本机库时发生异常: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
    }
}