using UniSky.Services.Navigation;

namespace UniSky.Navigation.Test;

/// <summary>
/// Exercises the rules that decide where a navigation request lands. This is the part that replaces
/// "look the navigation service up by name", so it is worth pinning down: a request raised deep
/// inside a feed has to reach the frame that feed lives in, and one raised inside a sheet must not
/// escape into the app behind it.
/// </summary>
public class NavigationWalkTests
{
    /// <summary>
    /// A stand-in for a scope. <see cref="Accepts"/> decides whether it can service a request, and
    /// <see cref="Handled"/> records what actually reached it.
    /// </summary>
    private sealed class Node : INavigationNode<string>
    {
        public Node(string name, bool accepts = false, NavigationScopePolicy policy = NavigationScopePolicy.BubbleUnknown)
        {
            Name = name;
            Accepts = accepts;
            Policy = policy;
        }

        public string Name { get; }
        public bool Accepts { get; set; }
        public NavigationScopePolicy Policy { get; }

        public Node? Parent { get; private set; }
        public Node? ActiveChild { get; private set; }

        /// <summary>Requests this node was asked to handle.</summary>
        public List<string> Handled { get; } = [];

        /// <summary>Requests this node saw pass through its interception hook.</summary>
        public List<string> Seen { get; } = [];

        /// <summary>Stands in for a host subscribed to Requesting.</summary>
        public Func<string, (bool cancel, bool handled, string? rewrite)>? Interceptor { get; set; }

        public INavigationNode<string> NodeParent => Parent!;
        public INavigationNode<string> NodeActiveChild => ActiveChild!;

        public Node WithChild(Node child)
        {
            child.Parent = this;
            ActiveChild ??= child;
            return this;
        }

        public Node WithActiveChild(Node child)
        {
            child.Parent = this;
            ActiveChild = child;
            return this;
        }

        /// <summary>Attaches to a parent without becoming its active child.</summary>
        public Node Under(Node parent)
        {
            Parent = parent;
            return this;
        }

        public bool TryIntercept(ref string request, out bool result)
        {
            Seen.Add(request);

            if (Interceptor == null)
            {
                result = false;
                return true;
            }

            var (cancel, handled, rewrite) = Interceptor(request);
            if (cancel)
            {
                result = false;
                return false;
            }

            if (handled)
            {
                result = true;
                return false;
            }

            if (rewrite != null)
                request = rewrite;

            result = false;
            return true;
        }

        public bool CanHandleHere(string request) => Accepts;

        public bool Handle(string request)
        {
            Handled.Add(request);
            return true;
        }
    }

    #region Handling in place

    [Fact]
    public void ANodeThatCanHandleARequestDoesSoImmediately()
    {
        var leaf = new Node("leaf", accepts: true);
        var shell = new Node("shell", accepts: true).WithChild(leaf);

        Assert.True(NavigationWalk.Navigate(leaf, "post"));

        Assert.Equal(["post"], leaf.Handled);
        Assert.Empty(shell.Handled);
    }

    [Fact]
    public void UnhandleableRequestsBubbleToAParentThatCanTakeThem()
    {
        var leaf = new Node("leaf");
        var shell = new Node("shell", accepts: true).WithChild(leaf);

        Assert.True(NavigationWalk.Navigate(leaf, "post"));

        Assert.Empty(leaf.Handled);
        Assert.Equal(["post"], shell.Handled);
    }

    [Fact]
    public void ARequestNobodyWantsIsDropped()
    {
        var leaf = new Node("leaf");
        var shell = new Node("shell").WithChild(leaf);

        Assert.False(NavigationWalk.Navigate(leaf, "post"));

        Assert.Empty(leaf.Handled);
        Assert.Empty(shell.Handled);
    }

    #endregion

    #region Regions

    [Fact]
    public void ARegionRoutesIntoWhicheverChildIsActive()
    {
        // The home tab host: frameless itself, delegating to the selected tab.
        var selected = new Node("tab:selected", accepts: true);
        var other = new Node("tab:other", accepts: true);

        var region = new Node("home");
        region.WithChild(other);
        region.WithActiveChild(selected);

        Assert.True(NavigationWalk.Navigate(region, "post"));

        Assert.Equal(["post"], selected.Handled);
        Assert.Empty(other.Handled);
    }

