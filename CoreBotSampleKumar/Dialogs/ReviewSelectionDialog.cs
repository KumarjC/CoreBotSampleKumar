// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreBotSampleKumar.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace CoreBotSampleKumar.Dialogs
{
    public class ReviewSelectionDialog : ComponentDialog
    {
        // Define a "done" response for the company selection prompt.
        private const string DoneOption = "Done";

        // Define value names for values tracked inside the dialogs.
        private const string OptionsSelected = "value-optionsSelected";
        
        // Define the company choices for the company selection prompt.
        private readonly string[] _selectionOptions = new string[]
        {
            "Create a New Booking", "Amend an existing Booking", "Cancel a Booking", 
        };

        public ReviewSelectionDialog()
            : base(nameof(ReviewSelectionDialog))
        {
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new AmendBookingDialog());

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
                {
                    SelectionStepAsync,
                    LoopStepAsync,
                }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> SelectionStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            // Continue using the same selection list, if any, from the previous iteration of this dialog.
            var list = stepContext.Options as List<string> ?? new List<string>();
            stepContext.Values[OptionsSelected] = list;

            // Create a prompt message.
            string message;
            if (list.Count is 0)
            {
                message = $"Please choose an option to start, or `{DoneOption}` to exit.";
            }
            else
            {
                message = $"You have selected **{list[0]}**. You can review an additional option as well, " +
                    $"or choose `{DoneOption}` to exit.";
            }

            // Create the list of options to choose from.
            var options = _selectionOptions.ToList();
            options.Add(DoneOption);
            if (list.Count > 0)
            {
                options.Remove(list[0]);
            }

            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text(message),
                RetryPrompt = MessageFactory.Text("Please choose an option from the list."),
                Choices = ChoiceFactory.ToChoices(options),
            };

            // Prompt the user for a choice.

            if(list.Count > 0 && list[0]=="Create a New Booking")
            {
                return await stepContext.ReplaceDialogAsync(nameof(BookingDialog),new BookingDetails(),cancellationToken);
            }
            else 
            { 
                return await stepContext.PromptAsync(nameof(ChoicePrompt), promptOptions, cancellationToken);
            }

        }

        private async Task<DialogTurnResult> LoopStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            // Retrieve their selection list, the choice they made, and whether they chose to finish.
            var list = stepContext.Values[OptionsSelected] as List<string>;
            var choice = (FoundChoice)stepContext.Result;
            var done = choice.Value == DoneOption;

            if (!done)
            {
                // If they chose a company, add it to the list.
                list.Add(choice.Value);
            }
            //if (choice.Value=="Amend an existing Booking" || choice.Value=="Cancel a Booking")
            //{
            //    return await stepContext.ReplaceDialogAsync(nameof(AmendBookingDialog),list, cancellationToken);
            //}

            if(choice.Value== "Create a New Booking")
            {
                return await stepContext.ReplaceDialogAsync(nameof(BookingDialog), new BookingDetails(), cancellationToken);
            }
            if (done || list.Count >= 2)
            {
                // If they're done, exit and return their list.
                 var messageText = $"You are about to exit the chat. Thank you for using our services.";
                 var promptMessage = MessageFactory.Text(messageText, messageText);
                 return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
         
            }
            else
            {
                // Otherwise, repeat this dialog, passing in the list from this iteration.
                return await stepContext.ReplaceDialogAsync(nameof(ReviewSelectionDialog), list, cancellationToken);
            }
        }
    }
}
