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
using AdaptiveCards;


namespace CoreBotSampleKumar.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly FlightBookingRecognizer _luisRecognizer;
        protected readonly ILogger Logger;
        private readonly UserState _userState;
        public string CancelFlag = "Cancel";

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(UserState userState, FlightBookingRecognizer luisRecognizer, BookingDialog bookingDialog, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;
            _userState = userState;

            AddDialog(new TopLevelDialog());
            AddDialog(new ReviewSelectionDialog());
            AddDialog(new ConfirmationDialog());
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(bookingDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                FirstStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }
        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken);
            }

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? "What can I help you with today?\nSay something like \"Book a flight from Paris to Berlin on March 22, 2020\"";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FirstStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (String.Equals(stepContext.Result.ToString(), CancelFlag,
                   StringComparison.OrdinalIgnoreCase))
            {

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else
            {
                //return await stepContext.BeginDialogAsync(nameof(TopLevelDialog), null, cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                // LUIS is not configured, we just run the BookingDialog path with an empty BookingDetailsInstance.
                return await stepContext.BeginDialogAsync(nameof(BookingDialog), new BookingDetails(), cancellationToken);
            }

            // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var luisResult = await _luisRecognizer.RecognizeAsync<FlightBooking>(stepContext.Context, cancellationToken);
            switch (luisResult.TopIntent().intent)
            {
                case FlightBooking.Intent.BookFlight:

                    // Initialize BookingDetails with any entities we may have found in the response.
                    var bookingDetails = new BookingDetails()
                    {
                        // Get destination and origin from the composite entities arrays.
                        Destination = luisResult.ToEntities.To,
                        Origin = luisResult.FromEntities.From,
                        TravelDate = luisResult.TravelDate,
                    };

                    // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(BookingDialog), bookingDetails, cancellationToken);

                case FlightBooking.Intent.AmendBooking:
                    // We haven't implemented the GetAmendBookingDialog so we just display a TODO message.
                    var getAmendMessageText = "Amend the Booking";
                    var getAmendMessage = MessageFactory.Text(getAmendMessageText, getAmendMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(getAmendMessage, cancellationToken);
                    break;
                case FlightBooking.Intent.Cancel:
                    // We haven't implemented the GetWeatherDialog so we just display a TODO message.
                    var getCancelMessageText = "Cancel the Booking";
                    var getCancelMessage = MessageFactory.Text(getCancelMessageText, getCancelMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(getCancelMessage, cancellationToken);
                    break;
                default:
                    // Catch all for unhandled intents
                    var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way (intent was {luisResult.TopIntent().intent})";
                    var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                    break;
            }
            // return await stepContext.NextAsync(null, cancellationToken);
            return await stepContext.NextAsync(new BookingDetails(), cancellationToken);
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
                var welcome = CreateAdaptiveCardAttachment("FlightItineraryCard.json", result);
                var response1 = MessageFactory.Attachment(welcome, ssml: "Final Confirmation!");

                await stepContext.Context.SendActivityAsync(response1, cancellationToken);
            }


            return await stepContext.ReplaceDialogAsync(nameof(ConfirmationDialog), null, cancellationToken);
        }

        private async Task<DialogTurnResult> EndStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userInfo = (UserProfile)stepContext.Result;

            string status = "You are signed up to review "
                + (userInfo.OptionsToReview.Count is 0 ? "no options" : string.Join(" and ", userInfo.OptionsToReview))
                + ".";

            await stepContext.Context.SendActivityAsync(status);

            var accessor = _userState.CreateProperty<UserProfile>(nameof(UserProfile));
            await accessor.SetAsync(stepContext.Context, userInfo, cancellationToken);

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }


        private Attachment CreateAdaptiveCardAttachment(string cardName, BookingDetails booking)
        {
            AdaptiveCard card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0));
            string[] Passengers = booking.PassengerName.Split(",");

            var messageText = $"I have {booking.PassengerName} ";
            var messageBody = $"booked to {booking.Destination} from {booking.Origin} on {booking.TravelDate}.";
            card.Speak = String.Concat(messageText, messageBody);
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = "Hello, I have",
                Type = "TextBlock",
                Weight = AdaptiveTextWeight.Bolder,
                IsSubtle = false,
                Size = AdaptiveTextSize.Default
            });
            for (int i = 0; i < Passengers.Length; i++)
            {
                string res = Passengers[i];
                card.Body.Add(new AdaptiveTextBlock()
                {
                    Text = res,
                    Size = AdaptiveTextSize.Default
                });
            }

            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = messageBody,
                Size = AdaptiveTextSize.Default
            });
            card.Body.Add(new AdaptiveImage()
            {
                Url = new Uri("https://adaptivecards.io/content/airplane.png")
            });

            // serialize the card to JSON
            string json = card.ToJson();

            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(json),
            };

        }

    }
}