    [Fact]
    public void ARegionDoesNotBounceARequestBackIntoTheChildItCameFrom()
    {
        // Without the "already declined" guard this recurses until the stack gives out.
        var leaf = new Node("tab", accepts: false);
        var region = new Node("home").WithChild(leaf);
        var shell = new Node("shell", accepts: true).WithChild(region);

        Assert.True(NavigationWalk.Navigate(leaf, "post"));

        Assert.Equal(["post"], shell.Handled);

        // The leaf is asked once, on the way out — not again on the way back down.
        Assert.Single(leaf.Seen);
    }

    [Fact]
    public void ARequestLandsWhereItWasRaisedEvenIfThatScopeIsNotOnScreen()
    {
        // A viewmodel in a background tab navigates. It lands in that tab, not in whichever tab
        // happens to be visible — the request belongs to the scope that raised it.
        //
        // The consequence is that the user sees nothing until they switch back to that tab. That is
        // the right trade: the alternative is a background timer or a late async continuation
        // yanking the visible tab somewhere the user didn't ask to go.
        var region = new Node("home");
        var active = new Node("tab:active", accepts: true);
        var background = new Node("tab:background", accepts: true).Under(region);

        region.WithActiveChild(active);

        Assert.True(NavigationWalk.Navigate(background, "post"));

        Assert.Equal(["post"], background.Handled);
        Assert.Empty(active.Handled);
    }

    [Fact]
    public void DelegationDescendsThroughSeveralLevels()
    {
        var deep = new Node("deep", accepts: true);
        var middle = new Node("middle").WithChild(deep);
        var region = new Node("region").WithChild(middle);

        Assert.True(NavigationWalk.Navigate(region, "post"));
        Assert.Equal(["post"], deep.Handled);
    }

    #endregion

    #region Containment

    [Fact]
    public void AContainedScopeDropsWhatItCannotHandleRatherThanLettingItEscape()
    {
        // The sheet case: opening something unsupported from inside a sheet must not navigate the
        // app sitting behind the sheet.
        var content = new Node("sheet:content", accepts: false);
        var sheet = new Node("sheet", policy: NavigationScopePolicy.Contain).WithChild(content);
        var shell = new Node("shell", accepts: true).WithChild(sheet);

        Assert.False(NavigationWalk.Navigate(content, "unsupported"));

        Assert.Empty(shell.Handled);
    }

    [Fact]
    public void AContainedScopeStillHandlesWhatItCan()
    {
        var content = new Node("sheet:content", accepts: true);
        var sheet = new Node("sheet", policy: NavigationScopePolicy.Contain).WithChild(content);
        var shell = new Node("shell", accepts: true).WithChild(sheet);

        Assert.True(NavigationWalk.Navigate(content, "post"));

        Assert.Equal(["post"], content.Handled);
        Assert.Empty(shell.Handled);
    }

    #endregion

    #region Interception

    [Fact]
    public void AHostCanClaimARequestOutright()
    {
        var leaf = new Node("leaf", accepts: true);
        var shell = new Node("shell").WithChild(leaf);

        leaf.Interceptor = _ => (cancel: false, handled: true, rewrite: null);

        Assert.True(NavigationWalk.Navigate(leaf, "post"));

        // Claimed before the node's own handling ran.
        Assert.Empty(leaf.Handled);
    }

    [Fact]
    public void AHostCanVetoARequest()
    {
        var leaf = new Node("leaf", accepts: true);
        var shell = new Node("shell", accepts: true).WithChild(leaf);

        leaf.Interceptor = _ => (cancel: true, handled: false, rewrite: null);

        Assert.False(NavigationWalk.Navigate(leaf, "post"));

        Assert.Empty(leaf.Handled);
        Assert.Empty(shell.Handled);
    }

    [Fact]
    public void AHostCanRewriteARequestAndTheRewriteTravelsOn()
    {
        // This is how "open that in a new column instead" is expressed.
        var leaf = new Node("leaf");
        var shell = new Node("shell", accepts: true).WithChild(leaf);

        leaf.Interceptor = _ => (cancel: false, handled: false, rewrite: "post-in-column");

        Assert.True(NavigationWalk.Navigate(leaf, "post"));

        Assert.Equal(["post-in-column"], shell.Handled);
        Assert.Equal(["post-in-column"], shell.Seen);
    }

    [Fact]
    public void EveryScopeOnTheWayOutGetsAChanceToIntercept()
    {
        var leaf = new Node("leaf");
        var region = new Node("region");
        var shell = new Node("shell", accepts: true);

        shell.WithChild(region);
        region.WithChild(leaf);

        Assert.True(NavigationWalk.Navigate(leaf, "post"));

        Assert.Equal(["post"], leaf.Seen);
        Assert.Equal(["post"], region.Seen);
        Assert.Equal(["post"], shell.Seen);
    }

    #endregion
}
