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
        public bool isFilter;   // if true, is a filter, if false, is a slicer
        public string filterName;
        public string filterValue;
    }

    [Serializable]
    public class FilterPickerDialog : IDialog<FilterResult>
    {
        const string filteredText = " (filter applied)";
        protected FilterResult currentFilterChoice;
        protected Dictionary<string, object> filterValues;
        protected readonly List<string> filterOptions = new List<string>() { "Country", "Release", "Is Mainstream", "Customer Type", "Flight Ring" , "Hmd Manufacturer", "Form Factor" };
        protected bool isFilter = true;

        public FilterPickerDialog(Dictionary<string, object> filterValues, bool isFilter)
        {
            this.filterValues = filterValues;
            this.isFilter = isFilter;
        }

        public async Task StartAsync(IDialogContext context)
        {
            List<string> modifiedFilterOptions = new List<string>();
            foreach (var filter in filterOptions)
            {
                if (filterValues.ContainsKey(filter))
                {
                    modifiedFilterOptions.Add($"{filter}{filteredText}");
                }
                else
                {
                    modifiedFilterOptions.Add(filter);
                }
            }
            modifiedFilterOptions.Add("Quit");

            PromptDialog.Choice<string>(
                context: context,
                resume: AfterFilterChoiceAsync,
                options: modifiedFilterOptions,
                prompt: "Please select from the options below to apply additional filters:",
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
                this.currentFilterChoice.filterName = filter.Replace(filteredText, string.Empty);
                context.Call(new FilterValueDialog(filter), this.FilterChoiceDialogResumeAfter);
            }
        }

        private async Task FilterChoiceDialogResumeAfter(IDialogContext context, IAwaitable<string> result)
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