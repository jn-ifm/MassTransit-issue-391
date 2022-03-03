using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Courier.Contracts;
using MassTransit.TestFramework;
using MassTransit.Testing;
using Xunit;

namespace MassTransit_issue_391;

// ReSharper disable once CheckNamespace
public class DemoActivityTests : InMemoryActivityTestFixture, IAsyncDisposable
{
    
    protected override void SetupActivities(BusTestHarness testHarness)
    {
        AddActivityContext<DemoActivity, DemoArguments>(() => new DemoActivity());
    }

    [Fact]
    public async Task Demo_should_not_fail_to_serialize()
    {
        await SetupInMemoryTestFixture();
        
        // Arrange
        var activity = GetActivityContext<DemoActivity>();
        
        var completed = InMemoryTestHarness.SubscribeHandler<RoutingSlipCompleted>();
        var activityCompleted = InMemoryTestHarness.SubscribeHandler<RoutingSlipActivityCompleted>();

        var trackingNumber = NewId.NextGuid();
        var builder = new RoutingSlipBuilder(trackingNumber);
        builder.AddSubscription(InMemoryTestHarness.BusAddress, RoutingSlipEvents.All);
        builder.AddActivity(activity.Name, activity.ExecuteUri, new DemoArguments(Guid.NewGuid()));

        await InMemoryTestHarness.Bus.Execute(builder.Build());

        await completed;

        ConsumeContext<RoutingSlipActivityCompleted> context = await activityCompleted!;

        // Assert
        Assert.True(await InMemoryTestHarness.Published.Any<DemoEvent>());
        Assert.Equal(trackingNumber, context.Message.TrackingNumber);
    }

    public async ValueTask DisposeAsync() => await TearDownInMemoryTestFixture();
}

public class DemoActivity : IExecuteActivity<DemoArguments>
{
    public async Task<ExecutionResult> Execute(ExecuteContext<DemoArguments> context)
    {
        await Task.Delay(500);

        await context.Publish(new DemoEvent(context.Arguments.Id)).ConfigureAwait(false);
        return context.Completed();
    }
}

public record DemoArguments(Guid Id);

public record DemoEvent(Guid Id);
