#include "pch.h"
#include "ImageProcessor.h"

namespace PixelPerfect {
    namespace ImageProcessing {
        namespace CLI {

            ImageProcessor::ImageProcessor()
            {
                // 构造函数，可以添加初始化代码
            }

            array<Byte>^ ImageProcessor::ApplyFilter(array<Byte>^ imageData, String^ filterType)
            {
                try {
                    Bitmap^ bitmap = BytesToBitmap(imageData);

                    if (String::Equals(filterType, "grayscale", StringComparison::OrdinalIgnoreCase))
                    {
                        // 实现灰度滤镜
                        for (int y = 0; y < bitmap->Height; y++)
                        {
                            for (int x = 0; x < bitmap->Width; x++)
                            {
                                Color pixel = bitmap->GetPixel(x, y);
                                int grayValue = (int)(pixel.R * 0.3 + pixel.G * 0.59 + pixel.B * 0.11);
                                bitmap->SetPixel(x, y, Color::FromArgb(pixel.A, grayValue, grayValue, grayValue));
                            }
                        }
                    }
                    else if (String::Equals(filterType, "sepia", StringComparison::OrdinalIgnoreCase))
                    {
                        // 实现棕褐色滤镜
                        for (int y = 0; y < bitmap->Height; y++)
                        {
                            for (int x = 0; x < bitmap->Width; x++)
                            {
                                Color pixel = bitmap->GetPixel(x, y);
                                int r = (int)(pixel.R * 0.393 + pixel.G * 0.769 + pixel.B * 0.189);
                                int g = (int)(pixel.R * 0.349 + pixel.G * 0.686 + pixel.B * 0.168);
                                int b = (int)(pixel.R * 0.272 + pixel.G * 0.534 + pixel.B * 0.131);

                                r = Math::Min(r, 255);
                                g = Math::Min(g, 255);
                                b = Math::Min(b, 255);

                                bitmap->SetPixel(x, y, Color::FromArgb(pixel.A, r, g, b));
                            }
                        }
                    }
                    else if (String::Equals(filterType, "negative", StringComparison::OrdinalIgnoreCase))
                    {
                        // 实现反色滤镜
                        for (int y = 0; y < bitmap->Height; y++)
                        {
                            for (int x = 0; x < bitmap->Width; x++)
                            {
                                Color pixel = bitmap->GetPixel(x, y);
                                bitmap->SetPixel(x, y, Color::FromArgb(
                                    pixel.A,
                                    255 - pixel.R,
                                    255 - pixel.G,
                                    255 - pixel.B));
                            }
                        }
                    }
                    // 添加新滤镜类型 - 高对比度
                    else if (String::Equals(filterType, "highcontrast", StringComparison::OrdinalIgnoreCase))
                    {
                        for (int y = 0; y < bitmap->Height; y++)
                        {
                            for (int x = 0; x < bitmap->Width; x++)
                            {
                                Color pixel = bitmap->GetPixel(x, y);
                                int r = pixel.R;
                                int g = pixel.G;
                                int b = pixel.B;

                                // 增加对比度的简单算法
                                r = (int)((r - 128) * 1.5 + 128);
                                g = (int)((g - 128) * 1.5 + 128);
                                b = (int)((b - 128) * 1.5 + 128);

                                r = Math::Max(0, Math::Min(255, r));
                                g = Math::Max(0, Math::Min(255, g));
                                b = Math::Max(0, Math::Min(255, b));

                                bitmap->SetPixel(x, y, Color::FromArgb(pixel.A, r, g, b));
                            }
                        }
                    }
                    // 添加新滤镜类型 - 模糊
                    else if (String::Equals(filterType, "blur", StringComparison::OrdinalIgnoreCase))
                    {
                        Bitmap^ result = gcnew Bitmap(bitmap->Width, bitmap->Height);

                        // 简单的3x3均值模糊
                        for (int y = 1; y < bitmap->Height - 1; y++)
                        {
                            for (int x = 1; x < bitmap->Width - 1; x++)
                            {
                                int sumR = 0, sumG = 0, sumB = 0;

                                // 3x3邻域像素求和
                                for (int ky = -1; ky <= 1; ky++)
                                {
                                    for (int kx = -1; kx <= 1; kx++)
                                    {
                                        Color pixel = bitmap->GetPixel(x + kx, y + ky);
                                        sumR += pixel.R;
                                        sumG += pixel.G;
                                        sumB += pixel.B;
                                    }
                                }

                                // 取平均值
                                int avgR = sumR / 9;
                                int avgG = sumG / 9;
                                int avgB = sumB / 9;

                                result->SetPixel(x, y, Color::FromArgb(bitmap->GetPixel(x, y).A, avgR, avgG, avgB));
                            }
                        }

                        delete bitmap;
                        bitmap = result;
                    }
                    // 添加新滤镜类型 - 红色增强
                    else if (String::Equals(filterType, "redboost", StringComparison::OrdinalIgnoreCase))
                    {
                        for (int y = 0; y < bitmap->Height; y++)
                        {
                            for (int x = 0; x < bitmap->Width; x++)
                            {
                                Color pixel = bitmap->GetPixel(x, y);
                                int r = Math::Min(255, (int)(pixel.R * 1.5));
                                bitmap->SetPixel(x, y, Color::FromArgb(pixel.A, r, pixel.G, pixel.B));
                            }
                        }
                    }
                    // 添加新滤镜类型 - 绿色增强
                    else if (String::Equals(filterType, "greenboost", StringComparison::OrdinalIgnoreCase))
                    {
                        for (int y = 0; y < bitmap->Height; y++)
                        {
                            for (int x = 0; x < bitmap->Width; x++)
                            {
                                Color pixel = bitmap->GetPixel(x, y);
                                int g = Math::Min(255, (int)(pixel.G * 1.5));
                                bitmap->SetPixel(x, y, Color::FromArgb(pixel.A, pixel.R, g, pixel.B));
                            }
                        }
                    }
                    // 添加新滤镜类型 - 蓝色增强
                    else if (String::Equals(filterType, "blueboost", StringComparison::OrdinalIgnoreCase))
                    {
                        for (int y = 0; y < bitmap->Height; y++)
                        {
                            for (int x = 0; x < bitmap->Width; x++)
                            {
                                Color pixel = bitmap->GetPixel(x, y);
                                int b = Math::Min(255, (int)(pixel.B * 1.5));
                                bitmap->SetPixel(x, y, Color::FromArgb(pixel.A, pixel.R, pixel.G, b));
                            }
                        }
                    }
                    // 添加新滤镜类型 - 冷色调
                    else if (String::Equals(filterType, "cold", StringComparison::OrdinalIgnoreCase))
                    {
                        for (int y = 0; y < bitmap->Height; y++)
                        {
                            for (int x = 0; x < bitmap->Width; x++)
                            {
                                Color pixel = bitmap->GetPixel(x, y);
                                int r = Math::Max(0, pixel.R - 30);
                                int b = Math::Min(255, pixel.B + 30);
                                bitmap->SetPixel(x, y, Color::FromArgb(pixel.A, r, pixel.G, b));
                            }
                        }
                    }
                    // 添加新滤镜类型 - 暖色调
                    else if (String::Equals(filterType, "warm", StringComparison::OrdinalIgnoreCase))
                    {
                        for (int y = 0; y < bitmap->Height; y++)
                        {
                            for (int x = 0; x < bitmap->Width; x++)
                            {
                                Color pixel = bitmap->GetPixel(x, y);
                                int r = Math::Min(255, pixel.R + 30);
                                int g = Math::Min(255, pixel.G + 15);
                                int b = Math::Max(0, pixel.B - 30);
                                bitmap->SetPixel(x, y, Color::FromArgb(pixel.A, r, g, b));
                            }
                        }
                    }

                    return BitmapToBytes(bitmap, ImageFormat::Jpeg);
                }
                catch (Exception^ ex) {
                    // 处理异常并返回原始图像
                    Console::WriteLine("图像滤镜处理失败: " + ex->Message);
                    return imageData;
                }
            }

