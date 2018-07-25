using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

#pragma warning disable 1998

namespace DataBot.Dialogs
{
    [Serializable]
    public struct FilterResult
    {
        public string filterName;
        public string filterValue;
    }

    [Serializable]
    public class FilterPickerDialog : IDialog<FilterResult>
    {
        const string filteredText = " (filter applied)";

        public const string slicerSwitchText = "Switch to add slicers";

        protected FilterResult currentFilterChoice;
        protected Dictionary<string, object> filterValues;
        protected bool isOasis = true;

        public FilterPickerDialog(Dictionary<string, object> filterValues, bool isOasis = true)
        {
            this.filterValues = filterValues;
            this.isOasis = isOasis;
        }

        public async Task StartAsync(IDialogContext context)
        {
            List<string> modifiedFilterOptions = new List<string>();
            List<string> baseOptions = isOasis ? RefinementPickerDialog.oasisFilterOptions : RefinementPickerDialog.holoFilterOptions;
            foreach (var filter in baseOptions)
            {
                if (filterValues.ContainsKey(filter))
                {
                    string modifiedFilter = $"{filter}{filteredText}";
                    modifiedFilterOptions.Add(modifiedFilter);
                }
                else
                {
                    modifiedFilterOptions.Add(filter);
                }
            }
            modifiedFilterOptions.Add(slicerSwitchText);
            modifiedFilterOptions.Add("Quit");

            PromptDialog.Choice<string>(
                context: context,
                resume: AfterFilterChoiceAsync,
                options: modifiedFilterOptions,
                prompt: $"Please select from the options below to apply additional filters",
                retry: "Sorry, didn't understand that input - try 'help' or 'quit'",
                attempts: 3,
                promptStyle: PromptStyle.Auto,
                descriptions: null
            );
        }

        public async Task AfterFilterChoiceAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var filter = await argument;
            var isQuit = await new RootLuisDialog().HandleQuitAsync(context);

            if (!isQuit)
            {
                if (filter.Equals(slicerSwitchText, StringComparison.OrdinalIgnoreCase))
                {
                    context.Done(new FilterResult() { filterName = filter, filterValue = string.Empty });
                }
                else 
                {
                    this.currentFilterChoice.filterName = filter.Replace(filteredText, string.Empty);
                    context.Call(new FilterValueDialog(filter), this.ResumeAfterFilterChoiceDialog);
                }
            }
        }

        private async Task ResumeAfterFilterChoiceDialog(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                string filterValue = await result;
                this.currentFilterChoice.filterValue = filterValue;

                await context.PostAsync($"You selected a filter of {currentFilterChoice.filterName} with value {currentFilterChoice.filterValue}.");
                context.Done(this.currentFilterChoice);
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
            }
        }
    }
}