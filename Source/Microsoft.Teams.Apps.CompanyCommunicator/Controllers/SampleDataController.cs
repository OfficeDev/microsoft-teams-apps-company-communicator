// <copyright file="SampleDataController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Controller for the sample data.
    /// </summary>
    [Route("api/[controller]")]
    public class SampleDataController : Controller
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching",
        };

        /// <summary>
        /// Receive a start date index from user and return the collection of weather forecasts matching the index.
        /// </summary>
        /// <param name="startDateIndex">Start date index.</param>
        /// <returns>A collection of <see cref="WeatherForecast"/> matching the passing in parameter <paramref name="startDateIndex"/>.</returns>
        [HttpGet("[action]")]
        public IEnumerable<WeatherForecast> WeatherForecasts(int startDateIndex)
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                DateFormatted = DateTime.Now.AddDays(index + startDateIndex).ToString("d"),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)],
            });
        }

        /// <summary>
        /// Weather forecast model class.
        /// </summary>
        public class WeatherForecast
        {
            /// <summary>
            /// Gets or sets the formatted date.
            /// </summary>
            public string DateFormatted { get; set; }

            /// <summary>
            /// Gets or sets the temperature in C.
            /// </summary>
            public int TemperatureC { get; set; }

            /// <summary>
            /// Gets or sets the summary.
            /// </summary>
            public string Summary { get; set; }

            /// <summary>
            /// Gets the temperature in F.
            /// </summary>
            public int TemperatureF
            {
                get
                {
                    return 32 + (int)(this.TemperatureC / 0.5556);
                }
            }
        }
    }
}
