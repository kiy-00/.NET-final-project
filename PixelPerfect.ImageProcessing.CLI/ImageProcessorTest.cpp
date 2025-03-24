// ImageProcessorTest.cpp
#include "pch.h"
#include "ImageProcessorTest.h"
#include "ImageProcessor.h"

namespace PixelPerfect {
    namespace ImageProcessing {
        namespace CLI {
            bool ImageProcessorTest::TestAllFunctions(String^ testImagePath)
            {
                try
                {
                    ImageProcessor^ processor = gcnew ImageProcessor();
                    // 加载测试图像
                    if (!File::Exists(testImagePath))
                    {
                        Console::WriteLine("测试图像不存在: " + testImagePath);
                        return false;
                    }
                    array<Byte>^ imageData = File::ReadAllBytes(testImagePath);
                    String^ outputDir = Path::GetDirectoryName(testImagePath);
                    // 测试滤镜
                    array<Byte>^ grayImage = processor->ApplyFilter(imageData, "grayscale");
                    File::WriteAllBytes(Path::Combine(outputDir, "test_gray.jpg"), grayImage);
                    array<Byte>^ sepiaImage = processor->ApplyFilter(imageData, "sepia");
                    File::WriteAllBytes(Path::Combine(outputDir, "test_sepia.jpg"), sepiaImage);
                    array<Byte>^ negativeImage = processor->ApplyFilter(imageData, "negative");
                    File::WriteAllBytes(Path::Combine(outputDir, "test_negative.jpg"), negativeImage);
                    // 测试水印
                    array<Byte>^ watermarkedImage = processor->AddWatermark(imageData, "PixelPerfect", 0.7f, 1);
                    File::WriteAllBytes(Path::Combine(outputDir, "test_watermark.jpg"), watermarkedImage);
                    // 测试质量分析
                    double quality = 0.0;
                    processor->AnalyzeImageQuality(imageData, quality);
                    Console::WriteLine("图像质量: " + quality);
                    // 测试元数据
                    auto metadata = processor->ExtractMetadata(imageData);
                    for each (auto pair in metadata)
                    {
                        Console::WriteLine(pair.Key + ": " + pair.Value);
                    }
                    // 测试调整大小
                    array<Byte>^ resizedImage = processor->ResizeImage(imageData, 640, 480);
                    File::WriteAllBytes(Path::Combine(outputDir, "test_resized.jpg"), resizedImage);
                    // 测试裁剪
                    int cropX = 50, cropY = 50, cropWidth = 300, cropHeight = 300;
                    array<Byte>^ croppedImage = processor->CropImage(imageData, cropX, cropY, cropWidth, cropHeight);
                    File::WriteAllBytes(Path::Combine(outputDir, "test_cropped.jpg"), croppedImage);
                    // 测试缩略图
                    array<Byte>^ thumbnailImage = processor->CreateThumbnail(imageData, 150, 150);
                    File::WriteAllBytes(Path::Combine(outputDir, "test_thumbnail.jpg"), thumbnailImage);
                    Console::WriteLine("测试完成，所有功能正常。");
                    return true;
                }
                catch (Exception^ ex)
                {
                    Console::WriteLine("测试过程中发生错误: " + ex->Message);
                    return false;
                }
            }
        }
    }
}

// 添加main函数作为程序入口点
int main(array<System::String^>^ args)
{
    try
    {
        // 设置控制台编码以正确显示中文
        System::Console::OutputEncoding = System::Text::Encoding::UTF8;

        System::String^ testImagePath;

        // 检查是否提供了命令行参数作为测试图像路径
        if (args->Length > 0)
        {
            testImagePath = args[0];
        }
        else
        {
            // 如果没有提供命令行参数，提示用户输入图像路径
            System::Console::WriteLine("请输入测试图像的完整路径:");
            testImagePath = System::Console::ReadLine();
        }

        // 创建测试类实例并运行测试
        PixelPerfect::ImageProcessing::CLI::ImageProcessorTest^ test =
            gcnew PixelPerfect::ImageProcessing::CLI::ImageProcessorTest();

        bool result = test->TestAllFunctions(testImagePath);

        if (result)
        {
            System::Console::WriteLine("所有测试通过!");
        }
        else
        {
            System::Console::WriteLine("测试失败!");
        }

        // 等待用户按键后退出
        System::Console::WriteLine("按任意键退出...");
        System::Console::ReadKey();

        return result ? 0 : 1;
    }
    catch (System::Exception^ ex)
    {
        System::Console::WriteLine("发生未处理的异常: " + ex->Message);
        System::Console::WriteLine("按任意键退出...");
        System::Console::ReadKey();
        return -1;
    }
}