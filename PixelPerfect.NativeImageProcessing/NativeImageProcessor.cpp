#include "pch.h"  // 这行必须添加在文件最顶部
#define PIXELPERFECT_NATIVEIMAGEPROCESSING_EXPORTS
#include "NativeImageProcessor.h"
#include <vector>
#include <algorithm>
#include <cstring>
#include <memory>
#include <string>
#include <stdexcept>  // 这个头文件包含 std::runtime_error

// 添加JPEG压缩所需的库
#include <jpeglib.h>
#include <jerror.h>
// 添加PNG压缩所需的库
#include <png.h>
#include <zlib.h>

// 全局错误消息
static std::string g_lastErrorMessage;

// 设置错误消息并返回false
bool SetError(const char* message) {
    g_lastErrorMessage = message;
    return false;
}

// 帮助函数：检测图像格式
enum ImageFormat {
    FORMAT_UNKNOWN,
    FORMAT_JPEG,
    FORMAT_PNG
};

ImageFormat DetectImageFormat(const unsigned char* data, size_t length) {
    if (length < 8) return FORMAT_UNKNOWN;

    // 检查JPEG签名 (FF D8)
    if (data[0] == 0xFF && data[1] == 0xD8) {
        return FORMAT_JPEG;
    }

    // 检查PNG签名 (89 50 4E 47 0D 0A 1A 0A)
    if (data[0] == 0x89 && data[1] == 'P' && data[2] == 'N' && data[3] == 'G' &&
        data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A) {
        return FORMAT_PNG;
    }

    return FORMAT_UNKNOWN;
}

// JPEG错误处理结构和函数
struct JPEGErrorManager {
    struct jpeg_error_mgr pub;  // "public" fields
    jmp_buf setjmp_buffer;      // for return to caller
    char errMsg[JMSG_LENGTH_MAX];
};

typedef struct JPEGErrorManager* JPEGErrorPtr;

METHODDEF(void) JPEGErrorExit(j_common_ptr cinfo) {
    // cinfo->err really points to a JPEGErrorManager struct
    JPEGErrorPtr myerr = (JPEGErrorPtr)cinfo->err;

    // Create the message
    (*cinfo->err->format_message)(cinfo, myerr->errMsg);

    // Return control to the setjmp point
    longjmp(myerr->setjmp_buffer, 1);
}

// JPEG压缩实现
bool CompressJPEG(const unsigned char* input, size_t inputSize,
    unsigned char* output, size_t* outputSize, int quality) {
    // 为jpeg库准备结构
    struct jpeg_decompress_struct dinfo;
    struct jpeg_compress_struct cinfo;
    struct JPEGErrorManager jerr;

    // 设置自定义错误处理
    dinfo.err = jpeg_std_error(&jerr.pub);
    jerr.pub.error_exit = JPEGErrorExit;

    // 设置返回点，以便在发生错误时可以进行清理
    if (setjmp(jerr.setjmp_buffer)) {
        // 如果我们到达这里，表示发生了错误
        jpeg_destroy_decompress(&dinfo);
        return SetError(jerr.errMsg);
    }

    // 初始化解压对象
    jpeg_create_decompress(&dinfo);

    // 设置输入数据源
    jpeg_mem_src(&dinfo, input, inputSize);

    // 读取JPEG头信息
    if (jpeg_read_header(&dinfo, TRUE) != JPEG_HEADER_OK) {
        jpeg_destroy_decompress(&dinfo);
        return SetError("Invalid JPEG header");
    }

    // 开始解压
    jpeg_start_decompress(&dinfo);

    // 读取图像信息
    int width = dinfo.output_width;
    int height = dinfo.output_height;
    int channels = dinfo.output_components;

    // 为解压后的图像分配内存
    std::vector<unsigned char> buffer(width * height * channels);

    // 按行读取图像
    JSAMPROW row_pointer[1];
    while (dinfo.output_scanline < dinfo.output_height) {
        row_pointer[0] = &buffer[dinfo.output_scanline * width * channels];
        jpeg_read_scanlines(&dinfo, row_pointer, 1);
    }

    // 完成解压
    jpeg_finish_decompress(&dinfo);
    jpeg_destroy_decompress(&dinfo);

    // 设置压缩对象的错误处理
    cinfo.err = jpeg_std_error(&jerr.pub);
    jerr.pub.error_exit = JPEGErrorExit;

    // 设置返回点，以便在发生错误时可以进行清理
    if (setjmp(jerr.setjmp_buffer)) {
        // 如果我们到达这里，表示发生了错误
        jpeg_destroy_compress(&cinfo);
        return SetError(jerr.errMsg);
    }

    // 初始化压缩对象
    jpeg_create_compress(&cinfo);

    // 准备输出内存缓冲区
    unsigned char* jpegBuf = NULL;
    unsigned long jpegSize = 0;

    // 设置输出目标
    jpeg_mem_dest(&cinfo, &jpegBuf, &jpegSize);

    // 设置压缩参数
    cinfo.image_width = width;
    cinfo.image_height = height;
    cinfo.input_components = channels;
    cinfo.in_color_space = channels == 3 ? JCS_RGB : JCS_GRAYSCALE;

    // 初始化默认压缩参数
    jpeg_set_defaults(&cinfo);
    // 设置质量 (0-100)
    jpeg_set_quality(&cinfo, quality, TRUE);

    // 开始压缩
    jpeg_start_compress(&cinfo, TRUE);

    // 按行写入图像
    while (cinfo.next_scanline < cinfo.image_height) {
        row_pointer[0] = &buffer[cinfo.next_scanline * width * channels];
        jpeg_write_scanlines(&cinfo, row_pointer, 1);
    }

    // 完成压缩
    jpeg_finish_compress(&cinfo);

    // 检查输出缓冲区大小
    if (*outputSize < jpegSize) {
        free(jpegBuf);
        jpeg_destroy_compress(&cinfo);
        return SetError("Output buffer too small");
    }

    // 复制压缩后的数据到输出缓冲区
    memcpy(output, jpegBuf, jpegSize);
    *outputSize = jpegSize;

    // 释放资源
    free(jpegBuf);
    jpeg_destroy_compress(&cinfo);

    return true;
}

