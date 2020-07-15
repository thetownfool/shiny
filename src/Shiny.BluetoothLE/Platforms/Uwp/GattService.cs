﻿using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceService;


namespace Shiny.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        readonly DeviceContext context;
        readonly Native native;


        public GattService(DeviceContext context, Native native) : base(context.Peripheral, native.Uuid, false)
        {
            this.context = context;
            this.native = native;
        }


        public override IObservable<IGattCharacteristic> GetKnownCharacteristics(params Guid[] characteristicIds)
            => Observable.Create<IGattCharacteristic>(async ob =>
            {
                var found = false;

                foreach (var uuid in characteristicIds)
                {
                    var result = await this.native.GetCharacteristicsForUuidAsync(uuid, BluetoothCacheMode.Cached);
                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        found = true;
                        var ch = new GattCharacteristic(this.context, result.Characteristics.First(), this);
                        ob.OnNext(ch);
                    }
                }
                // prevent NRE on observable
                if (!found)
                    ob.OnNext(null);

                ob.OnCompleted();

                return Disposable.Empty;
            });


        public override IObservable<IGattCharacteristic> DiscoverCharacteristics()
        {
            var discoverCharacteristics = new Subject<IGattCharacteristic>();
            Task.Run(async () => 
            {
                try
                {
                    var result = await this.native.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    foreach (var characteristic in result.Characteristics)
                    {
                        var wrap = new GattCharacteristic(this.context, characteristic, this);
                        discoverCharacteristics.OnNext(wrap);
                    }
                    discoverCharacteristics.OnCompleted();
                }
                catch(Exception ex) { discoverCharacteristics.OnError(ex); }
            });
            return discoverCharacteristics.AsObservable();
        }
    }
}
