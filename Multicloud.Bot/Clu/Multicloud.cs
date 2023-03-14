// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Multicloud.Bot.Clu
{
    /// <summary>
    /// An <see cref="IRecognizerConvert"/> implementation that provides helper methods and properties to interact with
    /// the CLU recognizer results.
    /// </summary>
    public class Multicloud : IRecognizerConvert
    {
        public enum Intent
        {
            Greeting,
            Cancel,
            None
        }

        public string Text { get; set; }

        public string AlteredText { get; set; }

        public Dictionary<Intent, IntentScore> Intents { get; set; }

        public CluEntities Entities { get; set; }

        public IDictionary<string, object> Properties { get; set; }

        public void Convert(dynamic result)
        {
            var jsonResult = JsonConvert.SerializeObject(result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var app = JsonConvert.DeserializeObject<Multicloud>(jsonResult);

            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        public (Intent intent, double score) GetTopIntent()
        {
            var maxIntent = Intent.None;
            var max = 0.0;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }

            return (maxIntent, max);
        }

        public class CluEntities
        {
            public CluEntity[] Entities;

            public CluEntity[] GetFromCityList() => Entities.Where(e => e.Category == "fromCity").ToArray();

            public CluEntity[] GetToCityList() => Entities.Where(e => e.Category == "toCity").ToArray();

            public CluEntity[] GetFlightDateList() => Entities.Where(e => e.Category == "flightDate").ToArray();

            public string GetFromCity() => GetFromCityList().FirstOrDefault()?.Text;

            public string GetToCity() => GetToCityList().FirstOrDefault()?.Text;

            public string GetFlightDate() => GetFlightDateList().FirstOrDefault()?.Text;
        }
    }
}