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
        protected bool isFirstRun = true;
        protected bool showFilterDialog = true;

        public static List<string> filterOptions = new List<string>() { "Country", "Release", "Is Mainstream", "Customer Type", "Flight Ring", "Hmd Manufacturer", "Form Factor" };

        public RefinementPickerDialog(Dictionary<string, object> filterValues, Dictionary<string, object> slicerValues, bool firstRun, bool showFilterDialog)
        {
            this.filterValues = filterValues;
            this.slicerValues = slicerValues;
            this.isFirstRun = firstRun; // only prompt for filters vs slicers the first time through the flow
            this.showFilterDialog = showFilterDialog;   // show filter dialog by default, but it can change in subsequent entries
        }

        public async Task StartAsync(IDialogContext context)
        {
            if (isFirstRun)
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
            else if (showFilterDialog)
            {
                context.Call(new FilterPickerDialog(filterValues), this.ResumeAfterFilterPickerDialog);
            }
            else
            {
                context.Call(new SlicerPickerDialog(slicerValues), this.ResumeAfterSlicerPickerDialog);
            }
        }

        public async Task AfterRefinementChoiceAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var selection = await argument;
            var isQuit = await new RootLuisDialog().HandleQuitAsync(context);

            if (!isQuit)
            {
                isFilterDialog = selection.Equals("filters", StringComparison.OrdinalIgnoreCase);
                context.Call(new FilterPickerDialog(filterValues, isFilterDialog), this.ResumeAfterFilterPickerDialog);
            }
        }

        private async Task ResumeAfterFilterPickerDialog(IDialogContext context, IAwaitable<FilterResult> result)
        {
            FilterResult currentFilterChoice = await result;
            var isQuit = await new RootLuisDialog().HandleQuitAsync(context);

            if (!isQuit)
            {
                if (currentFilterChoice.filterName.Equals(FilterPickerDialog.filterSwitchText, StringComparison.OrdinalIgnoreCase))
                {
                    isFilterDialog = true;
                    context.Call(new FilterPickerDialog(filterValues, true), this.ResumeAfterFilterPickerDialog);
                }
                else if (currentFilterChoice.filterName.Equals(FilterPickerDialog.slicerSwitchText, StringComparison.OrdinalIgnoreCase))
                {
                    isFilterDialog = false;
                    context.Call(new FilterPickerDialog(filterValues, false), this.ResumeAfterFilterPickerDialog);
                }
                else
                {
                    // this is the main case where the user picked a filter or slicer
                    Dictionary<string, object> lookupDict = currentFilterChoice.isFilter ? filterValues : slicerValues;
                    string refinementName = currentFilterChoice.isFilter ? "filter" : "slicer";

                    if (!lookupDict.ContainsKey(currentFilterChoice.filterName) && !currentFilterChoice.filterValue.Equals("clear", StringComparison.OrdinalIgnoreCase))
                    {
                        lookupDict.Add(currentFilterChoice.filterName, currentFilterChoice.filterValue);
                    }
                    else if (currentFilterChoice.filterValue.Equals("clear", StringComparison.OrdinalIgnoreCase))
                    {
                        lookupDict.Remove(currentFilterChoice.filterName);
                        await context.PostAsync($"Removing {refinementName} {currentFilterChoice.filterName}");
                    }
                    else
                    {
                        lookupDict[currentFilterChoice.filterName] = currentFilterChoice.filterValue;
                    }

                    string filterString = $"Your current filters are: ";
                    foreach (var filter in filterValues.Keys)
                    {
                        filterString += $" filter = {filter}, value = {filterValues[filter]}" + Environment.NewLine;
                    }
                    await context.PostAsync(filterString);

                    string slicerString = $"Your current slicers are: ";
                    foreach (var slicer in slicerValues.Keys)
                    {
                        slicerString += $" slicer = {slicer}, value = {slicerValues[slicer]}" + Environment.NewLine;
                    }
                    await context.PostAsync(slicerString);

                    context.Call(new FilterPickerDialog(filterValues, isFilterDialog), this.ResumeAfterFilterPickerDialog);
                }
            }
        }
    }
}