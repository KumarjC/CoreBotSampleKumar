// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CoreBotSampleKumar.Dialogs
{
    using System.Collections.Generic;

    /// <summary>Contains information about a user.</summary>
    public class UserProfile
    {
        public string Name { get; set; }
        public int Age { get; set; }

        // The list of companies the user wants to review.
        public List<string> OptionsToReview { get; set; } = new List<string>();
    }
}
