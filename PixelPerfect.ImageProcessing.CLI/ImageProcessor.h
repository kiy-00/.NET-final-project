#pragma once

using namespace System;
using namespace System::Drawing;
using namespace System::Drawing::Imaging;
using namespace System::IO;
using namespace System::Runtime::InteropServices;

namespace PixelPerfect {
    namespace ImageProcessing {
        namespace CLI {

            public ref class ImageProcessor
            {
            public:
                // 构造函数
                ImageProcessor();

                // 应用滤镜
                array<Byte>^ ApplyFilter(array<Byte>^ imageData, String^ filterType);

                // 添加水印
                array<Byte>^ AddWatermark(array<Byte>^ imageData, String^ watermarkText,
                    float opacity, int position);

                // 分析图像质量
                bool AnalyzeImageQuality(array<Byte>^ imageData, [Out] double% qualityScore);

                // 提取图像元数据
                System::Collections::Generic::Dictionary<String^, String^>^ ExtractMetadata(array<Byte>^ imageData);

                // 调整图像大小
                array<Byte>^ ResizeImage(array<Byte>^ imageData, int width, int height);

                // 裁剪图像
                array<Byte>^ CropImage(array<Byte>^ imageData, int x, int y, int width, int height);

                // 生成缩略图(与你现有的FileStorageService功能相似)
                array<Byte>^ CreateThumbnail(array<Byte>^ imageData, int width, int height);

            private:
                // 将字节数组转换为Bitmap
                Bitmap^ BytesToBitmap(array<Byte>^ imageBytes);

                // 将Bitmap转换为字节数组
                array<Byte>^ BitmapToBytes(Bitmap^ bitmap, ImageFormat^ format);
            };
        }
    }
}