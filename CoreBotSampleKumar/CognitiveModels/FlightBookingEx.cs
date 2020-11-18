// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

namespace CoreBotSampleKumar.CognitiveModels
{
    // Extends the partial FlightBooking class with methods and properties that simplify accessing entities in the luis results
    public partial class FlightBooking
    {
        public (string From, string Airport) FromEntities
        {
            get
            {
                var fromValue = Entities?._instance?.GeographyV22?.FirstOrDefault()?.Text;
                var fromAirportValue = Entities?.From?.FirstOrDefault()?.GeographyV22?.FirstOrDefault()?.FirstOrDefault();
                return (fromValue, fromAirportValue);
            }
        }

        public (string To, string Airport) ToEntities
        {
            get
            {
                var toValue = Entities?._instance?.GeographyV22?.LastOrDefault()?.Text;
                var toAirportValue = Entities?.To?.FirstOrDefault()?.GeographyV22?.LastOrDefault()?.FirstOrDefault();
                return (toValue, toAirportValue);
            }
        }

        // This value will be a TIMEX. And we are only interested in a Date so grab the first result and drop the Time part.
        // TIMEX is a format that represents DateTime expressions that include some ambiguity. e.g. missing a Year.
        public string TravelDate
            => Entities.datetime?.FirstOrDefault()?.Expressions.FirstOrDefault()?.Split('T')[0];
    }
}
