using System;
using System.Runtime.InteropServices;

namespace PixelPerfect.Services
{
    /// <summary>
    /// 本地图像处理DLL的C#包装类
    /// </summary>
    public class NativeImageProcessorWrapper
    {
        // DLL导入
        [DllImport("PixelPerfect.NativeImageProcessing.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool CompressImage(byte[] input, int inputSize, byte[] output,
                                               ref int outputSize, int quality);

        [DllImport("PixelPerfect.NativeImageProcessing.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int DetectFaces(byte[] imageData, int length);

        [DllImport("PixelPerfect.NativeImageProcessing.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int CompareImages(byte[] imageData1, int length1,
                                               byte[] imageData2, int length2);

        [DllImport("PixelPerfect.NativeImageProcessing.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SharpenImage(byte[] input, int inputSize,
                                              byte[] output, ref int outputSize);

        [DllImport("PixelPerfect.NativeImageProcessing.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetLastErrorMessage();

        /// <summary>
        /// 获取最后一个错误消息
        /// </summary>
        private static string GetLastError()
        {
            IntPtr ptr = GetLastErrorMessage();
            return Marshal.PtrToStringAnsi(ptr);
        }

        /// <summary>
        /// 压缩图像
        /// </summary>
        /// <param name="imageData">原始图像数据</param>
        /// <param name="quality">质量参数(0-100)</param>
        /// <returns>压缩后的图像数据</returns>
        public static byte[] CompressImage(byte[] imageData, int quality)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("图像数据不能为空");

            if (quality < 0 || quality > 100)
                throw new ArgumentException("质量参数必须在0-100范围内");

            // 分配足够大的输出缓冲区
            byte[] output = new byte[imageData.Length];
            int outputSize = output.Length;

            if (!CompressImage(imageData, imageData.Length, output, ref outputSize, quality))
                throw new InvalidOperationException($"图像压缩失败: {GetLastError()}");

            // 调整输出大小
            if (outputSize < output.Length)
            {
                byte[] result = new byte[outputSize];
                Array.Copy(output, result, outputSize);
                return result;
            }

            return output;
        }

        /// <summary>
        /// 检测图像中的人脸数量
        /// </summary>
        /// <param name="imageData">图像数据</param>
        /// <returns>检测到的人脸数量</returns>
        public static int DetectFaces(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("图像数据不能为空");

            return DetectFaces(imageData, imageData.Length);
        }

        /// <summary>
        /// 比较两个图像的相似度
        /// </summary>
        /// <param name="imageData1">第一个图像数据</param>
        /// <param name="imageData2">第二个图像数据</param>
        /// <returns>相似度百分比(0-100)</returns>
        public static int CompareImages(byte[] imageData1, byte[] imageData2)
        {
            if (imageData1 == null || imageData1.Length == 0)
                throw new ArgumentException("第一个图像数据不能为空");

            if (imageData2 == null || imageData2.Length == 0)
                throw new ArgumentException("第二个图像数据不能为空");

            return CompareImages(imageData1, imageData1.Length, imageData2, imageData2.Length);
        }

        /// <summary>
        /// 锐化图像
        /// </summary>
        /// <param name="imageData">原始图像数据</param>
        /// <returns>锐化后的图像数据</returns>
        public static byte[] SharpenImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("图像数据不能为空");

            byte[] output = new byte[imageData.Length];
            int outputSize = output.Length;

            if (!SharpenImage(imageData, imageData.Length, output, ref outputSize))
                throw new InvalidOperationException($"图像锐化失败: {GetLastError()}");

            return output;
        }
    }
}