            array<Byte>^ ImageProcessor::AddWatermark(array<Byte>^ imageData, String^ watermarkText,
                float opacity, int position)
            {
                try {
                    Bitmap^ bitmap = BytesToBitmap(imageData);
                    Graphics^ g = Graphics::FromImage(bitmap);

                    // 设置文本样式
                    Font^ font = gcnew Font("Arial", 20);
                    SolidBrush^ brush = gcnew SolidBrush(Color::FromArgb((int)(255 * opacity), 255, 255, 255));

                    // 设置水印位置
                    int x = 10;
                    int y = 10;

                    if (position == 1) // 中心
                    {
                        SizeF textSize = g->MeasureString(watermarkText, font);
                        x = (bitmap->Width - (int)textSize.Width) / 2;
                        y = (bitmap->Height - (int)textSize.Height) / 2;
                    }
                    else if (position == 2) // 右下角
                    {
                        SizeF textSize = g->MeasureString(watermarkText, font);
                        x = bitmap->Width - (int)textSize.Width - 10;
                        y = bitmap->Height - (int)textSize.Height - 10;
                    }

                    // 创建半透明背景区域增强可读性
                    SizeF textSize = g->MeasureString(watermarkText, font);
                    g->FillRectangle(
                        gcnew SolidBrush(Color::FromArgb(100, 0, 0, 0)),
                        (float)(x - 5), (float)(y - 5),
                        (float)(textSize.Width + 10),
                        (float)(textSize.Height + 10));

                    // 绘制水印
                    g->DrawString(watermarkText, font, brush, (float)x, (float)y);

                    delete g;
                    delete font;
                    delete brush;

                    return BitmapToBytes(bitmap, ImageFormat::Jpeg);
                }
                catch (Exception^ ex) {
                    // 处理异常并返回原始图像
                    Console::WriteLine("添加水印失败: " + ex->Message);
                    return imageData;
                }
            }

