﻿using System;
using Kontract;
using Kontract.Kanvas;
using Kontract.Kanvas.Configuration;
using Kontract.Kanvas.Quantization;

namespace Kanvas.Configuration
{
    // TODO: PixelShader
    public class ImageConfiguration : IColorConfiguration, IIndexConfiguration
    {
        private int _taskCount = Environment.ProcessorCount;

        private CreateColorEncoding _colorFunc;

        private CreateColorIndexEncoding _indexFunc;
        private CreatePaletteEncoding _paletteFunc;

        private CreatePixelRemapper _swizzleFunc;

        private Action<IQuantizationOptions> _quantizationAction;

        public IImageConfiguration WithTaskCount(int taskCount)
        {
            _taskCount = taskCount;

            return this;
        }

        public IColorConfiguration TranscodeWith(CreateColorEncoding func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            _colorFunc = func;
            _indexFunc = null;

            return this;
        }

        public IIndexConfiguration TranscodeWith(CreateColorIndexEncoding func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            _indexFunc = func;
            _colorFunc = null;

            return this;
        }

        public IIndexConfiguration TranscodePaletteWith(CreatePaletteEncoding func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            _paletteFunc = func;

            return this;
        }

        public IImageConfiguration RemapPixelsWith(CreatePixelRemapper func)
        {
            ContractAssertions.IsNotNull(func, nameof(func));

            _swizzleFunc = func;

            return this;
        }

        public IImageConfiguration QuantizeWith(Action<IQuantizationOptions> configure)
        {
            ContractAssertions.IsNotNull(configure, nameof(configure));

            _quantizationAction = configure;

            return this;
        }

        public IImageConfiguration WithoutQuantization()
        {
            _quantizationAction = null;

            return this;
        }

        public IImageConfiguration Clone()
        {
            var config=new ImageConfiguration();

            if (_taskCount != Environment.ProcessorCount)
                config.WithTaskCount(_taskCount);
            if (_swizzleFunc != null)
                config.RemapPixelsWith(_swizzleFunc);
            if (_quantizationAction != null)
                config.QuantizeWith(_quantizationAction);

            if (_colorFunc != null)
                return config.TranscodeWith(_colorFunc);

            if (_indexFunc != null)
            {
                var indexConfig=config.TranscodeWith(_indexFunc);
                if (_paletteFunc != null)
                    return indexConfig.TranscodePaletteWith(_paletteFunc);
            }

            return config;
        }

        IIndexTranscoder IIndexConfiguration.Build()
        {
            ContractAssertions.IsNotNull(_indexFunc, "indexFunc");
            ContractAssertions.IsNotNull(_paletteFunc, "paletteFunc");

            var quantizer = BuildQuantizer();

            return new Transcoder(_indexFunc, _paletteFunc, _swizzleFunc, quantizer, _taskCount);
        }

        IColorTranscoder IColorConfiguration.Build()
        {
            ContractAssertions.IsNotNull(_colorFunc, "colorFunc");

            // Quantization for normal images is optional
            // If no quantization configuration was done beforehand we assume no quantization to be used here
            var quantizer = _quantizationAction == null ? null : BuildQuantizer();

            return new Transcoder(_colorFunc, _swizzleFunc, quantizer, _taskCount);
        }

        IQuantizer BuildQuantizer()
        {
            var quantizationConfiguration = new QuantizationConfiguration();
            if (_quantizationAction != null)
                quantizationConfiguration.WithOptions(_quantizationAction);

            quantizationConfiguration.WithTaskCount(_taskCount);

            return quantizationConfiguration.Build();
        }
    }
}