// PNG读取回调函数
void PNGReadData(png_structp png_ptr, png_bytep data, png_size_t length) {
    // 获取用户指针(包含输入数据的指针和当前位置)
    struct {
        const unsigned char* data;
        size_t size;
        size_t position;
    }*io_ptr = (decltype(io_ptr))png_get_io_ptr(png_ptr);

    // 确保不会读取超过输入数据的大小
    if (io_ptr->position + length > io_ptr->size) {
        png_error(png_ptr, "Read beyond end of data");
        return;
    }

    // 复制数据
    memcpy(data, io_ptr->data + io_ptr->position, length);
    io_ptr->position += length;
}

// PNG写入回调函数
void PNGWriteData(png_structp png_ptr, png_bytep data, png_size_t length) {
    // 获取用户指针(包含输出数据的指针和当前位置)
    struct {
        unsigned char* data;
        size_t size;
        size_t position;
        bool overflow;
    }*io_ptr = (decltype(io_ptr))png_get_io_ptr(png_ptr);

    // 确保不会写入超过输出缓冲区的大小
    if (io_ptr->position + length > io_ptr->size) {
        io_ptr->overflow = true;
        png_error(png_ptr, "Write beyond buffer");
        return;
    }

    // 复制数据
    memcpy(io_ptr->data + io_ptr->position, data, length);
    io_ptr->position += length;
}

