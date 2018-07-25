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
        protected List<string> slicerValues;
        protected bool isFirstRun = true;
        protected bool showFilterDialog = true;
        protected bool isOasis = true;

        public static List<string> holoFilterOptions = new List<string>() { "Country", "Build Release", "Customer Type", "Flight Ring" };
        public static List<string> oasisFilterOptions = new List<string>() { "Country", "Build Release", "Is Mainstream", "Customer Type", "Flight Ring", "Hmd Manufacturer", "Form Factor" };

        public RefinementPickerDialog(Dictionary<string, object> filterValues, List<string> slicerValues, bool firstRun, bool showFilterDialog = true, bool isOasis = true)
        {
            this.filterValues = filterValues;
            this.slicerValues = slicerValues;
            this.isFirstRun = firstRun; // only prompt for filters vs slicers the first time through the flow
            this.showFilterDialog = showFilterDialog;   // show filter dialog by default, but it can change in subsequent entries
            this.isOasis = isOasis;
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
                context.Call(new FilterPickerDialog(filterValues, isOasis), this.ResumeAfterFilterPickerDialog);
            }
            else
            {
                context.Call(new SlicerPickerDialog(slicerValues, isOasis), this.ResumeAfterSlicerPickerDialog);
            }
        }
        
        public async Task AfterRefinementChoiceAsync(IDialogContext context, IAwaitable<string> argument)
        {
            var selection = await argument;
            var isQuit = await new RootLuisDialog().HandleQuitAsync(context);

            if (!isQuit)
            {
                if (selection.Equals("filters", StringComparison.OrdinalIgnoreCase))
                {
                    context.Call(new FilterPickerDialog(filterValues, isOasis), this.ResumeAfterFilterPickerDialog);
                }
                else
                {
                    context.Call(new SlicerPickerDialog(slicerValues, isOasis), this.ResumeAfterSlicerPickerDialog);
                }
            }
        }

        private async Task ResumeAfterFilterPickerDialog(IDialogContext context, IAwaitable<FilterResult> result)
        {
            FilterResult currentFilterChoice = await result;
            var isQuit = await new RootLuisDialog().HandleQuitAsync(context);

            if (!isQuit)
            {
                context.Done(currentFilterChoice);
            }
        }

        private async Task ResumeAfterSlicerPickerDialog(IDialogContext context, IAwaitable<string> result)
        {
            string slicer = await result;
            var isQuit = await new RootLuisDialog().HandleQuitAsync(context);

            if (!isQuit)
            {
                context.Done(new FilterResult() { filterName = slicer, filterValue = string.Empty });
            }
        }
    }
}