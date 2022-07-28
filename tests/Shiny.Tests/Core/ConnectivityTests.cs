﻿using Acr.UserDialogs;
using Shiny.Hosting;
using Shiny.Net;

namespace Shiny.Tests.Core;


public class ConnectivityTests : AbstractShinyTests
{
    public ConnectivityTests(ITestOutputHelper output) : base(output) {}
    protected override void Configure(IHostBuilder hostBuilder) => hostBuilder.Services.AddConnectivity();

    [Theory(DisplayName = "Connectivity - Access (Simulator)")]
    [InlineData(NetworkAccess.Internet, "")]
    [InlineData(NetworkAccess.ConstrainedInternet, "")]
    public async Task StateTests(NetworkAccess expectedAccess, string message)
    {
        var conn = this.GetService<IConnectivity>();
        await UserDialogs.Instance.AlertAsync(message + " then press OK");
        conn.Access.Should().Be(expectedAccess);
    }


    [Theory(DisplayName = "Connectivity - Access (Simulator)")]
    [InlineData(ConnectionTypes.Wifi, "")]
    [InlineData(ConnectionTypes.Cellular, "")]
    public async Task ConnectionTypeTests(ConnectionTypes type, string message)
    {
        var conn = this.GetService<IConnectivity>();
        await UserDialogs.Instance.AlertAsync(message + " then press OK");
        conn.ConnectionTypes.HasFlag(type).Should().BeTrue();
    }


    [Fact(DisplayName = "Connectivity - Change Monitoring (Simulator)")]
    public async Task ChangeMonitorTest()
    {
        var conn = this.GetService<IConnectivity>();
        await UserDialogs.Instance.AlertAsync("Change anything with the connection to pass this test AFTER pressing OK");
        await conn
            .WhenChanged()
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(20))
            .ToTask();
    }
}
