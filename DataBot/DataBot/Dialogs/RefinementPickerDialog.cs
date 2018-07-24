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

        public RefinementPickerDialog(Dictionary<string, object> filterValues)
        {
            this.filterValues = filterValues;
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
                bool isFilter = selection.Equals("filters", StringComparison.OrdinalIgnoreCase);
                context.Call(new FilterPickerDialog(filterValues, isFilter), this.ResumeAfterFilterPickerDialog);
            }
        }

        private async Task ResumeAfterFilterPickerDialog(IDialogContext context, IAwaitable<FilterResult> result)
        {
            try
            {
                try
                {
                    FilterResult currentFilterChoice = await result;

                    if (!filterValues.ContainsKey(currentFilterChoice.filterName) && !currentFilterChoice.filterValue.Equals("clear", StringComparison.OrdinalIgnoreCase))
                    {
                        filterValues.Add(currentFilterChoice.filterName, currentFilterChoice.filterValue);
                    }
                    else if (currentFilterChoice.filterValue.Equals("clear", StringComparison.OrdinalIgnoreCase))
                    {
                        filterValues.Remove(currentFilterChoice.filterName);
                        await context.PostAsync($"Removing filter {currentFilterChoice.filterName}");
                    }
                    else
                    {
                        filterValues[currentFilterChoice.filterName] = currentFilterChoice.filterValue;
                    }

                    string filterString = "Your current filters are: ";
                    foreach (var filter in filterValues.Keys)
                    {
                        filterString += $" filter = {filter}, value = {filterValues[filter]}" + Environment.NewLine;
                    }

                    await context.PostAsync($"{filterString}");
                }
                catch (TooManyAttemptsException)
                {
                    await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
                }
            }
            finally
            {
                context.Done(result);
            }
        }
    }
}