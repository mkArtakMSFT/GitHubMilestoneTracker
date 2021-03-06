﻿using AM.Common.Validation;
using GitHub.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MilestoneTracker.Contracts;
using MilestoneTracker.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MilestoneTracker.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private const char milestoneSeparatorCharacter = ';';

        private readonly WorkEstimatorFactory workEstimatorFactory;
        private readonly Lazy<IEnumerable<string>> lazyMilestonesLoader;
        private readonly Lazy<IEnumerable<string>> lazyLabelsLoader;
        private readonly ITeamsManager teamsManager;
        private readonly IUserTeamsManager userTeamsManager;
        private TeamInfo currentTeam = null;
        private string milestone;

        [FromQuery]
        public string Milestone
        {
            get => this.milestone ?? this.currentTeam?.DefaultMilestonesToTrack;
            set => this.milestone = value;
        }

        [FromQuery]
        public string TeamName { get; set; }

        [FromQuery]
        public string Label { get; set; }

        public IEnumerable<string> Labels { get => this.lazyLabelsLoader.Value; }

        public IEnumerable<string> Milestones { get => this.lazyMilestonesLoader.Value; }

        public WorkDataViewModel Work { get; set; }

        public IndexModel(
            WorkEstimatorFactory workEstimatorFactory,
            ITeamsManager teamsManager,
            IUserTeamsManager userTeamsManager)
        {
            this.workEstimatorFactory = workEstimatorFactory.Ensure(nameof(workEstimatorFactory)).IsNotNull().Value;
            this.userTeamsManager = userTeamsManager.Ensure(nameof(userTeamsManager)).IsNotNull().Value;
            this.teamsManager = teamsManager.Ensure(nameof(teamsManager)).IsNotNull().Value;

            this.lazyMilestonesLoader = new Lazy<IEnumerable<string>>(() =>
            {
                return this.Milestone?
                    .Split(milestoneSeparatorCharacter, StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Trim());
            });

            this.lazyLabelsLoader = new Lazy<IEnumerable<string>>(() =>
            {
                if (this.Label == null)
                    return null;

                return this.Label.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(item => item.Trim()).ToArray();
            });
        }

        public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
        {
            if (this.TeamName == null)
            {
                return Redirect("/Teams");
            }

            if (this.Milestone == null)
            {
                await this.GetCurrentTeamAsync(cancellationToken);
            }

            if (this.Milestones != null)
            {
                TeamInfo team = await this.GetCurrentTeamAsync(cancellationToken);
                if (team == null)
                {
                    return Redirect("/Teams");
                }

                IWorkEstimator workEstimator = await this.GetWorkEstimatorAsync(team, cancellationToken);

                await this.RetrieveWorkloadAsync(workEstimator, cancellationToken);
            }

            return Page();
        }

        private async Task RetrieveWorkloadAsync(IWorkEstimator workEstimator, CancellationToken cancellationToken)
        {
            this.Work = new WorkDataViewModel();

            IList<Task> tasks = new List<Task>();
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                TeamInfo currentTeam = await this.GetCurrentTeamAsync(cancellationToken);
                this.Work.TeamName = currentTeam.Name;
                this.Work.Label = this.Label;
                object syncRoot = new object();

                foreach (var milestone in this.Milestones)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        IEnumerable<WorkItem> issues = await workEstimator.GetAmountOfWorkAsync(
                            new IssuesQuery
                            {
                                Team = currentTeam,
                                Milestone = milestone,
                                FilterLabels = this.Labels
                            },
                            cancellationToken);
                        IEnumerable<string> members = currentTeam.TeamMembers?.Where(item => item.IncludeInReports)?.Select(item => item.Name)?.Union(new string[] { "Unassigned" });
                        if (members == null)
                        {
                            members = new string[] { "Unassigned" };
                        }

                        foreach (var member in members)
                        {
                            if (tokenSource.IsCancellationRequested)
                            {
                                break;
                            }

                            lock (syncRoot)
                            {
                                this.Work[member, milestone] = issues.Where(item => item.Owner == member).Sum(item => item.Cost);
                            }
                        }
                    }));
                }
            }

            await Task.WhenAll(tasks);
        }

        private async Task<IWorkEstimator> GetWorkEstimatorAsync(TeamInfo team, CancellationToken cancellationToken)
        {
            string accessToken = await this.HttpContext.GetTokenAsync("access_token");

            // This allows the client to make requests to the GitHub API on behalf of the user
            // without ever having the user's OAuth credentials.
            return this.workEstimatorFactory.Create(accessToken, team);
        }

        private async Task<TeamInfo> GetCurrentTeamAsync(CancellationToken token)
        {
            if (this.currentTeam == null)
            {
                this.currentTeam = await this.userTeamsManager.GetTeamInfoAsync(User.Identity.Name, this.TeamName, token);
            }

            return this.currentTeam;
        }
    }
}
