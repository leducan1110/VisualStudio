using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using GitHub.Models;
using GitHub.Services;
using GitHub.ViewModels.GitHubPane;
using ReactiveUI;

namespace GitHub.App.ViewModels.GitHubPane
{
    /// <inheritdoc cref="IPullRequestAnnotationsViewModel"/>
    [Export(typeof(IPullRequestAnnotationsViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class PullRequestAnnotationsViewModel : PanePageViewModelBase, IPullRequestAnnotationsViewModel
    {
        private readonly IPullRequestSessionManager sessionManager;

        IPullRequestSession session;
        string title;
        string checkSuiteName;
        string checkRunName;
        IReadOnlyList<IPullRequestAnnotationItemViewModel> annotations;

        /// <summary>
        /// Initializes a new instance of the <see cref="PullRequestAnnotationsViewModel"/> class.
        /// </summary>
        /// <param name="sessionManager">The pull request session manager.</param>
        [ImportingConstructor]
        public PullRequestAnnotationsViewModel(IPullRequestSessionManager sessionManager)
        {
            this.sessionManager = sessionManager;
            NavigateToPullRequest = ReactiveCommand.Create().OnExecuteCompleted(_ =>
                NavigateTo(FormattableString.Invariant($"{LocalRepository.Owner}/{LocalRepository.Name}/pull/{PullRequestNumber}")));
        }

        /// <inheritdoc/>
        public async Task InitializeAsync(ILocalRepositoryModel localRepository, IConnection connection, string owner,
            string repo, int pullRequestNumber, int checkRunId)
        {
            if (repo != localRepository.Name)
            {
                throw new NotSupportedException();
            }

            IsLoading = true;

            try
            {
                LocalRepository = localRepository;
                RemoteRepositoryOwner = owner;
                PullRequestNumber = pullRequestNumber;
                CheckRunId = checkRunId;
                session = await sessionManager.GetSession(owner, repo, pullRequestNumber);
                await Load(session.PullRequest);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <inheritdoc/>
        public ILocalRepositoryModel LocalRepository { get; private set; }

        /// <inheritdoc/>
        public string RemoteRepositoryOwner { get; private set; }

        /// <inheritdoc/>
        public int PullRequestNumber { get; private set; }

        /// <inheritdoc/>
        public int CheckRunId { get; private set; }

        /// <inheritdoc/>
        public ReactiveCommand<object> NavigateToPullRequest { get; private set; }

        /// <inheritdoc/>
        public string PullRequestTitle
        {
            get { return title; }
            private set { this.RaiseAndSetIfChanged(ref title, value); }
        }

        /// <inheritdoc/>
        public string CheckSuiteName
        {
            get { return checkSuiteName; }
            private set { this.RaiseAndSetIfChanged(ref checkSuiteName, value); }
        }

        /// <inheritdoc/>
        public string CheckRunName
        {
            get { return checkRunName; }
            private set { this.RaiseAndSetIfChanged(ref checkRunName, value); }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IPullRequestAnnotationItemViewModel> Annotations
        {
            get { return annotations; }
            private set { this.RaiseAndSetIfChanged(ref annotations, value); }
        }

        async Task Load(PullRequestDetailModel pullRequest)
        {
            IsBusy = true;

            try
            {
                await Task.Delay(0);
                PullRequestTitle = pullRequest.Title;

                var checkSuiteRun = pullRequest
                    .CheckSuites.SelectMany(checkSuite => checkSuite.CheckRuns
                            .Select(checkRun => new{checkSuite, checkRun}))
                    .First(arg => arg.checkRun.DatabaseId == CheckRunId);

                CheckSuiteName = checkSuiteRun.checkSuite.ApplicationName;
                CheckRunName = checkSuiteRun.checkRun.Name;
                Annotations = checkSuiteRun.checkRun.Annotations
                    .Select(annotation => new PullRequestAnnotationItemViewModel(annotation))
                    .ToArray();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}