            bool ImageProcessor::AnalyzeImageQuality(array<Byte>^ imageData, [Out] double% qualityScore)
            {
                try
                {
                    Bitmap^ bitmap = BytesToBitmap(imageData);

                    // 使用简单的方法分析图像质量

                    // 分析亮度变化作为边缘锐度指标
                    double contrastSum = 0.0;
                    int contrastCount = 0;

                    for (int y = 1; y < bitmap->Height; y++)
                    {
                        for (int x = 1; x < bitmap->Width; x++)
                        {
                            Color currentPixel = bitmap->GetPixel(x, y);
                            Color leftPixel = bitmap->GetPixel(x - 1, y);
                            Color topPixel = bitmap->GetPixel(x, y - 1);

                            int diffLeft = Math::Abs(currentPixel.GetBrightness() * 255 - leftPixel.GetBrightness() * 255);
                            int diffTop = Math::Abs(currentPixel.GetBrightness() * 255 - topPixel.GetBrightness() * 255);

                            contrastSum += diffLeft + diffTop;
                            contrastCount += 2;
                        }
                    }

                    // 分析噪点 - 在小范围区域内的颜色变化
                    double noiseScore = 0.0;
                    int sampleSize = 10; // 样本大小
                    int samples = 0;

                    Random^ random = gcnew Random();
                    for (int i = 0; i < 20; i++) // 取20个样本区域
                    {
                        int startX = random->Next(0, bitmap->Width - sampleSize);
                        int startY = random->Next(0, bitmap->Height - sampleSize);

                        double localVariance = 0.0;
                        for (int y = startY; y < startY + sampleSize; y++)
                        {
                            for (int x = startX; x < startX + sampleSize; x++)
                            {
                                Color pixel = bitmap->GetPixel(x, y);
                                for (int ny = Math::Max(startY, y - 1); ny <= Math::Min(startY + sampleSize - 1, y + 1); ny++)
                                {
                                    for (int nx = Math::Max(startX, x - 1); nx <= Math::Min(startX + sampleSize - 1, x + 1); nx++)
                                    {
                                        if (nx == x && ny == y) continue;

                                        Color neighbor = bitmap->GetPixel(nx, ny);

                                        int rDiff = Math::Abs(pixel.R - neighbor.R);
                                        int gDiff = Math::Abs(pixel.G - neighbor.G);
                                        int bDiff = Math::Abs(pixel.B - neighbor.B);

                                        localVariance += (rDiff + gDiff + bDiff) / 3.0;
                                        samples++;
                                    }
                                }
                            }
                        }

                        if (samples > 0)
                            noiseScore += localVariance / samples;
                    }

                    noiseScore = noiseScore / 20.0; // 平均噪点得分

                    // 综合得分 (锐度得分满分50，低噪点得分满分50)
                    double edgeScore = Math::Min(50.0, (contrastSum / contrastCount) * 5.0);
                    double cleanScore = Math::Max(0.0, 50.0 - (noiseScore * 2.0));

                    qualityScore = edgeScore + cleanScore;

                    // 限制在0-100范围内
                    qualityScore = Math::Max(0.0, Math::Min(100.0, qualityScore));

                    delete random;
                    delete bitmap;
                    return true;
                }
                catch (Exception^ ex)
                {
                    Console::WriteLine("图像质量分析失败: " + ex->Message);
                    qualityScore = 0.0;
                    return false;
                }
            }

