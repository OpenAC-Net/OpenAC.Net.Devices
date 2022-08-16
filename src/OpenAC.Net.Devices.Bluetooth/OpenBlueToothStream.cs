using System;

namespace OpenAC.Net.Devices.Bluetooth
{
    public sealed class OpenBlueToothStream : OpenDeviceStream
    {
        public OpenBlueToothStream(IDeviceConfig config) : base(config)
        {
        }

        protected override int Available => throw new NotImplementedException();

        protected override bool CloseInternal()
        {
            throw new NotImplementedException();
        }

        protected override bool OpenInternal()
        {
            throw new NotImplementedException();
        }
    }
}