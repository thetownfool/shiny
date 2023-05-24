﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid) => Observable.FromAsync<BleCharacteristicInfo>(async ct =>
    {
        var sid = Utils.ToUuidType(serviceUuid);
        var cid = Utils.ToUuidType(characteristicUuid);

        var service = await this.Native!
            .GetGattServicesForUuidAsync(sid)
            .AsTask(ct)
            .ConfigureAwait(false);

        service.Status.Assert();

        //service.ProtocolError
        var chars = await service.Services.First().GetCharacteristicsForUuidAsync(cid).AsTask(ct).ConfigureAwait(false);
        chars.Status.Assert();
        var ch = chars.Characteristics.First();

        return ToChar(ch);
    });


    public IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid) => Observable.FromAsync(async ct =>
    {
        var nativeUuid = Utils.ToUuidType(serviceUuid);

        //var result = await this.Native!.GetGattService(nativeUuid);

        //    .GetCharacteristicsAsync(BluetoothCacheMode.Uncached)
        //    .AsTask(ct)
        //    .ConfigureAwait(false);

        //result.Status.Assert();
        //return result
        //    .Characteristics
        //    .Select(x => ToChar(x))
        //    .ToList();
        return null;
    });


    protected static BleCharacteristicInfo ToChar(GattCharacteristic ch) => new BleCharacteristicInfo(
        new BleServiceInfo(ch.Service.Uuid.ToString()), 
        ch.Uuid.ToString(), 
        false, //ch.ValueChanged != null, 
        CharacteristicProperties.Broadcast
    );


    public IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvailable = true) => throw new NotImplementedException();
    public IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid) => throw new NotImplementedException();
    public IObservable<BleCharacteristicInfo> WhenCharacteristicSubscriptionChanged() => throw new NotImplementedException();
    public IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true) => throw new NotImplementedException();    
}

/*
 using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Shiny.BluetoothLE.Internals;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic;


namespace Shiny.BluetoothLE
{



 public override IObservable<IGattCharacteristic?> GetKnownCharacteristic(string characteristicUuid, bool throwIfNotFound = false) =>
            Observable.FromAsync(async () =>
            {
                var uuid = Utils.ToUuidType(characteristicUuid);
                var result = await this.native.GetCharacteristicsForUuidAsync(
                    uuid,
                    BluetoothCacheMode.Cached
                );
                if (result.Status != GattCommunicationStatus.Success)
                    throw new ArgumentException("GATT Communication failure - " + result.Status);

                var ch = new GattCharacteristic(this.context, result.Characteristics.First(), this);
                return ch;
            })
            .Assert(this.Uuid, characteristicUuid, throwIfNotFound);




        public override IObservable<IList<IGattDescriptor>> GetDescriptors() => Observable.FromAsync<IList<IGattDescriptor>>(async ct =>
        {
            var result = await this.Native
                .GetDescriptorsAsync(BluetoothCacheMode.Uncached)
                .AsTask(ct)
                .ConfigureAwait(false);

            result.Status.Assert();

            return result
                .Descriptors
                .Select(native => new GattDescriptor(native, this))
                .Cast<IGattDescriptor>()
                .ToList();
        });


        public override IObservable<GattCharacteristicResult> Write(byte[] value, bool withResponse) => Observable.FromAsync(async ct =>
        {
            this.AssertWrite(withResponse);

            var writeType = withResponse
                ? GattWriteOption.WriteWithResponse
                : GattWriteOption.WriteWithoutResponse;

            await this.Native
                .WriteValueAsync(value.AsBuffer(), writeType)
                .Execute(ct)
                .ConfigureAwait(false);

            return new GattCharacteristicResult(
                this,
                value,
                withResponse
                    ? GattCharacteristicResultType.Write
                    : GattCharacteristicResultType.WriteWithoutResponse
            );
        });


        public override IObservable<GattCharacteristicResult> Read() => Observable.FromAsync(async ct =>
        {
            this.AssertRead();
            var result = await this.Native
                .ReadValueAsync(BluetoothCacheMode.Uncached)
                .AsTask(ct)
                .ConfigureAwait(false);

            if (result.Status != GattCommunicationStatus.Success)
                throw new BleException($"Failed to read characteristic - {result.Status}");

            return new GattCharacteristicResult(
                this,
                result.Value?.ToArray(),
                GattCharacteristicResultType.Read
            );
        });


        public override IObservable<IGattCharacteristic> EnableNotifications(bool enable, bool useIndicationsIfAvailable) => Observable.FromAsync(async ct =>
        {
            if (!enable)
            {
                await this.Native.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                this.IsNotifying = false;
                this.context.SetNotifyCharacteristic(this);
            }
            else
            {
                var type = useIndicationsIfAvailable && this.CanIndicate()
                    ? GattClientCharacteristicConfigurationDescriptorValue.Indicate
                    : GattClientCharacteristicConfigurationDescriptorValue.Notify;

                var status = await this.Native.WriteClientCharacteristicConfigurationDescriptorAsync(type);
                if (status != GattCommunicationStatus.Success)
                    throw new BleException($"Failed to write client characteristic configuration descriptor - {status}");

                this.IsNotifying = true;
                this.context.SetNotifyCharacteristic(this);
            }
            return this;
        });


        readonly List<TypedEventHandler<Native, GattValueChangedEventArgs>> handlers = new List<TypedEventHandler<Native, GattValueChangedEventArgs>>();
        public override IObservable<GattCharacteristicResult> WhenNotificationReceived() => Observable.Create<GattCharacteristicResult>(async ob =>
        {
            var handler = new TypedEventHandler<Native, GattValueChangedEventArgs>((sender, args) =>
            {
                if (sender.Equals(this.Native))
                {
                    var bytes = args.CharacteristicValue.ToArray();
                    var result = new GattCharacteristicResult(this, bytes, GattCharacteristicResultType.Notification);
                    ob.OnNext(result);
                }
            });

            this.Native.ValueChanged += handler;
            this.handlers.Add(handler);

            this.context.SetNotifyCharacteristic(this);
            this.IsNotifying = true;

            return () =>
            {
                this.Native.ValueChanged -= handler;
                this.handlers.Remove(handler);
            };
        });


        internal async Task Disconnect()
        {
            await this.Native.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
            foreach (var handler in this.handlers)
                this.Native.ValueChanged -= handler;

            this.handlers.Clear();
        }
    }
}
 */