// PNG压缩实现
bool CompressPNG(const unsigned char* input, size_t inputSize,
    unsigned char* output, size_t* outputSize, int quality) {
    png_structp png_read_ptr = nullptr;
    png_infop info_ptr = nullptr;
    png_structp png_write_ptr = nullptr;
    png_infop write_info_ptr = nullptr;
    png_bytep* row_pointers = nullptr;

    try {
        // 创建读取结构
        png_read_ptr = png_create_read_struct(PNG_LIBPNG_VER_STRING, nullptr, nullptr, nullptr);
        if (!png_read_ptr) {
            return SetError("Failed to create PNG read struct");
        }

        // 创建信息结构
        info_ptr = png_create_info_struct(png_read_ptr);
        if (!info_ptr) {
            png_destroy_read_struct(&png_read_ptr, nullptr, nullptr);
            return SetError("Failed to create PNG info struct");
        }

        // 设置错误处理
        if (setjmp(png_jmpbuf(png_read_ptr))) {
            png_destroy_read_struct(&png_read_ptr, &info_ptr, nullptr);
            return SetError("Error during PNG read");
        }

        // 设置自定义读取函数
        struct {
            const unsigned char* data;
            size_t size;
            size_t position;
        } read_io = { input, inputSize, 0 };

        png_set_read_fn(png_read_ptr, &read_io, PNGReadData);

        // 读取PNG头
        png_read_info(png_read_ptr, info_ptr);

        // 获取图像信息
        png_uint_32 width, height;
        int bit_depth, color_type, interlace_type;
        png_get_IHDR(png_read_ptr, info_ptr, &width, &height, &bit_depth, &color_type,
            &interlace_type, nullptr, nullptr);

        // 设置转换，使输出为8位RGB/RGBA
        if (bit_depth == 16)
            png_set_strip_16(png_read_ptr);

        if (color_type == PNG_COLOR_TYPE_PALETTE)
            png_set_palette_to_rgb(png_read_ptr);

        if (color_type == PNG_COLOR_TYPE_GRAY && bit_depth < 8)
            png_set_expand_gray_1_2_4_to_8(png_read_ptr);

        if (png_get_valid(png_read_ptr, info_ptr, PNG_INFO_tRNS))
            png_set_tRNS_to_alpha(png_read_ptr);

        // 这些转换确保我们得到RGB或RGBA
        if (color_type == PNG_COLOR_TYPE_GRAY || color_type == PNG_COLOR_TYPE_GRAY_ALPHA)
            png_set_gray_to_rgb(png_read_ptr);

        // 更新信息
        png_read_update_info(png_read_ptr, info_ptr);

        // 获取更新后的信息
        png_get_IHDR(png_read_ptr, info_ptr, &width, &height, &bit_depth, &color_type,
            &interlace_type, nullptr, nullptr);

        // 计算行大小
        size_t rowbytes = png_get_rowbytes(png_read_ptr, info_ptr);

        // 为图像数据分配内存
        row_pointers = new png_bytep[height];
        for (png_uint_32 i = 0; i < height; i++) {
            row_pointers[i] = new png_byte[rowbytes];
        }

        // 读取图像数据
        png_read_image(png_read_ptr, row_pointers);

        // 创建写入结构
        png_write_ptr = png_create_write_struct(PNG_LIBPNG_VER_STRING, nullptr, nullptr, nullptr);
        if (!png_write_ptr) {
            throw std::runtime_error("Failed to create PNG write struct");
        }

        // 创建写入信息结构
        write_info_ptr = png_create_info_struct(png_write_ptr);
        if (!write_info_ptr) {
            throw std::runtime_error("Failed to create PNG write info struct");
        }

        // 设置错误处理
        if (setjmp(png_jmpbuf(png_write_ptr))) {
            throw std::runtime_error("Error during PNG write");
        }

        // 设置自定义写入函数
        struct {
            unsigned char* data;
            size_t size;
            size_t position;
            bool overflow;
        } write_io = { output, *outputSize, 0, false };

        png_set_write_fn(png_write_ptr, &write_io, PNGWriteData, nullptr);

        // 设置压缩级别 (0-9)，将quality 0-100映射到0-9
        int compressionLevel = quality / 10;
        if (compressionLevel > 9) compressionLevel = 9;

        png_set_compression_level(png_write_ptr, compressionLevel);

        // 写入头信息
        png_set_IHDR(png_write_ptr, write_info_ptr, width, height,
            bit_depth, color_type, interlace_type,
            PNG_COMPRESSION_TYPE_DEFAULT, PNG_FILTER_TYPE_DEFAULT);

        png_write_info(png_write_ptr, write_info_ptr);

        // 写入图像数据
        png_write_image(png_write_ptr, row_pointers);

        // 完成写入
        png_write_end(png_write_ptr, nullptr);

        // 检查是否发生溢出
        if (write_io.overflow) {
            throw std::runtime_error("Output buffer too small");
        }

        // 更新实际写入的大小
        *outputSize = write_io.position;

        // 清理
        for (png_uint_32 i = 0; i < height; i++) {
            delete[] row_pointers[i];
        }
        delete[] row_pointers;
        row_pointers = nullptr;

        png_destroy_write_struct(&png_write_ptr, &write_info_ptr);
        png_destroy_read_struct(&png_read_ptr, &info_ptr, nullptr);

        return true;
    }
    catch (const std::exception& e) {
        // 清理
        if (row_pointers) {
            for (png_uint_32 i = 0; i < png_get_image_height(png_read_ptr, info_ptr); i++) {
                delete[] row_pointers[i];
            }
            delete[] row_pointers;
        }

        if (png_write_ptr) {
            png_destroy_write_struct(&png_write_ptr, write_info_ptr ? &write_info_ptr : nullptr);
        }

        if (png_read_ptr) {
            png_destroy_read_struct(&png_read_ptr, info_ptr ? &info_ptr : nullptr, nullptr);
        }

        return SetError(e.what());
    }
}

