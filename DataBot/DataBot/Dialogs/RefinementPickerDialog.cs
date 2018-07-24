using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

#pragma warning disable 1998

namespace DataBot.Dialogs
{
    [Serializable]
    public class RefinementPickerDialog : IDialog<FilterResult>
    {
        protected Dictionary<string, object> filterValues;
        protected Dictionary<string, object> slicerValues;

        public RefinementPickerDialog(Dictionary<string, object> filterValues, Dictionary<string, object> slicerValues)
        {
            this.filterValues = filterValues;
            this.slicerValues = slicerValues;
        }

        public async Task StartAsync(IDialogContext context)
        {
            PromptDialog.Choice<string>(
                context: context,
                resume: AfterRefinementChoiceAsync,
                options: new List<string>() { "Filters", "Slicers", "Quit" },
                prompt: "Please select from the options below to apply refinements to this data:",
                retry: "Sorry, didn't understand that input - try 'help' or 'quit'",
                attempts: 3,
                promptStyle: PromptStyle.Auto,
                descriptions: null
            );
        }

        public async Task AfterRefinementChoiceAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var selection = await argument;
            var isQuit = await new RootLuisDialog().HandleQuitAsync(context);

            if (!isQuit)
            {
                if (selection.Equals("filters", StringComparison.OrdinalIgnoreCase))
                {
                    context.Call(new FilterPickerDialog(filterValues, true), this.ResumeAfterFilterPickerDialog);
                }
                else
                {
                    context.Call(new FilterPickerDialog(filterValues, false), this.ResumeAfterSlicerPickerDialog);
                }
            }
        }

        private async Task ResumeAfterFilterPickerDialog(IDialogContext context, IAwaitable<FilterResult> result)
        {
            FilterResult currentFilterChoice = await result;
            currentFilterChoice.isFilter = true;
            context.Done(currentFilterChoice);
        }

        private async Task ResumeAfterSlicerPickerDialog(IDialogContext context, IAwaitable<FilterResult> result)
        {
            FilterResult currentFilterChoice = await result;
            currentFilterChoice.isFilter = false;
            context.Done(currentFilterChoice);
        }
    }
}