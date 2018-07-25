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
        const string slicerText = " (filter applied)";

        public const string slicerSwitchText = "Switch to add slicers";
        public const string filterSwitchText = "Switch to add filters";

        protected FilterResult currentFilterChoice;
        protected Dictionary<string, object> filterValues;
        protected readonly List<string> filterOptions = new List<string>() { "Country", "Release", "Is Mainstream", "Customer Type", "Flight Ring" , "Hmd Manufacturer", "Form Factor" };
        protected bool isFilterDialog = true;

        public FilterPickerDialog(Dictionary<string, object> filterValues, bool isFilterDialog)
        {
            this.filterValues = filterValues;
            this.isFilterDialog = isFilterDialog;
        }

        public async Task StartAsync(IDialogContext context)
        {
            List<string> modifiedFilterOptions = new List<string>();
            foreach (var filter in filterOptions)
            {
                if (filterValues.ContainsKey(filter))
                {
                    string modifiedFilter = isFilterDialog ? $"{filter}{filteredText}" : $"{filter}{slicerText}";
                    modifiedFilterOptions.Add(modifiedFilter);
                }
                else
                {
                    modifiedFilterOptions.Add(filter);
                }
            }
            if (isFilterDialog)
            {
                modifiedFilterOptions.Add(slicerSwitchText);
            }
            else
            {
                modifiedFilterOptions.Add(filterSwitchText);
            }
            modifiedFilterOptions.Add("Quit");

            string promptText = isFilterDialog ? "filters" : "slicers";

            PromptDialog.Choice<string>(
                context: context,
                resume: AfterFilterChoiceAsync,
                options: modifiedFilterOptions,
                prompt: $"Please select from the options below to apply additional {promptText}",
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
                if (filter.Equals(slicerSwitchText, StringComparison.OrdinalIgnoreCase) ||
                    filter.Equals(filterSwitchText, StringComparison.OrdinalIgnoreCase))
                {
                    context.Done(new FilterResult() { filterName = filter, filterValue = string.Empty });
                }
                else if (isFilterDialog)
                {
                    this.currentFilterChoice.filterName = filter.Replace(filteredText, string.Empty);
                    context.Call(new FilterValueDialog(filter), this.ResumeAfterFilterChoiceDialog);
                }
                else
                {
                    this.currentFilterChoice.filterName = filter.Replace(filteredText, string.Empty);
                    context.Done(new FilterResult() { filterName = filter, filterValue = string.Empty, isFilter = false });
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