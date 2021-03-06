﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MilestoneTracker.Model;

namespace MilestoneTracker.Pages
{
    public class MilestoneBurndownModel : PageModel
    {
        [FromQuery]
        public string Milestone { get; set; }

        [FromQuery]
        public string TeamName { get; set; }

        [FromQuery]
        public string Label { get; set; }

        [FromQuery]
        public bool IncludeInvestigations { get; set; }

        public BurndownChartModel BurndownModel { get; set; }

        public void OnGet()
        {
            this.BurndownModel = new BurndownChartModel
            {
                Milestone = this.Milestone,
                TeamName = this.TeamName,
                LabelsFilter = this.Label,
                IncludeInvestigations = this.IncludeInvestigations
            };
        }
    }
}