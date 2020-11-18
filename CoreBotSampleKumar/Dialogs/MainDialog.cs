// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.10.3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using CoreBotSampleKumar.CognitiveModels;


namespace CoreBotSampleKumar.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly FlightBookingRecognizer _luisRecognizer;
        protected readonly ILogger Logger;
        private readonly UserState _userState;
        public string CancelFlag = "Cancel";

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(UserState userState,FlightBookingRecognizer luisRecognizer, BookingDialog bookingDialog, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;
            _userState = userState;

            AddDialog(new TopLevelDialog());
            AddDialog(new ReviewSelectionDialog());
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(bookingDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                FirstStepAsync,
                ActStepAsync,
                FinalStepAsync,
               // EndStepAsync,

            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        //private async Task<DialogTurnResult> EndStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        //{
        //    var userInfo = (UserProfile)stepContext.Result;

        //    string status = "You are signed up to review "
        //        + (userInfo.CompaniesToReview.Count is 0 ? "no companies" : string.Join(" and ", userInfo.CompaniesToReview))
        //        + ".";

        //    await stepContext.Context.SendActivityAsync(status);

        //    var accessor = _userState.CreateProperty<UserProfile>(nameof(UserProfile));
        //    await accessor.SetAsync(stepContext.Context, userInfo, cancellationToken);

        //    return await stepContext.EndDialogAsync(null, cancellationToken);
        //}
        private async Task<DialogTurnResult> FirstStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (String.Equals(stepContext.Result.ToString(), CancelFlag,
                   StringComparison.OrdinalIgnoreCase))
            {

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else {
                return await stepContext.BeginDialogAsync(nameof(TopLevelDialog), null, cancellationToken);
            }
        }
        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //if (!_luisRecognizer.IsConfigured)
            //{
            //    await stepContext.Context.SendActivityAsync(
            //        MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

            //    return await stepContext.NextAsync(null, cancellationToken);
            //}

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? "What can I help you with today?\nSay something like \"Book a flight from Paris to Berlin on March 22, 2020\"";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                // LUIS is not configured, we just run the BookingDialog path with an empty BookingDetailsInstance.
                return await stepContext.BeginDialogAsync(nameof(BookingDialog), new BookingDetails(), cancellationToken);
            }
         
            return await stepContext.NextAsync(null, cancellationToken);
        }


        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If the child dialog ("BookingDialog") was cancelled, the user failed to confirm or if the intent wasn't BookFlight
            // the Result here will be null.
            if (stepContext.Result is BookingDetails result)
            {
                // Now we have all the booking details call the booking service.
                // If the call to the booking service was successful tell the user.

                var timeProperty = new TimexProperty(result.TravelDate);
                var travelDateMsg = timeProperty.ToNaturalLanguage(DateTime.Now);
              
                //152346
                var welcome = CreateAdaptiveCardAttachment("FlightItineraryCard.json");
                var response1 = MessageFactory.Attachment(welcome, ssml: "Final Confirmation!");
                //end

               // var messageText = $"I have you booked to {result.Destination} from {result.Origin} on {travelDateMsg}";
               // var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(response1, cancellationToken);
            }

            // Restart the main dialog with a different message the second time around
         
            var promptMessage = "What else can I do for you?";
            //return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
            return await stepContext.EndDialogAsync (promptMessage, cancellationToken);
        }

        private Attachment CreateAdaptiveCardAttachment(string cardName)
        {
            var cardResourcePath = GetType().Assembly.GetManifestResourceNames().First(name => name.EndsWith(cardName));

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }

    }
}
