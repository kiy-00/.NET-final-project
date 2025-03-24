// ImageProcessorTest.h
#pragma once

using namespace System;
using namespace System::IO;

namespace PixelPerfect {
    namespace ImageProcessing {
        namespace CLI {

            public ref class ImageProcessorTest
            {
            public:
                static bool TestAllFunctions(String^ testImagePath);
            };
        }
    }
}