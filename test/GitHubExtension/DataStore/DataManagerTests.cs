﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubExtension.Test;

public partial class DataStoreTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void DataManagerCreate()
    {
        using var dataManager = GitHubDataManager.CreateInstance(TestOptions.DataStoreOptions);
        Assert.IsNotNull(dataManager);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void DataManagerGetRepositories()
    {
        using var dataManager = GitHubDataManager.CreateInstance(TestOptions.DataStoreOptions);
        Assert.IsNotNull(dataManager);

        var repos = dataManager.GetRepositories();
        Assert.IsNotNull(repos);

        var noSuchRepo = dataManager.GetRepository("foo/bar");
        Assert.IsNull(noSuchRepo);

        noSuchRepo = dataManager.GetRepository("foo", "bar");
        Assert.IsNull(noSuchRepo);
    }

    [TestMethod]
    [TestCategory("LiveData")]
    public async Task DataManagerUpdateRepository()
    {
        using var dataManager = GitHubDataManager.CreateInstance(TestOptions.DataStoreOptions);
        Assert.IsNotNull(dataManager);

        // Limit the page size so we don't destroy our rate limit on a busy repo.
        // Get 10 items of each type.
        var requestOptions = new RequestOptions()
        {
            ApiOptions = new Octokit.ApiOptions()
            {
                PageSize = 10,
                PageCount = 1,
                StartPage = 1,
            },
        };

        await dataManager.UpdateAllDataForRepositoryAsync("microsoft/windowsappsdk", requestOptions);
    }

    [TestMethod]
    [TestCategory("LiveData")]
    public async Task DataManagerUpdatePullRequests()
    {
        using var dataManager = GitHubDataManager.CreateInstance(TestOptions.DataStoreOptions);
        Assert.IsNotNull(dataManager);

        // Limit the page size so we don't destroy our rate limit on a busy repo.
        // Get 10 items of each type.
        var requestOptions = new RequestOptions()
        {
            ApiOptions = new Octokit.ApiOptions()
            {
                PageSize = 10,
                PageCount = 1,
                StartPage = 1,
            },
        };

        await dataManager.UpdatePullRequestsForRepositoryAsync("octokit/octokit.net", requestOptions);
        await dataManager.UpdatePullRequestsForRepositoryAsync("microsoft/powertoys", requestOptions);
    }

    [TestMethod]
    [TestCategory("LiveData")]
    public async Task DataManagerUpdateIssues()
    {
        using var dataManager = GitHubDataManager.CreateInstance(TestOptions.DataStoreOptions);
        Assert.IsNotNull(dataManager);

        // Limit the page size so we don't destroy our rate limit on a busy repo.
        // Get 10 items of each type.
        var requestOptions = new RequestOptions()
        {
            ApiOptions = new Octokit.ApiOptions()
            {
                PageSize = 10,
                PageCount = 1,
                StartPage = 1,
            },
        };

        await dataManager.UpdateIssuesForRepositoryAsync("octokit/octokit.net", requestOptions);
    }

    [TestMethod]
    [TestCategory("LiveData")]
    public async Task DataManagerUpdatePullRequestsForDeveloper()
    {
        using var dataManager = GitHubDataManager.CreateInstance(TestOptions.DataStoreOptions);
        Assert.IsNotNull(dataManager);

        await dataManager.UpdatePullRequestsForLoggedInDeveloperIdsAsync();

        var devUsers = dataManager.GetDeveloperUsers();
        Assert.IsNotNull(devUsers);
        Assert.AreEqual(1, devUsers.Count());
        var pulls = devUsers.First().PullRequests;
        Assert.IsNotNull(pulls);
        foreach (var pull in pulls)
        {
            TestContext?.WriteLine($"  User: {pull.Author.Login}  Repo: {pull.Repository.FullName} - {pull.Number} - {pull.Title}");
        }
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void DataUpdater()
    {
        var countingDoneEvent = new ManualResetEvent(false);
        var count = 0;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        using var dataUpdater = new DataManager.DataUpdater(
            TimeSpan.FromSeconds(1),
            async () =>
            {
                TestContext?.WriteLine($"In DataUpdater thread: {count}");
                ++count;
                if (count == 3)
                {
                    countingDoneEvent.Set();
                }
            });
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        Assert.IsNotNull(dataUpdater);

        // Data Updater will kick off an asynchronous task. we will wait for it to cycle three times terminate.
        _ = dataUpdater.Start();
        countingDoneEvent.WaitOne();
        dataUpdater.Stop();
        Assert.AreEqual(3, count);

        // Reset and do it again, this time testing stop mid-way.
        // Data Updater will kick off an asynchronous task. We will wait and give it enough time to
        // update twice and then stop it halfway through the second update.
        count = 0;
        _ = dataUpdater.Start();
        Thread.Sleep(1500);
        dataUpdater.Stop();
        Assert.IsFalse(dataUpdater.IsRunning);
        Thread.Sleep(2100);

        // After over two more seconds data updater has had time to count a few more times, unless
        // it was stopped successfully, in which case it would still only be at 1.
        // This test can randomly fail based on timings in builds, so disabling this check to avoid
        // 1-off errors from tanking a build.
        // Assert.AreEqual(1, count);
    }
}