extern "C" {
    NATIVEIMAGEAPI bool CompressImage(const unsigned char* input, size_t inputSize,
        unsigned char* output, size_t* outputSize, int quality) {
        if (!input || inputSize == 0 || !output || !outputSize) {
            return SetError("Invalid parameters");
        }

        try {
            // 检测图像格式
            ImageFormat format = DetectImageFormat(input, inputSize);
            if (format == FORMAT_UNKNOWN) {
                return SetError("Unsupported image format");
            }

            // 为防止加载问题，先复制原始数据到输出
            if (*outputSize >= inputSize) {
                memcpy(output, input, inputSize);
                *outputSize = inputSize;
            }

            // 确保质量值在有效范围内
            if (quality < 0) quality = 0;
            if (quality > 100) quality = 100;

            // 根据图像格式选择适当的压缩方法
            switch (format) {
            case FORMAT_JPEG:
            {
                // 为jpeg库准备结构
                struct jpeg_decompress_struct dinfo;
                struct jpeg_compress_struct cinfo;
                struct JPEGErrorManager jerr;

                // 设置自定义错误处理
                dinfo.err = jpeg_std_error(&jerr.pub);
                jerr.pub.error_exit = JPEGErrorExit;

                // 设置返回点，以便在发生错误时可以进行清理
                if (setjmp(jerr.setjmp_buffer)) {
                    // 如果我们到达这里，表示发生了错误
                    jpeg_destroy_decompress(&dinfo);
                    return SetError(jerr.errMsg);
                }

                // 初始化解压对象
                jpeg_create_decompress(&dinfo);

                // 设置输入数据源
                jpeg_mem_src(&dinfo, input, inputSize);

                // 读取JPEG头信息
                if (jpeg_read_header(&dinfo, TRUE) != JPEG_HEADER_OK) {
                    jpeg_destroy_decompress(&dinfo);
                    return SetError("Invalid JPEG header");
                }

                // 开始解压
                jpeg_start_decompress(&dinfo);

                // 读取图像信息
                int width = dinfo.output_width;
                int height = dinfo.output_height;
                int channels = dinfo.output_components;

                // 为解压后的图像分配内存
                std::vector<unsigned char> buffer(width * height * channels);

                // 按行读取图像
                JSAMPROW row_pointer[1];
                while (dinfo.output_scanline < dinfo.output_height) {
                    row_pointer[0] = &buffer[dinfo.output_scanline * width * channels];
                    jpeg_read_scanlines(&dinfo, row_pointer, 1);
                }

                // 完成解压
                jpeg_finish_decompress(&dinfo);
                jpeg_destroy_decompress(&dinfo);

                // 设置压缩对象的错误处理
                cinfo.err = jpeg_std_error(&jerr.pub);
                jerr.pub.error_exit = JPEGErrorExit;

                // 设置返回点，以便在发生错误时可以进行清理
                if (setjmp(jerr.setjmp_buffer)) {
                    // 如果我们到达这里，表示发生了错误
                    jpeg_destroy_compress(&cinfo);
                    return SetError(jerr.errMsg);
                }

                // 初始化压缩对象
                jpeg_create_compress(&cinfo);

                // 准备输出内存缓冲区
                unsigned char* jpegBuf = NULL;
                unsigned long jpegSize = 0;

                // 设置输出目标
                jpeg_mem_dest(&cinfo, &jpegBuf, &jpegSize);

                // 设置压缩参数
                cinfo.image_width = width;
                cinfo.image_height = height;
                cinfo.input_components = channels;
                cinfo.in_color_space = channels == 3 ? JCS_RGB : JCS_GRAYSCALE;

                // 初始化默认压缩参数
                jpeg_set_defaults(&cinfo);
                // 设置质量 (0-100)
                jpeg_set_quality(&cinfo, quality, TRUE);

                // 开始压缩
                jpeg_start_compress(&cinfo, TRUE);

                // 按行写入图像
                while (cinfo.next_scanline < cinfo.image_height) {
                    row_pointer[0] = &buffer[cinfo.next_scanline * width * channels];
                    jpeg_write_scanlines(&cinfo, row_pointer, 1);
                }

                // 完成压缩
                jpeg_finish_compress(&cinfo);

                // 检查输出缓冲区大小
                if (*outputSize < jpegSize) {
                    free(jpegBuf);
                    jpeg_destroy_compress(&cinfo);
                    return SetError("Output buffer too small");
                }

                // 复制压缩后的数据到输出缓冲区
                memcpy(output, jpegBuf, jpegSize);
                *outputSize = jpegSize;

                // 释放资源
                free(jpegBuf);
                jpeg_destroy_compress(&cinfo);

                return true;
            }
            case FORMAT_PNG:
            {
                png_structp png_read_ptr = nullptr;
                png_infop info_ptr = nullptr;
                png_structp png_write_ptr = nullptr;
                png_infop write_info_ptr = nullptr;
                png_bytep* row_pointers = nullptr;

                try {
                    // 创建读取结构
                    png_read_ptr = png_create_read_struct(PNG_LIBPNG_VER_STRING, nullptr, nullptr, nullptr);
                    if (!png_read_ptr) {
                        return SetError("Failed to create PNG read struct");
                    }

                    // 创建信息结构
                    info_ptr = png_create_info_struct(png_read_ptr);
                    if (!info_ptr) {
                        png_destroy_read_struct(&png_read_ptr, nullptr, nullptr);
                        return SetError("Failed to create PNG info struct");
                    }

                    // 设置错误处理
                    if (setjmp(png_jmpbuf(png_read_ptr))) {
                        png_destroy_read_struct(&png_read_ptr, &info_ptr, nullptr);
                        return SetError("Error during PNG read");
                    }

                    // 设置自定义读取函数
                    struct {
                        const unsigned char* data;
                        size_t size;
                        size_t position;
                    } read_io = { input, inputSize, 0 };

                    png_set_read_fn(png_read_ptr, &read_io, PNGReadData);

                    // 读取PNG头
                    png_read_info(png_read_ptr, info_ptr);

                    // 获取图像信息
                    png_uint_32 width, height;
                    int bit_depth, color_type, interlace_type;
                    png_get_IHDR(png_read_ptr, info_ptr, &width, &height, &bit_depth, &color_type,
                        &interlace_type, nullptr, nullptr);

                    // 存储原始信息以便写回
                    png_uint_32 orig_width = width;
                    png_uint_32 orig_height = height;
                    int orig_bit_depth = bit_depth;
                    int orig_color_type = color_type;
                    int orig_interlace_type = interlace_type;

                    // 设置转换，使输出为8位RGB/RGBA
                    if (bit_depth == 16)
                        png_set_strip_16(png_read_ptr);

                    if (color_type == PNG_COLOR_TYPE_PALETTE)
                        png_set_palette_to_rgb(png_read_ptr);

                    if (color_type == PNG_COLOR_TYPE_GRAY && bit_depth < 8)
                        png_set_expand_gray_1_2_4_to_8(png_read_ptr);

                    if (png_get_valid(png_read_ptr, info_ptr, PNG_INFO_tRNS))
                        png_set_tRNS_to_alpha(png_read_ptr);

                    // 这些转换确保我们得到RGB或RGBA
                    if (color_type == PNG_COLOR_TYPE_GRAY || color_type == PNG_COLOR_TYPE_GRAY_ALPHA)
                        png_set_gray_to_rgb(png_read_ptr);

                    // 更新信息
                    png_read_update_info(png_read_ptr, info_ptr);

                    // 计算行大小
                    size_t rowbytes = png_get_rowbytes(png_read_ptr, info_ptr);

                    // 为图像数据分配内存
                    std::vector<unsigned char> imageData(height * rowbytes);
                    row_pointers = new png_bytep[height];
                    for (png_uint_32 i = 0; i < height; i++) {
                        row_pointers[i] = &imageData[i * rowbytes];
                    }

                    // 读取图像数据
                    png_read_image(png_read_ptr, row_pointers);
                    png_read_end(png_read_ptr, NULL);

                    // 创建写入结构
                    png_write_ptr = png_create_write_struct(PNG_LIBPNG_VER_STRING, nullptr, nullptr, nullptr);
                    if (!png_write_ptr) {
                        throw std::runtime_error("Failed to create PNG write struct");
                    }

                    // 创建写入信息结构
                    write_info_ptr = png_create_info_struct(png_write_ptr);
                    if (!write_info_ptr) {
                        throw std::runtime_error("Failed to create PNG write info struct");
                    }

                    // 设置错误处理
                    if (setjmp(png_jmpbuf(png_write_ptr))) {
                        throw std::runtime_error("Error during PNG write");
                    }

                    // 设置自定义写入函数
                    struct {
                        unsigned char* data;
                        size_t size;
                        size_t position;
                        bool overflow;
                    } write_io = { output, *outputSize, 0, false };

                    png_set_write_fn(png_write_ptr, &write_io, PNGWriteData, nullptr);

                    // 设置压缩级别 (0-9)，将quality 0-100映射到0-9
                    int compressionLevel = 9 - (quality * 9 / 100);  // 反向映射: 高质量=低压缩
                    png_set_compression_level(png_write_ptr, compressionLevel);

                    // 写入头信息 - 使用原始图像的属性
                    png_set_IHDR(png_write_ptr, write_info_ptr, orig_width, orig_height,
                        orig_bit_depth, orig_color_type, orig_interlace_type,
                        PNG_COMPRESSION_TYPE_DEFAULT, PNG_FILTER_TYPE_DEFAULT);

                    // 复制所有其他块，除了IDAT和已经写入的IHDR
                    png_set_keep_unknown_chunks(png_write_ptr, PNG_HANDLE_CHUNK_ALWAYS, NULL, 0);

                    // 写入信息头
                    png_write_info(png_write_ptr, write_info_ptr);

                    // 如果需要，设置位深度转换
                    if (orig_bit_depth == 16 && bit_depth == 8)
                        png_set_packing(png_write_ptr);

                    // 写入图像数据
                    png_write_image(png_write_ptr, row_pointers);

                    // 写入结束块
                    png_write_end(png_write_ptr, nullptr);

                    // 检查是否发生溢出
                    if (write_io.overflow) {
                        throw std::runtime_error("Output buffer too small");
                    }

                    // 更新实际写入的大小
                    *outputSize = write_io.position;

                    // 清理
                    for (png_uint_32 i = 0; i < height; i++) {
                        row_pointers[i] = nullptr;  // 避免重复删除，因为指向的是imageData内存
                    }
                    delete[] row_pointers;
                    row_pointers = nullptr;

                    png_destroy_write_struct(&png_write_ptr, &write_info_ptr);
                    png_destroy_read_struct(&png_read_ptr, &info_ptr, nullptr);

                    return true;
                }
                catch (const std::exception& e) {
                    // 清理
                    if (row_pointers) {
                        delete[] row_pointers;
                    }

                    if (png_write_ptr) {
                        png_destroy_write_struct(&png_write_ptr, write_info_ptr ? &write_info_ptr : nullptr);
                    }

                    if (png_read_ptr) {
                        png_destroy_read_struct(&png_read_ptr, info_ptr ? &info_ptr : nullptr, nullptr);
                    }

                    return SetError(e.what());
                }
            }
            default:
                return SetError("Unsupported image format");
            }
        }
        catch (const std::exception& e) {
            return SetError(e.what());
        }
    }

    NATIVEIMAGEAPI int DetectFaces(const unsigned char* imageData, size_t length) {
        if (!imageData || length == 0) {
            SetError("Invalid parameters");
            return 0;
        }

        try {
            // 简单的模拟实现，实际应用中应该使用专业的人脸检测库(如OpenCV)
            // 检查图像数据中的特定模式以模拟人脸检测
            // 注意：这不是真正的人脸检测，仅用于演示DLL功能

            int faceCount = 0;
            std::vector<unsigned char> data(imageData, imageData + length);

            // 检查JPEG图像标记
            bool isJpeg = (length > 2 && data[0] == 0xFF && data[1] == 0xD8);
            bool isPng = (length > 8 && data[0] == 0x89 && data[1] == 'P' && data[2] == 'N' && data[3] == 'G');

            if (!isJpeg && !isPng) {
                SetError("Unsupported image format");
                return 0;
            }

            // 基于图像大小和复杂度模拟人脸检测结果
            if (length > 50000) {
                // 大图像假设包含更多的人脸
                faceCount = 2 + (length / 1000000); // 基于图像大小估计人脸数量
            }
            else {
                // 小图像可能只有一个人脸
                faceCount = 1;
            }

            // 限制在合理范围内
            return (std::min)(faceCount, 10);
        }
        catch (const std::exception& e) {
            SetError(e.what());
            return 0;
        }
    }

    NATIVEIMAGEAPI int CompareImages(const unsigned char* imageData1, size_t length1,
        const unsigned char* imageData2, size_t length2) {
        if (!imageData1 || !imageData2 || length1 == 0 || length2 == 0) {
            SetError("Invalid parameters");
            return 0;
        }

        try {
            // 简单的图像相似度比较实现
            // 实际应用中应使用更高级的图像处理技术如直方图比较或特征匹配

            // 首先比较图像大小
            if (std::abs((int)(length1 - length2)) > (int)(0.5 * (std::max)(length1, length2))) {
                // 如果大小差异过大，认为图像差异较大
                return 30; // 30%相似度
            }

            // 比较数据样本
            const size_t sampleSize = 1000;
            const size_t maxSamples = (std::min)(length1, length2) / 10;
            const size_t samples = (std::min)(sampleSize, maxSamples);

            if (samples == 0) {
                return 50; // 默认中等相似度
            }

            int matchCount = 0;
            for (size_t i = 0; i < samples; i++) {
                size_t pos = (i * (std::min)(length1, length2)) / samples;
                if (pos < length1 && pos < length2 && imageData1[pos] == imageData2[pos]) {
                    matchCount++;
                }
            }

            // 计算相似度百分比(0-100)
            int similarity = (matchCount * 100) / samples;

            return similarity;
        }
        catch (const std::exception& e) {
            SetError(e.what());
            return 0;
        }
    }

    NATIVEIMAGEAPI bool SharpenImage(const unsigned char* input, size_t inputSize,
        unsigned char* output, size_t* outputSize) {
        if (!input || inputSize == 0 || !output || !outputSize || *outputSize < inputSize) {
            return SetError("Invalid parameters");
        }

        try {
            // 检测图像格式
            ImageFormat format = DetectImageFormat(input, inputSize);
            if (format == FORMAT_UNKNOWN) {
                return SetError("Unsupported image format");
            }

            // 优先尝试保留原始图像格式的头部信息
            // 对于JPEG和PNG这类图像，保留文件头对于确保图像可以正确加载非常重要
            // 简单地复制原始数据到输出缓冲区
            memcpy(output, input, inputSize);
            *outputSize = inputSize;

            // 如果是JPEG格式，使用JPEG库处理
            if (format == FORMAT_JPEG) {
                // 为jpeg库准备结构
                struct jpeg_decompress_struct dinfo;
                struct JPEGErrorManager jerr;

                // 设置自定义错误处理
                dinfo.err = jpeg_std_error(&jerr.pub);
                jerr.pub.error_exit = JPEGErrorExit;

                // 设置返回点，以便在发生错误时可以进行清理
                if (setjmp(jerr.setjmp_buffer)) {
                    // 如果我们到达这里，表示发生了错误
                    jpeg_destroy_decompress(&dinfo);
                    return SetError(jerr.errMsg);
                }

                // 初始化解压对象
                jpeg_create_decompress(&dinfo);

                // 设置输入数据源
                jpeg_mem_src(&dinfo, input, inputSize);

                // 读取JPEG头信息
                if (jpeg_read_header(&dinfo, TRUE) != JPEG_HEADER_OK) {
                    jpeg_destroy_decompress(&dinfo);
                    return SetError("Invalid JPEG header");
                }

                // 开始解压
                jpeg_start_decompress(&dinfo);

                // 读取图像信息
                int width = dinfo.output_width;
                int height = dinfo.output_height;
                int channels = dinfo.output_components;

                // 为解压后的图像分配内存
                std::vector<unsigned char> imageData(width * height * channels);

                // 按行读取图像
                JSAMPROW row_pointer[1];
                while (dinfo.output_scanline < dinfo.output_height) {
                    row_pointer[0] = &imageData[dinfo.output_scanline * width * channels];
                    jpeg_read_scanlines(&dinfo, row_pointer, 1);
                }

                // 完成解压
                jpeg_finish_decompress(&dinfo);
                jpeg_destroy_decompress(&dinfo);

                // 创建一个副本用于存储锐化结果
                std::vector<unsigned char> sharpened = imageData;  // 先复制一份，以防万一

                // 锐化内核 (3x3锐化卷积核)
                const float sharpenKernel[3][3] = {
                    { 0, -1,  0},
                    {-1,  5, -1},
                    { 0, -1,  0}
                };

                // 应用锐化滤波器 (不包括边缘像素，以避免边界问题)
                for (int y = 1; y < height - 1; y++) {
                    for (int x = 1; x < width - 1; x++) {
                        for (int c = 0; c < channels; c++) {
                            float sum = 0.0f;

                            // 应用卷积核
                            for (int ky = -1; ky <= 1; ky++) {
                                for (int kx = -1; kx <= 1; kx++) {
                                    int ny = y + ky;
                                    int nx = x + kx;

                                    // 获取源像素值
                                    int pixel_index = (ny * width + nx) * channels + c;
                                    float pixel_value = static_cast<float>(imageData[pixel_index]);

                                    // 应用卷积
                                    sum += pixel_value * sharpenKernel[ky + 1][kx + 1];
                                }
                            }

                            // 限制结果在0-255范围内
                            int result = static_cast<int>(sum + 0.5f);
                            result = (result < 0) ? 0 : ((result > 255) ? 255 : result);

                            // 存储结果
                            sharpened[(y * width + x) * channels + c] = static_cast<unsigned char>(result);
                        }
                    }
                }

                // 使用JPEG压缩锐化后的图像
                struct jpeg_compress_struct cinfo;

                // 设置压缩对象的错误处理
                cinfo.err = jpeg_std_error(&jerr.pub);
                jerr.pub.error_exit = JPEGErrorExit;

                // 设置返回点，以便在发生错误时可以进行清理
                if (setjmp(jerr.setjmp_buffer)) {
                    // 如果我们到达这里，表示发生了错误
                    jpeg_destroy_compress(&cinfo);
                    return SetError(jerr.errMsg);
                }

                // 初始化压缩对象
                jpeg_create_compress(&cinfo);

                // 准备输出内存缓冲区
                unsigned char* jpegBuf = NULL;
                unsigned long jpegSize = 0;

                // 设置输出目标
                jpeg_mem_dest(&cinfo, &jpegBuf, &jpegSize);

                // 设置压缩参数
                cinfo.image_width = width;
                cinfo.image_height = height;
                cinfo.input_components = channels;
                cinfo.in_color_space = channels == 3 ? JCS_RGB : JCS_GRAYSCALE;

                // 初始化默认压缩参数
                jpeg_set_defaults(&cinfo);
                // 设置质量 (保持高质量98)
                jpeg_set_quality(&cinfo, 98, TRUE);

                // 开始压缩
                jpeg_start_compress(&cinfo, TRUE);

                // 按行写入图像
                while (cinfo.next_scanline < cinfo.image_height) {
                    row_pointer[0] = &sharpened[cinfo.next_scanline * width * channels];
                    jpeg_write_scanlines(&cinfo, row_pointer, 1);
                }

                // 完成压缩
                jpeg_finish_compress(&cinfo);

                // 检查输出缓冲区大小
                if (*outputSize < jpegSize) {
                    free(jpegBuf);
                    jpeg_destroy_compress(&cinfo);
                    return SetError("Output buffer too small");
                }

                // 复制压缩后的数据到输出缓冲区
                memcpy(output, jpegBuf, jpegSize);
                *outputSize = jpegSize;

                // 释放资源
                free(jpegBuf);
                jpeg_destroy_compress(&cinfo);
            }
            // 如果是PNG格式，使用libpng处理
            else if (format == FORMAT_PNG) {
                png_structp png_ptr = nullptr;
                png_infop info_ptr = nullptr;
                png_bytep* row_pointers = nullptr;

                try {
                    // 创建读取结构
                    png_ptr = png_create_read_struct(PNG_LIBPNG_VER_STRING, nullptr, nullptr, nullptr);
                    if (!png_ptr) {
                        return SetError("Failed to create PNG read struct");
                    }

                    // 创建信息结构
                    info_ptr = png_create_info_struct(png_ptr);
                    if (!info_ptr) {
                        png_destroy_read_struct(&png_ptr, nullptr, nullptr);
                        return SetError("Failed to create PNG info struct");
                    }

                    // 设置错误处理
                    if (setjmp(png_jmpbuf(png_ptr))) {
                        png_destroy_read_struct(&png_ptr, &info_ptr, nullptr);
                        return SetError("Error during PNG read");
                    }

                    // 设置自定义读取函数
                    struct {
                        const unsigned char* data;
                        size_t size;
                        size_t position;
                    } read_io = { input, inputSize, 0 };

                    png_set_read_fn(png_ptr, &read_io, PNGReadData);

                    // 读取PNG头
                    png_read_info(png_ptr, info_ptr);

                    // 获取图像信息
                    png_uint_32 width, height;
                    int bit_depth, color_type, interlace_type;
                    png_get_IHDR(png_ptr, info_ptr, &width, &height, &bit_depth, &color_type,
                        &interlace_type, nullptr, nullptr);

                    // 设置转换，使输出为8位RGB/RGBA
                    if (bit_depth == 16)
                        png_set_strip_16(png_ptr);

                    if (color_type == PNG_COLOR_TYPE_PALETTE)
                        png_set_palette_to_rgb(png_ptr);

                    if (color_type == PNG_COLOR_TYPE_GRAY && bit_depth < 8)
                        png_set_expand_gray_1_2_4_to_8(png_ptr);

                    if (png_get_valid(png_ptr, info_ptr, PNG_INFO_tRNS))
                        png_set_tRNS_to_alpha(png_ptr);

                    // 更新信息以获取正确的值
                    png_read_update_info(png_ptr, info_ptr);

                    // 重新获取信息以获取更新后的值
                    png_get_IHDR(png_ptr, info_ptr, &width, &height, &bit_depth, &color_type,
                        &interlace_type, nullptr, nullptr);

                    // 计算通道数
                    int channels;
                    if (color_type == PNG_COLOR_TYPE_RGB)
                        channels = 3;
                    else if (color_type == PNG_COLOR_TYPE_RGBA)
                        channels = 4;
                    else if (color_type == PNG_COLOR_TYPE_GRAY)
                        channels = 1;
                    else if (color_type == PNG_COLOR_TYPE_GRAY_ALPHA)
                        channels = 2;
                    else
                        channels = 3;  // 默认为RGB

                    // 计算行大小
                    size_t rowbytes = png_get_rowbytes(png_ptr, info_ptr);

                    // 为图像数据分配内存
                    std::vector<unsigned char> imageData(height * rowbytes);

                    // 创建行指针数组
                    row_pointers = new png_bytep[height];
                    for (png_uint_32 i = 0; i < height; i++) {
                        row_pointers[i] = &imageData[i * rowbytes];
                    }

                    // 读取图像数据
                    png_read_image(png_ptr, row_pointers);

                    // 创建一个副本用于存储锐化结果
                    std::vector<unsigned char> sharpened = imageData;

                    // 锐化内核 (3x3锐化卷积核)
                    const float sharpenKernel[3][3] = {
                        { 0, -1,  0},
                        {-1,  5, -1},
                        { 0, -1,  0}
                    };

                    // 应用锐化滤波器 (不包括边缘像素)
                    for (png_uint_32 y = 1; y < height - 1; y++) {
                        for (png_uint_32 x = 1; x < width - 1; x++) {
                            // 计算每行的字节数
                            size_t bytesPerPixel = rowbytes / width;

                            // 只处理RGB/灰度通道，保留Alpha通道不变
                            for (size_t c = 0; c < bytesPerPixel && c < 3; c++) {  // 限制为最多3个通道(RGB)
                                float sum = 0.0f;

                                // 应用卷积核
                                for (int ky = -1; ky <= 1; ky++) {
                                    for (int kx = -1; kx <= 1; kx++) {
                                        png_uint_32 ny = y + ky;
                                        png_uint_32 nx = x + kx;

                                        // 获取源像素值
                                        size_t pixel_index = ny * rowbytes + nx * bytesPerPixel + c;
                                        float pixel_value = static_cast<float>(imageData[pixel_index]);

                                        // 应用卷积
                                        sum += pixel_value * sharpenKernel[ky + 1][kx + 1];
                                    }
                                }

                                // 限制结果在0-255范围内
                                int result = static_cast<int>(sum + 0.5f);
                                result = (result < 0) ? 0 : ((result > 255) ? 255 : result);

                                // 存储结果
                                sharpened[y * rowbytes + x * bytesPerPixel + c] = static_cast<unsigned char>(result);
                            }
                        }
                    }

                    // 清理行指针和PNG结构
                    delete[] row_pointers;
                    row_pointers = nullptr;
                    png_destroy_read_struct(&png_ptr, &info_ptr, nullptr);

                    // 创建新的PNG写入结构
                    png_ptr = png_create_write_struct(PNG_LIBPNG_VER_STRING, nullptr, nullptr, nullptr);
                    if (!png_ptr) {
                        return SetError("Failed to create PNG write struct");
                    }

                    info_ptr = png_create_info_struct(png_ptr);
                    if (!info_ptr) {
                        png_destroy_write_struct(&png_ptr, nullptr);
                        return SetError("Failed to create PNG write info struct");
                    }

                    // 设置错误处理
                    if (setjmp(png_jmpbuf(png_ptr))) {
                        png_destroy_write_struct(&png_ptr, &info_ptr);
                        return SetError("Error during PNG write");
                    }

                    // 设置自定义写入函数
                    struct {
                        unsigned char* data;
                        size_t size;
                        size_t position;
                        bool overflow;
                    } write_io = { output, *outputSize, 0, false };

                    png_set_write_fn(png_ptr, &write_io, PNGWriteData, nullptr);

                    // 写入PNG头
                    png_set_IHDR(png_ptr, info_ptr, width, height, bit_depth, color_type,
                        interlace_type, PNG_COMPRESSION_TYPE_DEFAULT, PNG_FILTER_TYPE_DEFAULT);

                    // 如果原始PNG有PLTE、tRNS或其他关键块，需要复制这些信息
                    // 但这需要更复杂的代码，超出了此示例范围

                    // 写入信息块
                    png_write_info(png_ptr, info_ptr);

                    // 创建行指针数组指向锐化后的数据
                    row_pointers = new png_bytep[height];
                    for (png_uint_32 i = 0; i < height; i++) {
                        row_pointers[i] = &sharpened[i * rowbytes];
                    }

                    // 写入图像数据
                    png_write_image(png_ptr, row_pointers);

                    // 完成写入
                    png_write_end(png_ptr, nullptr);

                    // 清理
                    delete[] row_pointers;
                    png_destroy_write_struct(&png_ptr, &info_ptr);

                    // 检查是否溢出
                    if (write_io.overflow) {
                        return SetError("Output buffer too small");
                    }

                    // 更新实际写入的大小
                    *outputSize = write_io.position;
                }
                catch (const std::exception& e) {
                    // 清理
                    if (row_pointers) {
                        delete[] row_pointers;
                    }

                    if (png_ptr) {
                        png_destroy_write_struct(&png_ptr, info_ptr ? &info_ptr : nullptr);
                    }

                    return SetError(e.what());
                }
            }

            return true;
        }
        catch (const std::exception& e) {
            return SetError(e.what());
        }
    }

    NATIVEIMAGEAPI const char* GetLastErrorMessage() {
        return g_lastErrorMessage.c_str();
    }
}