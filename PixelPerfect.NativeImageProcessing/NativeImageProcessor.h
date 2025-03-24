#pragma once

// 在DLL内部定义为dllexport，在外部定义为dllimport
#ifdef PIXELPERFECT_NATIVEIMAGEPROCESSING_EXPORTS
#define NATIVEIMAGEAPI __declspec(dllexport)
#else
#define NATIVEIMAGEAPI __declspec(dllimport)
#endif

#ifdef __cplusplus
extern "C" {
#endif

    // 高级图像压缩
    NATIVEIMAGEAPI bool CompressImage(const unsigned char* input, size_t inputSize,
        unsigned char* output, size_t* outputSize, int quality);

    // 简单的人脸检测 (返回检测到的人脸数量)
    NATIVEIMAGEAPI int DetectFaces(const unsigned char* imageData, size_t length);

    // 图像相似度比较 (返回0-100的相似度值)
    NATIVEIMAGEAPI int CompareImages(const unsigned char* imageData1, size_t length1,
        const unsigned char* imageData2, size_t length2);

    // 图像锐化处理
    NATIVEIMAGEAPI bool SharpenImage(const unsigned char* input, size_t inputSize,
        unsigned char* output, size_t* outputSize);

    // 获取最后一个错误信息
    NATIVEIMAGEAPI const char* GetLastErrorMessage();

#ifdef __cplusplus
}
#endif