            System::Collections::Generic::Dictionary<String^, String^>^ ImageProcessor::ExtractMetadata(array<Byte>^ imageData)
            {
                auto metadata = gcnew System::Collections::Generic::Dictionary<String^, String^>();

                try
                {
                    MemoryStream^ stream = gcnew MemoryStream(imageData);
                    Bitmap^ bitmap = gcnew Bitmap(stream);

                    // 提取基本属性
                    metadata->Add("Width", bitmap->Width.ToString());
                    metadata->Add("Height", bitmap->Height.ToString());
                    metadata->Add("PixelFormat", bitmap->PixelFormat.ToString());
                    metadata->Add("Resolution", bitmap->HorizontalResolution.ToString() + " x " + bitmap->VerticalResolution.ToString());
                    metadata->Add("AspectRatio", Math::Round(bitmap->Width / (double)bitmap->Height, 2).ToString());
                    metadata->Add("FileSize", (imageData->Length / 1024.0).ToString("F2") + " KB");
                    metadata->Add("ExtractedDate", DateTime::Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    // 计算平均亮度
                    double totalBrightness = 0.0;
                    for (int y = 0; y < bitmap->Height; y += 10) // 每10像素采样一次
                    {
                        for (int x = 0; x < bitmap->Width; x += 10)
                        {
                            Color pixel = bitmap->GetPixel(x, y);
                            totalBrightness += pixel.GetBrightness();
                        }
                    }
                    int samples = (bitmap->Width / 10 + 1) * (bitmap->Height / 10 + 1);
                    double avgBrightness = totalBrightness / samples;
                    metadata->Add("AverageBrightness", Math::Round(avgBrightness * 100, 2).ToString() + "%");

                    // 记录色彩分布
                    long totalR = 0, totalG = 0, totalB = 0;
                    for (int y = 0; y < bitmap->Height; y += 10)
                    {
                        for (int x = 0; x < bitmap->Width; x += 10)
                        {
                            Color pixel = bitmap->GetPixel(x, y);
                            totalR += pixel.R;
                            totalG += pixel.G;
                            totalB += pixel.B;
                        }
                    }

                    double avgR = totalR / (double)samples;
                    double avgG = totalG / (double)samples;
                    double avgB = totalB / (double)samples;

                    metadata->Add("AverageRed", Math::Round(avgR, 2).ToString());
                    metadata->Add("AverageGreen", Math::Round(avgG, 2).ToString());
                    metadata->Add("AverageBlue", Math::Round(avgB, 2).ToString());

                    // 提取EXIF数据
                    if (bitmap->PropertyIdList->Length > 0)
                    {
                        for each (int propId in bitmap->PropertyIdList)
                        {
                            PropertyItem^ item = bitmap->GetPropertyItem(propId);

                            try
                            {
                                String^ value = "";

                                // 根据不同的属性类型进行处理
                                if (item->Type == 2) // ASCII字符串
                                {
                                    array<Byte>^ bytes = item->Value;
                                    value = System::Text::Encoding::ASCII->GetString(bytes, 0, bytes->Length - 1);
                                }
                                else if (item->Type == 3) // 16位无符号整数
                                {
                                    if (item->Len >= 2)
                                    {
                                        array<Byte>^ bytes = item->Value;
                                        value = BitConverter::ToUInt16(bytes, 0).ToString();
                                    }
                                }

                                // 添加到字典
                                metadata->Add("Exif_" + propId.ToString(), value);
                            }
                            catch (Exception^) {}
                        }
                    }

                    delete stream;
                    delete bitmap;
                }
                catch (Exception^ ex)
                {
                    metadata->Add("Error", "Failed to extract metadata: " + ex->Message);
                }

                return metadata;
            }

            array<Byte>^ ImageProcessor::ResizeImage(array<Byte>^ imageData, int width, int height)
            {
                try
                {
                    Bitmap^ sourceBitmap = BytesToBitmap(imageData);
                    Bitmap^ resizedBitmap = gcnew Bitmap(width, height);

                    Graphics^ g = Graphics::FromImage(resizedBitmap);
                    // 设置高质量缩放选项
                    g->CompositingQuality = System::Drawing::Drawing2D::CompositingQuality::HighQuality;
                    g->SmoothingMode = System::Drawing::Drawing2D::SmoothingMode::HighQuality;
                    g->InterpolationMode = System::Drawing::Drawing2D::InterpolationMode::HighQualityBicubic;
                    g->PixelOffsetMode = System::Drawing::Drawing2D::PixelOffsetMode::HighQuality;

                    // 绘制调整大小后的图像
                    g->DrawImage(sourceBitmap, 0, 0, width, height);

                    delete g;
                    delete sourceBitmap;

                    return BitmapToBytes(resizedBitmap, ImageFormat::Jpeg);
                }
                catch (Exception^ ex)
                {
                    Console::WriteLine("调整图像大小失败: " + ex->Message);
                    return imageData; // 发生错误时返回原始图像
                }
            }

            array<Byte>^ ImageProcessor::CropImage(array<Byte>^ imageData, int x, int y, int width, int height)
            {
                try
                {
                    Bitmap^ sourceBitmap = BytesToBitmap(imageData);

                    // 确保裁剪区域在图像范围内
                    if (x < 0) x = 0;
                    if (y < 0) y = 0;
                    if (x + width > sourceBitmap->Width) width = sourceBitmap->Width - x;
                    if (y + height > sourceBitmap->Height) height = sourceBitmap->Height - y;

                    Rectangle cropRect = Rectangle(x, y, width, height);
                    Bitmap^ croppedBitmap = gcnew Bitmap(width, height);

                    Graphics^ g = Graphics::FromImage(croppedBitmap);
                    g->DrawImage(sourceBitmap, Rectangle(0, 0, width, height), cropRect, GraphicsUnit::Pixel);

                    delete g;
                    delete sourceBitmap;

                    return BitmapToBytes(croppedBitmap, ImageFormat::Jpeg);
                }
                catch (Exception^ ex)
                {
                    Console::WriteLine("裁剪图像失败: " + ex->Message);
                    return imageData; // 发生错误时返回原始图像
                }
            }

            array<Byte>^ ImageProcessor::CreateThumbnail(array<Byte>^ imageData, int width, int height)
            {
                try
                {
                    // 使用ResizeImage方法实现，但对缩略图应用不同的压缩率
                    Bitmap^ sourceBitmap = BytesToBitmap(imageData);

                    // 保持宽高比
                    double ratio = Math::Min((double)width / sourceBitmap->Width, (double)height / sourceBitmap->Height);
                    int newWidth = (int)(sourceBitmap->Width * ratio);
                    int newHeight = (int)(sourceBitmap->Height * ratio);

                    Bitmap^ thumbnail = gcnew Bitmap(newWidth, newHeight);

                    Graphics^ g = Graphics::FromImage(thumbnail);
                    g->CompositingQuality = System::Drawing::Drawing2D::CompositingQuality::HighSpeed;
                    g->SmoothingMode = System::Drawing::Drawing2D::SmoothingMode::HighSpeed;
                    g->InterpolationMode = System::Drawing::Drawing2D::InterpolationMode::Default;
                    g->DrawImage(sourceBitmap, 0, 0, newWidth, newHeight);

                    delete g;
                    delete sourceBitmap;

                    // 为缩略图使用更高的压缩率
                    ImageCodecInfo^ jpegCodec = nullptr;
                    for each (ImageCodecInfo ^ codec in ImageCodecInfo::GetImageDecoders())
                    {
                        if (codec->FormatID == ImageFormat::Jpeg->Guid)
                        {
                            jpegCodec = codec;
                            break;
                        }
                    }

                    EncoderParameters^ encoderParams = gcnew EncoderParameters(1);
                    // 如果要使用long long重载
                    encoderParams->Param[0] = gcnew EncoderParameter(System::Drawing::Imaging::Encoder::Quality, (long long)70);

                    MemoryStream^ ms = gcnew MemoryStream();
                    thumbnail->Save(ms, jpegCodec, encoderParams);

                    delete encoderParams;
                    delete thumbnail;

                    return ms->ToArray();
                }
                catch (Exception^ ex)
                {
                    Console::WriteLine("生成缩略图失败: " + ex->Message);
                    // 生成缩略图失败时，使用普通的调整大小方法
                    return ResizeImage(imageData, width, height);
                }
            }

            // 实用方法
            Bitmap^ ImageProcessor::BytesToBitmap(array<Byte>^ imageBytes)
            {
                MemoryStream^ ms = gcnew MemoryStream(imageBytes);
                Bitmap^ bitmap = gcnew Bitmap(ms);
                return bitmap;
            }

            array<Byte>^ ImageProcessor::BitmapToBytes(Bitmap^ bitmap, ImageFormat^ format)
            {
                MemoryStream^ ms = gcnew MemoryStream();
                bitmap->Save(ms, format);
                return ms->ToArray();
            }
        }
    }
}