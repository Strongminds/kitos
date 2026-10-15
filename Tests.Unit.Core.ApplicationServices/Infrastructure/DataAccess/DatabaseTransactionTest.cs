using System;
using Infrastructure.DataAccess.Services;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Tests.Toolkit.Patterns;
using Xunit;

namespace Tests.Unit.Core.Infrastructure.DataAccess;

public class DatabaseTransactionTest : WithAutoFixture
{
    private readonly Mock<IDbContextTransaction> _transaction = new();

    [Fact]
    public void Commit_NestedScopes_PublishesOnlyAfterOutermostCommit()
    {
        using var root = new DatabaseTransaction(_transaction.Object);
        using var archive = new WithinAmbienTransaction(root);
        using var deletion = new WithinAmbienTransaction(archive);
        var calls = 0;
        _transaction.Setup(x => x.Commit()).Callback(() => Assert.Equal(0, calls));
        deletion.AfterCommit(() => calls++);

        deletion.Commit();
        archive.Commit();
        Assert.Equal(0, calls);
        root.Commit();

        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Dispose_UncommittedRoot_DiscardsCallbacks(bool rollback)
    {
        var calls = 0;
        using (var root = new DatabaseTransaction(_transaction.Object))
        {
            using var nested = new WithinAmbienTransaction(root);
            nested.AfterCommit(() => calls++);
            nested.Commit();
            if (rollback)
                root.Rollback();
        }

        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Commit_UncommittedNestedScope_DiscardsItsCallbacks(bool rollback)
    {
        using var root = new DatabaseTransaction(_transaction.Object);
        var calls = 0;
        using (var nested = new WithinAmbienTransaction(root))
        {
            nested.AfterCommit(() => calls++);
            if (rollback)
                nested.Rollback();
        }

        root.Commit();

        Assert.Equal(0, calls);
    }

    [Fact]
    public void Commit_DatabaseCommitFails_DoesNotInvokeCallbacks()
    {
        using var root = new DatabaseTransaction(_transaction.Object);
        var callback = new Mock<Action>();
        root.AfterCommit(callback.Object);
        _transaction.Setup(x => x.Commit()).Throws<InvalidOperationException>();

        Assert.Throws<InvalidOperationException>(() => root.Commit());

        callback.Verify(x => x(), Times.Never);
    }

    [Fact]
    public void Commit_MultipleDeletions_InvokesEachCallbackOnce()
    {
        using var root = new DatabaseTransaction(_transaction.Object);
        var calls = 0;
        for (var i = 0; i < 3; i++)
        {
            using var nested = new WithinAmbienTransaction(root);
            nested.AfterCommit(() => calls++);
            nested.Commit();
        }

        root.Commit();

        Assert.Equal(3, calls);
    }

    [Fact]
    public void AfterCommit_UnmanagedTransaction_ThrowsExplicitError()
    {
        using var nested = new WithinAmbienTransaction();

        Assert.Throws<InvalidOperationException>(() => nested.AfterCommit(() => { }));
    }
}
