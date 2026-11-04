using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A value others watch: who hears its changes, and who no longer does once let go.</summary>
public class StoreTests
{
    [Fact]
    public void TellsEveryListenerOfAChangeInOrderAndNothingWhenSetToWhatItIs()
    {
        var store = new Store<int>(0);
        var heard = new List<string>();
        store.Subscribe(() => heard.Add("a"));
        store.Subscribe(() => heard.Add("b"));
        store.Set(0);
        store.Set(1);
        Assert.Equal(["a", "b"], heard);
    }

    [Fact]
    public void ListenerLetGoHearsNoMoreTheOthersStill()
    {
        var store = new Store<int>(0);
        var heard = new List<string>();
        var a = store.Subscribe(() => heard.Add("a"));
        store.Subscribe(() => heard.Add("b"));
        a.Dispose();
        a.Dispose();
        store.Set(1);
        Assert.Equal(["b"], heard);
    }

    [Fact]
    public void ListenerLetGoDuringAChangeIsNotCalledLaterInIt()
    {
        // A panel freed by one listener's work, its own listener later in the list, never paints its freed buttons.
        var store = new Store<int>(0);
        var heard = new List<string>();
        IDisposable? b = null;
        store.Subscribe(() => b!.Dispose());
        b = store.Subscribe(() => heard.Add("b"));
        store.Set(1);
        store.Set(2);
        Assert.Empty(heard);
    }

    [Fact]
    public void PanelsMadeAndLetGoManyTimesLeaveNoListenersBehind()
    {
        var store = new Store<int>(0);
        int calls = 0;
        for (int i = 0; i < 50; i++) store.Subscribe(() => calls++).Dispose();
        store.Set(1);
        Assert.Equal(0, calls);
    }
}
